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
    private readonly List<LogEntry> _entries = [];
    private CabrilloLogFile? _logFile;

    // Events for CRUD operations
    public event EventHandler<LogEntry>? EntryAdded;
    public event EventHandler<LogEntry>? EntryUpdated;
    public event EventHandler<string>? EntryDeleted;

    public void ImportFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string[] lines = File.ReadAllLines(filePath);
        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<LogEntry> entries = [];

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];

            if (line.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase))
            {
                // Cabrillo QSO line format: QSO: <freq> <mode> <date> <time> <mycall> ...
                string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6)
                {
                    // Basic parsing: parts indices reflect the common Cabrillo layout
                    DateTime qsoDt = DateTime.MinValue;
                    if (parts.Length > 4)
                    {
                        string datePart = parts[3];
                        string timePart = parts[4];
                        string combined = datePart + " " + timePart;
                        string[] formats = new[] { "yyyy-MM-dd HHmm", "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd Hm", "yyyyMMdd HHmm" };
                        if (!DateTime.TryParseExact(combined, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsed))
                        {
                            // Fallback to a permissive parse if exact formats fail
                            DateTime.TryParse(combined, out parsed);
                        }
                        if (parsed != default) qsoDt = parsed;
                    }

                    LogEntry entry = new LogEntry
                    {
                        RawLine = line,
                        Frequency = parts.Length > 1 ? parts[1] : null,
                        Mode = parts.Length > 2 ? parts[2] : null,
                        QsoDateTime = qsoDt,
                        CallSign = parts.Length > 5 ? parts[5] : null
                    };

                    // Record source line number (1-based)
                    entry.SourceLineNumber = lineIndex + 1;

                    // Attempt to parse up to five exchange tokens per side.
                    (Exchange? sentExch, string? theirCall, Exchange? recvExch) = ParseExchanges(parts, 6);
                    entry.SentExchange = sentExch;
                    entry.ReceivedExchange = recvExch;
                    entry.TheirCall = theirCall;

                    if (string.IsNullOrWhiteSpace(entry.Id))
                    {
                        entry.Id = Guid.NewGuid().ToString();
                    }

                    entries.Add(entry);
                }
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                int idx = line.IndexOf(':');
                if (idx > 0)
                {
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
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

    /// <summary>
    /// Parse exchange tokens from the parts starting at the provided index.
    /// Returns (sentExchange, theirCall, receivedExchange).
    /// This tries to be tolerant: it will consume up to 5 tokens for the sent exchange,
    /// then the next token may be their call, and then up to 5 tokens for the received exchange.
    /// </summary>
    private static (Exchange? sent, string? theirCall, Exchange? recv) ParseExchanges(string[] parts, int startIndex)
    {
        if (parts == null)
        {
            return (null, null, null);
        }
        // Collect remaining tokens after the fixed-position fields (freq, mode, date, time, mycall)
        int idx = startIndex;
    string[] tokens = parts.Skip(startIndex).ToArray();

        Exchange sent = new Exchange();
        Exchange recv = new Exchange();
        string? theirCall = null;

        // Common Cabrillo layout after the "mycall" is typically:
        // <sentSig> <sentMsg> <theirCall> <recvSig> <recvMsg>
        // We'll handle the common 5-token case first, then fall back to a best-effort mapping for shorter variants.
        if (tokens.Length >= 5)
        {
            sent.SentSig = tokens[0];
            sent.SentMsg = tokens[1];
            theirCall = tokens[2];
            recv.ReceivedSig = tokens[3];
            recv.ReceivedMsg = tokens[4];
        }
        else
        {
            // Best-effort: assign what we can in order.
            int p = 0;
            if (p < tokens.Length) sent.SentSig = tokens[p++];
            if (p < tokens.Length) sent.SentMsg = tokens[p++];

            if (p < tokens.Length)
            {
                // If the next token looks like a callsign, treat it as theirCall; otherwise try to infer.
                if (IsLikelyCallsign(tokens[p]))
                {
                    theirCall = tokens[p++];
                }
                else if (tokens[p].Any(ch => char.IsLetter(ch)))
                {
                    // contains letters — likely not a pure numeric signal, treat as theirCall
                    theirCall = tokens[p++];
                }
            }

            if (p < tokens.Length) recv.ReceivedSig = tokens[p++];
            if (p < tokens.Length) recv.ReceivedMsg = tokens[p++];
        }

        bool sentAny = !string.IsNullOrWhiteSpace(sent.SentSig) || !string.IsNullOrWhiteSpace(sent.SentMsg);
        bool recvAny = !string.IsNullOrWhiteSpace(recv.ReceivedSig) || !string.IsNullOrWhiteSpace(recv.ReceivedMsg);

        return (sentAny ? sent : null, theirCall, recvAny ? recv : null);
    }

    private static bool IsLikelyCallsign(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        // Heuristic: callsigns are alphanumeric and often contain digits and/or a '/'
        bool hasLetter = false;
        bool hasDigit = false;
        bool hasSlash = false;
        foreach (char ch in token)
        {
            if (char.IsLetter(ch)) { hasLetter = true; continue; }
            if (char.IsDigit(ch)) { hasDigit = true; continue; }
            if (ch == '/') { hasSlash = true; continue; }
            return false; // other chars make it unlikely to be a callsign
        }

        // Require at least one letter and either a digit or a slash to be confident it's a callsign
        return hasLetter && (hasDigit || hasSlash);
    }

    public IEnumerable<LogEntry> ReadEntries(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null)
    {
        IEnumerable<LogEntry> result = _entries;
        if (filter != null)
        {
            result = result.Where(filter);
        }

        if (orderBy != null)
        {
            result = result.OrderBy(orderBy);
        }

        if (skip.HasValue)
        {
            result = result.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            result = result.Take(take.Value);
        }
        return result;
    }

    /// <summary>
    /// Try to read a header value from the currently loaded Cabrillo file (if any).
    /// Returns false when no file is loaded or the header is not present.
    /// </summary>
    public bool TryGetHeader(string key, out string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        if (_logFile == null)
        {
            value = null;
            return false;
        }

        return _logFile.TryGetHeader(key, out value);
    }

    /// <summary>
    /// Create a new log entry in the in-memory collection.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="entry"/> is copied (deep copy of exchanges) before being stored
    /// so callers may reuse or modify the original instance without affecting the stored entry.
    /// If the incoming entry has no <see cref="LogEntry.Id"/>, an Id (GUID) will be assigned.
    /// After the entry is added the <see cref="EntryAdded"/> event is raised with the stored copy.
    /// </remarks>
    /// <param name="entry">The entry to add to the processor. Must not be <c>null</c>.</param>
    /// <returns>The copy of the entry that was stored.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public LogEntry CreateEntry(LogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Id)) entry.Id = Guid.NewGuid().ToString();

        // TheirCall is required for a valid QSO entry
        if (string.IsNullOrWhiteSpace(entry.TheirCall))
        {
            throw new ArgumentException("TheirCall (the other station's callsign) is required for a log entry.", nameof(entry));
        }

    LogEntry copy = new LogEntry
        {
            Id = entry.Id,
            RawLine = entry.RawLine,
            Frequency = entry.Frequency,
            Mode = entry.Mode,
            QsoDateTime = entry.QsoDateTime,
            CallSign = entry.CallSign,
            Band = entry.Band,
            IsXQso = entry.IsXQso,
            TheirCall = entry.TheirCall
        };

        if (entry.SentExchange != null)
        {
            copy.SentExchange = new Exchange
            {
                SentSig = entry.SentExchange.SentSig,
                SentMsg = entry.SentExchange.SentMsg,
                TheirCall = entry.SentExchange.TheirCall,
                ReceivedSig = entry.SentExchange.ReceivedSig,
                ReceivedMsg = entry.SentExchange.ReceivedMsg
            };
        }

        if (entry.ReceivedExchange != null)
        {
            copy.ReceivedExchange = new Exchange
            {
                SentSig = entry.ReceivedExchange.SentSig,
                SentMsg = entry.ReceivedExchange.SentMsg,
                TheirCall = entry.ReceivedExchange.TheirCall,
                ReceivedSig = entry.ReceivedExchange.ReceivedSig,
                ReceivedMsg = entry.ReceivedExchange.ReceivedMsg
            };
        }

        // Created entries are in-memory items and should not have a source line number
        copy.SourceLineNumber = null;

        // Insert the new entry into _entries according to QsoDateTime ordering rules:
        // - Compare DateTimes normalized to UTC and truncated to minute precision
        // - If entries exist with equal timestamp, insert after the last of them
        // - If no entries have equal timestamp, insert before the first entry with later timestamp
        // - If the new entry is earlier than all, insert at index 0
        DateTime Normalize(DateTime dt)
        {
            if (dt == DateTime.MinValue) return DateTime.MinValue;
            DateTime u = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            return new DateTime(u.Year, u.Month, u.Day, u.Hour, u.Minute, 0, DateTimeKind.Utc);
        }

        DateTime key = Normalize(copy.QsoDateTime);
        int insertIndex = _entries.Count; // default append
        for (int i = 0; i < _entries.Count; i++)
        {
            DateTime other = Normalize(_entries[i].QsoDateTime);
            if (other > key)
            {
                insertIndex = i;
                break;
            }
            // otherwise continue; this will cause insertIndex to be set to last equal/less +1
            insertIndex = i + 1;
        }

        if (insertIndex >= 0 && insertIndex <= _entries.Count)
        {
            _entries.Insert(insertIndex, copy);
        }
        else
        {
            _entries.Add(copy);
        }

        // Keep _logFile entries in sync, using the same insertion logic
        if (_logFile != null && _logFile.Entries != null)
        {
            int insertIndexInLog = _logFile.Entries.Count;
            for (int i = 0; i < _logFile.Entries.Count; i++)
            {
                DateTime other = Normalize(_logFile.Entries[i].QsoDateTime);
                if (other > key)
                {
                    insertIndexInLog = i;
                    break;
                }
                insertIndexInLog = i + 1;
            }

            if (insertIndexInLog >= 0 && insertIndexInLog <= _logFile.Entries.Count)
            {
                _logFile.Entries.Insert(insertIndexInLog, copy);
            }
            else
            {
                _logFile.Entries.Add(copy);
            }
        }
        EntryAdded?.Invoke(this, copy);
        return copy;
    }

    /// <summary>
    /// Duplicate an existing entry. Copies all fields and exchanges, assigns a new Id,
    /// and optionally replaces a field on the SentExchange with the supplied new value.
    /// </summary>
    public LogEntry DuplicateEntry(string id, ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None, string? newValue = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        LogEntry? existing = _entries.FirstOrDefault(x => x.Id == id);
        if (existing == null) throw new ArgumentException($"No entry found with id {id}", nameof(id));

        // Create a deep copy
        LogEntry copy = new LogEntry
        {
            Id = Guid.NewGuid().ToString(),
            RawLine = existing.RawLine,
            Frequency = existing.Frequency,
            Mode = existing.Mode,
            QsoDateTime = existing.QsoDateTime,
            CallSign = existing.CallSign,
            Band = existing.Band,
            IsXQso = existing.IsXQso,
            TheirCall = existing.TheirCall
        };

    if (existing.SentExchange != null)
        {
            string? sentSig = existing.SentExchange.SentSig;
            string? sentMsg = existing.SentExchange.SentMsg;
            string? theirCall = existing.SentExchange.TheirCall;

            switch (field)
            {
                case ILogProcessor.DuplicateField.SentSig:
                    sentSig = newValue;
                    break;
                case ILogProcessor.DuplicateField.SentMsg:
                    sentMsg = newValue;
                    break;
                case ILogProcessor.DuplicateField.TheirCall:
                    // Update the sent exchange's stored their-call (if present) and the top-level TheirCall on the duplicate
                    theirCall = newValue;
                    copy.TheirCall = newValue;
                    break;
                case ILogProcessor.DuplicateField.None:
                default:
                    break;
            }

            copy.SentExchange = new Exchange
            {
                SentSig = sentSig,
                SentMsg = sentMsg,
                TheirCall = theirCall,
                ReceivedSig = existing.SentExchange.ReceivedSig,
                ReceivedMsg = existing.SentExchange.ReceivedMsg
            };
        }

        if (existing.ReceivedExchange != null)
        {
            copy.ReceivedExchange = new Exchange
            {
                SentSig = existing.ReceivedExchange.SentSig,
                SentMsg = existing.ReceivedExchange.SentMsg,
                TheirCall = existing.ReceivedExchange.TheirCall,
                ReceivedSig = existing.ReceivedExchange.ReceivedSig,
                ReceivedMsg = existing.ReceivedExchange.ReceivedMsg
            };
        }

        // Insert the duplicate immediately after the existing entry to preserve import/order semantics
        int existingIndex = _entries.IndexOf(existing);
        if (existingIndex >= 0 && existingIndex < _entries.Count)
        {
            _entries.Insert(existingIndex + 1, copy);
        }
        else
        {
            _entries.Add(copy);
        }

        if (_logFile != null && _logFile.Entries != null)
        {
            int existingIndexInLog = _logFile.Entries.IndexOf(existing);
            if (existingIndexInLog >= 0 && existingIndexInLog < _logFile.Entries.Count)
            {
                _logFile.Entries.Insert(existingIndexInLog + 1, copy);
            }
            else
            {
                _logFile.Entries.Add(copy);
            }
        }

        EntryAdded?.Invoke(this, copy);
        return copy;
    }

    public LogEntry? GetEntryById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _entries.FirstOrDefault(x => x.Id == id);
    }

    public bool UpdateEntry(string id, Action<LogEntry> editAction)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
    LogEntry? entry = _entries.FirstOrDefault(x => x.Id == id);
        if (entry == null) return false;
        editAction?.Invoke(entry);
        EntryUpdated?.Invoke(this, entry);
        return true;
    }

    public bool DeleteEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
    LogEntry? entry = _entries.FirstOrDefault(x => x.Id == id);
        if (entry == null) return false;
        _entries.Remove(entry);
        EntryDeleted?.Invoke(this, id);
        return true;
    }

    public void ExportFile(string filePath, bool useCanonicalFormat = true)
    {
        if (_logFile == null || _logFile.Entries == null || _logFile.Entries.Count == 0)
        {
            throw new InvalidOperationException("No log data available to export.");
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }

        if (!filePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".log";
        }

        List<string> lines = new List<string>();
        // Write headers, but never emit an END-OF-LOG header here (we will append a single END-OF-LOG: as the final line)
        foreach (KeyValuePair<string, string> kvp in _logFile.Headers)
        {
            if (!string.Equals(kvp.Key, "END-OF-LOG", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add($"{kvp.Key}: {kvp.Value}");
            }
        }

        // Write log entries in the same order they were imported/added (import order / in-memory order)
        foreach (LogEntry entry in _logFile.Entries)
        {
            if (useCanonicalFormat)
            {
                lines.Add(entry.ToCabrilloLine());
            }
            else
            {
                lines.Add(entry.RawLine ?? string.Empty);
            }
        }

        // Append a single END-OF-LOG: line as the final line and ensure the file ends with CRLF
        lines.Add("END-OF-LOG:");

        // Force CRLF as the line terminator regardless of platform
        const string crlf = "\r\n";
        string content = string.Join(crlf, lines) + crlf;
        File.WriteAllText(filePath, content);
    }
}
