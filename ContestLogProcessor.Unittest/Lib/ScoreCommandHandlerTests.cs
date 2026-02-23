using System;
using Xunit;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Unittest.Lib;
using System.Threading.Tasks;

namespace ContestLogProcessor.Unittest.Lib;

public class ScoreCommandHandlerTests
{
    [Fact]
    public async Task ScoreHandler_HappyPath_PrintsReport()
    {
        // Arrange
        var proc = new ContestLogProcessor.Lib.CabrilloLogProcessor();
        // Create a temp file with minimal Cabrillo content and import it
        string tmp = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllLines(tmp, new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7XXX",
            "CONTEST: WA-SALMON-RUN",
            "QSO: 7265 PH 2025-09-20 0019 K7XXX 59 OKA N7UK 59 ADA",
            "QSO: 14000 PH 2025-09-20 0300 K7XXX 59 OKA W7DX 59 ON",
            "END-OF-LOG:"
        });
        var importRes = proc.ImportFileResult(tmp);
        Assert.True(importRes.IsSuccess);

        var testConsole = new TestConsole(new string[0]);
        var ctx = new CommandContext(proc, testConsole, debug: false);
        var handler = new ScoreCommandHandler();

        // Act
        await handler.HandleAsync(new string[] { "score" }, ctx);

        // Assert - output contains known fields
        string all = string.Join("\n", testConsole.Outputs);
        Assert.Contains("Salmon Run Score Report", all);
        Assert.Contains("Final score", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreHandler_W7DxBonus_CountsPerMode()
    {
        var proc = new ContestLogProcessor.Lib.CabrilloLogProcessor();
        string tmp2 = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllLines(tmp2, new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7XXX",
            "CONTEST: WA-SALMON-RUN",
            // PH W7DX
            "QSO: 14000 PH 2025-09-20 0100 K7XXX 59 OKA W7DX 59 ON",
            // PH same band duplicate - should not count
            "QSO: 14050 PH 2025-09-20 0200 K7XXX 59 OKA W7DX 59 ON",
            // CW different mode - should count
            "QSO: 7073 CW 2025-09-20 0300 K7XXX 59 OKA W7DX 59 ADA",
            "END-OF-LOG:"
        });
        var importRes2 = proc.ImportFileResult(tmp2);
        Assert.True(importRes2.IsSuccess);

        var testConsole = new TestConsole(new string[0]);
        var ctx = new CommandContext(proc, testConsole, debug: false);
        var handler = new ScoreCommandHandler();

        await handler.HandleAsync(new string[] { "score" }, ctx);

        string output = string.Join("\n", testConsole.Outputs);
        // Expect W7DX bonus line to show 1000 (two modes counted)
        Assert.Contains("W7DX bonus", output);
        Assert.Matches(@"W7DX bonus\s*:\s*1000", output);
    }

    [Fact]
    public void ScoreHandler_MultiplierAndQsoCounts_AreCalculated()
    {
        var proc = new ContestLogProcessor.Lib.CabrilloLogProcessor();
        string tmp3 = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllLines(tmp3, new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7XXX",
            // WA county token ADA
            "QSO: 7265 PH 2025-09-20 0019 K7XXX 59 OKA N7UK 59 ADA",
            // US state CA
            "QSO: 3655 CW 2025-09-20 0230 K7XXX 59 OKA W7M 59 CA",
            // DXCC entity F
            "QSO: 28000 PH 2025-09-20 0400 K7XXX 59 OKA K1ABC 59 F",
            "END-OF-LOG:"
        });
        var importRes3 = proc.ImportFileResult(tmp3);
        Assert.True(importRes3.IsSuccess);

        var svc = new ContestLogProcessor.SalmonRun.SalmonRunScoringService(new ContestLogProcessor.Lib.InMemoryLocationLookup());
        var log = new ContestLogProcessor.Lib.CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["END-OF-LOG"] = "";
        log.Entries = proc.ReadEntriesResult().Value!.ToList();

        var resOp = svc.CalculateScore(log);
        Assert.True(resOp.IsSuccess);
        var res = resOp.Value!;

        // QSO points expected: PH=2, CW=3, PH=2 => total 7
        Assert.Equal(7, res.QsoPoints);
        // Multiplier expected: ADA (WA county), CA (US state), F (DXCC) => multiplier 3
        Assert.Equal(3, res.Multiplier);
    }
}
