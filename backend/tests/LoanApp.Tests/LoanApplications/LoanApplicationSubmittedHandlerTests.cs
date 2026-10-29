using LoanApp.Application.LoanApplications;
using LoanApp.Tests.Support;

namespace LoanApp.Tests.LoanApplications;

public sealed class LoanApplicationSubmittedHandlerTests
{
    private readonly FakeExternalCustomerService _external = new();

    [Theory]
    [InlineData(true, "create")]
    [InlineData(false, "update")]
    public async Task New_customer_is_created_and_returning_customer_is_updated(bool isNewCustomer, string expected)
    {
        var handler = new LoanApplicationSubmittedHandler(_external);

        await handler.HandleAsync(Submission(isNewCustomer), CancellationToken.None);

        Assert.Equal(expected, Assert.Single(_external.Calls).Operation);
    }

    internal static LoanApplicationSubmitted Submission(bool isNewCustomer, decimal amount = 25_000) => new(
        isNewCustomer, Guid.NewGuid(), "123456789", "Jane", "Doe", "Acme LLC",
        "1 Main St", "Newark", "NJ", "07101", Guid.NewGuid(), amount);
}
