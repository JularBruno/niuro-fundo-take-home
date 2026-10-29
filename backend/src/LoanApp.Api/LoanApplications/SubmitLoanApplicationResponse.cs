using LoanApp.Application.Decisions;

namespace LoanApp.Api.LoanApplications;

/// <summary>Approved: applicationId + isReturningCustomer. Denied: denialReason. Unused fields are omitted.</summary>
public sealed record SubmitLoanApplicationResponse(
    bool Approved,
    Guid? ApplicationId = null,
    bool? IsReturningCustomer = null,
    DenialReason? DenialReason = null);
