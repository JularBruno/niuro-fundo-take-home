namespace LoanApp.Application.Decisions;

public sealed record LoanDecision(DenialReason? DenialReason)
{
    public bool IsApproved => DenialReason is null;

    public static LoanDecision Approved { get; } = new((DenialReason?)null);

    public static LoanDecision Denied(DenialReason reason) => new(reason);
}
