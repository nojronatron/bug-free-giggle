using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Bootstrap class for registering contest scoring services with dependency injection.
/// Provides a testable, single-responsibility configuration entry point.
/// </summary>
public static class ContestBootstrap
{
    /// <summary>
    /// Register core contest infrastructure with the DI container.
    /// Contest-specific services should be registered separately to avoid circular dependencies.
    /// </summary>
    /// <param name="services">Service collection to register services with</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection RegisterContestInfrastructure(this IServiceCollection services)
    {
        // Register core contest registry and detector
        services.AddSingleton<IContestRegistry, ContestRegistry>();
        services.AddSingleton<IContestDetector, ContestDetector>();
        
        // Register exchange strategy registry
        services.AddSingleton<ContestExchangeStrategyRegistry>();

        return services;
    }

    /// <summary>
    /// Configure the contest registry with available contest services.
    /// This method should be called after all contest-specific services are registered.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="configureRegistry">Action to configure the registry</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection ConfigureContestRegistry(this IServiceCollection services, 
        Action<IContestRegistryConfigurator> configureRegistry)
    {
        services.AddSingleton<IContestRegistryConfigurator>(provider =>
        {
            IContestRegistry registry = provider.GetRequiredService<IContestRegistry>();
            return new ContestRegistryConfigurator(registry, provider);
        });

        // Configure the registry during startup
        services.AddSingleton(provider =>
        {
            IContestRegistryConfigurator configurator = provider.GetRequiredService<IContestRegistryConfigurator>();
            configureRegistry(configurator);
            return provider.GetRequiredService<IContestRegistry>();
        });

        return services;
    }

    /// <summary>
    /// Configure the contest exchange strategy registry with available strategies.
    /// This method should be called after all contest-specific services are registered.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection ConfigureExchangeStrategyRegistry(this IServiceCollection services)
    {
        // Remove the existing registration to avoid circular dependency
        ServiceDescriptor? existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ContestExchangeStrategyRegistry));
        if (existingDescriptor != null)
        {
            services.Remove(existingDescriptor);
        }
        
        services.AddSingleton<ContestExchangeStrategyRegistry>(provider =>
        {
            // Create new instance directly instead of calling GetRequiredService to avoid circular dependency
            ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
            
            // Get all registered exchange strategies from DI and register them with the strategy registry
            IEnumerable<IContestExchangeStrategy> strategies = provider.GetServices<IContestExchangeStrategy>();
            
            foreach (IContestExchangeStrategy strategy in strategies)
            {
                registry.RegisterStrategy(strategy.ContestId, () => 
                {
                    // Create new instance from DI container each time
                    return provider.GetServices<IContestExchangeStrategy>()
                        .FirstOrDefault(s => s.ContestId == strategy.ContestId)
                        ?? throw new InvalidOperationException($"Strategy for contest '{strategy.ContestId}' not found in DI container");
                });
            }
            
            return registry;
        });

        return services;
    }
}

/// <summary>
/// Interface for configuring contest registry during startup.
/// </summary>
public interface IContestRegistryConfigurator
{
    /// <summary>
    /// Register a contest service with the registry.
    /// </summary>
    void RegisterContest<TResult>(string contestId, Func<IContestScoringService<TResult>> serviceFactory);
}

/// <summary>
/// Implementation of contest registry configurator.
/// </summary>
public class ContestRegistryConfigurator : IContestRegistryConfigurator
{
    private readonly IContestRegistry _registry;
    private readonly IServiceProvider _serviceProvider;

    public ContestRegistryConfigurator(IContestRegistry registry, IServiceProvider serviceProvider)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void RegisterContest<TResult>(string contestId, Func<IContestScoringService<TResult>> serviceFactory)
    {
        _registry.RegisterContestService(contestId, serviceFactory);
    }
}