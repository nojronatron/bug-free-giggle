using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;
using Xunit;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

/// <summary>
/// Tests for contest header validation in WFD scoring service.
/// </summary>
public class WfdContestValidationTests
{
    private readonly WfdExchangeStrategy _strategy;

    public WfdContestValidationTests()
    {
        _strategy = new WfdExchangeStrategy();
    }

    [Fact]
    public void CalculateScore_WithMissingContestHeader_ReturnsFailure()
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        // Don't add CONTEST header

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Missing or empty CONTEST header", result.ErrorMessage);
    }

    [Fact]
    public void CalculateScore_WithEmptyContestHeader_ReturnsFailure()
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        log.Headers["CONTEST"] = "";

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Missing or empty CONTEST header", result.ErrorMessage);
    }

    [Fact]
    public void CalculateScore_WithWhitespaceContestHeader_ReturnsFailure()
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        log.Headers["CONTEST"] = "   ";

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Missing or empty CONTEST header", result.ErrorMessage);
    }

    [Fact]
    public void CalculateScore_WithPlaceholderContestName_ReturnsFailure()
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        log.Headers["CONTEST"] = "[Contest Name]";

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("placeholder '[Contest Name]'", result.ErrorMessage);
    }

    [Theory]
    [InlineData("ARRL Field Day")]
    [InlineData("CQ WPX")]
    [InlineData("IARU HF")]
    [InlineData("Sweepstakes")]
    public void CalculateScore_WithWrongContestName_ReturnsFailure(string contestName)
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        log.Headers["CONTEST"] = contestName;

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("does not match expected contest", result.ErrorMessage);
        Assert.Contains("WFD", result.ErrorMessage);
    }

    [Theory]
    [InlineData("WFD")]
    [InlineData("wfd")]
    [InlineData("Winter Field Day")]
    [InlineData("WINTER FIELD DAY")]
    [InlineData("winter field day")]
    public void CalculateScore_WithValidContestName_ProcessesLog(string contestName)
    {
        // Arrange
        CabrilloLogFile log = CreateValidLogWithoutContest();
        log.Headers["CONTEST"] = contestName;
        AddValidEntry(log);

        WinterFieldDayScoringService service = new WinterFieldDayScoringService(_strategy);

        // Act
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(log);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void CalculateScore_WithNullStrategy_ReturnsFailure()
    {
        // Arrange - Create service with null strategy (should not happen in production due to DI)
        Assert.Throws<ArgumentNullException>(() => new WinterFieldDayScoringService(null!));
    }

    private static CabrilloLogFile CreateValidLogWithoutContest()
    {
        CabrilloLogFile log = new CabrilloLogFile();

        // Set required headers
        log.Headers["START-OF-LOG"] = "3.0";
        log.Headers["END-OF-LOG"] = "";
        log.Headers["CALLSIGN"] = "K7RMZ";
        // CONTEST header intentionally not set

        return log;
    }

    private static void AddValidEntry(CabrilloLogFile log)
    {
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
    }
}
