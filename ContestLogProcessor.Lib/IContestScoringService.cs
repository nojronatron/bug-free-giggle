namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for contest-specific scoring services.
/// Contest implementations provide concrete scoring logic for their specific rules.
/// </summary>
/// <typeparam name="TResult">Contest-specific score result type</typeparam>
public interface IContestScoringService<TResult>
{
    /// <summary>
    /// Calculate the score for a given contest log.
    /// Returns OperationResult with calculated score or failure with validation errors.
    /// </summary>
    OperationResult<TResult> CalculateScore(CabrilloLogFile log);
    
    /// <summary>
    /// Get the contest identifier that this scoring service handles.
    /// Should match the CONTEST header field value (e.g., "SALMON-RUN", "WFD").
    /// </summary>
    string ContestId { get; }
}