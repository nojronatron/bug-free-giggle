using System;
using Xunit;
using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;

namespace ContestLogProcessor.Unittest.Lib;

public class SalmonRunScoringServiceTests
{
    [Fact]
    public void CalculateScore_MissingCallsignHeader_Throws()
    {
        // Arrange
        var log = new CabrilloLogFile();
        var svc = new SalmonRunScoringService(new ContestLogProcessor.Lib.InMemoryLocationLookup());

    // Act & Assert: expect a BadFormat failure via the new OperationResult wrapper
    var failed = svc.CalculateScore(log);
    Assert.False(failed.IsSuccess);
    Assert.Equal(ResponseStatus.BadFormat, failed.Status);
    }

    [Fact]
    public void CalculateScore_BasicHappyPath_ReturnsResult()
    {
        // Arrange - small synthetic log
        var log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["END-OF-LOG"] = "";

        // Entry 1: 40m PH, TheirCall N7UK, ReceivedMsg ADA (WA)
        var e1 = new LogEntry
        {
            Frequency = "7265",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 0, 19, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "N7UK",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ADA" },
            SourceLineNumber = 1,
            RawLine = "QSO: 7265 PH 2025-09-20 0019 K7XXX 59 OKA N7UK 59 ADA"
        };

        // Entry 2: 40m CW, same TheirCall N7UK -> different Mode so counts separately
        var e2 = new LogEntry
        {
            Frequency = "7073",
            Mode = "CW",
            QsoDateTime = new DateTime(2025, 9, 20, 1, 23, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "N7UK",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ADA" },
            SourceLineNumber = 2,
            RawLine = "QSO: 7073 CW 2025-09-20 0123 K7XXX 59 OKA N7UK 59 ADA"
        };

        // Entry 3: 80m CW, TheirCall W7M, ReceivedMsg CA (US)
        var e3 = new LogEntry
        {
            Frequency = "3655",
            Mode = "CW",
            QsoDateTime = new DateTime(2025, 9, 20, 2, 30, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7M",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "CA" },
            SourceLineNumber = 3,
            RawLine = "QSO: 3655 CW 2025-09-20 0230 K7XXX 59 OKA W7M 59 CA"
        };

        // Entry 4: 20m PH, TheirCall W7DX (bonus), ReceivedMsg ON (Canada)
        var e4 = new LogEntry
        {
            Frequency = "14000",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 3, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ON" },
            SourceLineNumber = 4,
            RawLine = "QSO: 14000 PH 2025-09-20 0300 K7XXX 59 OKA W7DX 59 ON"
        };

        // Entry 5: 10m PH, TheirCall K1ABC, ReceivedMsg F (DXCC)
        var e5 = new LogEntry
        {
            Frequency = "28000",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 4, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "K1ABC",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "F" },
            SourceLineNumber = 5,
            RawLine = "QSO: 28000 PH 2025-09-20 0400 K7XXX 59 OKA K1ABC 59 F"
        };

        log.Entries.Add(e1);
        log.Entries.Add(e2);
        log.Entries.Add(e3);
        log.Entries.Add(e4);
        log.Entries.Add(e5);

        var svc = new SalmonRunScoringService(new ContestLogProcessor.Lib.InMemoryLocationLookup());

    // Act
    var resultOp = svc.CalculateScore(log);
    Assert.True(resultOp.IsSuccess);
    var result = resultOp.Value!;

        // Assert expected points:
        // QSO points: e1=2 (PH), e2=3 (CW), e3=3 (CW), e4=2 (PH), e5=2 (PH) => total 12
        Assert.Equal(12, result.QsoPoints);

        // Multiplier unique ReceivedMsg tokens: ADA (WA), CA (US), ON (CA), G (DXCC) => 4
        Assert.Equal(4, result.Multiplier);

        // W7DX bonus: one PH instance => 500
        Assert.Equal(500, result.W7DxBonusPoints);

        // Final score = (12 * 4) + 500 = 548
        Assert.Equal(548, result.FinalScore);

        // Check that lists contain expected canonical abbreviations
        Assert.Contains("ADA", result.UniqueWashingtonCounties);
        Assert.Contains("CA", result.UniqueUSStates);
        Assert.Contains("ON", result.UniqueCanadianProvinces);
        Assert.Contains("F", result.UniqueDxccEntities);

        // No skipped entries for this happy path
        Assert.Empty(result.SkippedEntries);
    }

    [Fact]
    public void CalculateScore_W7DX_BonusRules()
    {
        // Arrange - create several W7DX entries to exercise bonus rules
        var log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["END-OF-LOG"] = "";

        // First W7DX PH - eligible and should count for PH
        var w1 = new LogEntry
        {
            Frequency = "14000",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 1, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ON" },
            SourceLineNumber = 10,
        };

        // Second W7DX PH (same mode and same band) - should NOT count for bonus or QSO points
        var w2 = new LogEntry
        {
            Frequency = "14050",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 2, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ON" },
            SourceLineNumber = 11,
        };

        // Third W7DX CW - eligible and should count for CW (different mode)
        var w3 = new LogEntry
        {
            Frequency = "7073",
            Mode = "CW",
            QsoDateTime = new DateTime(2025, 9, 20, 3, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ADA" },
            SourceLineNumber = 12,
        };

        // Fourth W7DX CW but missing ReceivedMsg -> ineligible and should be skipped
        var w4 = new LogEntry
        {
            Frequency = "7075",
            Mode = "CW",
            QsoDateTime = new DateTime(2025, 9, 20, 4, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = null },
            SourceLineNumber = 13,
        };

        // Fifth W7DX PH but marked X-QSO -> ignored/skipped
        var w5 = new LogEntry
        {
            Frequency = "14020",
            Mode = "PH",
            QsoDateTime = new DateTime(2025, 9, 20, 5, 0, 0, DateTimeKind.Utc),
            CallSign = "K7XXX",
            TheirCall = "W7DX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "ON" },
            SourceLineNumber = 14,
            IsXQso = true,
        };

        log.Entries.Add(w1);
        log.Entries.Add(w2);
        log.Entries.Add(w3);
        log.Entries.Add(w4);
        log.Entries.Add(w5);

        var svc = new SalmonRunScoringService(new ContestLogProcessor.Lib.InMemoryLocationLookup());

        // Act
        var resultOp = svc.CalculateScore(log);
        Assert.True(resultOp.IsSuccess);
        var result = resultOp.Value!;

        // Assert: both modes (PH and CW) should have been counted once => 2 * 500 = 1000
        Assert.Equal(1000, result.W7DxBonusPoints);

        // QSO points: w1 PH => 2, w2 duplicate PH same band => 0 additional, w3 CW => 3 => total 5
        Assert.Equal(5, result.QsoPoints);

        // Multiplier: ON (Canada) and ADA (WA) => 2
        Assert.Equal(2, result.Multiplier);

        // Final score = (5 * 2) + 1000 = 1010
        Assert.Equal(1010, result.FinalScore);

        // Skipped entries should include the missing ReceivedMsg and the X-QSO
        Assert.Contains(result.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Missing required tokens"));
        Assert.Contains(result.SkippedEntries, s => s.Reason != null && s.Reason.Contains("X-QSO"));
    }

    [Fact]
    public void CalculateScore_DxccCapOfTen_IsEnforced()
    {
        // Arrange - create 12 unique DXCC ReceivedMsg entries; only first 10 should be counted
        var log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["END-OF-LOG"] = "";

        string[] dxccs = new[] { "1A","3A","3B6","3B8","3B9","3C","3C0","3D2","3DA","3V","3W","3X" };

        for (int i = 0; i < dxccs.Length; i++)
        {
            var e = new LogEntry
            {
                Frequency = "28000",
                Mode = "PH",
                QsoDateTime = new DateTime(2025, 9, 21, 0, i, 0, DateTimeKind.Utc),
                CallSign = "K7XXX",
                TheirCall = $"DX{i}",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = dxccs[i] },
                SourceLineNumber = i + 1,
            };

            log.Entries.Add(e);
        }

        var svc = new SalmonRunScoringService(new ContestLogProcessor.Lib.InMemoryLocationLookup());

        // Act
        var resultOp = svc.CalculateScore(log);
        Assert.True(resultOp.IsSuccess);
        var result = resultOp.Value!;

        // Assert: only first 10 DXCC entries were counted
        Assert.Equal(10, result.UniqueDxccEntities.Count);
        Assert.Equal(10, result.Multiplier);

        // First 10 should be present
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains(dxccs[i], result.UniqueDxccEntities);
        }

        // Last two should NOT be present
        Assert.DoesNotContain(dxccs[10], result.UniqueDxccEntities);
        Assert.DoesNotContain(dxccs[11], result.UniqueDxccEntities);
    }
}
