using Microsoft.Extensions.DependencyInjection;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.SalmonRun;

/// <summary>
/// Bootstrap extensions for registering Salmon Run contest services.
/// </summary>
public static class SalmonRunBootstrap
{
    /// <summary>
    /// Register Salmon Run contest services with the DI container.
    /// </summary>
    /// <param name="services">Service collection to register services with</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection RegisterSalmonRunContest(this IServiceCollection services)
    {
        // Register Salmon Run specific services
        services.AddSingleton<ILocationLookup, InMemoryLocationLookup>();
        services.AddSingleton<SalmonRunScoringService>();
        services.AddSingleton<IContestScoringService<SalmonRunScoreResult>, SalmonRunScoringService>();

        // Register Salmon Run exchange strategy
        services.AddSingleton<SalmonRunExchangeStrategy>();
        services.AddSingleton<IContestExchangeStrategy>(provider => provider.GetRequiredService<SalmonRunExchangeStrategy>());

        return services;
    }
}
