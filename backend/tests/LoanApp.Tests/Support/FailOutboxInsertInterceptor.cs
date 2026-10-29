using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LoanApp.Tests.Support;

/// <summary>
/// Throws right after Postgres has executed the statement that inserts the outbox row, i.e. inside the
/// open transaction with the customer and application already written — the worst moment to fail.
/// </summary>
public sealed class FailOutboxInsertInterceptor : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        ThrowIfOutboxInsert(command);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        ThrowIfOutboxInsert(command);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private static void ThrowIfOutboxInsert(DbCommand command)
    {
        if (command.CommandText.Contains("INSERT INTO \"OutboxMessages\""))
            throw new InvalidOperationException("Simulated failure while publishing the event");
    }
}
