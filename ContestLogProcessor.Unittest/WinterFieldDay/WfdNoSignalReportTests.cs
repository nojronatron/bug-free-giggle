using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;

using Xunit;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

/// <summary>
/// Tests for WFD logs that omit signal reports (common in many logging programs).
/// </summary>
public class WfdNoSignalReportTests
{
    [Fact]
    public void ValidateSentExchange_WithoutSignalReport_Succeeds()
    {
        // Arrange
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();

        // Act - No signal report provided (null)
        OperationResult<bool> result = strategy.ValidateSentExchange(null, "1M WWA");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void ValidateReceivedExchange_WithoutSignalReport_Succeeds()
    {
        // Arrange
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();

        // Act - No signal report provided (empty string), with valid WFD exchange
        OperationResult<bool> result = strategy.ValidateReceivedExchange("", "1O SV");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void CalculateScore_WithLogMissingSignalReports_ProcessesSuccessfully()
    {
        // Arrange - Simulate a log entry like: QSO: 14287 PH 2026-01-24 2028 K7XXX 1M WWA N3BEN 1O SV
        CabrilloLogFile log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["END-OF-LOG"] = "";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["CONTEST"] = "WFD";

        LogEntry entry = new LogEntry
        {
            SourceLineNumber = 1,
            RawLine = "QSO:   14287 PH 2026-01-24 2028 K7XXX           1M     WWA      N3BEN           1O       SV",
            Frequency = "14287",
            Mode = "PH",
            QsoDateTime = new System.DateTime(2026, 1, 24, 20, 28, 0),
            CallSign = "K7XXX",
            SentExchange = new Exchange { SentSig = null, SentMsg = "1M WWA" },
            TheirCall = "N3BEN",
            ReceivedExchange = new Exchange { ReceivedSig = null, ReceivedMsg = "1O SV" }
        };
        log.Entries.Add(entry);

        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalContacts);
        Assert.Empty(result.Value.SkippedEntries);
    }

    [Fact]
    public void CalculateScore_WithMixedSignalReportPresence_ProcessesAllSuccessfully()
    {
        // Arrange - Some entries have signal reports, some don't
        CabrilloLogFile log = new CabrilloLogFile();
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["END-OF-LOG"] = "";
        log.Headers["CALLSIGN"] = "K7XXX";
        log.Headers["CONTEST"] = "WFD";

        // Entry 1: No signal reports
        LogEntry entry1 = new LogEntry
        {
            SourceLineNumber = 1,
            RawLine = "QSO:   14287 PH 2026-01-24 2028 K7XXX           1M     WWA      N3BEN           1O       SV",
            Frequency = "14287",
            Mode = "PH",
            QsoDateTime = new System.DateTime(2026, 1, 24, 20, 28, 0),
            CallSign = "K7XXX",
            SentExchange = new Exchange { SentSig = null, SentMsg = "1M WWA" },
            TheirCall = "N3BEN",
            ReceivedExchange = new Exchange { ReceivedSig = null, ReceivedMsg = "1O SV" }
        };
        log.Entries.Add(entry1);

        // Entry 2: With signal reports
        LogEntry entry2 = new LogEntry
        {
            SourceLineNumber = 2,
            RawLine = "QSO:   7000 PH 2026-01-24 2030 K7XXX         59    2O WA      W1AW         599   3I CT",
            Frequency = "7000",
            Mode = "PH",
            QsoDateTime = new System.DateTime(2026, 1, 24, 20, 30, 0),
            CallSign = "K7XXX",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "2O WA" },
            TheirCall = "W1AW",
            ReceivedExchange = new Exchange { ReceivedSig = "599", ReceivedMsg = "3I CT" }
        };
        log.Entries.Add(entry2);

        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalContacts);
        Assert.Empty(result.Value.SkippedEntries);
    }
}
