using LoanApp.Application.Decisions;
using LoanApp.Application.Decisions.Rules;
using LoanApp.Application.LoanApplications;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Deny rules run in this order. Adding a rule = new IDenyRule class + one line here.
        services.AddScoped<IDenyRule, UnsupportedStateRule>();
        services.AddScoped<IDenyRule, BlacklistedSsnRule>();
        services.AddScoped<LoanDecisionEngine>();
        services.AddScoped<SubmitLoanApplicationHandler>();

        return services;
    }
}
