namespace LoanApp.Application.Decisions.Rules;

/// <summary>Denies applicants living in states where loans are not offered (currently NY).</summary>
public sealed class UnsupportedStateRule : IDenyRule
{
    // Address.State is already normalized to uppercase by the domain.
    private static readonly HashSet<string> UnsupportedStates = ["NY"];

    public Task<DenialReason?> EvaluateAsync(LoanRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<DenialReason?>(
            UnsupportedStates.Contains(request.Address.State) ? DenialReason.StateNotSupported : null);
}
