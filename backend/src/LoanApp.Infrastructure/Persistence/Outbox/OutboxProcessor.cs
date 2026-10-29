using System.Text.Json;
using LoanApp.Application.LoanApplications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoanApp.Infrastructure.Persistence.Outbox;

/// <summary>
/// Polls pending outbox messages (oldest first) and dispatches them to their handler, outside any HTTP request.
/// A failure stops the batch so later messages never overtake earlier ones (an UPDATE can't arrive before its
/// CREATE); the message is retried on the next poll until <see cref="MaxAttempts"/>, then left for inspection.
/// </summary>
public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    public const int MaxAttempts = 20;
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox polling failed; retrying on next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.Attempts < MaxAttempts)
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await DispatchAsync(scope.ServiceProvider, message, cancellationToken);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                logger.LogInformation("Outbox message {MessageId} ({Type}) delivered", message.Id, message.Type);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = exception.Message;
                logger.LogWarning("Outbox message {MessageId} ({Type}) failed, attempt {Attempt}/{MaxAttempts}: {Error}",
                    message.Id, message.Type, message.Attempts, MaxAttempts, exception.Message);
                break;
            }
            finally
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static Task DispatchAsync(IServiceProvider services, OutboxMessage message, CancellationToken cancellationToken) =>
        message.Type switch
        {
            nameof(LoanApplicationSubmitted) => services.GetRequiredService<LoanApplicationSubmittedHandler>()
                .HandleAsync(Deserialize<LoanApplicationSubmitted>(message), cancellationToken),
            _ => throw new InvalidOperationException($"No handler for outbox message type '{message.Type}'."),
        };

    private static T Deserialize<T>(OutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.Payload, JsonSerializerOptions.Web)
        ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload.");
}
