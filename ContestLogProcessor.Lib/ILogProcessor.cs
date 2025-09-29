using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for log processing operations.
/// </summary>
public interface ILogProcessor
{
    /// <summary>
    /// Fields that may be changed on a duplicated entry's SentExchange.
    /// </summary>
    enum DuplicateField
    {
        None = 0,
        SentSig,
        SentMsg,
        TheirCall
    }
    // File operations
    void ImportFile(string filePath);
    void ExportFile(string filePath, bool useCanonicalFormat = true);

    // CRUD operations
    LogEntry CreateEntry(LogEntry entry);
    /// <summary>
    /// Duplicate an existing entry identified by id. The returned entry is a new stored copy.
    /// The duplicate will copy all fields except for Id (new GUID) and may replace a SentExchange field
    /// (SentSig, SentMsg, or TheirCall) when <paramref name="field"/> and <paramref name="newValue"/> are provided.
    /// </summary>
    LogEntry DuplicateEntry(string id, DuplicateField field = DuplicateField.None, string? newValue = null);
    IEnumerable<LogEntry> ReadEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null);
    LogEntry? GetEntryById(string id);
    bool UpdateEntry(string id, Action<LogEntry> editAction);
    bool DeleteEntry(string id);
}
