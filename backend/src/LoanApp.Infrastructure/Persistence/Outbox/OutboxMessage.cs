namespace LoanApp.Infrastructure.Persistence.Outbox;

/// <summary>
/// An event stored in the same transaction as the business data. The background processor sends it
/// and marks it processed; until then it is retried.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
