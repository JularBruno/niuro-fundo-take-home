using LoanApp.Application.Decisions;
using LoanApp.Infrastructure.Blacklist;
using LoanApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        var blacklistedSsns = configuration.GetSection("Blacklist:Ssns").GetChildren().Select(entry => entry.Value!);
        services.AddSingleton<ISsnBlacklist>(new ConfiguredSsnBlacklist(blacklistedSsns));

        return services;
    }
}
