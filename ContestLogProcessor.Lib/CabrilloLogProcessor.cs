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

    public void ReadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string[] lines = File.ReadAllLines(filePath);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<LogEntry> entries = [];

        foreach (var line in lines)
        {
            if (line.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase))
            {
                // Cabrillo QSO line format: QSO: <freq> <mode> <date> <time> <mycall> ...
                // Example: QSO: 14000 CW 2025-09-26 2100 K7RMZ ...
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6)
                {
                    // Basic parsing: parts indices reflect the common Cabrillo layout:
                    // QSO: <frequency> <mode> <date> <time> <call> <sent-exch...> <their-call> <recv-exch...>
                    // This implementation conservatively populates the most common tokens and preserves the raw line.
                    var entry = new LogEntry
                    {
                        RawLine = line,
                        Frequency = parts.Length > 1 ? parts[1] : null,
                        Mode = parts.Length > 2 ? parts[2] : null,
                        QsoDateTime = (parts.Length > 4 && DateTime.TryParse(parts[3] + " " + parts[4], out var dt)) ? dt : DateTime.MinValue,
                        CallSign = parts.Length > 5 ? parts[5] : null
                    };

                    // Attempt to parse up to five exchange tokens per side.
                    var (sentExch, theirCall, recvExch) = ParseExchanges(parts, 6);
                    entry.SentExchange = sentExch;
                    entry.ReceivedExchange = recvExch;
                    entry.TheirCall = theirCall;

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

    /// <summary>
    /// Parse exchange tokens from the parts starting at the provided index.
    /// Returns (sentExchange, theirCall, receivedExchange).
    /// This tries to be tolerant: it will consume up to 5 tokens for the sent exchange,
    /// then the next token may be their call, and then up to 5 tokens for the received exchange.
    /// </summary>
    private static (Exchange? sent, string? theirCall, Exchange? recv) ParseExchanges(string[] parts, int startIndex)
    {
        if (parts == null) return (null, null, null);
        int i = startIndex;
        var sent = new Exchange();

        // Fill sent parts up to 5 tokens or until we run out or encounter probable their-call (heuristic)
        int sentFilled = 0;
        while (i < parts.Length && sentFilled < 5)
        {
            // If the part looks like a date/time or a frequency indicator it's unlikely to be part of the exchange;
            // but for now accept most tokens. We'll map tokens sequentially.
            switch (sentFilled)
            {
                case 0: sent.SentSig = parts[i]; break;
                case 1: sent.SentMsg = parts[i]; break;
                case 2: sent.TheirCall = parts[i]; break;
                case 3: sent.ReceivedSig = parts[i]; break;
                case 4: sent.ReceivedMsg = parts[i]; break;
            }
            i++; sentFilled++;
            // Break early if next token looks like a callsign (contains a digit or '/'), heuristically treat that as theirCall
            if (i < parts.Length && IsLikelyCallsign(parts[i]))
            {
                break;
            }
        }

        // Next token may be their call
        string? theirCall = null;
        if (i < parts.Length && IsLikelyCallsign(parts[i]))
        {
            theirCall = parts[i];
            i++;
        }

        // Remaining tokens up to 5 form the received exchange
        var recv = new Exchange();
        int recvFilled = 0;
        while (i < parts.Length && recvFilled < 5)
        {
            switch (recvFilled)
            {
                case 0: recv.SentSig = parts[i]; break;
                case 1: recv.SentMsg = parts[i]; break;
                case 2: recv.TheirCall = parts[i]; break;
                case 3: recv.ReceivedSig = parts[i]; break;
                case 4: recv.ReceivedMsg = parts[i]; break;
            }
            i++; recvFilled++;
        }

        // If we didn't populate any part, return nulls to indicate absence
        var sentAny = !string.IsNullOrWhiteSpace(sent.SentSig) || !string.IsNullOrWhiteSpace(sent.SentMsg) || !string.IsNullOrWhiteSpace(sent.TheirCall) || !string.IsNullOrWhiteSpace(sent.ReceivedSig) || !string.IsNullOrWhiteSpace(sent.ReceivedMsg);
        var recvAny = !string.IsNullOrWhiteSpace(recv.SentSig) || !string.IsNullOrWhiteSpace(recv.SentMsg) || !string.IsNullOrWhiteSpace(recv.TheirCall) || !string.IsNullOrWhiteSpace(recv.ReceivedSig) || !string.IsNullOrWhiteSpace(recv.ReceivedMsg);

        return (sentAny ? sent : null, theirCall, recvAny ? recv : null);
    }

    private static bool IsLikelyCallsign(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        // Heuristic: callsigns are alphanumeric and often contain digits and/or a '/'
        foreach (var ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch == '/') continue;
            return false;
        }
        // Also reject pure numeric tokens to avoid mixing with serials — consider those not callsigns
        bool hasLetter = token.Any(char.IsLetter);
        return hasLetter;
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
                Frequency = entry.Frequency,
                RawLine = entry.RawLine,
                IsXQso = entry.IsXQso
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
            editAction?.Invoke(copy);
            _entries.Add(copy);
        }
    }

    public void UpdateEntry(Predicate<LogEntry> match, Action<LogEntry>? editAction)
    {
        LogEntry? entry = _entries.Find(match);

        if (entry != null)
        {
            editAction?.Invoke(entry);
        }
    }

    public void ExportFile(string filePath, bool useCanonicalFormat = true)
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
