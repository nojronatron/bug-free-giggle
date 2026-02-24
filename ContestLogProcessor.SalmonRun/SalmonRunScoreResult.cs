using System.Collections.Generic;

using ContestLogProcessor.Lib;

namespace ContestLogProcessor.SalmonRun;

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
