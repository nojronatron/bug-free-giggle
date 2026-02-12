using System.Collections.Generic;
using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;
using Xunit;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

/// <summary>
/// Tests for Winter Field Day scoring service validation of required Cabrillo v3 markers.
/// </summary>
public class WinterFieldDayMarkerValidationTests
{
    [Fact]
    public void CalculateScore_WithMissingStartOfLog_ReturnsFailure()
    {
        CabrilloLogFile logFile = new CabrilloLogFile
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "CALLSIGN", "K7XXX" },
                { "CONTEST", "WFD" },
                { "END-OF-LOG", "" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "1O WA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "2O OR" },
                    Mode = "PH",
                    Band = "40M"
                }
            }
        };

        WinterFieldDayScoringService service = new WinterFieldDayScoringService();
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(logFile);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("START-OF-LOG", result.ErrorMessage);
    }

    [Fact]
    public void CalculateScore_WithMissingEndOfLog_ReturnsFailure()
    {
        CabrilloLogFile logFile = new CabrilloLogFile
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "START-OF-LOG", "3.0" },
                { "CALLSIGN", "K7XXX" },
                { "CONTEST", "WFD" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "1O WA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "2O OR" },
                    Mode = "PH",
                    Band = "40M"
                }
            }
        };

        WinterFieldDayScoringService service = new WinterFieldDayScoringService();
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(logFile);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("END-OF-LOG", result.ErrorMessage);
    }

    [Fact]
    public void CalculateScore_WithBothMarkers_Succeeds()
    {
        CabrilloLogFile logFile = new CabrilloLogFile
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "START-OF-LOG", "3.0" },
                { "CALLSIGN", "K7XXX" },
                { "CONTEST", "WFD" },
                { "END-OF-LOG", "" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "1O WA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "2O OR" },
                    Mode = "PH",
                    Band = "40M",
                    QsoDateTime = System.DateTime.UtcNow
                }
            }
        };

        WinterFieldDayScoringService service = new WinterFieldDayScoringService();
        OperationResult<WinterFieldDayScoreResult> result = service.CalculateScore(logFile);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
