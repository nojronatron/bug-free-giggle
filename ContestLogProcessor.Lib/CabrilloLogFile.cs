using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents an entire Cabrillo log file, including headers and log entries.
/// </summary>
public class CabrilloLogFile
{
    /// <summary>
    /// Dictionary of header fields (e.g., "CALLSIGN", "CONTEST", etc.)
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Collection of QSO log entries.
    /// </summary>
    public List<LogEntry> Entries { get; set; } = new();
}
