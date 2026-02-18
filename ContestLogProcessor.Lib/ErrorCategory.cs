namespace ContestLogProcessor.Lib;

/// <summary>
/// Categories of errors that can occur during contest log processing and scoring.
/// Provides hierarchical grouping of error types for better reporting and filtering.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// General or unspecified error category.
    /// </summary>
    General = 0,

    /// <summary>
    /// Errors related to log file format or structure (Cabrillo markers, headers).
    /// </summary>
    FileFormat = 1,

    /// <summary>
    /// Errors related to missing or invalid required fields in log entries.
    /// </summary>
    MissingData = 2,

    /// <summary>
    /// Errors related to data validation (modes, bands, callsigns, formats).
    /// </summary>
    Validation = 3,

    /// <summary>
    /// Errors related to exchange information parsing or validation.
    /// </summary>
    Exchange = 4,

    /// <summary>
    /// Duplicate QSO entries that cannot be counted for scoring.
    /// </summary>
    Duplicate = 5,

    /// <summary>
    /// Entries explicitly marked as invalid (X-QSO).
    /// </summary>
    Excluded = 6,

    /// <summary>
    /// Errors related to date/time parsing or validation.
    /// </summary>
    DateTime = 7,

    /// <summary>
    /// Errors related to contest-specific rules violations.
    /// </summary>
    ContestRules = 8
}
