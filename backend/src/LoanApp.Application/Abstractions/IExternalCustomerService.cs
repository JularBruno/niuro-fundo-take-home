using LoanApp.Application.LoanApplications;

namespace LoanApp.Application.Abstractions;

/// <summary>The external system that mirrors our customers and their applications.</summary>
public interface IExternalCustomerService
{
    Task CreateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken);

    Task UpdateAsync(LoanApplicationSubmitted submission, CancellationToken cancellationToken);
}
