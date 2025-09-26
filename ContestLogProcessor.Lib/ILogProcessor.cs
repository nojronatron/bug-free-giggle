using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for log processing operations.
/// </summary>
public interface ILogProcessor
{
    void ReadFile(string filePath);
    IEnumerable<LogEntry> GetEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null);
    void DuplicateEntry(Predicate<LogEntry> match, Action<LogEntry>? editAction);
    void UpdateEntry(Predicate<LogEntry> match, Action<LogEntry>? editAction);
    void ExportFile(string filePath);
}
