using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for log processing operations.
/// </summary>
public interface ILogProcessor
{
    // File operations
    void ImportFile(string filePath);
    void ExportFile(string filePath, bool useCanonicalFormat = true);

    // CRUD operations
    LogEntry CreateEntry(LogEntry entry);
    /// <summary>
    /// Duplicate an existing entry identified by id. The returned entry is a new stored copy.
    /// The duplicate will copy all fields except for Id (new GUID) and may replace SentExchange.SentMsg
    /// with the supplied <paramref name="newSentMsg"/> when provided.
    /// </summary>
    LogEntry DuplicateEntry(string id, string? newSentMsg = null);
    IEnumerable<LogEntry> ReadEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null);
    LogEntry? GetEntryById(string id);
    bool UpdateEntry(string id, Action<LogEntry> editAction);
    bool DeleteEntry(string id);
}
