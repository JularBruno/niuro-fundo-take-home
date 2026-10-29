using System.Net.Http.Json;
using LoanApp.Application.Abstractions;
using LoanApp.Application.LoanApplications;

namespace LoanApp.Infrastructure.ExternalService;

/// <summary>
/// HTTP contract: POST /customers creates, PUT /customers/{customerId} updates; both carry
/// <c>{ customer, application }</c>. Any non-2xx response throws, so the outbox retries the message.
/// </summary>
public sealed class HttpExternalCustomerService(HttpClient http) : IExternalCustomerService
{
    public async Task CreateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("customers", ToPayload(submission), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken)
    {
        using var response = await http.PutAsJsonAsync($"customers/{submission.CustomerId}", ToPayload(submission), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static ExternalCustomerPayload ToPayload(LoanApplicationSubmitted s) => new(
        new ExternalCustomer(
            s.CustomerId, s.Ssn, s.FirstName, s.LastName, s.CompanyName,
            new ExternalAddress(s.Street, s.City, s.State, s.ZipCode)),
        new ExternalApplication(s.ApplicationId, s.RequestedAmount));

    private sealed record ExternalCustomerPayload(ExternalCustomer Customer, ExternalApplication Application);

    private sealed record ExternalCustomer(
        Guid Id, string Ssn, string FirstName, string LastName, string CompanyName, ExternalAddress Address);

    private sealed record ExternalAddress(string Street, string City, string State, string ZipCode);

    private sealed record ExternalApplication(Guid Id, decimal RequestedAmount);
}
