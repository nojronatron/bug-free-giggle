using System.Collections.Generic;

namespace ContestLogProcessor.Lib;

public class SalmonRunScoreResult
{
    public int FinalScore { get; set; }
    public int Multiplier { get; set; }
    public int QsoPoints { get; set; }
    public int W7DxBonusPoints { get; set; }

    public List<string> UniqueWashingtonCounties { get; } = new();
    public List<string> UniqueUSStates { get; } = new();
    public List<string> UniqueCanadianProvinces { get; } = new();
    public List<string> UniqueDxccEntities { get; } = new();

    public List<SkippedEntryInfo> SkippedEntries { get; } = new();
}

public class SkippedEntryInfo
{
    public int? SourceLineNumber { get; set; }
    public string? Reason { get; set; }
    public string? RawLine { get; set; }
}
