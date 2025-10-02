namespace ContestLogProcessor.Lib;

/// <summary>
/// A structured representation of the five exchange elements used in many Cabrillo QSO formats.
/// The Cabrillo spec defines five space-separated elements for the exchange; the semantic meaning
/// varies by contest but commonly includes a serial number, state/section/club code, locator or name, etc.
/// All elements are stored as strings to keep the original formatting and to avoid rejecting borderline values.
/// </summary>
public class Exchange
{
    // Using string properties keeps the model tolerant to malformed input; validation/parsing to numeric types
    // should be done by caller code when strict typing is required.
    public string? SentSig { get; set; } // up to 3 whole numbers (sent signal report 59, 599, 5NN, etc)
    public string? SentMsg { get; set; } // up to 5 alpha characters (sent message e.g. county ID, serial number, etc)
    public string? TheirCall { get; set; } // up to 15 alphanumeric incl optional '/' (call sign with or without slant)
    public string? ReceivedSig { get; set; } // up to 3 whole numbers (received signal report 59, 599, 5NN, etc)
    public string? ReceivedMsg { get; set; } // up to 5 alpha characters (received message e.g. county ID, serial number, etc)

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
