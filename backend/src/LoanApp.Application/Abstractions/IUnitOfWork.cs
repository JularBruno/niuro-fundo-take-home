namespace LoanApp.Application.Abstractions;

/// <summary>Commits every pending change (customer, application, queued events) atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
