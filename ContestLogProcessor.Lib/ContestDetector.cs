using System;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Service for detecting contest type and resolving appropriate scoring services.
/// </summary>
public interface IContestDetector
{
    /// <summary>
    /// Detect contest type from log file headers and resolve the appropriate scoring service.
    /// Returns OperationResult with the service or failure if contest is not registered.
    /// </summary>
    OperationResult<object> DetectAndResolveContestService(CabrilloLogFile logFile);
    
    /// <summary>
    /// Detect contest type from log file headers and return the normalized contest type string.
    /// Returns OperationResult with the contest type string or failure if contest header is missing.
    /// </summary>
    OperationResult<string> DetectContestType(CabrilloLogFile logFile);
}

/// <summary>
/// Implementation of contest detection based on CONTEST header field.
/// </summary>
public class ContestDetector : IContestDetector
{
    private readonly IContestRegistry _registry;

    public ContestDetector(IContestRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public OperationResult<object> DetectAndResolveContestService(CabrilloLogFile logFile)
    {
        if (logFile == null)
        {
            return OperationResult.Failure<object>("Log file cannot be null", ResponseStatus.BadFormat);
        }

        if (!logFile.Headers.TryGetValue("CONTEST", out string? contestId) || string.IsNullOrWhiteSpace(contestId))
        {
            return OperationResult.Failure<object>(
                "Missing or empty CONTEST header field. Contest detection requires a valid CONTEST header.",
                ResponseStatus.BadFormat);
        }

        // Normalize the contest ID to handle common variations
        string normalizedContestId = NormalizeContestId(contestId.Trim());

        return _registry.ResolveContestScoringService(normalizedContestId);
    }

    public OperationResult<string> DetectContestType(CabrilloLogFile logFile)
    {
        if (logFile == null)
        {
            return OperationResult.Failure<string>("Log file cannot be null", ResponseStatus.BadFormat);
        }

        if (!logFile.Headers.TryGetValue("CONTEST", out string? contestId) || string.IsNullOrWhiteSpace(contestId))
        {
            return OperationResult.Failure<string>(
                "Missing or empty CONTEST header field. Contest detection requires a valid CONTEST header.",
                ResponseStatus.BadFormat);
        }

        // Normalize the contest ID to handle common variations
        string normalizedContestId = NormalizeContestId(contestId.Trim());

        return OperationResult.Success(normalizedContestId);
    }

    private string NormalizeContestId(string contestId)
    {
        // Handle common contest ID variations
        return contestId.ToUpperInvariant() switch
        {
            "SALMON RUN" or "SALMONRUN" or "SALMON-RUN" or "WA-SALMON-RUN" => "SALMON-RUN",
            "WINTER FIELD DAY" or "WINTERFIELDDAY" or "WINTER-FIELD-DAY" or "WFD" => "WFD",
            _ => contestId.ToUpperInvariant()
        };
    }
}