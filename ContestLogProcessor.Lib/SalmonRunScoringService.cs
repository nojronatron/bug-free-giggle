using System;
using System.Collections.Generic;
using System.Linq;

namespace ContestLogProcessor.Lib;

public class SalmonRunScoringService
{
    private readonly ILocationLookup _lookup;

    public SalmonRunScoringService(ILocationLookup? lookup = null)
    {
        _lookup = lookup ?? new InMemoryLocationLookup();
    }

    [Obsolete("Use CalculateScoreResult(CabrilloLogFile) which returns an OperationResult<SalmonRunScoreResult> for recoverable failures.")]
    public SalmonRunScoreResult CalculateScore(CabrilloLogFile log)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));

    SalmonRunScoreResult result = new SalmonRunScoreResult();

        // Validate CALLSIGN header exists and that at least one entry matches a header CALLSIGN
    if (!log.Headers.TryGetValue("CALLSIGN", out string? headerCall) || string.IsNullOrWhiteSpace(headerCall))
        {
            throw new InvalidOperationException("Missing CALLSIGN header - update the header so at least one CALLSIGN matches at least one LogEntry Call field");
        }

        // Build set of allowable call signs from header (support multiple CALLSIGN lines separated by commas)
        System.Collections.Generic.HashSet<string> allowableCalls = headerCall.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Preprocess entries: ensure ordering by QsoDateTime ascending, then original SourceLineNumber
        System.Collections.Generic.List<LogEntry> ordered = log.Entries
            .OrderBy(e => e.QsoDateTime)
            .ThenBy(e => e.SourceLineNumber ?? int.MaxValue)
            .ToList();

        // Quick check: ensure at least one entry's Call matches header set
        bool anyMatch = ordered.Any(e => !string.IsNullOrWhiteSpace(e.CallSign) && allowableCalls.Contains(e.CallSign.Trim().ToUpperInvariant()));
        if (!anyMatch)
        {
            throw new InvalidOperationException("No LogEntry Call field matches any CALLSIGN header value - update the header or log entries to match.");
        }

        // Track seen (TheirCall, Mode, Band) to award QSO points only once per combination
    System.Collections.Generic.HashSet<string> seenQso = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Track unique ReceivedMsg tokens for multiplier per category
    System.Collections.Generic.HashSet<string> seenRecv = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
    System.Collections.Generic.List<LogEntry> uniqueWa = new System.Collections.Generic.List<LogEntry>();
    System.Collections.Generic.List<LogEntry> uniqueUs = new System.Collections.Generic.List<LogEntry>();
    System.Collections.Generic.List<LogEntry> uniqueCa = new System.Collections.Generic.List<LogEntry>();
    System.Collections.Generic.List<LogEntry> uniqueDx = new System.Collections.Generic.List<LogEntry>();

    int qsoPoints = 0;
    int w7dxPoints = 0;
    System.Collections.Generic.HashSet<string> w7dxModesSeen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (LogEntry entry in ordered)
        {
            // Skip X-QSO
            if (entry.IsXQso)
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "X-QSO (ignored)", RawLine = entry.RawLine });
                continue;
            }

            // Validate required fields per spec
            if (string.IsNullOrWhiteSpace(entry.TheirCall) || string.IsNullOrWhiteSpace(entry.ReceivedExchange?.ReceivedMsg) || string.IsNullOrWhiteSpace(entry.SentExchange?.SentMsg))
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Missing required tokens (TheirCall, SentMsg, or ReceivedMsg)", RawLine = entry.RawLine });
                continue;
            }

            // Mode
            if (string.IsNullOrWhiteSpace(entry.Mode))
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Mode missing", RawLine = entry.RawLine });
                continue;
            }

            string mode = entry.Mode.Trim().ToUpperInvariant();
            if (mode != "PH" && mode != "CW")
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Unsupported Mode", RawLine = entry.RawLine });
                continue;
            }

            // Call must match at least one header CALLSIGN
            if (string.IsNullOrWhiteSpace(entry.CallSign) || !allowableCalls.Contains(entry.CallSign.Trim().ToUpperInvariant()))
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Call does not match CALLSIGN header", RawLine = entry.RawLine });
                continue;
            }

            // Frequency/Band resolution
            FrequencyBand band = FrequencyBand.Unknown;
            if (!string.IsNullOrWhiteSpace(entry.Band))
            {
                // Try to normalize the band token (e.g., "40m")
                try
                {
                    string b = entry.Band.Trim().ToLowerInvariant();
                    if (b.EndsWith("m")) b = b.Substring(0, b.Length - 1);
                    switch (b)
                    {
                        case "160": band = FrequencyBand.M160; break;
                        case "80": band = FrequencyBand.M80; break;
                        case "40": band = FrequencyBand.M40; break;
                        case "20": band = FrequencyBand.M20; break;
                        case "15": band = FrequencyBand.M15; break;
                        case "10": band = FrequencyBand.M10; break;
                        case "6": band = FrequencyBand.M6; break;
                    }
                }
                catch { }
            }

            if (band == FrequencyBand.Unknown && !string.IsNullOrWhiteSpace(entry.Frequency))
            {
                if (FrequencyParser.TryParseFrequencyToken(entry.Frequency, out int freqK))
                {
                    band = FrequencyParser.GetBandForFrequency(freqK);
                }
            }

            if (band == FrequencyBand.Unknown)
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Unknown or invalid band/frequency", RawLine = entry.RawLine });
                continue;
            }

            // Validate SentMsg/ReceivedMsg token shapes
            string sentMsg = entry.SentExchange!.SentMsg!.Trim();
            string recvMsg = entry.ReceivedExchange!.ReceivedMsg!.Trim();
            if (!IsValidSentRecvToken(sentMsg) || !IsValidSentRecvToken(recvMsg))
            {
                result.SkippedEntries.Add(new SkippedEntryInfo { SourceLineNumber = entry.SourceLineNumber, Reason = "Invalid SentMsg/ReceivedMsg token format", RawLine = entry.RawLine });
                continue;
            }

            // Award QSO points once per TheirCall+Mode+Band
            string qsoKey = string.Join('|', entry.TheirCall!.Trim().ToUpperInvariant(), mode, band.ToString());
            if (!seenQso.Contains(qsoKey))
            {
                seenQso.Add(qsoKey);
                qsoPoints += mode == "PH" ? 2 : 3;
            }

            // W7DX handling (eligible only for valid entries)
            if (string.Equals(entry.TheirCall.Trim(), "W7DX", StringComparison.OrdinalIgnoreCase))
            {
                if (!w7dxModesSeen.Contains(mode) && w7dxModesSeen.Count < 2)
                {
                    w7dxModesSeen.Add(mode);
                    w7dxPoints += 500;
                }
            }

            // Multiplier accumulation: check ReceivedMsg uniqueness and category
            string normalizedRecv = recvMsg.Trim();
            if (!seenRecv.Contains(normalizedRecv))
            {
                seenRecv.Add(normalizedRecv);

                if (_lookup.TryMatchWashingtonCounty(normalizedRecv, out string? wa))
                {
                    uniqueWa.Add(entry);
                    result.UniqueWashingtonCounties.Add(wa);
                }
                else if (_lookup.TryMatchUSState(normalizedRecv, out string? us))
                {
                    // Exclude WA because it's in WA counties list; but per rules that is handled by order of checks.
                    uniqueUs.Add(entry);
                    result.UniqueUSStates.Add(us);
                }
                else if (_lookup.TryMatchCanadianProvince(normalizedRecv, out string? ca))
                {
                    uniqueCa.Add(entry);
                    result.UniqueCanadianProvinces.Add(ca);
                }
                else if (_lookup.TryMatchDxcc(normalizedRecv, out string? dx))
                {
                    if (uniqueDx.Count < 10)
                    {
                        uniqueDx.Add(entry);
                        result.UniqueDxccEntities.Add(dx);
                    }
                }
            }
        }

        result.QsoPoints = qsoPoints;
        result.W7DxBonusPoints = w7dxPoints;

        // Multiplier value is count of unique items across the four lists
        int multiplier = result.UniqueWashingtonCounties.Count + result.UniqueUSStates.Count + result.UniqueCanadianProvinces.Count + result.UniqueDxccEntities.Count;
        result.Multiplier = multiplier;

        result.FinalScore = (qsoPoints * multiplier) + w7dxPoints;

        return result;
    }

    /// <summary>
    /// New wrapper that returns an OperationResult. Recoverable validation failures are returned as OperationResult.Failure
    /// with ResponseStatus.BadFormat. Unexpected exceptions are returned with ResponseStatus.Error and Diagnostic populated.
    /// OperationCanceledException is rethrown.
    /// </summary>
    public OperationResult<SalmonRunScoreResult> CalculateScoreResult(CabrilloLogFile log)
    {
        try
        {
            if (log == null)
            {
                return OperationResult.Failure<SalmonRunScoreResult>("Log file is null", ResponseStatus.BadFormat);
            }

            // Validate CALLSIGN header exists and that at least one entry matches a header CALLSIGN
            if (!log.Headers.TryGetValue("CALLSIGN", out string? headerCall) || string.IsNullOrWhiteSpace(headerCall))
            {
                return OperationResult.Failure<SalmonRunScoreResult>("Missing CALLSIGN header - update the header so at least one CALLSIGN matches at least one LogEntry Call field", ResponseStatus.BadFormat);
            }

            // Build set of allowable call signs from header (support multiple CALLSIGN lines separated by commas)
            System.Collections.Generic.HashSet<string> allowableCalls = headerCall.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Preprocess entries: ensure ordering by QsoDateTime ascending, then original SourceLineNumber
            System.Collections.Generic.List<LogEntry> ordered = log.Entries
                .OrderBy(e => e.QsoDateTime)
                .ThenBy(e => e.SourceLineNumber ?? int.MaxValue)
                .ToList();

            // Quick check: ensure at least one entry's Call matches header set
            bool anyMatch = ordered.Any(e => !string.IsNullOrWhiteSpace(e.CallSign) && allowableCalls.Contains(e.CallSign.Trim().ToUpperInvariant()));
            if (!anyMatch)
            {
                return OperationResult.Failure<SalmonRunScoreResult>("No LogEntry Call field matches any CALLSIGN header value - update the header or log entries to match.", ResponseStatus.BadFormat);
            }

            // Now reuse existing implementation by calling the obsolete method to compute the result. This keeps behavior identical.
#pragma warning disable CS0618 // Suppress obsolete warning for internal compatibility shim
            SalmonRunScoreResult computed = CalculateScore(log);
#pragma warning restore CS0618
            return OperationResult.Success(computed);
        }
        catch (OperationCanceledException)
        {
            // Preserve cancellation semantics; do not convert to OperationResult
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Failure<SalmonRunScoreResult>("Unexpected error while calculating score", ResponseStatus.Error, ex);
        }
    }

    private static bool IsValidSentRecvToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        // Regex: ^[A-Za-z0-9]+(?:/[A-Za-z0-9]+)?$
        int slashCount = token.Count(c => c == '/');
        if (slashCount > 1) return false;

        foreach (char c in token)
        {
            if (c == '/') continue;
            if (!char.IsLetterOrDigit(c)) return false;
        }

        // slash cannot be first or last
        if (token.StartsWith('/') || token.EndsWith('/')) return false;

        return true;
    }
}
