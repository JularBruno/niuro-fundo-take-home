using LoanApp.Application.Decisions;
using LoanApp.Domain.Customers;

namespace LoanApp.Infrastructure.Blacklist;

/// <summary>Blacklist loaded from configuration (<c>Blacklist:Ssns</c>). Entries are normalized like any SSN.</summary>
public sealed class ConfiguredSsnBlacklist(IEnumerable<string> ssns) : ISsnBlacklist
{
    private readonly HashSet<Ssn> _ssns = ssns.Select(ssn => new Ssn(ssn)).ToHashSet();

    public Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken) =>
        Task.FromResult(_ssns.Contains(ssn));
}
