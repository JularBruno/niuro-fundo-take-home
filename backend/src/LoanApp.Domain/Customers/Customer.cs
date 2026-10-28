namespace LoanApp.Domain.Customers;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string Ssn { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;
    public Address Address { get; private set; } = null!;

    private Customer() { } // EF Core

    public Customer(string ssn, string firstName, string lastName, string companyName, Address address)
    {
        Id = Guid.NewGuid();
        Ssn = ssn;
        Update(firstName, lastName, companyName, address);
    }

    public void Update(string firstName, string lastName, string companyName, Address address)
    {
        FirstName = firstName;
        LastName = lastName;
        CompanyName = companyName;
        Address = address;
    }
}
