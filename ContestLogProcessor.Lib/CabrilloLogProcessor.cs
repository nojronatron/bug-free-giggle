using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Cabrillo log processor implementation.
/// </summary>
public class CabrilloLogProcessor : ILogProcessor
{
    private readonly List<LogEntry> _entries = new();
    private CabrilloLogFile? _logFile;

    public void ReadFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        var headers = new Dictionary<string, string>();
        var entries = new List<LogEntry>();

        foreach (var line in lines)
        {
            if (line.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase))
            {
                // Cabrillo QSO line format: QSO: <freq> <mode> <date> <time> <mycall> ...
                // Example: QSO: 14000 CW 2025-09-26 2100 K7RMZ ...
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 7)
                {
                    // Basic parsing, adjust as needed for full Cabrillo spec
                    var entry = new LogEntry
                    {
                        RawLine = line,
                        Band = parts[1],
                        Mode = parts[2],
                        QsoDateTime = DateTime.TryParse(parts[3] + " " + parts[4], out var dt) ? dt : DateTime.MinValue,
                        CallSign = parts[5],
                        SentInfo = parts[6],
                        ReceivedInfo = parts.Length > 7 ? parts[7] : null
                    };
                    entries.Add(entry);
                }
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                var idx = line.IndexOf(':');
                if (idx > 0)
                {
                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();
                    headers[key] = value;
                }
            }
        }

        _logFile = new CabrilloLogFile
        {
            Headers = headers,
            Entries = entries
        };

        _entries.Clear();
        _entries.AddRange(entries);
    }

    public IEnumerable<LogEntry> GetEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null)
    {
        IEnumerable<LogEntry> result = _entries;
        if (filter != null)
            result = _entries.FindAll(new Predicate<LogEntry>(filter));
        if (orderBy != null)
            result = new List<LogEntry>(result).OrderBy(orderBy);
        return result;
    }

    public void DuplicateEntry(Predicate<LogEntry> match, Action<LogEntry>? editAction)
    {
        var entry = _entries.Find(match);
        if (entry != null)
        {
            var copy = new LogEntry
            {
                QsoDateTime = entry.QsoDateTime,
                CallSign = entry.CallSign,
                Band = entry.Band,
                Mode = entry.Mode,
                SentInfo = entry.SentInfo,
                ReceivedInfo = entry.ReceivedInfo,
                RawLine = entry.RawLine
            };
            editAction?.Invoke(copy);
            _entries.Add(copy);
        }
    }

    public void UpdateEntry(Predicate<LogEntry> match, Action<LogEntry>? editAction)
    {
        var entry = _entries.Find(match);
        if (entry != null)
        {
            editAction?.Invoke(entry);
        }
    }

    public void ExportFile(string filePath)
    {
        if (_logFile == null || _logFile.Entries == null || _logFile.Entries.Count == 0)
        {
            throw new InvalidOperationException("No log data available to export.");
        }

        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }

        if (!filePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".log";
        }

        var lines = new List<string>();
        // Write headers
        foreach (var kvp in _logFile.Headers)
        {
            lines.Add($"{kvp.Key}: {kvp.Value}");
        }

        // Write log entries in forward-time order
        foreach (var entry in _logFile.Entries.OrderBy(e => e.QsoDateTime))
        {
            lines.Add(entry.RawLine ?? "");
        }

        File.WriteAllLines(filePath, lines);
    }
}
