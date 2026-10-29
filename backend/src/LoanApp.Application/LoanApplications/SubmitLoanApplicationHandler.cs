using LoanApp.Application.Abstractions;
using LoanApp.Application.Decisions;
using LoanApp.Domain.Customers;

namespace LoanApp.Application.LoanApplications;

/// <summary>
/// Decides the application and, if approved, creates or updates the customer and its single application
/// and queues <see cref="LoanApplicationSubmitted"/> — all committed in one unit of work.
/// </summary>
public sealed class SubmitLoanApplicationHandler(
    LoanDecisionEngine decisionEngine,
    ICustomerRepository customers,
    IEventPublisher events,
    IUnitOfWork unitOfWork)
{
    public async Task<SubmitLoanApplicationResult> HandleAsync(LoanRequest request, CancellationToken cancellationToken)
    {
        var decision = await decisionEngine.DecideAsync(request, cancellationToken);
        if (decision.DenialReason is { } reason)
            return new SubmitLoanApplicationResult.Denied(reason);

        var customer = await customers.GetBySsnAsync(request.Ssn, cancellationToken);
        var isNewCustomer = customer is null;

        if (customer is null)
        {
            customer = new Customer(request.Ssn, request.FirstName, request.LastName, request.CompanyName, request.Address);
            customers.Add(customer);
        }
        else
        {
            customer.UpdateDetails(request.FirstName, request.LastName, request.CompanyName, request.Address);
        }

        var application = customer.Apply(request.RequestedAmount);
        events.Publish(LoanApplicationSubmitted.From(customer, application, isNewCustomer));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitLoanApplicationResult.Approved(application.Id, IsReturningCustomer: !isNewCustomer);
    }
}
