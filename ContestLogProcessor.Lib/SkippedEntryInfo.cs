namespace ContestLogProcessor.Lib;

/// <summary>
/// Information about skipped log entries during contest scoring.
/// This is used by various contest scoring services to report
/// entries that couldn't be processed.
/// </summary>
public class SkippedEntryInfo
{
    public int? SourceLineNumber { get; set; }
    public string? Reason { get; set; }
    public string? RawLine { get; set; }
}