using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Contest registry implementation that manages contest scoring service registration and resolution.
/// </summary>
public class ContestRegistry : IContestRegistry
{
    private readonly Dictionary<string, Func<object>> _contestServiceFactories;

    public ContestRegistry()
    {
        _contestServiceFactories = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);
    }

    public OperationResult<object> ResolveContestScoringService(string contestId)
    {
        if (string.IsNullOrWhiteSpace(contestId))
        {
            return OperationResult.Failure<object>("Contest ID cannot be null or empty", ResponseStatus.BadFormat);
        }

        string normalizedContestId = contestId.Trim().ToUpperInvariant();
        
        if (!_contestServiceFactories.TryGetValue(normalizedContestId, out Func<object>? serviceFactory))
        {
            string[] registered = GetRegisteredContests();
            string availableContests = registered.Length > 0 ? string.Join(", ", registered) : "None";
            return OperationResult.Failure<object>(
                $"Contest '{contestId}' is not registered. Available contests: {availableContests}",
                ResponseStatus.NotFound);
        }

        try
        {
            object service = serviceFactory();
            return OperationResult.Success(service);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<object>(
                $"Failed to create contest scoring service for '{contestId}'",
                ResponseStatus.Error,
                ex);
        }
    }

    public void RegisterContestService<TResult>(string contestId, Func<IContestScoringService<TResult>> serviceFactory)
    {
        if (string.IsNullOrWhiteSpace(contestId))
        {
            throw new ArgumentNullException(nameof(contestId));
        }

        if (serviceFactory == null)
        {
            throw new ArgumentNullException(nameof(serviceFactory));
        }

        string normalizedContestId = contestId.Trim().ToUpperInvariant();
        
        _contestServiceFactories[normalizedContestId] = () => serviceFactory();
    }

    public string[] GetRegisteredContests()
    {
        string[] contests = new string[_contestServiceFactories.Count];
        _contestServiceFactories.Keys.CopyTo(contests, 0);
        return contests;
    }
}