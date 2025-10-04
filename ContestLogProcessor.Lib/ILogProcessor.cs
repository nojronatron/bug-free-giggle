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
    /// <summary>
    /// Export the in-memory log to a file. New API returns an OperationResult indicating success/failure.
    /// </summary>
    OperationResult<Unit> ExportFileResult(string filePath, bool useCanonicalFormat = true);

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
    /// <summary>
    /// OperationResult-based variant of ReadEntries. Returns a success OperationResult with the enumerable of defensive clones,
    /// or a failure OperationResult with Diagnostic populated when an unexpected error occurs.
    /// </summary>
    OperationResult<IEnumerable<LogEntry>> ReadEntriesResult(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null);
    LogEntry? GetEntryById(string id);
    /// <summary>
    /// OperationResult-based variant of GetEntryById. Returns Success with the found entry (defensive clone)
    /// or a Failure with ResponseStatus.NotFound when no entry exists for the provided id.
    /// </summary>
    OperationResult<LogEntry> GetEntryByIdResult(string id);
    /// <summary>
    /// Update an existing entry by id using the provided edit action. Returns an OperationResult indicating success or failure.
    /// Use OperationResult.Unit for void-like semantics; callers may inspect ErrorMessage and Diagnostic on failure.
    /// </summary>
    OperationResult<Unit> UpdateEntryResult(string id, Action<LogEntry> editAction);

    bool UpdateEntry(string id, Action<LogEntry> editAction);
    /// <summary>
    /// OperationResult-based variant of DeleteEntry. Returns Success when the entry was removed,
    /// NotFound when no entry exists for the provided id, or BadFormat when id is null/whitespace.
    /// </summary>
    OperationResult<Unit> DeleteEntryResult(string id);
}
