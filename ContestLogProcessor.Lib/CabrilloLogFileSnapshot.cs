using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Snapshot representation of a Cabrillo log file suitable for read-only consumption by callers.
/// Contains IReadOnly collection types and cloned elements to avoid exposing internal mutable state.
/// </summary>
public class CabrilloLogFileSnapshot
{
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    public IReadOnlyList<LogEntry> Entries { get; init; } = new ReadOnlyCollection<LogEntry>(new List<LogEntry>());
    public IReadOnlyList<SkippedEntryInfo> SkippedEntries { get; init; } = new ReadOnlyCollection<SkippedEntryInfo>(new List<SkippedEntryInfo>());

    public bool HasStartOfLog => Headers.ContainsKey("START-OF-LOG");
    public bool HasEndOfLog => Headers.ContainsKey("END-OF-LOG");

    /// <summary>
    /// Helper to read a header value in a null-safe manner.
    /// </summary>
    public string? GetHeader(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (Headers == null) return null;
        if (Headers.TryGetValue(key, out string? v)) return v;
        return null;
    }
}
