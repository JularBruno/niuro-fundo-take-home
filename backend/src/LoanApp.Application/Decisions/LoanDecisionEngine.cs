namespace LoanApp.Application.Decisions;

/// <summary>
/// Runs every registered <see cref="IDenyRule"/> in registration order. The first denial wins;
/// if no rule denies, the loan is approved.
/// </summary>
public sealed class LoanDecisionEngine(IEnumerable<IDenyRule> rules)
{
    public async Task<LoanDecision> DecideAsync(LoanRequest request, CancellationToken cancellationToken)
    {
        foreach (var rule in rules)
        {
            if (await rule.EvaluateAsync(request, cancellationToken) is { } reason)
                return LoanDecision.Denied(reason);
        }

        return LoanDecision.Approved;
    }
}
