using LoanApp.Domain.Customers;

namespace LoanApp.Application.LoanApplications;

/// <summary>
/// Raised when an application is approved and saved. A snapshot of the customer and application, so the
/// background handler can sync the external service without reading the database again.
/// </summary>
public sealed record LoanApplicationSubmitted(
    bool IsNewCustomer,
    Guid CustomerId,
    string Ssn,
    string FirstName,
    string LastName,
    string CompanyName,
    string Street,
    string City,
    string State,
    string ZipCode,
    Guid ApplicationId,
    decimal RequestedAmount)
{
    public static LoanApplicationSubmitted From(Customer customer, LoanApplication application, bool isNewCustomer) =>
        new(
            isNewCustomer,
            customer.Id,
            customer.Ssn.Value,
            customer.FirstName,
            customer.LastName,
            customer.CompanyName,
            customer.Address.Street,
            customer.Address.City,
            customer.Address.State,
            customer.Address.ZipCode,
            application.Id,
            application.RequestedAmount);
}
