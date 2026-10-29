using LoanApp.Application;
using LoanApp.Application.Decisions;
using LoanApp.Application.Decisions.Rules;
using LoanApp.Domain.Customers;
using LoanApp.Infrastructure.Blacklist;
using LoanApp.Tests.Support;

namespace LoanApp.Tests.Decisions;

public sealed class LoanDecisionEngineTests
{
    private readonly LoanDecisionEngine _engine = new(
    [
        new UnsupportedStateRule(),
        new BlacklistedSsnRule(new ConfiguredSsnBlacklist([TestData.BlacklistedSsn, "222222222"])),
    ]);

    private Task<LoanDecision> Decide(LoanRequest request) => _engine.DecideAsync(request, CancellationToken.None);

    [Fact]
    public async Task Approves_when_no_rule_denies()
    {
        var decision = await Decide(TestData.LoanRequest(state: "NJ"));

        Assert.True(decision.IsApproved);
    }

    [Theory]
    [InlineData("NY")]
    [InlineData("ny")]
    [InlineData(" Ny ")]
    public async Task Denies_applicants_in_New_York(string state)
    {
        var decision = await Decide(TestData.LoanRequest(state: state));

        Assert.Equal(DenialReason.StateNotSupported, decision.DenialReason);
    }

    [Theory]
    [InlineData("111-11-1111")]
    [InlineData("111111111")]
    [InlineData("222-22-2222")] // configured without dashes
    public async Task Denies_blacklisted_ssn_in_any_format(string ssn)
    {
        var decision = await Decide(TestData.LoanRequest(ssn: ssn));

        Assert.Equal(DenialReason.SsnBlacklisted, decision.DenialReason);
    }

    [Fact]
    public async Task First_matching_rule_wins()
    {
        var decision = await Decide(TestData.LoanRequest(ssn: TestData.BlacklistedSsn, state: "NY"));

        Assert.Equal(DenialReason.StateNotSupported, decision.DenialReason);
    }

    [Fact]
    public async Task A_new_rule_plugs_in_without_changing_the_engine_or_existing_rules()
    {
        var engine = new LoanDecisionEngine([new UnsupportedStateRule(), new DenyEverythingRule()]);

        var decision = await engine.DecideAsync(TestData.LoanRequest(state: "NJ"), CancellationToken.None);

        Assert.Equal(DenialReason.SsnBlacklisted, decision.DenialReason);
    }

    private sealed class DenyEverythingRule : IDenyRule
    {
        public Task<DenialReason?> EvaluateAsync(LoanRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<DenialReason?>(DenialReason.SsnBlacklisted);
    }
}
