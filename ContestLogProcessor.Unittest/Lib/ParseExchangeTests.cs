using System.Reflection;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class ParseExchangeTests
{
    [Fact]
    public void ParseExchange_SentMsgAndTheirCall_AreParsedCorrectly()
    {
        // Arrange - create a minimal Cabrillo log containing the QSO line of interest
        string tmpDir = Path.GetTempPath();
        string filePath = Path.Combine(tmpDir, "clp_parse_exchange_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 3930 PH 2025-09-20 1605 K7RMZ 59 OKA AC7DC 59 WHI",
            "END-OF-LOG:" };

        try
        {
            File.WriteAllLines(filePath, lines);

            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> imp = processor.ImportFileResult(filePath);
            Assert.True(imp.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);

            // Verify the sent exchange SentMsg parsed as "OKA" and TheirCall parsed as "AC7DC"
            Assert.NotNull(entry.SentExchange);
            Assert.Equal("OKA", entry.SentExchange.SentMsg);
            Assert.Equal("AC7DC", entry.TheirCall);
        }
        finally
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
        }
    }

    [Fact]
    public void ParseExchange_HappyPath_AllTokensParsed()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_parse_exchange_happy_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 7265 PH 2025-09-20 1405 K7RMZ 59 OKA N7UK 59 KITT",
            "END-OF-LOG:" };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor p = new CabrilloLogProcessor();
            OperationResult<Unit> imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            LogEntry? e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);
            Assert.NotNull(e.SentExchange);
            Assert.Equal("59", e.SentExchange.SentSig);
            Assert.Equal("OKA", e.SentExchange.SentMsg);
            Assert.Equal("N7UK", e.TheirCall);
            Assert.NotNull(e.ReceivedExchange);
            Assert.Equal("59", e.ReceivedExchange.ReceivedSig);
            Assert.Equal("KITT", e.ReceivedExchange.ReceivedMsg);
        }
        finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
    }

    [Fact]
    public void ParseExchange_InvalidTokens_AreRecordedAsSkipped()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_parse_exchange_invalid_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            // invalid SentSig (abc), invalid SentMsg (too-long), invalid ReceivedSig (0), invalid ReceivedMsg (contains '-')
            "QSO: 7265 PH 2025-09-20 1415 K7RMZ abc toolongtoken N7UK 0 BAD-V",
            "END-OF-LOG:" };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor p = new CabrilloLogProcessor();
            OperationResult<Unit> imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            LogEntry? e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);

            // The processor should have recorded skipped entries for invalid tokens
            CabrilloLogFile? logFile = GetPrivateCabrilloLogFile(p);
            Assert.NotNull(logFile);
            Assert.NotEmpty(logFile.SkippedEntries);
            Assert.Contains(logFile.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Invalid SentSig"));
            Assert.Contains(logFile.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Invalid SentMsg"));
            Assert.Contains(logFile.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Invalid ReceivedSig"));
            Assert.Contains(logFile.SkippedEntries, s => s.Reason != null && s.Reason.Contains("Invalid ReceivedMsg"));
        }
        finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
    }

    // Helper to obtain the internal CabrilloLogFile via reflection (tests may access for diagnostics)
    private static CabrilloLogFile? GetPrivateCabrilloLogFile(CabrilloLogProcessor p)
    {
        FieldInfo? fi = typeof(CabrilloLogProcessor).GetField("_logFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fi?.GetValue(p) as CabrilloLogFile;
    }
}
