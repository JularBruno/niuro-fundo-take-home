using LoanApp.Domain.Customers;

namespace LoanApp.Application.Decisions;

public interface ISsnBlacklist
{
    Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken);
}
