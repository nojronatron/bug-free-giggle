using Microsoft.Extensions.DependencyInjection;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Bootstrap extensions for registering Winter Field Day contest services.
/// </summary>
public static class WinterFieldDayBootstrap
{
    /// <summary>
    /// Register Winter Field Day contest services with the DI container.
    /// </summary>
    /// <param name="services">Service collection to register services with</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection RegisterWinterFieldDayContest(this IServiceCollection services)
    {
        // Register Winter Field Day exchange strategy first
        services.AddSingleton<WfdExchangeStrategy>();
        services.AddSingleton<IContestExchangeStrategy>(provider => provider.GetRequiredService<WfdExchangeStrategy>());

        // Register Winter Field Day specific services
        services.AddSingleton<WinterFieldDayExchangeParser>();
        services.AddSingleton<WinterFieldDayScoringService>(provider =>
        {
            WfdExchangeStrategy strategy = provider.GetRequiredService<WfdExchangeStrategy>();
            return new WinterFieldDayScoringService(strategy);
        });
        services.AddSingleton<IContestScoringService<WinterFieldDayScoreResult>>(provider =>
            provider.GetRequiredService<WinterFieldDayScoringService>());

        return services;
    }
}
