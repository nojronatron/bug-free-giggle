using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Registry service for discovering and resolving contest-specific exchange strategies.
/// Provides centralized management of exchange validation strategies mapped by contest ID.
/// </summary>
public class ContestExchangeStrategyRegistry
{
    private readonly Dictionary<string, Func<IContestExchangeStrategy>> _strategies;

    public ContestExchangeStrategyRegistry()
    {
        _strategies = new Dictionary<string, Func<IContestExchangeStrategy>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Register a contest exchange strategy for a specific contest identifier.
    /// The factory function will be invoked each time the strategy is resolved.
    /// </summary>
    /// <param name="contestId">Contest identifier (e.g., "SALMON-RUN", "WFD")</param>
    /// <param name="strategyFactory">Factory function that creates strategy instances</param>
    public void RegisterStrategy(string contestId, Func<IContestExchangeStrategy> strategyFactory)
    {
        if (string.IsNullOrWhiteSpace(contestId))
        {
            throw new ArgumentException("Contest ID cannot be null or empty", nameof(contestId));
        }

        if (strategyFactory == null)
        {
            throw new ArgumentNullException(nameof(strategyFactory));
        }

        _strategies[contestId.Trim()] = strategyFactory;
    }

    /// <summary>
    /// Resolve a contest exchange strategy based on the contest identifier.
    /// Returns OperationResult with the strategy or failure if contest is not registered.
    /// </summary>
    /// <param name="contestId">Contest identifier to look up</param>
    public OperationResult<IContestExchangeStrategy> ResolveStrategy(string contestId)
    {
        if (string.IsNullOrWhiteSpace(contestId))
        {
            return OperationResult.Failure<IContestExchangeStrategy>(
                "Contest ID cannot be null or empty",
                ResponseStatus.BadFormat);
        }

        string key = contestId.Trim();

        if (!_strategies.TryGetValue(key, out Func<IContestExchangeStrategy>? factory))
        {
            return OperationResult.Failure<IContestExchangeStrategy>(
                $"No exchange strategy registered for contest '{contestId}'",
                ResponseStatus.NotFound);
        }

        try
        {
            IContestExchangeStrategy strategy = factory();
            return OperationResult.Success(strategy);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<IContestExchangeStrategy>(
                $"Failed to create exchange strategy for contest '{contestId}'",
                ResponseStatus.Error,
                ex);
        }
    }

    /// <summary>
    /// Check if a strategy is registered for the given contest ID.
    /// </summary>
    /// <param name="contestId">Contest identifier to check</param>
    public bool IsRegistered(string contestId)
    {
        if (string.IsNullOrWhiteSpace(contestId))
        {
            return false;
        }

        return _strategies.ContainsKey(contestId.Trim());
    }

    /// <summary>
    /// Get all registered contest identifiers.
    /// Returns array of contest IDs in registration order.
    /// </summary>
    public string[] GetRegisteredContests()
    {
        List<string> contests = new List<string>(_strategies.Keys);
        return contests.ToArray();
    }

    /// <summary>
    /// Clear all registered strategies. Useful for testing.
    /// </summary>
    public void Clear()
    {
        _strategies.Clear();
    }

    /// <summary>
    /// Get the count of registered strategies.
    /// </summary>
    public int Count => _strategies.Count;
}
