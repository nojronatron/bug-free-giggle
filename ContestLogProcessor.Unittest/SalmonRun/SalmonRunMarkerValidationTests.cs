using System.Collections.Generic;
using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;
using Xunit;

namespace ContestLogProcessor.Unittest.SalmonRun;

/// <summary>
/// Tests for Salmon Run scoring service validation of required Cabrillo v3 markers.
/// </summary>
public class SalmonRunMarkerValidationTests
{
    [Fact]
    public void CalculateScore_WithMissingStartOfLog_ReturnsFailure()
    {
        CabrilloLogFile logFile = new CabrilloLogFile
        {
            Headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "CALLSIGN", "K7XXX" },
                { "CONTEST", "SALMON-RUN" },
                { "END-OF-LOG", "" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "OKA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "SAN" },
                    Mode = "PH",
                    Band = "40M"
                }
            }
        };

        SalmonRunScoringService service = new SalmonRunScoringService();
        OperationResult<SalmonRunScoreResult> result = service.CalculateScore(logFile);

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
                { "CONTEST", "SALMON-RUN" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "OKA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "SAN" },
                    Mode = "PH",
                    Band = "40M"
                }
            }
        };

        SalmonRunScoringService service = new SalmonRunScoringService();
        OperationResult<SalmonRunScoreResult> result = service.CalculateScore(logFile);

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
                { "CONTEST", "SALMON-RUN" },
                { "END-OF-LOG", "" }
            },
            Entries = new List<LogEntry>
            {
                new LogEntry 
                { 
                    CallSign = "K7XXX",
                    TheirCall = "W7TMT",
                    SentExchange = new Exchange { SentMsg = "OKA" },
                    ReceivedExchange = new Exchange { ReceivedMsg = "SAN" },
                    Mode = "PH",
                    Band = "40M",
                    QsoDateTime = System.DateTime.UtcNow
                }
            }
        };

        SalmonRunScoringService service = new SalmonRunScoringService();
        OperationResult<SalmonRunScoreResult> result = service.CalculateScore(logFile);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
