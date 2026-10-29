using LoanApp.Application.Decisions;

namespace LoanApp.Application.LoanApplications;

public abstract record SubmitLoanApplicationResult
{
    public sealed record Approved(Guid ApplicationId, bool IsReturningCustomer) : SubmitLoanApplicationResult;

    public sealed record Denied(DenialReason Reason) : SubmitLoanApplicationResult;
}
