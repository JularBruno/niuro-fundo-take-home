using LoanApp.Application.Abstractions;

namespace LoanApp.Application.LoanApplications;

/// <summary>
/// Runs in the background (outside the HTTP request): syncs the submission to the external service,
/// creating the record for a new customer and updating it for a returning one.
/// </summary>
public sealed class LoanApplicationSubmittedHandler(IExternalCustomerService externalCustomers)
{
    public Task HandleAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken) =>
        submission.IsNewCustomer
            ? externalCustomers.CreateAsync(submission, cancellationToken)
            : externalCustomers.UpdateAsync(submission, cancellationToken);
}
