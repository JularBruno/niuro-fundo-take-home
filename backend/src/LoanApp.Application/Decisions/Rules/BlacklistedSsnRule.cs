namespace LoanApp.Application.Decisions.Rules;

/// <summary>Denies applicants whose SSN is on the blacklist.</summary>
public sealed class BlacklistedSsnRule(ISsnBlacklist blacklist) : IDenyRule
{
    public async Task<DenialReason?> EvaluateAsync(LoanRequest request, CancellationToken cancellationToken) =>
        await blacklist.ContainsAsync(request.Ssn, cancellationToken) ? DenialReason.SsnBlacklisted : null;
}
