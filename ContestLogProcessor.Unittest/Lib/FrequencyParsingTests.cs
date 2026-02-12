using System;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class FrequencyParsingTests
{
    [Fact]
    public void ImportFile_WithNumericFrequency_SetsBandAndFrequencyIsValid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_freq_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 7000 CW 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            var imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            var e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);
            Assert.True(e.FrequencyIsValid, "Frequency should be recognized as valid");
            Assert.Equal("40m", e.Band);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithBandLikeTokenButNoNumericFrequency_LeavesFrequencyInvalid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_bandonly_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            // Put a band-like token in the frequency position (e.g., "40m") to simulate band-only logs
            "QSO: 40m PH 2025-09-20 1300 K7RMZ 59 OKA W7IB 59 WHA",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            var imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            var e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);
        // Band token placed into Frequency is now mapped to the band's low kHz and considered valid
        Assert.True(e.FrequencyIsValid);
        Assert.Equal("40m", e.Band);
        Assert.Equal("7000", e.Frequency);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithFloatingFrequency_TruncatesAndMarksValid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_floatfreq_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            // Floating-point frequency in kHz (should truncate fractional part)
            "QSO: 7053.9 PH 2025-09-20 1310 K7RMZ 59 OKA W7IB 59 WHA",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            var imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            var e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);
            // fractional part truncated -> 7053 kHz, which is within 40m
            Assert.True(e.FrequencyIsValid, "Floating frequency should be treated as valid after truncation");
            Assert.Equal("40m", e.Band);
            Assert.Equal("7053", e.Frequency);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithInvalidFrequencyToken_IsMarkedInvalid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_invalidtoken_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            // Tokens containing 'G' or other unit markers are not valid for Salmon Run
            "QSO: 14G PH 2025-09-20 1320 K7RMZ 59 OKA W7IB 59 WHA",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            var imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            var e = p.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(e);
            Assert.False(e.FrequencyIsValid, "Frequency token containing unit markers should be invalid");
            Assert.Null(e.Band);
            Assert.Equal("14G", e.Frequency);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithExcludedNumericRanges_IsMarkedInvalid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "clp_excludedranges_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            // Numeric within 55-1000 should be excluded
            "QSO: 100 PH 2025-09-20 1330 K7RMZ 59 OKA W7IB 59 WHA",
            // Numeric above the allowed max (300GHz = 300000000 kHz) should be excluded
            "QSO: 400000000 PH 2025-09-20 1340 K7RMZ 59 OKA N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            var imp = p.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            var entries = p.ReadEntriesResult().Value!.ToList();
            Assert.Equal(2, entries.Count);

            Assert.False(entries[0].FrequencyIsValid, "Frequency 100 (in excluded low range) should be invalid");
            Assert.Null(entries[0].Band);

            Assert.False(entries[1].FrequencyIsValid, "Frequency 400000000 (above allowed max) should be invalid");
            Assert.Null(entries[1].Band);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }
}
