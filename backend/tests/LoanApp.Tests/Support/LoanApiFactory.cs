using LoanApp.Application.Abstractions;
using LoanApp.Infrastructure.Persistence;
using LoanApp.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Tests.Support;

/// <summary>
/// Runs the real API in memory against a real Postgres database created for this test class
/// (transactions are the point, so no in-memory provider). Needs the docker compose Postgres;
/// override the server with LOANAPP_TEST_POSTGRES.
/// </summary>
public sealed class LoanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string PostgresServer = Environment.GetEnvironmentVariable("LOANAPP_TEST_POSTGRES")
        ?? "Host=localhost;Port=5433;Username=loanapp;Password=loanapp";

    private readonly string _connectionString = $"{PostgresServer};Database=loanapp_test_{Guid.NewGuid():N}";

    public FakeExternalCustomerService ExternalCustomers { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // applies migrations on startup
        builder.UseSetting("ConnectionStrings:Database", _connectionString);
        builder.ConfigureTestServices(services =>
        {
            // Tests drive the outbox explicitly instead of racing a background timer.
            services.Remove(services.Single(d => d.ImplementationType == typeof(OutboxProcessor)));
            services.AddSingleton<IExternalCustomerService>(ExternalCustomers);
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>Empties every table so each test starts from a clean database.</summary>
    public async Task ResetAsync()
    {
        await WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
            """TRUNCATE "OutboxMessages", "Applications", "Customers" """));
        ExternalCustomers.Reset();
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WithDbAsync(async db => await db.Database.EnsureDeletedAsync());
        await DisposeAsync();
    }
}
