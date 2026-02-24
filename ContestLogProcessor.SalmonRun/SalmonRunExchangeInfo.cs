using System;

using ContestLogProcessor.Lib;

namespace ContestLogProcessor.SalmonRun;

/// <summary>
/// Salmon Run sent exchange information (Location/County).
/// Example: "KING" (King County, WA) or "OR" (Oregon)
/// Per Salmon Run rules: 1-5 character location identifier
/// </summary>
public class SalmonRunInfoSent : IInfoSent
{
    public string RawExchange { get; }

    /// <summary>
    /// Location identifier (county abbreviation or state/province code).
    /// Examples: "KING", "WHI", "OR", "BC"
    /// </summary>
    public string Location { get; }

    public SalmonRunInfoSent(string rawExchange, string location)
    {
        RawExchange = rawExchange ?? throw new ArgumentNullException(nameof(rawExchange));
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }
}

/// <summary>
/// Salmon Run received exchange information (Location/County).
/// Example: "KING" (King County, WA) or "OR" (Oregon)
/// </summary>
public class SalmonRunInfoReceived : IInfoReceived
{
    public string RawExchange { get; }

    /// <summary>
    /// Location identifier (county abbreviation or state/province code).
    /// Examples: "KING", "WHI", "OR", "BC"
    /// </summary>
    public string Location { get; }

    public SalmonRunInfoReceived(string rawExchange, string location)
    {
        RawExchange = rawExchange ?? throw new ArgumentNullException(nameof(rawExchange));
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }
}
