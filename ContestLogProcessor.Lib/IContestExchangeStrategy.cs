namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for contest-specific exchange validation and parsing strategies.
/// Provides contest-specific logic for validating and interpreting exchange data
/// according to each contest's unique rules and field requirements.
/// </summary>
public interface IContestExchangeStrategy
{
    /// <summary>
    /// Get the contest identifier that this strategy handles.
    /// Should match the CONTEST header field value (e.g., "SALMON-RUN", "WFD").
    /// </summary>
    string ContestId { get; }

    /// <summary>
    /// Validate the sent exchange fields according to contest-specific rules.
    /// Returns OperationResult with validation status or failure with specific validation errors.
    /// </summary>
    /// <param name="sentSig">Sent signal report (e.g., "59", "599", "5NN")</param>
    /// <param name="sentMsg">Sent exchange message (contest-specific data)</param>
    OperationResult<bool> ValidateSentExchange(string? sentSig, string? sentMsg);

    /// <summary>
    /// Validate the received exchange fields according to contest-specific rules.
    /// Returns OperationResult with validation status or failure with specific validation errors.
    /// </summary>
    /// <param name="receivedSig">Received signal report (e.g., "59", "599", "5NN")</param>
    /// <param name="receivedMsg">Received exchange message (contest-specific data)</param>
    OperationResult<bool> ValidateReceivedExchange(string? receivedSig, string? receivedMsg);

    /// <summary>
    /// Validate a complete exchange (both sent and received).
    /// Returns OperationResult with validation status or failure with specific validation errors.
    /// </summary>
    /// <param name="exchange">The complete exchange object to validate</param>
    OperationResult<bool> ValidateExchange(Exchange exchange);

    /// <summary>
    /// Get the required field names for this contest's exchange format.
    /// Returns array of field names in the order they should appear.
    /// </summary>
    string[] GetRequiredFields();

    /// <summary>
    /// Get a human-readable description of the exchange format requirements.
    /// Useful for error messages and documentation.
    /// </summary>
    string GetExchangeFormatDescription();
}
