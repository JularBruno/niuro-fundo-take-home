namespace LoanApp.Domain.Customers;

/// <summary>Postal address. State is a 2-letter code normalized to uppercase ("ny" → "NY").</summary>
public sealed record Address
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string ZipCode { get; private set; } = null!;

    private Address() { } // EF Core

    public Address(string street, string city, string state, string zipCode)
    {
        Street = Guard.Required(street, "Street");
        City = Guard.Required(city, "City");
        ZipCode = Guard.Required(zipCode, "Zip code");

        State = Guard.Required(state, "State").ToUpperInvariant();
        if (State.Length != 2 || !State.All(char.IsAsciiLetterUpper))
            throw new DomainException("State must be a 2-letter code.");
    }
}
