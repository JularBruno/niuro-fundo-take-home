using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoanApp.Application.LoanApplications;
using LoanApp.Domain.Customers;
using LoanApp.Infrastructure.Persistence;
using LoanApp.Tests.Support;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Tests.Api;

/// <summary>POST /api/applications through the real HTTP pipeline and a real Postgres database.</summary>
public sealed class SubmitApplicationEndpointTests(LoanApiFactory api) : IClassFixture<LoanApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = api.CreateClient();

    public Task InitializeAsync() => api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<SubmitResponse> SubmitOk(object body, HttpClient? client = null)
    {
        var response = await (client ?? _client).PostAsJsonAsync("/api/applications", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubmitResponse>())!;
    }

    private Task<(int Customers, int Applications, int Events)> CountRows() => api.WithDbAsync(async db => (
        await db.Customers.CountAsync(),
        await db.Set<LoanApplication>().CountAsync(),
        await db.OutboxMessages.CountAsync()));

    [Fact]
    public async Task Approved_application_saves_customer_application_and_event()
    {
        var result = await SubmitOk(TestData.ApplicationBody());

        Assert.True(result.Approved);
        Assert.False(result.IsReturningCustomer);
        Assert.Equal((1, 1, 1), await CountRows());

        var customer = await api.WithDbAsync(db => db.Customers.SingleAsync());
        Assert.Equal(result.ApplicationId, customer.Application!.Id);
        Assert.Equal(25_000, customer.Application.RequestedAmount);

        var submitted = await LatestEvent();
        Assert.True(submitted.IsNewCustomer);
        Assert.Equal(customer.Id, submitted.CustomerId);
    }

    [Fact]
    public async Task Returning_customer_updates_the_existing_customer_and_application()
    {
        var first = await SubmitOk(TestData.ApplicationBody(ssn: "123-45-6789", state: "NJ", amount: 25_000));

        var second = await SubmitOk(TestData.ApplicationBody(ssn: "123456789", state: "CA", amount: 40_000, firstName: "Janet"));

        Assert.True(second.IsReturningCustomer);
        Assert.Equal(first.ApplicationId, second.ApplicationId);
        Assert.Equal((1, 1, 2), await CountRows()); // one customer, one application, one event per submission

        var customer = await api.WithDbAsync(db => db.Customers.SingleAsync());
        Assert.Equal("Janet", customer.FirstName);
        Assert.Equal("CA", customer.Address.State);
        Assert.Equal(40_000, customer.Application!.RequestedAmount);

        var submitted = await LatestEvent();
        Assert.False(submitted.IsNewCustomer); // → the external service gets an update, not a create
        Assert.Equal(40_000, submitted.RequestedAmount);
    }

    [Theory]
    [InlineData("987-65-4321", "NY", "StateNotSupported")]
    [InlineData(TestData.BlacklistedSsn, "NJ", "SsnBlacklisted")]
    public async Task Denied_application_returns_the_reason_and_saves_nothing(string ssn, string state, string reason)
    {
        var result = await SubmitOk(TestData.ApplicationBody(ssn: ssn, state: state));

        Assert.False(result.Approved);
        Assert.Equal(reason, result.DenialReason);
        Assert.Equal((0, 0, 0), await CountRows());
    }

    [Fact]
    public async Task Invalid_request_returns_400_with_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            TestData.ApplicationBody(ssn: "1234", state: "New York", amount: 0, firstName: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[] { "FirstName", "Ssn", "RequestedAmount", "Address.State" })
            Assert.True(errors.TryGetProperty(field, out _), $"expected an error for {field}");
        Assert.Equal((0, 0, 0), await CountRows());
    }

    [Fact]
    public async Task Failure_while_publishing_the_event_rolls_back_a_new_customer()
    {
        using var failing = WithFailingEventPublish();

        var response = await failing.CreateClient().PostAsJsonAsync("/api/applications", TestData.ApplicationBody());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal((0, 0, 0), await CountRows());
    }

    [Fact]
    public async Task Failure_while_publishing_the_event_rolls_back_a_returning_customer_update()
    {
        await SubmitOk(TestData.ApplicationBody(amount: 25_000, firstName: "Jane"));
        using var failing = WithFailingEventPublish();

        var response = await failing.CreateClient().PostAsJsonAsync("/api/applications",
            TestData.ApplicationBody(amount: 99_000, firstName: "Changed"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal((1, 1, 1), await CountRows());
        var customer = await api.WithDbAsync(db => db.Customers.SingleAsync());
        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal(25_000, customer.Application!.RequestedAmount);
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> WithFailingEventPublish() =>
        api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(new FailOutboxInsertInterceptor()))));

    private Task<LoanApplicationSubmitted> LatestEvent() => api.WithDbAsync(async db =>
    {
        var message = await db.OutboxMessages.OrderByDescending(m => m.OccurredAt).FirstAsync();
        Assert.Equal(nameof(LoanApplicationSubmitted), message.Type);
        return JsonSerializer.Deserialize<LoanApplicationSubmitted>(message.Payload, JsonSerializerOptions.Web)!;
    });
}
