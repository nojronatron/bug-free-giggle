using System;
using System.IO;
using System.Linq;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

/// <summary>
/// Tests for Cabrillo v3 required marker validation (START-OF-LOG and END-OF-LOG).
/// </summary>
public class CabrilloMarkerValidationTests
{
    [Fact]
    public void Import_WithMissingStartOfLog_RecordsSkippedEntry()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            string[] lines = new[]
            {
                "CALLSIGN: K7XXX",
                "CONTEST: SALMON-RUN",
                "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
                "END-OF-LOG:"
            };

            File.WriteAllLines(tmp, lines);

            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? snapshot = processor.GetReadOnlyLogFile();
            Assert.NotNull(snapshot);
            Assert.False(snapshot.HasStartOfLog);
            Assert.True(snapshot.HasEndOfLog);

            bool hasSkippedMarker = snapshot.SkippedEntries.Any(s => 
                s.Reason != null && s.Reason.Contains("START-OF-LOG", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasSkippedMarker, "Expected skipped entry for missing START-OF-LOG marker");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public void Import_WithMissingEndOfLog_RecordsSkippedEntry()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            string[] lines = new[]
            {
                "START-OF-LOG: 3.0",
                "CALLSIGN: K7XXX",
                "CONTEST: SALMON-RUN",
                "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL"
            };

            File.WriteAllLines(tmp, lines);

            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? snapshot = processor.GetReadOnlyLogFile();
            Assert.NotNull(snapshot);
            Assert.True(snapshot.HasStartOfLog);
            Assert.False(snapshot.HasEndOfLog);

            bool hasSkippedMarker = snapshot.SkippedEntries.Any(s => 
                s.Reason != null && s.Reason.Contains("END-OF-LOG", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasSkippedMarker, "Expected skipped entry for missing END-OF-LOG marker");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public void Import_WithBothMarkers_NoSkippedEntries()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            string[] lines = new[]
            {
                "START-OF-LOG: 3.0",
                "CALLSIGN: K7XXX",
                "CONTEST: SALMON-RUN",
                "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
                "END-OF-LOG:"
            };

            File.WriteAllLines(tmp, lines);

            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? snapshot = processor.GetReadOnlyLogFile();
            Assert.NotNull(snapshot);
            Assert.True(snapshot.HasStartOfLog);
            Assert.True(snapshot.HasEndOfLog);

            bool hasSkippedMarker = snapshot.SkippedEntries.Any(s => 
                s.Reason != null && (s.Reason.Contains("START-OF-LOG", StringComparison.OrdinalIgnoreCase) ||
                                     s.Reason.Contains("END-OF-LOG", StringComparison.OrdinalIgnoreCase)));
            Assert.False(hasSkippedMarker, "Expected no skipped entries for missing markers when both are present");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public void Import_WithMissingBothMarkers_RecordsTwoSkippedEntries()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            string[] lines = new[]
            {
                "CALLSIGN: K7XXX",
                "CONTEST: SALMON-RUN",
                "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL"
            };

            File.WriteAllLines(tmp, lines);

            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? snapshot = processor.GetReadOnlyLogFile();
            Assert.NotNull(snapshot);
            Assert.False(snapshot.HasStartOfLog);
            Assert.False(snapshot.HasEndOfLog);

            int markerSkippedCount = snapshot.SkippedEntries.Count(s => 
                s.Reason != null && (s.Reason.Contains("START-OF-LOG", StringComparison.OrdinalIgnoreCase) ||
                                     s.Reason.Contains("END-OF-LOG", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(2, markerSkippedCount);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
