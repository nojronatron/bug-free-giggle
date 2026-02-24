using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class ReadEntriesByBandTests
{
    [Fact]
    public void ReadEntriesByBand_MatchesBandToken()
    {
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        // Create entries with explicit Band set
        LogEntry e1 = new LogEntry { CallSign = "A", TheirCall = "B", Band = "40m", QsoDateTime = DateTime.UtcNow };
        LogEntry e2 = new LogEntry { CallSign = "C", TheirCall = "D", Band = "20m", QsoDateTime = DateTime.UtcNow };
        Assert.True(proc.CreateEntryResult(e1).IsSuccess);
        Assert.True(proc.CreateEntryResult(e2).IsSuccess);

        OperationResult<IEnumerable<LogEntry>> res40 = proc.ReadEntriesByBandResult("40m");
        Assert.True(res40.IsSuccess);
        Assert.Single(res40.Value!);
        Assert.Equal("A", res40.Value!.First().CallSign);
    }

    [Fact]
    public void ReadEntriesByBand_MapsFrequencyToBand()
    {
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        // Frequency numeric should map to 40m band (7000..7300)
        LogEntry e1 = new LogEntry { CallSign = "X", TheirCall = "Y", Frequency = "7073", QsoDateTime = DateTime.UtcNow };
        LogEntry e2 = new LogEntry { CallSign = "Z", TheirCall = "W", Frequency = "14000", QsoDateTime = DateTime.UtcNow };
        Assert.True(proc.CreateEntryResult(e1).IsSuccess);
        Assert.True(proc.CreateEntryResult(e2).IsSuccess);

        OperationResult<IEnumerable<LogEntry>> r40 = proc.ReadEntriesByBandResult("40m");
        Assert.True(r40.IsSuccess);
        Assert.Single(r40.Value!);
        Assert.Equal("X", r40.Value!.First().CallSign);
    }
}
