using System;

namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents a single Cabrillo log entry (QSO).
/// This type models the common Cabrillo QSO token elements such as frequency, mode,
/// date/time, callsign and structured exchanges (sent and received).
/// Parsing of the raw line into these fields is performed by the processor; this
/// class is a simple container that preserves both structured values and the original raw line.
/// </summary>
public class LogEntry
{
    public LogEntry()
    {
        Id = Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Unique identifier for this LogEntry (GUID string).
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Raw original QSO line from the file (preserved for fidelity when exporting if desired).
    /// </summary>
    public string? RawLine { get; set; }

    /// <summary>
    /// Frequency token exactly as found in the QSO line (e.g. "3930", "7.053", "14G").
    /// Keep as string to preserve formats like "LIGHT" or those containing a trailing 'G'.
    /// </summary>
    public string? Frequency { get; set; }

    /// <summary>
    /// Mode token (PH, CW, FM, RY, DG, etc.).
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Combined date/time of the QSO expressed in UTC. Consumer code should ensure
    /// values are provided in UTC; when unknown this may be DateTime.MinValue.
    /// </summary>
    public DateTime QsoDateTime { get; set; }

    /// <summary>
    /// Callsign of the logging operator (CALLSIGN element in the log header).
    /// </summary>
    public string? CallSign { get; set; }

    /// <summary>
    /// Structured representation of the sent exchange (the five exchange elements defined by the Cabrillo spec).
    /// Parts are stored as strings to preserve formatting (leading zeros are not expected by the spec but may be present in malformed logs).
    /// </summary>
    public Exchange? SentExchange { get; set; }

    /// <summary>
    /// Structured representation of the received exchange (the five exchange elements defined by the Cabrillo spec).
    /// </summary>
    public Exchange? ReceivedExchange { get; set; }

    /// <summary>
    /// Optional band label when the parser determines one (e.g., "80m", "20m"). This is distinct from the Frequency token.
    /// </summary>
    public string? Band { get; set; }

    /// <summary>
    /// Flag indicating the QSO was marked with an X-QSO tag (ignored by some processors).
    /// </summary>
    public bool IsXQso { get; set; }

    /// <summary>
    /// Convenience: returns the canonical (string) representation of the sent exchange
    /// matching the original Cabrillo token layout (space-separated five elements).
    /// If SentExchange is null, null is returned.
    /// </summary>
    public string? SentExchangeString => SentExchange?.ToString();

    /// <summary>
    /// Convenience: returns the canonical (string) representation of the received exchange
    /// matching the original Cabrillo token layout (space-separated five elements).
    /// If ReceivedExchange is null, null is returned.
    /// </summary>
    public string? ReceivedExchangeString => ReceivedExchange?.ToString();

    /// <summary>
    /// The "their callsign" token that commonly appears between the sent and received exchange elements.
    /// This is optional and may be null when not present in the original line.
    /// </summary>
    public string? TheirCall { get; set; }

    /// <summary>
    /// Produce a canonical Cabrillo QSO line from the structured fields.
    /// If a field is missing, an empty placeholder is used to preserve token positions where possible.
    /// Example: "QSO: 14000 CW 2025-09-26 2100 K7RMZ 001 WA OR 59" (simplified example)
    /// </summary>
    public string ToCabrilloLine()
    {
        // Date/time formatting per spec: yyyy-MM-dd and HHmm in UTC.
        var datePart = QsoDateTime == DateTime.MinValue ? string.Empty : QsoDateTime.ToString("yyyy-MM-dd");
        var timePart = QsoDateTime == DateTime.MinValue ? string.Empty : QsoDateTime.ToString("HHmm");

        string freq = Frequency ?? string.Empty;
        string mode = Mode ?? string.Empty;
        string call = CallSign ?? string.Empty;

        string sent = SentExchangeString ?? string.Empty;
        string recv = ReceivedExchangeString ?? string.Empty;

        // Compose with single spaces and trim end. We don't include "theirCall" separately because the
        // parser does not currently store it as a separate field; if it is needed later we can add it.
        var tokens = new List<string> { "QSO:", freq, mode, datePart, timePart, call };

        if (!string.IsNullOrEmpty(sent)) tokens.Add(sent);
        if (!string.IsNullOrEmpty(TheirCall)) tokens.Add(TheirCall);
        if (!string.IsNullOrEmpty(recv)) tokens.Add(recv);

        return string.Join(' ', tokens).TrimEnd();
    }
}
