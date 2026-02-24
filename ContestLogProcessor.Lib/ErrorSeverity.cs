namespace ContestLogProcessor.Lib;

/// <summary>
/// Severity levels for contest log processing errors.
/// Allows for filtering and prioritization in error reporting.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message - entry was processed but has notable characteristics.
    /// Examples: X-QSO entries, entries that were explicitly excluded.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning - entry has issues but may be recoverable or expected in some cases.
    /// Examples: Duplicate entries, minor format inconsistencies.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error - entry could not be scored due to validation or data issues.
    /// Examples: Invalid exchanges, missing required fields, unsupported modes.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical - fundamental issue that prevents processing.
    /// Examples: Missing required Cabrillo markers, missing CALLSIGN header.
    /// </summary>
    Critical = 3
}
