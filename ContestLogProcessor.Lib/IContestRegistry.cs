using System;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for discovering and resolving contest scoring services based on contest type.
/// </summary>
public interface IContestRegistry
{
    /// <summary>
    /// Resolve a contest scoring service based on the contest identifier.
    /// Returns OperationResult with the service or failure if contest is not registered.
    /// </summary>
    OperationResult<object> ResolveContestScoringService(string contestId);

    /// <summary>
    /// Register a contest scoring service for a specific contest identifier.
    /// </summary>
    void RegisterContestService<TResult>(string contestId, Func<IContestScoringService<TResult>> serviceFactory);

    /// <summary>
    /// Get all registered contest identifiers.
    /// </summary>
    string[] GetRegisteredContests();
}