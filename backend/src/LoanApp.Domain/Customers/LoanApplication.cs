namespace LoanApp.Domain.Customers;

/// <summary>A customer's loan application. Created and changed only through <see cref="Customer.Apply"/>.</summary>
public sealed class LoanApplication
{
    public Guid Id { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public Guid CustomerId { get; private set; }

    private LoanApplication() { } // EF Core

    internal LoanApplication(Guid customerId, decimal requestedAmount)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        UpdateRequestedAmount(requestedAmount);
    }

    internal void UpdateRequestedAmount(decimal requestedAmount)
    {
        if (requestedAmount <= 0)
            throw new DomainException("Requested amount must be greater than zero.");

        RequestedAmount = requestedAmount;
    }
}
