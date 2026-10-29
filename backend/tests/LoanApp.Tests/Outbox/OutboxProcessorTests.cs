using System.Net.Http.Json;
using LoanApp.Infrastructure.Persistence.Outbox;
using LoanApp.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApp.Tests.Outbox;

/// <summary>The background step: pending events are sent to the external service, in order, with retries.</summary>
public sealed class OutboxProcessorTests(LoanApiFactory api) : IClassFixture<LoanApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = api.CreateClient();

    public Task InitializeAsync() => api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task ProcessOutbox() =>
        new OutboxProcessor(api.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance)
            .ProcessPendingAsync(CancellationToken.None);

    private async Task Submit(decimal amount) =>
        (await _client.PostAsJsonAsync("/api/applications", TestData.ApplicationBody(amount: amount))).EnsureSuccessStatusCode();

    private Task<List<OutboxMessage>> Messages() =>
        api.WithDbAsync(db => db.OutboxMessages.OrderBy(m => m.OccurredAt).ToListAsync());

    [Fact]
    public async Task Nothing_is_sent_during_the_request_only_when_the_outbox_is_processed()
    {
        await Submit(25_000);
        Assert.Empty(api.ExternalCustomers.Calls);

        await ProcessOutbox();

        Assert.Equal("create", Assert.Single(api.ExternalCustomers.Calls).Operation);
        Assert.NotNull(Assert.Single(await Messages()).ProcessedAt);
    }

    [Fact]
    public async Task New_then_returning_customer_is_sent_as_create_then_update_and_only_once()
    {
        await Submit(25_000);
        await Submit(40_000);

        await ProcessOutbox();
        await ProcessOutbox(); // already processed messages are not sent again

        Assert.Equal(["create", "update"], api.ExternalCustomers.Calls.Select(c => c.Operation));
        Assert.Equal(40_000, api.ExternalCustomers.Calls[1].Submission.RequestedAmount);
        Assert.All(await Messages(), m => Assert.NotNull(m.ProcessedAt));
    }

    [Fact]
    public async Task Failed_delivery_is_retried_later_and_blocks_newer_events_to_keep_order()
    {
        await Submit(25_000);
        await Submit(40_000);
        api.ExternalCustomers.FailuresLeft = 1;

        await ProcessOutbox();

        Assert.Empty(api.ExternalCustomers.Calls); // the update did not overtake the failed create
        var failed = (await Messages())[0];
        Assert.Null(failed.ProcessedAt);
        Assert.Equal(1, failed.Attempts);
        Assert.Contains("Simulated outage", failed.LastError);

        await ProcessOutbox();

        Assert.Equal(["create", "update"], api.ExternalCustomers.Calls.Select(c => c.Operation));
    }
}
