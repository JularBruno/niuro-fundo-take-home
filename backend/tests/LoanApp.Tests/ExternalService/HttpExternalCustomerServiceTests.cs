using System.Net;
using System.Text.Json;
using LoanApp.Infrastructure.ExternalService;
using LoanApp.Tests.LoanApplications;

namespace LoanApp.Tests.ExternalService;

/// <summary>Pins the HTTP contract with the external service.</summary>
public sealed class HttpExternalCustomerServiceTests
{
    private readonly RecordingHandler _http = new();
    private readonly HttpExternalCustomerService _service;

    public HttpExternalCustomerServiceTests() =>
        _service = new HttpExternalCustomerService(new HttpClient(_http) { BaseAddress = new Uri("http://external/") });

    [Fact]
    public async Task Create_posts_customer_and_application_to_customers()
    {
        var submission = LoanApplicationSubmittedHandlerTests.Submission(isNewCustomer: true);

        await _service.CreateAsync(submission, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, _http.Method);
        Assert.Equal("http://external/customers", _http.Uri);
        var body = JsonDocument.Parse(_http.Body!).RootElement;
        Assert.Equal(submission.CustomerId, body.GetProperty("customer").GetProperty("id").GetGuid());
        Assert.Equal("NJ", body.GetProperty("customer").GetProperty("address").GetProperty("state").GetString());
        Assert.Equal(submission.ApplicationId, body.GetProperty("application").GetProperty("id").GetGuid());
        Assert.Equal(25_000, body.GetProperty("application").GetProperty("requestedAmount").GetDecimal());
    }

    [Fact]
    public async Task Update_puts_to_the_customer_resource()
    {
        var submission = LoanApplicationSubmittedHandlerTests.Submission(isNewCustomer: false);

        await _service.UpdateAsync(submission, CancellationToken.None);

        Assert.Equal(HttpMethod.Put, _http.Method);
        Assert.Equal($"http://external/customers/{submission.CustomerId}", _http.Uri);
    }

    [Fact]
    public async Task Non_success_response_throws_so_the_outbox_retries()
    {
        _http.Status = HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.CreateAsync(LoanApplicationSubmittedHandlerTests.Submission(isNewCustomer: true), CancellationToken.None));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public HttpMethod? Method { get; private set; }
        public string? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri!.ToString();
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status);
        }
    }
}
