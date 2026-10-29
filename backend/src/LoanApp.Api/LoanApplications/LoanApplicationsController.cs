using LoanApp.Application;
using LoanApp.Application.LoanApplications;
using LoanApp.Domain.Customers;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.LoanApplications;

[ApiController]
[Route("api/applications")]
public sealed class LoanApplicationsController(SubmitLoanApplicationHandler handler) : ControllerBase
{
    /// <summary>Submits a loan application. A denial is a normal outcome (200), not an error.</summary>
    [HttpPost]
    [ProducesResponseType<SubmitLoanApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<SubmitLoanApplicationResponse> Submit(
        SubmitLoanApplicationRequest request, CancellationToken cancellationToken)
    {
        var loanRequest = new LoanRequest(
            new Ssn(request.Ssn),
            request.FirstName,
            request.LastName,
            request.CompanyName,
            new Address(request.Address.Street, request.Address.City, request.Address.State, request.Address.ZipCode),
            request.RequestedAmount);

        return await handler.HandleAsync(loanRequest, cancellationToken) switch
        {
            SubmitLoanApplicationResult.Approved approved =>
                new(Approved: true, ApplicationId: approved.ApplicationId, IsReturningCustomer: approved.IsReturningCustomer),
            SubmitLoanApplicationResult.Denied denied =>
                new(Approved: false, DenialReason: denied.Reason),
            var unknown => throw new InvalidOperationException($"Unhandled result {unknown.GetType().Name}."),
        };
    }
}
