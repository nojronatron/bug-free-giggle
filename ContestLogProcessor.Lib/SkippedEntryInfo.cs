using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Information about skipped log entries during contest scoring.
/// This is used by various contest scoring services to report
/// entries that couldn't be processed.
/// 
/// Supports hierarchical error reporting with categories, severity levels,
/// and detailed diagnostic information.
/// </summary>
public class SkippedEntryInfo
{
    /// <summary>
    /// Line number in the source log file (1-based).
    /// </summary>
    public int? SourceLineNumber { get; set; }

    /// <summary>
    /// Human-readable reason why the entry was skipped.
    /// For backward compatibility, this remains the primary error message.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// The raw line from the log file that was skipped.
    /// </summary>
    public string? RawLine { get; set; }

    /// <summary>
    /// Hierarchical error code for programmatic access and filtering.
    /// Format: CONTEST.CATEGORY.SPECIFIC (e.g., "WFD.EXCHANGE.MALFORMED", "WFD.RULES.INVALID_CLASS")
    /// Enables precise error handling and automated processing of validation failures.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Category of the error for hierarchical grouping and filtering.
    /// Defaults to General for backward compatibility.
    /// </summary>
    public ErrorCategory Category { get; set; } = ErrorCategory.General;

    /// <summary>
    /// Severity level of the error for prioritization and filtering.
    /// Defaults to Error for backward compatibility with existing skipped entries.
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

    /// <summary>
    /// Optional field name or token that caused the error.
    /// Examples: "Mode", "TheirCall", "SentExchange", "Frequency"
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Optional invalid value that caused the error.
    /// </summary>
    public string? InvalidValue { get; set; }

    /// <summary>
    /// Optional expected value or format description.
    /// Helps explain what was expected vs. what was found.
    /// </summary>
    public string? ExpectedFormat { get; set; }

    /// <summary>
    /// Additional diagnostic details for complex errors.
    /// Can include validation rule violations, parsing issues, etc.
    /// </summary>
    public List<string> Details { get; } = new List<string>();

    /// <summary>
    /// Optional reference to contest-specific rule that was violated.
    /// Example: "WFD-MAX-CONTACTS-PER-BAND", "SR-COUNTY-REQUIRED"
    /// </summary>
    public string? RuleReference { get; set; }
}
