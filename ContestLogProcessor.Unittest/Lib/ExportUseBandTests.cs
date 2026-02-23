using System;
using System.IO;
using System.Linq;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class ExportUseBandTests
{
    [Fact]
    public void Export_WithUseBand_IncludesBandTokenInFirstSlot()
    {
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        // Entry has Band set explicitly
        LogEntry e = new LogEntry { CallSign = "K1", TheirCall = "N1", Band = "40m", Mode = "PH", QsoDateTime = DateTime.UtcNow };
        Assert.True(proc.CreateEntryResult(e).IsSuccess);

        string temp = Path.Combine(Path.GetTempPath(), "clp_export_band_test_" + Guid.NewGuid().ToString("N"));
        string expected = temp + ".log";
        try
        {
            var r = proc.ExportFileResult(temp, useCanonicalFormat: true, useBandToken: true);
            Assert.True(r.IsSuccess);
            Assert.True(File.Exists(expected));
            var lines = File.ReadAllLines(expected);
            // find the QSO line and verify the token after 'QSO:' starts with '40m'
            var qso = lines.FirstOrDefault(l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(qso);
            string after = qso!.Substring(4).TrimStart();
            string firstToken = after.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            Assert.Equal("40m", firstToken, ignoreCase: true);
        }
        finally
        {
            try { if (File.Exists(expected)) File.Delete(expected); } catch { }
        }
    }

    [Fact]
    public void Export_WithoutUseBand_UsesFrequencySlot()
    {
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        // Entry has Frequency set (numeric)
        LogEntry e = new LogEntry { CallSign = "K2", TheirCall = "N2", Frequency = "7000", Mode = "PH", QsoDateTime = DateTime.UtcNow };
        Assert.True(proc.CreateEntryResult(e).IsSuccess);

        string temp = Path.Combine(Path.GetTempPath(), "clp_export_freq_test_" + Guid.NewGuid().ToString("N"));
        string expected = temp + ".log";
        try
        {
            var r = proc.ExportFileResult(temp, useCanonicalFormat: true, useBandToken: false);
            Assert.True(r.IsSuccess);
            Assert.True(File.Exists(expected));
            var lines = File.ReadAllLines(expected);
            var qso = lines.FirstOrDefault(l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(qso);
            string after = qso!.Substring(4).TrimStart();
            string firstToken = after.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            Assert.Equal("7000", firstToken, ignoreCase: true);
        }
        finally
        {
            try { if (File.Exists(expected)) File.Delete(expected); } catch { }
        }
    }
}
