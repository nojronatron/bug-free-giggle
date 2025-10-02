using System;
using System.IO;
using System.Linq;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class DateTimeParsingTests
{
    [Fact]
    public void ImportFile_Parses_DateTime_To_Utc_MinutePrecision()
    {
        string tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCREATED-BY: Test\r\nQSO: 7265 PH 2025-09-20 1715 K7XXX 59 OKA N7UK 59 KITT\r\nEND-OF-LOG:\r\n");

        var proc = new CabrilloLogProcessor();
        proc.ImportFile(tmp);

        var entry = proc.ReadEntries().FirstOrDefault();
        Assert.NotNull(entry);
        Assert.Equal(DateTimeKind.Utc, entry.QsoDateTime.Kind);
        Assert.Equal(2025, entry.QsoDateTime.Year);
        Assert.Equal(9, entry.QsoDateTime.Month);
        Assert.Equal(20, entry.QsoDateTime.Day);
        Assert.Equal(17, entry.QsoDateTime.Hour);
        Assert.Equal(15, entry.QsoDateTime.Minute);
        Assert.Equal(0, entry.QsoDateTime.Second);

        File.Delete(tmp);
    }

    [Fact]
    public void ImportFile_Records_Unparseable_DateTime_In_SkippedEntries()
    {
        string tmp = Path.GetTempFileName();
        // malformed date/time
        File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCREATED-BY: Test\r\nQSO: 7265 PH BADDATE BADTIME K7XXX 59 OKA N7UK 59 KITT\r\nEND-OF-LOG:\r\n");

        var proc = new CabrilloLogProcessor();
        proc.ImportFile(tmp);

        // access internal log via TryGetHeader and entries; SkippedEntries are stored in the internal CabrilloLogFile which is not public.
        // However ImportFile stores the SkippedEntries in the internal _logFile and tests elsewhere rely on that via imports that check for missing headers.
        // We'll assert that a parsed entry exists but has DateTime.MinValue and that a skipped entry with reason exists in the exported file via ExportFile attempt.

        var entry = proc.ReadEntries().FirstOrDefault();
        Assert.NotNull(entry);
        Assert.Equal(DateTime.MinValue, entry.QsoDateTime);

    // Verify that the processor recorded a skipped entry for the unparsable date/time via the public snapshot accessor
    CabrilloLogFileSnapshot? snapshot = proc.GetReadOnlyLogFile();
    Assert.NotNull(snapshot);
    var skipped = snapshot!.SkippedEntries;
    Assert.Contains(skipped, s => s.Reason == "Unparseable date/time" && s.SourceLineNumber == 3);

        // Since SkippedEntries are available, we can also verify that malformed QSO was not fatal by ensuring entry exists
        File.Delete(tmp);
    }

    [Fact]
    public void ImportFile_Permissive_Parse_Accepts_Various_Formats()
    {
        string tmp = Path.GetTempFileName();
        // Use a format with colon in time which is listed in supported formats
        File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCREATED-BY: Test\r\nQSO: 7265 PH 2025-09-20 17:15 K7XXX 59 OKA N7UK 59 KITT\r\nEND-OF-LOG:\r\n");

        var proc = new CabrilloLogProcessor();
        proc.ImportFile(tmp);

        var entry = proc.ReadEntries().FirstOrDefault();
        Assert.NotNull(entry);
        Assert.Equal(DateTimeKind.Utc, entry.QsoDateTime.Kind);
        Assert.Equal(17, entry.QsoDateTime.Hour);
        Assert.Equal(15, entry.QsoDateTime.Minute);
        Assert.Equal(0, entry.QsoDateTime.Second);

        File.Delete(tmp);
    }
}
