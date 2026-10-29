namespace LoanApp.Application.Decisions;

/// <summary>A single reason to deny a loan. Returns null when the rule does not apply.</summary>
public interface IDenyRule
{
    Task<DenialReason?> EvaluateAsync(LoanRequest request, CancellationToken cancellationToken);
}
