using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;
using Xunit;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

/// <summary>
/// Tests for hierarchical error code generation in WFD scoring.
/// </summary>
public class WfdHierarchicalErrorCodeTests
{
    [Fact]
    public void ExcludedEntry_HasCorrectErrorCode()
    {
        // Arrange
        CabrilloLogFile log = CreateMinimalValidLog();
        log.Entries[0].IsXQso = true;
        
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.SkippedEntries);
        
        SkippedEntryInfo error = result.Value.SkippedEntries[0];
        Assert.Equal("WFD.EXCLUDED.X_QSO", error.ErrorCode);
        Assert.Equal(ErrorCategory.Excluded, error.Category);
        Assert.Equal(ErrorSeverity.Info, error.Severity);
    }

    [Fact]
    public void MissingMode_HasCorrectErrorCode()
    {
        // Arrange
        CabrilloLogFile log = CreateMinimalValidLog();
        log.Entries[0].Mode = null;
        
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.SkippedEntries);
        
        SkippedEntryInfo error = result.Value.SkippedEntries[0];
        Assert.Equal("WFD.MISSING.MODE", error.ErrorCode);
        Assert.Equal(ErrorCategory.MissingData, error.Category);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
        Assert.Equal("MODE", error.FieldName);
    }

    [Fact]
    public void InvalidMode_HasCorrectErrorCode()
    {
        // Arrange
        CabrilloLogFile log = CreateMinimalValidLog();
        log.Entries[0].Mode = "INVALID";
        
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.SkippedEntries);
        
        SkippedEntryInfo error = result.Value.SkippedEntries[0];
        Assert.Equal("WFD.RULES.INVALID_MODE", error.ErrorCode);
        Assert.Equal(ErrorCategory.Validation, error.Category);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
        Assert.Equal("Mode", error.FieldName);
        Assert.Equal("INVALID", error.InvalidValue);
    }

    [Fact]
    public void DuplicateEntry_HasCorrectErrorCode()
    {
        // Arrange
        CabrilloLogFile log = CreateMinimalValidLog();
        
        // Add duplicate entry (same call, same band, same mode)
        LogEntry duplicate = new LogEntry
        {
            SourceLineNumber = 100,
            RawLine = "QSO: 7000 PH 2026-01-25 2001 K7RMZ 59 3O OR W1AW 59 1O CT",
            Frequency = "7000",
            Mode = "PH",
            QsoDateTime = new DateTime(2026, 1, 25, 20, 1, 0),
            CallSign = "K7RMZ",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "3O OR" },
            TheirCall = "W1AW",
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "1O CT" }
        };
        log.Entries.Add(duplicate);
        
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.SkippedEntries);
        
        SkippedEntryInfo error = result.Value.SkippedEntries[0];
        Assert.Equal("WFD.DUPLICATE.BAND_MODE_STATION", error.ErrorCode);
        Assert.Equal(ErrorCategory.Duplicate, error.Category);
        Assert.Equal(ErrorSeverity.Warning, error.Severity);
        Assert.Equal("WFD-ONE-CONTACT-PER-STATION-BAND-MODE", error.RuleReference);
        Assert.Equal(3, error.Details.Count);
    }

    [Fact]
    public void InvalidExchange_HasCorrectErrorCode()
    {
        // Arrange
        CabrilloLogFile log = CreateMinimalValidLog();
        log.Entries[0].SentExchange = new Exchange { SentSig = "59", SentMsg = "INVALID" };
        
        WfdExchangeStrategy strategy = new WfdExchangeStrategy();
        WinterFieldDayScoringService service = new WinterFieldDayScoringService(strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.SkippedEntries);
        
        SkippedEntryInfo error = result.Value.SkippedEntries[0];
        Assert.StartsWith("WFD.EXCHANGE.", error.ErrorCode);
        Assert.Equal(ErrorCategory.Exchange, error.Category);
        Assert.Equal(ErrorSeverity.Error, error.Severity);
        Assert.Equal("SentExchange", error.FieldName);
    }

    private static CabrilloLogFile CreateMinimalValidLog()
    {
        CabrilloLogFile log = new CabrilloLogFile();
        
        // Set required headers
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["END-OF-LOG"] = "";
        log.Headers["CALLSIGN"] = "K7RMZ";
        
        LogEntry entry = new LogEntry
        {
            SourceLineNumber = 99,
            RawLine = "QSO: 7000 PH 2026-01-25 2000 K7RMZ 59 3O OR W1AW 59 1O CT",
            Frequency = "7000",
            Mode = "PH",
            QsoDateTime = new DateTime(2026, 1, 25, 20, 0, 0),
            CallSign = "K7RMZ",
            SentExchange = new Exchange { SentSig = "59", SentMsg = "3O OR" },
            TheirCall = "W1AW",
            ReceivedExchange = new Exchange { ReceivedSig = "59", ReceivedMsg = "1O CT" }
        };
        
        log.Entries.Add(entry);
        
        return log;
    }
}
