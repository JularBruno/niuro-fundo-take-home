using LoanApp.Application;
using LoanApp.Domain.Customers;

namespace LoanApp.Tests.Support;

public static class TestData
{
    public const string BlacklistedSsn = "111-11-1111"; // from appsettings.json

    public static LoanRequest LoanRequest(string ssn = "123-45-6789", string state = "NJ", decimal amount = 25_000) =>
        new(new Ssn(ssn), "Jane", "Doe", "Acme LLC", new Address("1 Main St", "Newark", state, "07101"), amount);

    /// <summary>JSON body for POST /api/applications.</summary>
    public static object ApplicationBody(
        string ssn = "123-45-6789", string state = "NJ", decimal amount = 25_000, string firstName = "Jane") => new
    {
        firstName,
        lastName = "Doe",
        companyName = "Acme LLC",
        address = new { street = "1 Main St", city = "Newark", state, zipCode = "07101" },
        ssn,
        requestedAmount = amount,
    };
}

public sealed record SubmitResponse(bool Approved, Guid? ApplicationId, bool? IsReturningCustomer, string? DenialReason);
