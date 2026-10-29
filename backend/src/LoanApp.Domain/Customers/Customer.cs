namespace LoanApp.Domain.Customers;

/// <summary>
/// Aggregate root. A customer is identified by SSN and has at most one loan application:
/// a returning customer updates both instead of creating new records.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; private set; }
    public Ssn Ssn { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public LoanApplication? Application { get; private set; }

    private Customer() { } // EF Core

    public Customer(Ssn ssn, string firstName, string lastName, string companyName, Address address)
    {
        Id = Guid.NewGuid();
        Ssn = ssn;
        UpdateDetails(firstName, lastName, companyName, address);
    }

    public void UpdateDetails(string firstName, string lastName, string companyName, Address address)
    {
        FirstName = Guard.Required(firstName, "First name");
        LastName = Guard.Required(lastName, "Last name");
        CompanyName = Guard.Required(companyName, "Company name");
        Address = address;
    }

    /// <summary>Creates the customer's application, or updates it if one already exists.</summary>
    public void Apply(decimal requestedAmount)
    {
        if (Application is null)
            Application = new LoanApplication(Id, requestedAmount);
        else
            Application.UpdateRequestedAmount(requestedAmount);
    }
}
