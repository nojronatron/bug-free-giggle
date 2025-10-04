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
    /// <summary>
    /// Import file with an OperationResult return that describes success or failure.
    /// Implementations should propagate OperationCanceledException for cancellations.
    /// </summary>
    OperationResult<Unit> ImportFileResult(string filePath);
    void ExportFile(string filePath, bool useCanonicalFormat = true);

    // CRUD operations
    /// <summary>
    /// Create a new LogEntry and return the stored snapshot wrapped in an OperationResult.
    /// Implementations should return a failure OperationResult when the entry cannot be created for recoverable reasons.
    /// </summary>
    OperationResult<LogEntry> CreateEntryResult(LogEntry entry);

    /// <summary>
    /// Obsolete shim that preserves the original synchronous CreateEntry behavior. New callers should use CreateEntryResult.
    /// </summary>
    LogEntry CreateEntry(LogEntry entry);
    /// <summary>
    /// Duplicate an existing entry identified by id and return the stored duplicate as an OperationResult.
    /// The duplicate will copy all fields except for Id (new GUID) and may replace a SentExchange field
    /// (SentSig, SentMsg, or TheirCall) when <paramref name="field"/> and <paramref name="newValue"/> are provided.
    /// </summary>
    OperationResult<LogEntry> DuplicateEntryResult(string id, DuplicateField field = DuplicateField.None, string? newValue = null);

    /// <summary>
    /// Obsolete shim preserving the original synchronous DuplicateEntry behavior. New callers should use DuplicateEntryResult.
    /// </summary>
    LogEntry DuplicateEntry(string id, DuplicateField field = DuplicateField.None, string? newValue = null);
    IEnumerable<LogEntry> ReadEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null);
    LogEntry? GetEntryById(string id);
    bool UpdateEntry(string id, Action<LogEntry> editAction);
    bool DeleteEntry(string id);
}
