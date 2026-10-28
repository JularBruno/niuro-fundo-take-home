namespace LoanApp.Domain.Applications;

public sealed class LoanApplication
{
    public Guid Id { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public Guid CustomerId { get; private set; }

    private LoanApplication() { } // EF Core

    public LoanApplication(Guid customerId, decimal requestedAmount)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        RequestedAmount = requestedAmount;
    }

    public void UpdateRequestedAmount(decimal requestedAmount) => RequestedAmount = requestedAmount;
}
