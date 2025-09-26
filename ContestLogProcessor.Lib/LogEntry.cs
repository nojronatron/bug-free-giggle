using System;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents a single Cabrillo log entry (QSO).
/// </summary>
public class LogEntry
{
    public DateTime QsoDateTime { get; set; }
    public string? CallSign { get; set; }
    public string? Band { get; set; }
    public string? Mode { get; set; }
    public string? SentInfo { get; set; }
    public string? ReceivedInfo { get; set; }
    public string? RawLine { get; set; } // For original line preservation
                                         // Add more Cabrillo fields as needed
}
