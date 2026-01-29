using System.Collections.Generic;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Winter Field Day score result containing points, multiplier, and summary information.
/// </summary>
public class WinterFieldDayScoreResult
{
    public int FinalScore { get; set; }
    public int QsoPoints { get; set; }
    public int PhoneQsos { get; set; }
    public int CwDigitalQsos { get; set; }
    public int TotalContacts { get; set; }
    public int DuplicateContacts { get; set; }
    
    // Collections for reporting
    public List<string> UniqueStationCategories { get; } = new();
    public List<string> UniqueLocations { get; } = new();
    
    // Band/Mode statistics for analysis
    public Dictionary<string, int> ContactsByBand { get; } = new();
    public Dictionary<string, int> ContactsByMode { get; } = new();
    public List<SkippedEntryInfo> SkippedEntries { get; } = new();
}