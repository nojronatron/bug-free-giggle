using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents an entire Cabrillo log file, including headers and log entries.
/// The Headers dictionary is case-insensitive to be tolerant of source files that vary capitalization.
/// </summary>
public class CabrilloLogFile
{
    /// <summary>
    /// Dictionary of header fields (e.g., "CALLSIGN", "CONTEST", etc.).
    /// Keys are stored in upper-invariant form and lookups are case-insensitive.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Collection of QSO log entries.
    /// </summary>
    public List<LogEntry> Entries { get; set; } = new();

    /// <summary>
    /// Whether the file contained the required START-OF-LOG marker.
    /// </summary>
    public bool HasStartOfLog => Headers.ContainsKey("START-OF-LOG");

    /// <summary>
    /// Whether the file contained the required END-OF-LOG marker.
    /// </summary>
    public bool HasEndOfLog => Headers.ContainsKey("END-OF-LOG");

    /// <summary>
    /// Try to get a header value by key. Returns true when present and non-empty.
    /// </summary>
    public bool TryGetHeader(string key, out string? value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Headers.TryGetValue(key, out value);
    }

    /// <summary>
    /// Get a header value or null when not present.
    /// </summary>
    public string? GetHeader(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        
        return Headers.TryGetValue(key, out var val) ? val : null;
    }
}
