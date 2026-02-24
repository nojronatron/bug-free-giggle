using System;

using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Winter Field Day sent exchange information (Category + Class + Location).
/// Example: "3O AL" means 3 stations, Outdoor, Alabama
/// </summary>
public class WfdInfoSent : IInfoSent
{
    public string RawExchange { get; }
    public int Category { get; }
    public char Class { get; }
    public string Location { get; }

    public WfdInfoSent(string rawExchange, int category, char classId, string location)
    {
        RawExchange = rawExchange ?? throw new ArgumentNullException(nameof(rawExchange));
        Category = category;
        Class = classId;
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }
}

/// <summary>
/// Winter Field Day received exchange information (Category + Class + Location).
/// </summary>
public class WfdInfoReceived : IInfoReceived
{
    public string RawExchange { get; }
    public int Category { get; }
    public char Class { get; }
    public string Location { get; }

    public WfdInfoReceived(string rawExchange, int category, char classId, string location)
    {
        RawExchange = rawExchange ?? throw new ArgumentNullException(nameof(rawExchange));
        Category = category;
        Class = classId;
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }
}
