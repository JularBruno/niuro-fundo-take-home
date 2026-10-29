using LoanApp.Application.Abstractions;
using LoanApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    // The application is auto-included (see AppDbContext), so this loads the whole aggregate.
    public Task<Customer?> GetBySsnAsync(Ssn ssn, CancellationToken cancellationToken) =>
        db.Customers.SingleOrDefaultAsync(customer => customer.Ssn == ssn, cancellationToken);

    public void Add(Customer customer) => db.Customers.Add(customer);
}
