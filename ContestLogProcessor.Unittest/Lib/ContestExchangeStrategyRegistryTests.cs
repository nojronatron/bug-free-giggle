using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;
using ContestLogProcessor.WinterFieldDay;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class ContestExchangeStrategyRegistryTests
{
    [Fact]
    public void Constructor_CreatesEmptyRegistry()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.Equal(0, registry.Count);
        Assert.Empty(registry.GetRegisteredContests());
    }

    [Fact]
    public void RegisterStrategy_WithValidData_RegistersSuccessfully()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        registry.RegisterStrategy("TEST", () => new SalmonRunExchangeStrategy());

        Assert.Equal(1, registry.Count);
        Assert.True(registry.IsRegistered("TEST"));
    }

    [Fact]
    public void RegisterStrategy_WithNullContestId_ThrowsException()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.Throws<ArgumentException>(() =>
            registry.RegisterStrategy(null!, () => new SalmonRunExchangeStrategy()));
    }

    [Fact]
    public void RegisterStrategy_WithEmptyContestId_ThrowsException()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.Throws<ArgumentException>(() =>
            registry.RegisterStrategy("", () => new SalmonRunExchangeStrategy()));
    }

    [Fact]
    public void RegisterStrategy_WithNullFactory_ThrowsException()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.RegisterStrategy("TEST", null!));
    }

    [Fact]
    public void RegisterStrategy_CaseInsensitiveContestId()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        registry.RegisterStrategy("salmon-run", () => new SalmonRunExchangeStrategy());

        Assert.True(registry.IsRegistered("SALMON-RUN"));
        Assert.True(registry.IsRegistered("Salmon-Run"));
        Assert.True(registry.IsRegistered("salmon-run"));
    }

    [Fact]
    public void RegisterStrategy_OverwritesExistingStrategy()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        registry.RegisterStrategy("TEST", () => new SalmonRunExchangeStrategy());
        registry.RegisterStrategy("TEST", () => new WfdExchangeStrategy());

        Assert.Equal(1, registry.Count);

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("TEST");
        Assert.True(result.IsSuccess);
        Assert.Equal("WFD", result.Value!.ContestId);
    }

    [Fact]
    public void ResolveStrategy_WithRegisteredContest_ReturnsStrategy()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("SALMON-RUN");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SALMON-RUN", result.Value!.ContestId);
    }

    [Fact]
    public void ResolveStrategy_WithUnregisteredContest_ReturnsFailure()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("UNKNOWN");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.NotFound, result.Status);
        Assert.Contains("No exchange strategy registered", result.ErrorMessage);
    }

    [Fact]
    public void ResolveStrategy_WithNullContestId_ReturnsFailure()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Contest ID cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public void ResolveStrategy_WithEmptyContestId_ReturnsFailure()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void ResolveStrategy_WhenFactoryThrows_ReturnsFailure()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("FAIL", () => throw new InvalidOperationException("Test error"));

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("FAIL");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Failed to create exchange strategy", result.ErrorMessage);
        Assert.NotNull(result.Diagnostic);
    }

    [Fact]
    public void IsRegistered_WithRegisteredContest_ReturnsTrue()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());

        Assert.True(registry.IsRegistered("SALMON-RUN"));
    }

    [Fact]
    public void IsRegistered_WithUnregisteredContest_ReturnsFalse()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.False(registry.IsRegistered("UNKNOWN"));
    }

    [Fact]
    public void IsRegistered_WithNullContestId_ReturnsFalse()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        Assert.False(registry.IsRegistered(null!));
    }

    [Fact]
    public void GetRegisteredContests_ReturnsAllContestIds()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());
        registry.RegisterStrategy("WFD", () => new WfdExchangeStrategy());

        string[] contests = registry.GetRegisteredContests();

        Assert.Equal(2, contests.Length);
        Assert.Contains("SALMON-RUN", contests);
        Assert.Contains("WFD", contests);
    }

    [Fact]
    public void Clear_RemovesAllStrategies()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());
        registry.RegisterStrategy("WFD", () => new WfdExchangeStrategy());

        Assert.Equal(2, registry.Count);

        registry.Clear();

        Assert.Equal(0, registry.Count);
        Assert.Empty(registry.GetRegisteredContests());
    }

    [Fact]
    public void MultipleResolves_CreatesSeparateInstances()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("TEST", () => new SalmonRunExchangeStrategy());

        OperationResult<IContestExchangeStrategy> result1 = registry.ResolveStrategy("TEST");
        OperationResult<IContestExchangeStrategy> result2 = registry.ResolveStrategy("TEST");

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
        // Verify they are separate instances
        Assert.NotSame(result1.Value, result2.Value);
    }

    [Fact]
    public void RegisterStrategy_WorksWithBothContestStrategies()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();

        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());
        registry.RegisterStrategy("WFD", () => new WfdExchangeStrategy());

        OperationResult<IContestExchangeStrategy> salmonResult = registry.ResolveStrategy("SALMON-RUN");
        OperationResult<IContestExchangeStrategy> wfdResult = registry.ResolveStrategy("WFD");

        Assert.True(salmonResult.IsSuccess);
        Assert.True(wfdResult.IsSuccess);
        Assert.Equal("SALMON-RUN", salmonResult.Value!.ContestId);
        Assert.Equal("WFD", wfdResult.Value!.ContestId);
    }

    [Fact]
    public void ResolveStrategy_TrimsWhitespaceFromContestId()
    {
        ContestExchangeStrategyRegistry registry = new ContestExchangeStrategyRegistry();
        registry.RegisterStrategy("SALMON-RUN", () => new SalmonRunExchangeStrategy());

        OperationResult<IContestExchangeStrategy> result = registry.ResolveStrategy("  SALMON-RUN  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("SALMON-RUN", result.Value!.ContestId);
    }
}
