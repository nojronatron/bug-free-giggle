using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Cabrillo log processor implementation.
/// </summary>
public partial class CabrilloLogProcessor : ILogProcessor
{
    private readonly List<LogEntry> _entries = new List<LogEntry>();
    private CabrilloLogFile? _logFile;
    private readonly Action<string>? _warn;

    public CabrilloLogProcessor(Action<string>? warn = null)
    {
        // Default to writing warnings to the console if no callback is provided.
        _warn = warn ?? (msg => Console.WriteLine("WARN: " + msg));
    }

    // Note: The internal `_logFile` instance is intentionally not exposed publicly.
    // Use `GetReadOnlyLogFile()` to obtain a defensive snapshot suitable for callers.

    /// <summary>
    /// Returns a deep copy of the currently loaded CabrilloLogFile suitable for read-only inspection by callers.
    /// This prevents external callers from mutating internal parser state. Returns null when no file is loaded.
    /// </summary>
    public CabrilloLogFileSnapshot? GetReadOnlyLogFile()
    {
        if (_logFile == null) return null;

    // Clone headers into a new dictionary (strings are immutable so shallow copy is sufficient)
    Dictionary<string, string> headersCopyMutable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> kvp in _logFile.Headers)
        {
            headersCopyMutable[kvp.Key] = kvp.Value;
        }

        // Clone entries via LogEntry.Clone() into a mutable list
        List<LogEntry> entriesCopy = new List<LogEntry>();
        if (_logFile.Entries != null)
        {
            foreach (LogEntry e in _logFile.Entries)
            {
                entriesCopy.Add(e.Clone());
            }
        }

        // Clone skipped entries
        List<SkippedEntryInfo> skippedCopy = new List<SkippedEntryInfo>();
        if (_logFile.SkippedEntries != null)
        {
            foreach (SkippedEntryInfo s in _logFile.SkippedEntries)
            {
                skippedCopy.Add(new SkippedEntryInfo { SourceLineNumber = s.SourceLineNumber, Reason = s.Reason, RawLine = s.RawLine });
            }
        }

        // Expose read-only wrappers and return a snapshot
        return new CabrilloLogFileSnapshot
        {
            Headers = new ReadOnlyDictionary<string, string>(headersCopyMutable),
            Entries = new ReadOnlyCollection<LogEntry>(entriesCopy),
            SkippedEntries = new ReadOnlyCollection<SkippedEntryInfo>(skippedCopy)
        };
    }

    /// <summary>
    /// Sanitize header values by masking obviously malicious-looking substrings when the overall
    /// value is longer than 13 characters. This is intentionally conservative: we only mask a few
    /// high-confidence patterns and replace the matching substring with '*' characters of the same length.
    /// </summary>
    private string SanitizeHeaderValue(string value, string key)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        string trimmed = value.Trim();
        if (trimmed.Length <= 13) return trimmed;

        // Patterns that are likely to indicate code or commands. Keep the patterns simple and case-insensitive.
        string lowered = trimmed.ToLowerInvariant();
        foreach (string pat in CabrilloConstants.SuspiciousPatterns)
        {
            int idx = lowered.IndexOf(pat, StringComparison.Ordinal);
            if (idx >= 0)
            {
                // Mask only the matching substring in the original-cased value
                int len = pat.Length;
                string masked = new string('*', len);
                string result = trimmed.Substring(0, idx) + masked + trimmed.Substring(idx + len);
                // Emit a warning so callers/operators can be notified of sanitization
                _warn?.Invoke($"Sanitized header '{key}' (length {trimmed.Length}) by masking a suspicious substring.");
                return result;
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Sanitize fields on a LogEntry that may contain long string values and should be inspected
    /// for suspicious substrings. This is intentionally conservative and reuses the header sanitizer
    /// rules to produce consistent warnings.
    /// </summary>
    private void SanitizeLogEntry(LogEntry entry)
    {
        if (entry == null) return;

        if (!string.IsNullOrWhiteSpace(entry.CallSign))
        {
            entry.CallSign = SanitizeHeaderValue(entry.CallSign!, "CALLSIGN");
        }

        if (!string.IsNullOrWhiteSpace(entry.TheirCall))
        {
            entry.TheirCall = SanitizeHeaderValue(entry.TheirCall!, "THEIRCALL");
        }

        if (entry.SentExchange != null && !string.IsNullOrWhiteSpace(entry.SentExchange.TheirCall))
        {
            entry.SentExchange.TheirCall = SanitizeHeaderValue(entry.SentExchange.TheirCall!, "THEIRCALL");
        }

        if (entry.ReceivedExchange != null && !string.IsNullOrWhiteSpace(entry.ReceivedExchange.TheirCall))
        {
            entry.ReceivedExchange.TheirCall = SanitizeHeaderValue(entry.ReceivedExchange.TheirCall!, "THEIRCALL");
        }
    }

    // Events for CRUD operations
    public event EventHandler<LogEntry>? EntryAdded;
    public event EventHandler<LogEntry>? EntryUpdated;
    public event EventHandler<string>? EntryDeleted;

    /// <summary>
    /// Import the Cabrillo log file into the in-memory store and return an OperationResult describing success/failure.
    /// This method is tolerant of malformed log lines and will record skipped entries; it will not throw for common
    /// parse problems but will return a failure when the file cannot be read.
    /// </summary>
    public OperationResult<Unit> ImportFileResult(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return OperationResult.Failure<Unit>($"File not found: {filePath}", ResponseStatus.NotFound);
            }

            IEnumerable<string> lines = File.ReadLines(filePath);
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<LogEntry> entries = new List<LogEntry>();
            List<SkippedEntryInfo> skipped = new List<SkippedEntryInfo>();

            int lineIndex = 0;
            foreach (string line in lines)
            {
                // advance to 1-based line number at start so 'continue' does not skip counting
                lineIndex++;
                // Stop processing when END-OF-LOG is encountered; don't read the remainder of the file
                if (line.StartsWith("END-OF-LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    headers["END-OF-LOG"] = string.Empty;
                    break;
                }

                // Mark that we've seen a START-OF-LOG tag
                if (line.StartsWith("START-OF-LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf(':');
                    string val = idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;
                    headers["START-OF-LOG"] = val;
                    continue;
                }

                // Accept QSO: and X-QSO: lines. X-QSO lines should be parsed but marked as ignored for scoring.
                bool isXQsoLine = line.StartsWith("X-QSO:", StringComparison.OrdinalIgnoreCase);
                if (line.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase) || isXQsoLine)
                {
                    // Cabrillo QSO line format: QSO: <freq> <mode> <date> <time> <mycall> ...
                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 6)
                    {
                        // Basic parsing: parts indices reflect the common Cabrillo layout
                        DateTime qsoDt = DateTime.MinValue;
                        if (parts.Length > 4)
                        {
                            string datePart = parts[3];
                            string timePart = parts[4];
                            string combined = datePart + " " + timePart;
                            DateTime parsed = default;
                            bool parsedOk = DateTime.TryParseExact(combined, CabrilloConstants.DateTimeFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out parsed);
                            if (!parsedOk)
                            {
                                // Fallback to a permissive parse; ensure we treat parsed times as UTC
                                if (!DateTime.TryParse(combined, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out parsed))
                                {
                                    parsed = default;
                                }
                            }

                            if (parsed != default)
                            {
                                // Normalize to UTC and truncate seconds to zero (minute precision)
                                DateTime u = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
                                qsoDt = new DateTime(u.Year, u.Month, u.Day, u.Hour, u.Minute, 0, DateTimeKind.Utc);
                            }
                            else
                            {
                                // note the unparseable date/time; keep importing but record for diagnostics
                                skipped.Add(new SkippedEntryInfo { SourceLineNumber = lineIndex, Reason = "Unparseable date/time", RawLine = line });
                            }
                        }

                        LogEntry entry = new LogEntry
                        {
                            // Do not keep RawLine for parsed entries to reduce memory usage for large logs.
                            RawLine = null,
                            Frequency = parts.Length > 1 ? parts[1] : null,
                            Mode = parts.Length > 2 ? parts[2] : null,
                            QsoDateTime = qsoDt,
                            CallSign = parts.Length > 5 ? parts[5] : null
                        };

                        // Sanitize CallSign when it's long enough to warrant inspection (SanitizeHeaderValue will
                        // return unchanged for null/short values). Use header-style key for consistent warnings.
                        if (!string.IsNullOrWhiteSpace(entry.CallSign))
                        {
                            entry.CallSign = SanitizeHeaderValue(entry.CallSign!, "CALLSIGN");
                        }

                        // Determine Band and Frequency validity from the Frequency token (or Band token if Frequency missing)
                        if (!string.IsNullOrWhiteSpace(entry.Frequency))
                        {
                            // Try to parse frequency token into integer kHz (truncate decimals)
                            int? freqKHz = ParseFrequencyToken(entry.Frequency);
                            if (freqKHz.HasValue)
                            {
                                entry.FrequencyIsValid = true;
                                // If the original token was a band mapping, we may have returned the mapped frequency
                                entry.Band = MapFrequencyToBand(freqKHz.Value) ?? entry.Band;
                                // Normalize Frequency string to the integer kHz representation so downstream code can rely on numeric values
                                // EXCEPTION: Special tokens like "LIGHT" should preserve their original form
                                if (entry.Frequency.Equals("LIGHT", StringComparison.OrdinalIgnoreCase))
                                {
                                    entry.Frequency = "LIGHT"; // Preserve special token
                                }
                                else
                                {
                                    entry.Frequency = freqKHz.Value.ToString();
                                }
                            }
                            else
                            {
                                entry.FrequencyIsValid = false;
                                // if frequency token is invalid, leave Band null for now
                            }
                        }
                        else
                        {
                            // No frequency token; attempt to map Band token if present
                            // Some logs may supply Band in a different token position; we look at parts[1] earlier for Frequency.
                            entry.FrequencyIsValid = false;
                        }

                        // If this was an X-QSO line, mark it so.
                        if (isXQsoLine)
                        {
                            entry.IsXQso = true;
                        }

                        // Record source line number (1-based)
                        entry.SourceLineNumber = lineIndex;

                        // Attempt to parse up to five exchange tokens per side.
                        (Exchange? sentExch, string? theirCall, Exchange? recvExch) = ParseExchanges(parts, 6, skipped, lineIndex, line);
                        entry.SentExchange = sentExch;
                        entry.ReceivedExchange = recvExch;
                        // Sanitize TheirCall similarly to CALLSIGN. The sanitizer is conservative and will
                        // only act when the value length is greater than 13 characters.
                        if (!string.IsNullOrWhiteSpace(theirCall))
                        {
                            theirCall = SanitizeHeaderValue(theirCall!, "THEIRCALL");
                        }
                        entry.TheirCall = theirCall;
                        
                        // Parse optional transmitter ID (Cabrillo v3 spec: 0 or 1)
                        entry.TransmitterId = ParseTransmitterId(parts, sentExch, recvExch, theirCall);

                        if (string.IsNullOrWhiteSpace(entry.Id))
                        {
                            entry.Id = Guid.NewGuid().ToString();
                        }

                        // If exchange parsing failed to find their call, record a skipped entry for diagnostics
                        if (string.IsNullOrWhiteSpace(entry.TheirCall))
                        {
                            skipped.Add(new SkippedEntryInfo { SourceLineNumber = lineIndex, Reason = "Missing TheirCall token", RawLine = line });
                        }

                        entries.Add(entry);
                    }
                    else
                    {
                        // Malformed QSO line (not enough tokens)
                        skipped.Add(new SkippedEntryInfo { SourceLineNumber = lineIndex, Reason = "Malformed QSO line (insufficient tokens)", RawLine = line });
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    int idx = line.IndexOf(':');
                        if (idx > 0)
                        {
                            string key = line.Substring(0, idx).Trim();
                            string value = line.Substring(idx + 1).Trim();

                            // Only apply sanitizer to a conservative list of header keys that may contain
                            // long string values. The sanitizer itself is conservative and will no-op for
                            // short values (<= 13 chars). Keys are compared case-insensitively.
                            if (SanitizableHeaderExtensions.IsSanitizable(key))
                            {
                                headers[key] = SanitizeHeaderValue(value, key);
                            }
                            else
                            {
                                headers[key] = value;
                            }

                            // Validate Cabrillo v3 enumerated header values
                            OperationResult<Unit> validationResult = ValidateHeaderEnumeratedValue(key, value, lineIndex);
                            if (!validationResult.IsSuccess)
                            {
                                skipped.Add(new SkippedEntryInfo
                                {
                                    SourceLineNumber = lineIndex,
                                    Reason = $"Invalid header value: {validationResult.ErrorMessage}",
                                    RawLine = line
                                });
                            }
                        }
                }
            }

            _logFile = new CabrilloLogFile
            {
                Headers = headers,
                Entries = entries,
                SkippedEntries = skipped
            };

            _entries.Clear();
            _entries.AddRange(entries);

            // Validate required Cabrillo v3 markers
            if (!_logFile.HasStartOfLog)
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = null, Reason = "Missing required START-OF-LOG marker", RawLine = null });
            }

            if (!_logFile.HasEndOfLog)
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = null, Reason = "Missing required END-OF-LOG marker", RawLine = null });
            }

            // If there are parsed QSO entries but no CALLSIGN header, record a skipped-header item so callers
            // can inspect problems. Do not throw here to keep import tolerant for unit tests and tools.
            if (_logFile.Entries.Count > 0)
            {
                if (!_logFile.Headers.ContainsKey("CALLSIGN") || string.IsNullOrWhiteSpace(_logFile.Headers["CALLSIGN"]))
                {
                    skipped.Add(new SkippedEntryInfo { SourceLineNumber = null, Reason = "Missing CALLSIGN header", RawLine = null });
                }
            }

            return OperationResult.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            // Preserve cancellation semantics by propagating
            throw;
        }
        catch (Exception ex)
        {
            // Convert unexpected exceptions into a failure OperationResult. Caller may log Diagnostic.
            return OperationResult.Failure<Unit>($"Failed to import file: {ex.Message}", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Parse exchange tokens from the parts starting at the provided index.
    /// Returns (sentExchange, theirCall, receivedExchange).
    /// Performs validation of tokens and records any unparsable or invalid tokens to the provided skipped list.
    /// This tries to be tolerant: it will consume up to 5 tokens for the sent exchange, then the next token may be their call,
    /// and then up to 5 tokens for the received exchange.
    /// </summary>
    private static (Exchange? sent, string? theirCall, Exchange? recv) ParseExchanges(string[] parts, int startIndex, List<SkippedEntryInfo> skipped, int sourceLineNumber, string rawLine)
        {
        if (parts == null)
        {
            return (null, null, null);
        }
    // Use source-generated Regex instances (GeneratedRegex) for best runtime performance
    // and to avoid allocating/parsing patterns on every invocation.
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
            // Assign and validate each token; record skips for invalid tokens but still store raw values
            sent.SentSig = tokens[0];
            if (!SigRegex().IsMatch(tokens[0]))
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid SentSig token", RawLine = rawLine });
            }

            sent.SentMsg = tokens[1];
            if (!MsgRegex().IsMatch(tokens[1]))
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid SentMsg token", RawLine = rawLine });
            }

            theirCall = tokens[2];
            if (!CallRegex().IsMatch(tokens[2]))
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid TheirCall token", RawLine = rawLine });
            }

            recv.ReceivedSig = tokens[3];
            if (!SigRegex().IsMatch(tokens[3]))
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid ReceivedSig token", RawLine = rawLine });
            }

            recv.ReceivedMsg = tokens[4];
            if (!MsgRegex().IsMatch(tokens[4]))
            {
                skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid ReceivedMsg token", RawLine = rawLine });
            }
        }
        else
        {
            // Best-effort: assign what we can in order, validating present tokens
            int p = 0;
            if (p < tokens.Length)
            {
                sent.SentSig = tokens[p++];
                if (!SigRegex().IsMatch(sent.SentSig)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid SentSig token", RawLine = rawLine });
            }
            if (p < tokens.Length)
            {
                sent.SentMsg = tokens[p++];
                if (!MsgRegex().IsMatch(sent.SentMsg)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid SentMsg token", RawLine = rawLine });
            }

            if (p < tokens.Length)
            {
                // If the next token looks like a callsign, treat it as theirCall; otherwise try to infer.
                if (IsLikelyCallsign(tokens[p]))
                {
                    theirCall = tokens[p++];
                    if (!CallRegex().IsMatch(theirCall)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid TheirCall token", RawLine = rawLine });
                }
                else if (tokens[p].Any(ch => char.IsLetter(ch)))
                {
                    // contains letters — likely not a pure numeric signal, treat as theirCall
                    theirCall = tokens[p++];
                    if (!CallRegex().IsMatch(theirCall)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid TheirCall token", RawLine = rawLine });
                }
            }

            if (p < tokens.Length)
            {
                recv.ReceivedSig = tokens[p++];
                if (!SigRegex().IsMatch(recv.ReceivedSig)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid ReceivedSig token", RawLine = rawLine });
            }
            if (p < tokens.Length)
            {
                recv.ReceivedMsg = tokens[p++];
                if (!MsgRegex().IsMatch(recv.ReceivedMsg)) skipped.Add(new SkippedEntryInfo { SourceLineNumber = sourceLineNumber, Reason = "Invalid ReceivedMsg token", RawLine = rawLine });
            }
        }

        bool sentAny = !string.IsNullOrWhiteSpace(sent.SentSig) || !string.IsNullOrWhiteSpace(sent.SentMsg);
        bool recvAny = !string.IsNullOrWhiteSpace(recv.ReceivedSig) || !string.IsNullOrWhiteSpace(recv.ReceivedMsg);

        return (sentAny ? sent : null, theirCall, recvAny ? recv : null);
    }

    /// <summary>
    /// Heuristic check to determine whether the supplied token looks like a callsign.
    /// This is intentionally permissive: it accepts alphanumeric tokens and tokens containing
    /// a single '/' separator commonly used for portable or special-event suffixes.
    /// </summary>
    /// <param name="token">Input token to evaluate for callsign-likeness.</param>
    /// <returns><c>true</c> when the token plausibly represents a callsign; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Parse a frequency token to support both legacy and modern Cabrillo frequency patterns.
    /// Supports official frequency values, integer kHz values, and band tokens for backward compatibility.
    /// Returns frequency in kHz or a mapped value for validation purposes.
    /// </summary>
    /// <param name="token">The frequency token to parse</param>
    /// <param name="logFile">The log file context (optional, for future extensibility)</param>
    private static int? ParseFrequencyToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        token = token.Trim();

        // Handle official Cabrillo frequency patterns (both legacy and v3)
        if (CabrilloConstants.OfficialFrequencies.TryGetValue(token, out int officialFreq))
        {
            return officialFreq;
        }

        // If token looks like a band token (e.g., "40m"), map to the band's lowest frequency kHz value
        Match m = Regex.Match(token, "^(\\d{1,3})m$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            if (int.TryParse(m.Groups[1].Value, out int bandNum))
            {
                int? mapped = MapBandTokenToFrequency(bandNum);
                if (mapped.HasValue) return mapped.Value;
            }
            return null;
        }

        // For backward compatibility with Salmon Run, try parse as double for floating formats
        if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
        {
            // Only consider whole-number portion (truncate) per rules
            int whole = (int)Math.Truncate(d);
            
            // Extended range validation - support HF through microwave frequencies
            if (whole < 1800) return null; // Below HF band
            
            // Reject the 55..1000 range per Salmon Run rules (invalid for any amateur frequency)
            if (whole >= 55 && whole <= 1000) return null;
            
            // Support extended microwave frequencies (up to 300 GHz)
            if (whole > 300000000) return null; // Above 300 GHz limit
            
            return whole;
        }

        // Try parse integer without decimals for backward compatibility
        if (int.TryParse(token, out int i))
        {
            // Extended range validation - support HF through microwave frequencies
            if (i < 1800) return null; // Below HF band
            
            // Reject the 55..1000 range per Salmon Run rules
            if (i >= 55 && i <= 1000) return null;
            
            // Support extended microwave frequencies (up to 300 GHz)
            if (i > 300000000) return null; // Above 300 GHz limit
            
            return i;
        }

        return null;
    }

    /// <summary>
    /// Map an integer kHz frequency to one of the Salmon Run bands (e.g., "40m"). Returns null when no band matches.
    /// </summary>
    private static string? MapFrequencyToBand(int freqKHz)
    {
        // 160m <-> 1800 kHz through 2000 kHz
        if (freqKHz >= 1800 && freqKHz <= 2000) return "160m";
        // 80m <-> 3500 kHz through 4000 kHz
        if (freqKHz >= 3500 && freqKHz <= 4000) return "80m";
        // 40m <-> 7000 kHz through 7300 kHz
        if (freqKHz >= 7000 && freqKHz <= 7300) return "40m";
        // 20m <-> 14000 kHz through 14350 kHz
        if (freqKHz >= 14000 && freqKHz <= 14350) return "20m";
        // 15m <-> 21000 kHz through 21450 kHz
        if (freqKHz >= 21000 && freqKHz <= 21450) return "15m";
        // 10m <-> 28000 kHz through 29700 kHz
        if (freqKHz >= 28000 && freqKHz <= 29700) return "10m";
        // 6m <-> 50000 kHz through 54000 kHz
        if (freqKHz >= 50000 && freqKHz <= 54000) return "6m";

        return null;
    }

    /// <summary>
    /// Map a band number like 40 -> low frequency (kHz) for that band, or null when unknown.
    /// This is used when logs supply a band token instead of a numeric frequency.
    /// </summary>
    private static int? MapBandTokenToFrequency(int bandNumber)
    {
        return bandNumber switch
        {
            160 => 1800,
            80 => 3500,
            40 => 7000,
            20 => 14000,
            15 => 21000,
            10 => 28000,
            6 => 50000,
            _ => null
        };
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

        // Return defensive clones so external callers cannot mutate internal state
        return result.Select(e => e.Clone());
    }

    // Helper to determine if an entry belongs to the specified band.
    // Band must match the canonical list (e.g., "40m"). Comparison is case-insensitive.
    private static bool EntryMatchesBand(LogEntry entry, string band)
    {
        if (entry == null || string.IsNullOrWhiteSpace(band)) return false;
        string normalized = band.Trim();
        // Require band in the form '40m', '20m', etc. Do not accept '40' (per user request).
        // Normalize casing for comparison.
        string target = normalized.ToLowerInvariant();

        // If entry.Band is present, compare directly.
        if (!string.IsNullOrWhiteSpace(entry.Band))
        {
            return string.Equals(entry.Band!.Trim(), target, StringComparison.OrdinalIgnoreCase);
        }

        // Otherwise, try to map frequency to band.
        if (!string.IsNullOrWhiteSpace(entry.Frequency))
        {
            int? freq = ParseFrequencyToken(entry.Frequency); // Use standard parsing for static method
            if (freq.HasValue)
            {
                string? mapped = MapFrequencyToBand(freq.Value);
                return mapped != null && string.Equals(mapped, target, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    /// <summary>
    /// Return a sequence of defensive clones of stored <see cref="LogEntry"/> items.
    /// The returned sequence respects optional filtering, ordering and simple paging parameters.
    /// Callers receive clones so modifications do not affect the processor's internal state.
    /// </summary>
    /// <param name="filter">Optional predicate to filter returned entries.</param>
    /// <param name="orderBy">Optional key selector to order results.</param>
    /// <param name="skip">Optional number of items to skip (for paging).</param>
    /// <param name="take">Optional maximum number of items to return (for paging).</param>
    /// <returns>An <see cref="IEnumerable{LogEntry}"/> of defensive clones matching the query.</returns>

    /// <summary>
    /// OperationResult-based wrapper for ReadEntries to support a result-returning API surface.
    /// Returns a successful OperationResult containing defensive clones when the operation completes normally.
    /// Unexpected exceptions are converted into a failure OperationResult with Diagnostic populated.
    /// </summary>
    public OperationResult<IEnumerable<LogEntry>> ReadEntriesResult(Func<LogEntry, bool>? filter = null, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null)
    {
        try
        {
            // ReadEntries returns a deferred IEnumerable that clones entries on enumeration.
            // Materialize into a list here so any cloning-time exceptions are observed and
            // can be converted into a failure OperationResult rather than escaping later.
            IEnumerable<LogEntry> entries = ReadEntries(filter, orderBy, skip, take);
            List<LogEntry> materialized = entries.ToList();
            return OperationResult.Success<IEnumerable<LogEntry>>(materialized);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<IEnumerable<LogEntry>>("Failed to read entries.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Read entries that match the specified band (e.g., "40m"). Band comparison requires the canonical
    /// band token (including the trailing 'm') and is case-insensitive. Returns defensive clones.
    /// </summary>
    public OperationResult<IEnumerable<LogEntry>> ReadEntriesByBandResult(string band, Func<LogEntry, object>? orderBy = null, int? skip = null, int? take = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(band)) return OperationResult.Failure<IEnumerable<LogEntry>>("Band must not be null or whitespace.", ResponseStatus.BadFormat);

            IEnumerable<LogEntry> entries = ReadEntries(entry => EntryMatchesBand(entry, band), orderBy, skip, take);
            List<LogEntry> materialized = entries.ToList();
            return OperationResult.Success<IEnumerable<LogEntry>>(materialized);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<IEnumerable<LogEntry>>("Failed to read entries by band.", ResponseStatus.Error, ex);
        }
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
    // Internal synchronous implementations (private) restored to support OperationResult wrappers.
    private LogEntry CreateEntry(LogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Id)) entry.Id = Guid.NewGuid().ToString();

        // TheirCall is required for a valid QSO entry
        if (string.IsNullOrWhiteSpace(entry.TheirCall))
        {
            throw new ArgumentException("TheirCall (the other station's callsign) is required for a log entry.", nameof(entry));
        }

        // Ensure we have an internal CabrilloLogFile structure so ExportFile can operate
        if (_logFile == null)
        {
            _logFile = new CabrilloLogFile
            {
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Entries = new List<LogEntry>()
            };
        }
        else if (_logFile.Entries == null)
        {
            _logFile.Entries = new List<LogEntry>();
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

        // Sanitize any long/suspicious values on the copy before inserting
        SanitizeLogEntry(copy);

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
        EntryAdded?.Invoke(this, copy.Clone());
        return copy.Clone();
    }

    /// <summary>
    /// OperationResult-based wrapper for CreateEntry to support the new API on ILogProcessor.
    /// </summary>
    public OperationResult<LogEntry> CreateEntryResult(LogEntry entry)
    {
        try
        {
            LogEntry created = CreateEntry(entry);
            return OperationResult.Success(created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException argEx)
        {
            return OperationResult.Failure<LogEntry>(argEx.Message, ResponseStatus.BadFormat, argEx);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<LogEntry>("Failed to create entry.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Duplicate an existing entry. Copies all fields and exchanges, assigns a new Id,
    /// and optionally replaces a field on the SentExchange with the supplied new value.
    /// </summary>
    private LogEntry DuplicateEntry(string id, ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None, string? newValue = null)
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

        // Sanitize the duplicate copy before inserting/returning
        SanitizeLogEntry(copy);

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

        EntryAdded?.Invoke(this, copy.Clone());
        return copy.Clone();
    }

    /// <summary>
    /// OperationResult-based wrapper for DuplicateEntry to support the new API on ILogProcessor.
    /// </summary>
    public OperationResult<LogEntry> DuplicateEntryResult(string id, ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None, string? newValue = null)
    {
        try
        {
            LogEntry dup = DuplicateEntry(id, field, newValue);
            return OperationResult.Success(dup);
        }
        catch (ArgumentNullException an)
        {
            return OperationResult.Failure<LogEntry>(an.Message, ResponseStatus.BadFormat, an);
        }
        catch (ArgumentException a)
        {
            return OperationResult.Failure<LogEntry>(a.Message, ResponseStatus.NotFound, a);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<LogEntry>("Failed to duplicate entry.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Retrieve a defensive clone of the stored entry with the specified <paramref name="id"/>, or <c>null</c> when not found.
    /// This private helper returns a clone to maintain the processor's internal invariants.
    /// </summary>
    /// <param name="id">The identifier of the entry to retrieve.</param>
    /// <returns>A clone of the stored <see cref="LogEntry"/> when found; otherwise <c>null</c>.</returns>
    private LogEntry? GetEntryById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        LogEntry? found = _entries.FirstOrDefault(x => x.Id == id);
        return found?.Clone();
    }

    /// <summary>
    /// OperationResult-based wrapper for GetEntryById to support the new API on ILogProcessor.
    /// Returns Success with a defensive clone of the found entry, or NotFound when no entry exists.
    /// </summary>
    public OperationResult<LogEntry> GetEntryByIdResult(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return OperationResult.Failure<LogEntry>("Id must not be null or whitespace.", ResponseStatus.BadFormat);
            }

            LogEntry? found = GetEntryById(id);
            if (found == null)
            {
                return OperationResult.Failure<LogEntry>($"No entry found with id {id}", ResponseStatus.NotFound);
            }

            return OperationResult.Success(found);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<LogEntry>("Failed to get entry by id.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Apply an in-place edit action to the stored entry with the given <paramref name="id"/>.
    /// The edit action is applied to the stored instance and the <see cref="EntryUpdated"/> event
    /// is raised with a defensive clone when the update succeeds.
    /// </summary>
    /// <param name="id">Identifier of the entry to update.</param>
    /// <param name="editAction">Action to apply to the stored <see cref="LogEntry"/> instance.</param>
    /// <returns><c>true</c> when the entry was found and updated; otherwise <c>false</c>.</returns>
    private bool UpdateEntry(string id, Action<LogEntry> editAction)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        LogEntry? entry = _entries.FirstOrDefault(x => x.Id == id);
        if (entry == null) return false;
        editAction?.Invoke(entry);

        // Sanitize any fields modified by the edit action
        SanitizeLogEntry(entry);

        EntryUpdated?.Invoke(this, entry.Clone());
        return true;
    }

    /// <summary>
    /// OperationResult-based wrapper for UpdateEntry to support the new API on ILogProcessor.
    /// </summary>
    public OperationResult<Unit> UpdateEntryResult(string id, Action<LogEntry> editAction)
    {
        try
        {
            bool ok = UpdateEntry(id, editAction);
            if (ok) return OperationResult.Success(Unit.Value);
            return OperationResult.Failure<Unit>($"No entry found with id {id}", ResponseStatus.NotFound);
        }
        catch (ArgumentNullException an)
        {
            return OperationResult.Failure<Unit>(an.Message, ResponseStatus.BadFormat, an);
        }
        catch (ArgumentException a)
        {
            return OperationResult.Failure<Unit>(a.Message, ResponseStatus.BadFormat, a);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<Unit>("Failed to update entry.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// OperationResult-based deletion of an entry. Removes the entry from the internal lists and
    /// keeps the internal `_logFile.Entries` collection in sync when present.
    /// </summary>
    public OperationResult<Unit> DeleteEntryResult(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return OperationResult.Failure<Unit>("Id must not be null or whitespace.", ResponseStatus.BadFormat);
            }

            LogEntry? entry = _entries.FirstOrDefault(x => x.Id == id);
            if (entry == null)
            {
                return OperationResult.Failure<Unit>($"No entry found with id {id}", ResponseStatus.NotFound);
            }

            // Remove from the in-memory list
            _entries.Remove(entry);

            // Keep _logFile entries in sync if present
            if (_logFile != null && _logFile.Entries != null)
            {
                // Find and remove the item with the same Id
                int idx = _logFile.Entries.FindIndex(e => e != null && e.Id == id);
                if (idx >= 0)
                {
                    _logFile.Entries.RemoveAt(idx);
                }
            }

            EntryDeleted?.Invoke(this, id);
            return OperationResult.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<Unit>("Failed to delete entry.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Export the current in-memory log to the specified file path.
    /// Writes header tags and QSO lines in the processor's in-memory order, appends a single
    /// <c>END-OF-LOG:</c> line, and forces CRLF line endings for compatibility with Cabrillo
    /// consumers. The method will add a <c>.log</c> extension when one is not present.
    /// </summary>
    /// <param name="filePath">Destination file path to write the exported log.</param>
    /// <param name="useCanonicalFormat">If <c>true</c>, write canonical, padded Cabrillo QSO lines; otherwise prefer raw lines when available.</param>
    /// <exception cref="InvalidOperationException">Thrown when no log data is available to export.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the destination directory does not exist.</exception>
    private void ExportFile(string filePath, bool useCanonicalFormat = true, bool useBandToken = false)
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
                lines.Add(entry.ToCabrilloLine(useBandToken));
            }
            else
            {
                // If no raw line is available (we drop RawLine for parsed entries to save memory),
                // fall back to canonical formatting so exported files remain valid.
                lines.Add(entry.RawLine ?? entry.ToCabrilloLine(useBandToken));
            }
        }

        // Append a single END-OF-LOG: line as the final line and ensure the file ends with CRLF
        lines.Add("END-OF-LOG:");

        // Force CRLF as the line terminator regardless of platform
        const string crlf = "\r\n";
        string content = string.Join(crlf, lines) + crlf;
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// OperationResult-based wrapper for ExportFile to support the new API on ILogProcessor.
    /// </summary>
    public OperationResult<Unit> ExportFileResult(string filePath, bool useCanonicalFormat = true, bool useBandToken = false)
    {
        try
        {
            ExportFile(filePath, useCanonicalFormat, useBandToken);
            return OperationResult.Success(Unit.Value);
        }
        catch (ArgumentNullException an)
        {
            return OperationResult.Failure<Unit>(an.Message, ResponseStatus.BadFormat, an);
        }
        catch (DirectoryNotFoundException dn)
        {
            return OperationResult.Failure<Unit>(dn.Message, ResponseStatus.NotFound, dn);
        }
        catch (OperationCanceledException)
        {
            throw; // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<Unit>("Failed to export file.", ResponseStatus.Error, ex);
        }
    }

    /// <summary>
    /// Parse optional transmitter ID from QSO line tokens.
    /// Per Cabrillo v3 spec, transmitter ID is 0 or 1 and appears at the end of the QSO line.
    /// Returns null if not present or invalid.
    /// </summary>
    private static int? ParseTransmitterId(string[] parts, Exchange? sentExch, Exchange? recvExch, string? theirCall)
    {
        // Calculate expected position: QSO: freq mode date time mycall + exchanges + transmitterId
        // Minimum tokens: QSO: freq mode date time mycall (5 base tokens + exchanges)
        int expectedMinTokens = 6; // Base tokens
        if (sentExch != null) expectedMinTokens += CountNonEmptyExchangeFields(sentExch);
        if (!string.IsNullOrWhiteSpace(theirCall)) expectedMinTokens += 1;
        if (recvExch != null) expectedMinTokens += CountNonEmptyExchangeFields(recvExch);
        
        // Check if there's one more token that could be transmitter ID
        if (parts.Length > expectedMinTokens)
        {
            string lastToken = parts[parts.Length - 1];
            if (int.TryParse(lastToken, out int transmitterId) && (transmitterId == 0 || transmitterId == 1))
            {
                return transmitterId;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Count non-empty exchange fields to help calculate token positions.
    /// </summary>
    private static int CountNonEmptyExchangeFields(Exchange exchange)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(exchange.SentSig)) count++;
        if (!string.IsNullOrWhiteSpace(exchange.SentMsg)) count++;
        if (!string.IsNullOrWhiteSpace(exchange.ReceivedSig)) count++;
        if (!string.IsNullOrWhiteSpace(exchange.ReceivedMsg)) count++;
        return count;
    }

    /// <summary>
    /// Validate header value against Cabrillo v3 enumerated requirements.
    /// Returns validation result with error message if invalid.
    /// </summary>
    private static OperationResult<Unit> ValidateHeaderEnumeratedValue(string key, string value, int lineNumber)
    {
        return key.ToUpperInvariant() switch
        {
            "CATEGORY-TIME" => ValidateCategoryTime(value),
            "CATEGORY-OVERLAY" => ValidateCategoryOverlay(value),
            "CATEGORY-ASSISTED" => ValidateFromSet(value, CategoryAssistedExtensions.GetAllValidValues(), "CATEGORY-ASSISTED"),
            "CATEGORY-BAND" => ValidateFromSet(value, CabrilloConstants.CategoryBandValues, "CATEGORY-BAND"),
            "CATEGORY-MODE" => ValidateFromSet(value, CategoryModeExtensions.GetAllValidValues(), "CATEGORY-MODE"),
            "CATEGORY-OPERATOR" => ValidateFromSet(value, CategoryOperatorExtensions.GetAllValidValues(), "CATEGORY-OPERATOR"),
            "CATEGORY-POWER" => ValidateFromSet(value, CategoryPowerExtensions.GetAllValidValues(), "CATEGORY-POWER"),
            "CATEGORY-STATION" => ValidateCategoryStation(value),
            "CATEGORY-TRANSMITTER" => ValidateCategoryTransmitter(value),
            _ => OperationResult.Success(Unit.Value) // No validation required for other headers
        };
    }

    private static OperationResult<Unit> ValidateCategoryTime(string value)
    {
        return CategoryTimeExtensions.TryParse(value, out CategoryTime _)
            ? OperationResult.Success(Unit.Value)
            : OperationResult.Failure<Unit>($"Invalid CATEGORY-TIME value '{value}'. Must be one of: {string.Join(", ", CategoryTimeExtensions.GetAllValidValues())}", ResponseStatus.BadFormat);
    }

    private static OperationResult<Unit> ValidateCategoryOverlay(string value)
    {
        return CategoryOverlayExtensions.TryParse(value, out CategoryOverlay _)
            ? OperationResult.Success(Unit.Value)
            : OperationResult.Failure<Unit>($"Invalid CATEGORY-OVERLAY value '{value}'. Must be one of: {string.Join(", ", CategoryOverlayExtensions.GetAllValidValues())}", ResponseStatus.BadFormat);
    }

    private static OperationResult<Unit> ValidateCategoryStation(string value)
    {
        return CategoryStationExtensions.TryParse(value, out CategoryStation _)
            ? OperationResult.Success(Unit.Value)
            : OperationResult.Failure<Unit>($"Invalid CATEGORY-STATION value '{value}'. Must be one of: {string.Join(", ", CategoryStationExtensions.GetAllValidValues())}", ResponseStatus.BadFormat);
    }

    private static OperationResult<Unit> ValidateCategoryTransmitter(string value)
    {
        return CategoryTransmitterExtensions.TryParse(value, out CategoryTransmitter _)
            ? OperationResult.Success(Unit.Value)
            : OperationResult.Failure<Unit>($"Invalid CATEGORY-TRANSMITTER value '{value}'. Must be one of: {string.Join(", ", CategoryTransmitterExtensions.GetAllValidValues())}", ResponseStatus.BadFormat);
    }

    private static OperationResult<Unit> ValidateFromSet(string value, string[] validValues, string headerName)
    {
        HashSet<string> validSet = new HashSet<string>(validValues, StringComparer.OrdinalIgnoreCase);
        return validSet.Contains(value)
            ? OperationResult.Success(Unit.Value)
            : OperationResult.Failure<Unit>($"Invalid {headerName} value '{value}'. Must be one of: {string.Join(", ", validValues)}", ResponseStatus.BadFormat);
    }

    // Source-generated regex helpers (faster at runtime and AOT/trimming friendly)
    [System.Text.RegularExpressions.GeneratedRegex("^(?:[1-5][0-9]{1,2}|[1-5][nN]{1,2})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex SigRegex();

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9]{1,5}(?:/[A-Za-z0-9]{1,5})?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex MsgRegex();

    [System.Text.RegularExpressions.GeneratedRegex("^(?:[A-Za-z0-9]{2,5}/)?[A-Za-z0-9]{1,5}(?:/[A-Za-z0-9]{2,5})?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex CallRegex();
}
