using LoanApp.Domain.Customers;

namespace LoanApp.Application.Abstractions;

public interface ICustomerRepository
{
    /// <summary>Loads the whole aggregate (customer + application), or null for a new customer.</summary>
    Task<Customer?> GetBySsnAsync(Ssn ssn, CancellationToken cancellationToken);

    void Add(Customer customer);
}
