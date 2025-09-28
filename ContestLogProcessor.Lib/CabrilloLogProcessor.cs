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

    foreach (string line in lines)
        {
            if (line.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase))
            {
                // Cabrillo QSO line format: QSO: <freq> <mode> <date> <time> <mycall> ...
                // Example: QSO: 14000 CW 2025-09-26 2100 K7RMZ ...
                string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6)
                {
                    // Basic parsing: parts indices reflect the common Cabrillo layout
                    // Parse the QSO date/time more robustly using known formats
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

        int idx = startIndex;
        Exchange sent = new();

        // Fill sent parts up to 5 tokens or until we run out or encounter probable their-call (heuristic)
        int sentFilled = 0;
        while (idx < parts.Length && sentFilled < 5)
        {
            // If the part looks like a date/time or a frequency indicator it's unlikely to be part of the exchange;
            // but for now accept most tokens. We'll map tokens sequentially.
            switch (sentFilled)
            {
                case 0: sent.SentSig = parts[idx]; break;
                case 1: sent.SentMsg = parts[idx]; break;
                case 2: sent.TheirCall = parts[idx]; break;
                case 3: sent.ReceivedSig = parts[idx]; break;
                case 4: sent.ReceivedMsg = parts[idx]; break;
            }
            idx++; sentFilled++;
            // Break early if next token looks like a callsign (contains a digit or '/'), heuristically treat that as theirCall
            if (idx < parts.Length && IsLikelyCallsign(parts[idx]))
            {
                break;
            }
        }

        // Next token may be their call
        string? theirCall = null;
        if (idx < parts.Length && IsLikelyCallsign(parts[idx]))
        {
            theirCall = parts[idx];
            idx++;
        }

        // Remaining tokens up to 5 form the received exchange
    Exchange recv = new Exchange();
        int recvFilled = 0;
        while (idx < parts.Length && recvFilled < 5)
        {
            // todo: consider adding a default case
            switch (recvFilled)
            {
                case 0: recv.SentSig = parts[idx]; break;
                case 1: recv.SentMsg = parts[idx]; break;
                case 2: recv.TheirCall = parts[idx]; break;
                case 3: recv.ReceivedSig = parts[idx]; break;
                case 4: recv.ReceivedMsg = parts[idx]; break;
            }
            idx++; recvFilled++;
        }

        // If we didn't populate any part, return nulls to indicate absence
        bool sentAny = !string.IsNullOrWhiteSpace(sent.SentSig) || !string.IsNullOrWhiteSpace(sent.SentMsg) || !string.IsNullOrWhiteSpace(sent.TheirCall) || !string.IsNullOrWhiteSpace(sent.ReceivedSig) || !string.IsNullOrWhiteSpace(sent.ReceivedMsg);
        bool recvAny = !string.IsNullOrWhiteSpace(recv.SentSig) || !string.IsNullOrWhiteSpace(recv.SentMsg) || !string.IsNullOrWhiteSpace(recv.TheirCall) || !string.IsNullOrWhiteSpace(recv.ReceivedSig) || !string.IsNullOrWhiteSpace(recv.ReceivedMsg);

        return (sentAny ? sent : null, theirCall, recvAny ? recv : null);
    }

    private static bool IsLikelyCallsign(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        // Heuristic: callsigns are alphanumeric and often contain digits and/or a '/'
        foreach (char ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch == '/') continue;
            return false;
        }
        // Also reject pure numeric tokens to avoid mixing with serials — consider those not callsigns
        bool hasLetter = token.Any(char.IsLetter);
        return hasLetter;
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

        _entries.Add(copy);
        // If a log file has been imported into memory, keep its entry collection in sync
        if (_logFile != null && _logFile.Entries != null)
        {
            _logFile.Entries.Add(copy);
        }
        EntryAdded?.Invoke(this, copy);
        return copy;
    }

    /// <summary>
    /// Duplicate an existing entry. Copies all fields and exchanges, assigns a new Id,
    /// and optionally replaces the SentExchange.SentMsg value with <paramref name="newSentMsg"/>.
    /// </summary>
    public LogEntry DuplicateEntry(string id, string? newSentMsg = null)
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
            copy.SentExchange = new Exchange
            {
                SentSig = existing.SentExchange.SentSig,
                SentMsg = string.IsNullOrWhiteSpace(newSentMsg) ? existing.SentExchange.SentMsg : newSentMsg,
                TheirCall = existing.SentExchange.TheirCall,
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

        _entries.Add(copy);
        if (_logFile != null && _logFile.Entries != null)
        {
            _logFile.Entries.Add(copy);
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
        // Write headers
    foreach (KeyValuePair<string, string> kvp in _logFile.Headers)
        {
            lines.Add($"{kvp.Key}: {kvp.Value}");
        }

        // Write log entries in forward-time order
    foreach (LogEntry entry in _logFile.Entries.OrderBy(e => e.QsoDateTime))
        {
            if (useCanonicalFormat)
            {
                lines.Add(entry.ToCabrilloLine());
            }
            else
            {
                lines.Add(entry.RawLine ?? "");
            }
        }

        File.WriteAllLines(filePath, lines);
    }
}
