using LoanApp.Domain;
using LoanApp.Domain.Customers;

namespace LoanApp.Tests.Domain;

public sealed class CustomerTests
{
    private static Customer NewCustomer() =>
        new(new Ssn("123-45-6789"), "Jane", "Doe", "Acme LLC", new Address("1 Main St", "Newark", "NJ", "07101"));

    [Fact]
    public void Applying_again_updates_the_single_application_instead_of_creating_another()
    {
        var customer = NewCustomer();

        var first = customer.Apply(25_000);
        var second = customer.Apply(40_000);

        Assert.Same(first, second);
        Assert.Equal(40_000, customer.Application!.RequestedAmount);
        Assert.Equal(customer.Id, second.CustomerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_requested_amount_that_is_not_positive(decimal amount) =>
        Assert.Throws<DomainException>(() => NewCustomer().Apply(amount));

    [Fact]
    public void Ssn_is_normalized_so_formatting_does_not_create_a_different_customer()
    {
        Assert.Equal(new Ssn("123-45-6789"), new Ssn(" 123456789 "));
        Assert.Equal("***-**-6789", new Ssn("123456789").ToString());
        Assert.Throws<DomainException>(() => new Ssn("12345678"));
    }
}
