using System;
using System.Collections.Generic;
using System.Linq;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Winter Field Day scoring service implementing contest rules:
/// - Phone contacts: 1 point each
/// - CW and Digital contacts: 2 points each
/// - Only one contact per station per band-mode combination
/// - Maximum 3 contacts per band (1 Phone, 1 CW, 1 Digital)
/// </summary>
public class WinterFieldDayScoringService : IContestScoringService<WinterFieldDayScoreResult>
{
    private readonly WinterFieldDayExchangeParser _exchangeParser;

    public string ContestId => "WFD";

    public WinterFieldDayScoringService()
    {
        _exchangeParser = new WinterFieldDayExchangeParser();
    }

    public OperationResult<WinterFieldDayScoreResult> CalculateScore(CabrilloLogFile log)
    {
        if (log == null)
        {
            return OperationResult.Failure<WinterFieldDayScoreResult>("Log file cannot be null", ResponseStatus.BadFormat);
        }

        WinterFieldDayScoreResult result = new WinterFieldDayScoreResult();

        // Validate CALLSIGN header exists
        if (!log.Headers.TryGetValue("CALLSIGN", out string? headerCall) || string.IsNullOrWhiteSpace(headerCall))
        {
            return OperationResult.Failure<WinterFieldDayScoreResult>(
                "Missing CALLSIGN header - update the header so at least one CALLSIGN matches at least one LogEntry Call field",
                ResponseStatus.BadFormat);
        }

        // Build set of allowable call signs from header
        HashSet<string> allowableCalls = headerCall.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Preprocess entries: ensure ordering by QsoDateTime ascending
        List<LogEntry> ordered = log.Entries
            .OrderBy(e => e.QsoDateTime)
            .ThenBy(e => e.SourceLineNumber ?? int.MaxValue)
            .ToList();

        // Quick check: ensure at least one entry's Call matches header set
        bool anyMatch = ordered.Any(e => !string.IsNullOrWhiteSpace(e.CallSign) && allowableCalls.Contains(e.CallSign.Trim().ToUpperInvariant()));
        if (!anyMatch)
        {
            return OperationResult.Failure<WinterFieldDayScoreResult>(
                "No LogEntry Call field matches any CALLSIGN header value - update the header or log entries to match.",
                ResponseStatus.BadFormat);
        }

        // Track contacts: TheirCall + Band + Mode (only one contact per combination)
        HashSet<string> seenContacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int qsoPoints = 0;
        int validContacts = 0;
        int totalContacts = 0;
        int duplicates = 0;

        foreach (LogEntry entry in ordered)
        {
            totalContacts++;

            // Skip X-QSO entries
            if (entry.IsXQso)
            {
                result.SkippedEntries.Add(new SkippedEntryInfo
                {
                    SourceLineNumber = entry.SourceLineNumber,
                    Reason = "X-QSO (ignored)",
                    RawLine = entry.RawLine
                });
                continue;
            }

            // Validate eligibility
            OperationResult<Unit> eligibilityResult = ValidateEntryEligibility(entry, allowableCalls);
            if (!eligibilityResult.IsSuccess)
            {
                result.SkippedEntries.Add(new SkippedEntryInfo
                {
                    SourceLineNumber = entry.SourceLineNumber,
                    Reason = eligibilityResult.ErrorMessage,
                    RawLine = entry.RawLine
                });
                continue;
            }

            string mode = entry.Mode!.Trim().ToUpperInvariant();
            string theirCall = entry.TheirCall!.Trim().ToUpperInvariant();
            string band = GetBandFromFrequency(entry.Frequency);

            // Check for duplicate contact (same call, same band, same mode)
            string contactKey = $"{theirCall}|{band}|{mode}";
            if (seenContacts.Contains(contactKey))
            {
                duplicates++;
                result.SkippedEntries.Add(new SkippedEntryInfo
                {
                    SourceLineNumber = entry.SourceLineNumber,
                    Reason = $"Duplicate contact with {theirCall} on {band} using {mode}",
                    RawLine = entry.RawLine
                });
                continue;
            }

            seenContacts.Add(contactKey);

            // Award QSO points based on mode
            int points = mode switch
            {
                "PH" or "FM" => 1,  // Phone modes
                "CW" => 2,          // CW
                "DG" or "RY" => 2,  // Digital modes
                _ => 1              // Default to 1 point for unknown modes
            };

            qsoPoints += points;
            validContacts++;

            // Count phone vs CW/Digital QSOs for reporting
            if (mode == "PH" || mode == "FM")
            {
                result.PhoneQsos++;
            }
            else if (mode == "CW" || mode == "DG" || mode == "RY")
            {
                result.CwDigitalQsos++;
            }

            // Track unique categories and locations (if we have exchange info)
            // TODO: Parse exchange information to get station category and location

            // Update statistics
            if (result.ContactsByBand.ContainsKey(band))
                result.ContactsByBand[band]++;
            else
                result.ContactsByBand[band] = 1;

            if (result.ContactsByMode.ContainsKey(mode))
                result.ContactsByMode[mode]++;
            else
                result.ContactsByMode[mode] = 1;
        }

        // Calculate final score (no multiplier for WFD - just straight points)
        result.FinalScore = qsoPoints;
        result.QsoPoints = qsoPoints;
        result.TotalContacts = validContacts;
        result.DuplicateContacts = duplicates;

        return OperationResult.Success(result);
    }

    private OperationResult<Unit> ValidateEntryEligibility(LogEntry entry, HashSet<string> allowableCalls)
    {
        // Must not be empty or contain only whitespace
        if (string.IsNullOrWhiteSpace(entry.Frequency) ||
            string.IsNullOrWhiteSpace(entry.Mode) ||
            string.IsNullOrWhiteSpace(entry.CallSign) ||
            string.IsNullOrWhiteSpace(entry.TheirCall) ||
            string.IsNullOrWhiteSpace(entry.SentExchange?.SentMsg) ||
            string.IsNullOrWhiteSpace(entry.ReceivedExchange?.ReceivedMsg))
        {
            return OperationResult.Failure<Unit>("Missing required fields", ResponseStatus.BadFormat);
        }

        // Mode must be valid Winter Field Day mode
        string mode = entry.Mode.Trim().ToUpperInvariant();
        if (mode != "PH" && mode != "CW" && mode != "DG" && mode != "RY" && mode != "FM")
        {
            return OperationResult.Failure<Unit>($"Unsupported mode: {mode}", ResponseStatus.BadFormat);
        }

        // Call must match header CALLSIGN
        string call = entry.CallSign.Trim().ToUpperInvariant();
        if (!allowableCalls.Contains(call))
        {
            return OperationResult.Failure<Unit>("Call does not match CALLSIGN header", ResponseStatus.BadFormat);
        }

        // Validate exchange format
        OperationResult<WfdInfoSent> sentResult = _exchangeParser.ParseSentExchange(entry.SentExchange?.SentSig ?? "", entry.SentExchange?.SentMsg ?? "");
        if (!sentResult.IsSuccess)
        {
            return OperationResult.Failure<Unit>($"Invalid sent exchange: {sentResult.ErrorMessage}", ResponseStatus.BadFormat);
        }

        OperationResult<WfdInfoReceived> receivedResult = _exchangeParser.ParseReceivedExchange(entry.ReceivedExchange?.ReceivedSig ?? "", entry.ReceivedExchange?.ReceivedMsg ?? "");
        if (!receivedResult.IsSuccess)
        {
            return OperationResult.Failure<Unit>($"Invalid received exchange: {receivedResult.ErrorMessage}", ResponseStatus.BadFormat);
        }

        return OperationResult.Success(Unit.Value);
    }

    private string GetBandFromFrequency(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            return "UNKNOWN";

        // Try to parse frequency and map to band
        if (int.TryParse(frequency.Trim(), out int freq))
        {
            // Map frequency (kHz) to band
            return freq switch
            {
                >= 1800 and <= 2000 => "160M",
                >= 3500 and <= 4000 => "80M", 
                >= 7000 and <= 7300 => "40M",
                >= 14000 and <= 14350 => "20M",
                >= 21000 and <= 21450 => "15M",
                >= 28000 and <= 29700 => "10M",
                >= 50000 and <= 54000 => "6M",
                >= 144000 and <= 148000 => "2M",
                >= 222000 and <= 225000 => "1.25M",
                >= 420000 and <= 450000 => "70CM",
                _ => frequency.Trim().ToUpperInvariant()
            };
        }

        return frequency.Trim().ToUpperInvariant();
    }
}