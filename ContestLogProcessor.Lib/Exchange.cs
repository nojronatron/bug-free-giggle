namespace ContestLogProcessor.Lib;

/// <summary>
/// A structured representation of the five exchange elements used in Cabrillo v3 QSO formats.
/// Per Cabrillo v3 spec, the QSO line contains: mycall, sent-rst, sent-exch, theircall, rcvd-rst, rcvd-exch.
/// All elements are stored as strings to preserve original formatting and maintain tolerance to borderline values.
/// Validation is performed separately by parser regex patterns and contest-specific scoring services.
/// </summary>
public class Exchange
{
    /// <summary>
    /// Sent signal report (RST). Typically "59", "599", "5NN" or similar 2-3 character pattern.
    /// Valid pattern: ^(?:[1-5][0-9]{1,2}|[1-5][nN]{1,2})$
    /// </summary>
    public string? SentSig { get; set; }

    /// <summary>
    /// Sent exchange message. Contest-specific data such as serial number, section, county, etc.
    /// Per Cabrillo v3: 1-6 characters total.
    /// Valid pattern: ^(?:[A-Za-z0-9]{1,6}|[A-Za-z0-9]{1,2}/[A-Za-z0-9]{1,3})$
    /// Examples: "WA", "KING", "001", "3O", "OR/WA"
    /// </summary>
    public string? SentMsg { get; set; }

    /// <summary>
    /// Their callsign (the contacted station's call). May include prefix/suffix with slashes.
    /// Per Cabrillo v3: 3-13 characters total with possible '/' characters.
    /// Valid pattern: ^(?:[A-Za-z0-9]{2,3}/)?[A-Za-z0-9]{3,5}(?:/[A-Za-z0-9]{2,3})?$
    /// Examples: "K7XXX", "W7/K7XXX", "K7XXX/W7"
    /// </summary>
    public string? TheirCall { get; set; }

    /// <summary>
    /// Received signal report (RST). Typically "59", "599", "5NN" or similar 2-3 character pattern.
    /// Valid pattern: ^(?:[1-5][0-9]{1,2}|[1-5][nN]{1,2})$
    /// </summary>
    public string? ReceivedSig { get; set; }

    /// <summary>
    /// Received exchange message. Contest-specific data such as serial number, section, county, etc.
    /// Per Cabrillo v3: 1-6 characters total.
    /// Valid pattern: ^(?:[A-Za-z0-9]{1,6}|[A-Za-z0-9]{1,2}/[A-Za-z0-9]{1,3})$
    /// Examples: "OR", "KING", "042", "1A", "CT/MA"
    /// </summary>
    public string? ReceivedMsg { get; set; }

    public override string ToString()
    {
        // Compose using empty string for missing parts to preserve token positions when possible.
        return string.Join(
            ' ',
            new[] {
                SentSig ?? string.Empty,
                SentMsg ?? string.Empty,
                TheirCall ?? string.Empty,
                ReceivedSig ?? string.Empty,
                ReceivedMsg ?? string.Empty
                }).TrimEnd();
    }

    /// <summary>
    /// Create a deep copy of this Exchange instance.
    /// </summary>
    public Exchange Clone()
    {
        return new Exchange
        {
            SentSig = this.SentSig,
            SentMsg = this.SentMsg,
            TheirCall = this.TheirCall,
            ReceivedSig = this.ReceivedSig,
            ReceivedMsg = this.ReceivedMsg
        };
    }
}
