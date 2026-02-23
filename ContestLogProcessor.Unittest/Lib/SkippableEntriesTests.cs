using System;
using System.IO;
using System.Linq;

using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class SkippableEntriesTests
{
    private static string FindTestDataPath(string filename)
    {
        string dir = AppContext.BaseDirectory;
        DirectoryInfo? d = new DirectoryInfo(dir);
        while (d != null)
        {
            if (string.Equals(d.Name, "ContestLogProcessor.Unittest", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(d.FullName, "Lib", "TestData", filename);
            }
            d = d.Parent;
        }
        throw new InvalidOperationException("Could not locate test data directory");
    }

    [Fact]
    public void Import_SkippableEntries_File_ParsesExpectedNumberOfEntries()
    {
        string source = FindTestDataPath("K7XXX_Test_Skippable_Entries.log");
        Assert.True(File.Exists(source), "Test data file must exist");

        CabrilloLogProcessor p = new CabrilloLogProcessor();
        var imp = p.ImportFileResult(source);
        Assert.True(imp.IsSuccess);

        List<LogEntry> entries = p.ReadEntriesResult().Value!.ToList();
        // Expect 10 QSO/X-QSO entries recognized (one malformed X0QSO header line should not produce a QSO)
        Assert.Equal(10, entries.Count);
    }

    [Fact]
    public void Score_SkippableEntries_File_RecordsSkippedEntries()
    {
        string source = FindTestDataPath("K7XXX_Test_Skippable_Entries.log");
        Assert.True(File.Exists(source), "Test data file must exist");

        CabrilloLogProcessor p = new CabrilloLogProcessor();
        var imp = p.ImportFileResult(source);
        Assert.True(imp.IsSuccess);

        CabrilloLogFile log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["END-OF-LOG"] = "";
        if (p.TryGetHeader("CALLSIGN", out string? call) && !string.IsNullOrWhiteSpace(call))
        {
            log.Headers["CALLSIGN"] = call!;
        }
        else
        {
            string? inferred = p.ReadEntriesResult().Value!.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
            if (!string.IsNullOrWhiteSpace(inferred)) log.Headers["CALLSIGN"] = inferred!;
        }

        log.Entries = p.ReadEntriesResult().Value!.ToList();

        SalmonRunScoringService svc = new();
        var resOp = svc.CalculateScore(log);
        Assert.True(resOp.IsSuccess);
        SalmonRunScoreResult res = resOp.Value!;

        // We expect at least:
        // - X-QSO entries marked as skipped
        // - An unsupported mode (js8) to be skipped
        // - An unknown/invalid band/frequency (the 'm' token) to be skipped

        Assert.NotEmpty(res.SkippedEntries);
        Assert.Contains(res.SkippedEntries, s => s.Reason != null && s.Reason.Contains("X-QSO"));
        Assert.Contains(res.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Unsupported Mode"));
        Assert.Contains(res.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Unknown or invalid band/frequency"));
    }
}
