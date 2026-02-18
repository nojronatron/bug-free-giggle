using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;
using ContestLogProcessor.WinterFieldDay;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class DependencyInjectionTests
{
    [Fact]
    public void RegisterContestInfrastructure_RegistersExchangeStrategyRegistry()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterContestInfrastructure();
        ServiceProvider provider = services.BuildServiceProvider();
        
        ContestExchangeStrategyRegistry registry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        
        Assert.NotNull(registry);
    }

    [Fact]
    public void RegisterSalmonRunContest_RegistersSalmonRunExchangeStrategy()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterSalmonRunContest();
        ServiceProvider provider = services.BuildServiceProvider();
        
        SalmonRunExchangeStrategy strategy = provider.GetRequiredService<SalmonRunExchangeStrategy>();
        
        Assert.NotNull(strategy);
        Assert.Equal("SALMON-RUN", strategy.ContestId);
    }

    [Fact]
    public void RegisterWinterFieldDayContest_RegistersWfdExchangeStrategy()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterWinterFieldDayContest();
        ServiceProvider provider = services.BuildServiceProvider();
        
        WfdExchangeStrategy strategy = provider.GetRequiredService<WfdExchangeStrategy>();
        
        Assert.NotNull(strategy);
        Assert.Equal("WFD", strategy.ContestId);
    }

    [Fact]
    public void ConfigureExchangeStrategyRegistry_RegistersBothStrategies()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterContestInfrastructure();
        services.RegisterSalmonRunContest();
        services.RegisterWinterFieldDayContest();
        services.ConfigureExchangeStrategyRegistry();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ContestExchangeStrategyRegistry registry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        
        Assert.NotNull(registry);
        Assert.Equal(2, registry.Count);
        Assert.True(registry.IsRegistered("SALMON-RUN"));
        Assert.True(registry.IsRegistered("WFD"));
    }

    [Fact]
    public void ConfigureExchangeStrategyRegistry_ResolveSalmonRunStrategy()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterContestInfrastructure();
        services.RegisterSalmonRunContest();
        services.RegisterWinterFieldDayContest();
        services.ConfigureExchangeStrategyRegistry();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ContestExchangeStrategyRegistry registry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        
        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("SALMON-RUN");
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SALMON-RUN", result.Value!.ContestId);
    }

    [Fact]
    public void ConfigureExchangeStrategyRegistry_ResolveWfdStrategy()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterContestInfrastructure();
        services.RegisterSalmonRunContest();
        services.RegisterWinterFieldDayContest();
        services.ConfigureExchangeStrategyRegistry();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ContestExchangeStrategyRegistry registry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        
        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("WFD");
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("WFD", result.Value!.ContestId);
    }

    [Fact]
    public void ConfigureExchangeStrategyRegistry_GetAllRegisteredContests()
    {
        ServiceCollection services = new ServiceCollection();
        
        services.RegisterContestInfrastructure();
        services.RegisterSalmonRunContest();
        services.RegisterWinterFieldDayContest();
        services.ConfigureExchangeStrategyRegistry();
        
        ServiceProvider provider = services.BuildServiceProvider();
        ContestExchangeStrategyRegistry registry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        
        string[] contests = registry.GetRegisteredContests();
        
        Assert.Equal(2, contests.Length);
        Assert.Contains("SALMON-RUN", contests);
        Assert.Contains("WFD", contests);
    }

    [Fact]
    public void FullDIConfiguration_ResolvesAllServices()
    {
        ServiceCollection services = new ServiceCollection();
        
        // Mimic Program.cs ConfigureServices
        services.RegisterContestInfrastructure();
        services.RegisterSalmonRunContest();
        services.RegisterWinterFieldDayContest();
        services.ConfigureExchangeStrategyRegistry();
        
        ServiceProvider provider = services.BuildServiceProvider();
        
        // Verify all key services can be resolved
        IContestRegistry contestRegistry = provider.GetRequiredService<IContestRegistry>();
        IContestDetector detector = provider.GetRequiredService<IContestDetector>();
        ContestExchangeStrategyRegistry strategyRegistry = provider.GetRequiredService<ContestExchangeStrategyRegistry>();
        SalmonRunScoringService salmonRunService = provider.GetRequiredService<SalmonRunScoringService>();
        WinterFieldDayScoringService wfdService = provider.GetRequiredService<WinterFieldDayScoringService>();
        
        Assert.NotNull(contestRegistry);
        Assert.NotNull(detector);
        Assert.NotNull(strategyRegistry);
        Assert.NotNull(salmonRunService);
        Assert.NotNull(wfdService);
    }
}
