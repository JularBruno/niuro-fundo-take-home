using LoanApp.Application.Abstractions;
using LoanApp.Application.LoanApplications;

namespace LoanApp.Tests.Support;

/// <summary>Records calls instead of doing HTTP; can simulate an outage.</summary>
public sealed class FakeExternalCustomerService : IExternalCustomerService
{
    private readonly List<(string Operation, LoanApplicationSubmitted Submission)> _calls = [];

    public IReadOnlyList<(string Operation, LoanApplicationSubmitted Submission)> Calls => _calls;

    public int FailuresLeft { get; set; }

    public Task CreateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken) => Record("create", submission);

    public Task UpdateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken) => Record("update", submission);

    public void Reset()
    {
        _calls.Clear();
        FailuresLeft = 0;
    }

    private Task Record(string operation, LoanApplicationSubmitted submission)
    {
        if (FailuresLeft > 0)
        {
            FailuresLeft--;
            throw new HttpRequestException("Simulated outage");
        }

        _calls.Add((operation, submission));
        return Task.CompletedTask;
    }
}
