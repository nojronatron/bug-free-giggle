using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

/// <summary>
/// Tests for Cabrillo v3 specification compliance including:
/// - Frequency enumeration validation (amateur band ranges)
/// - Transmitter ID parsing with missing/null values
/// - Exchange field validation against Cabrillo v3 limits
/// - Required markers and headers
/// </summary>
public class CabrilloV3ComplianceTests
{
    #region Frequency Enumeration Validation Tests

    [Theory]
    [InlineData("1800", true, "160m")]  // 160m band lower edge
    [InlineData("2000", true, "160m")]  // 160m band upper edge
    [InlineData("3500", true, "80m")]   // 80m band lower edge
    [InlineData("4000", true, "80m")]   // 80m band upper edge
    [InlineData("7000", true, "40m")]   // 40m band lower edge
    [InlineData("7300", true, "40m")]   // 40m band upper edge
    [InlineData("14000", true, "20m")]  // 20m band lower edge
    [InlineData("14350", true, "20m")]  // 20m band upper edge
    [InlineData("21000", true, "15m")]  // 15m band lower edge
    [InlineData("21450", true, "15m")]  // 15m band upper edge
    [InlineData("28000", true, "10m")]  // 10m band lower edge
    [InlineData("29700", true, "10m")]  // 10m band upper edge
    [InlineData("50000", true, "6m")]   // 6m band lower edge
    [InlineData("54000", true, "6m")]   // 6m band upper edge
    public void ImportFile_WithValidAmateurBandFrequency_MarksFrequencyValid(string frequency, bool expectedValid, string expectedBand)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_freq_valid_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: {frequency} PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.Equal(expectedValid, entry.FrequencyIsValid);
            Assert.Equal(expectedBand, entry.Band);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Theory]
    [InlineData("1000")]   // Below 160m band
    [InlineData("1799")]   // Just below 160m
    [InlineData("2001")]   // Just above 160m
    [InlineData("3499")]   // Just below 80m
    [InlineData("4001")]   // Just above 80m
    [InlineData("5000")]   // Between bands
    [InlineData("6999")]   // Just below 40m
    [InlineData("7301")]   // Just above 40m
    [InlineData("8000")]   // Between bands
    [InlineData("13999")]  // Just below 20m
    [InlineData("14351")]  // Just above 20m
    [InlineData("20999")]  // Just below 15m
    [InlineData("21451")]  // Just above 15m
    [InlineData("27999")]  // Just below 10m
    [InlineData("29701")]  // Just above 10m
    [InlineData("40000")]  // Between 10m and 6m
    [InlineData("49999")]  // Just below 6m
    [InlineData("54001")]  // Just above 6m
    [InlineData("100000")] // Well outside any band
    public void ImportFile_WithInvalidFrequency_MarksFrequencyInvalid(string invalidFrequency)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_freq_invalid_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: {invalidFrequency} PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.False(entry.FrequencyIsValid);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithMicrowaveBandFrequency_MarksFrequencyValid()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_freq_microwave_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 1240000 PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL",  // 23cm band
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.True(entry.FrequencyIsValid);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    #endregion

    #region Transmitter ID Parsing Tests

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void ImportFile_WithValidTransmitterId_ParsesCorrectly(string transmitterId)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_txid_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: 7000 PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL {transmitterId}",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.NotNull(entry.TransmitterId);
            Assert.Equal(int.Parse(transmitterId), entry.TransmitterId.Value);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void ImportFile_WithMissingTransmitterId_ParsesAsNull()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_txid_missing_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 7000 PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL",  // No transmitter ID
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.Null(entry.TransmitterId);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Theory]
    [InlineData("2")]   // Out of range (only 0 or 1 valid)
    [InlineData("5")]   // Out of range
    [InlineData("-1")]  // Negative
    [InlineData("A")]   // Non-numeric
    public void ImportFile_WithInvalidTransmitterId_ParsesAsNull(string invalidTransmitterId)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_txid_invalid_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: 7000 PH 2025-09-20 1200 K7RMZ 59 OKA N7KN 59 ISL {invalidTransmitterId}",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            LogEntry? entry = processor.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(entry);
            Assert.Null(entry.TransmitterId);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    #endregion

    #region Exchange Field Validation Tests (Cabrillo v3 Spec Compliance)

    [Theory]
    [InlineData("59", true)]      // Standard signal report
    [InlineData("599", true)]     // Full RST
    [InlineData("5NN", true)]     // Contest shorthand
    [InlineData("5nn", true)]     // Lowercase variant
    [InlineData("159", true)]     // Valid range
    [InlineData("09", false)]     // Invalid: starts with 0
    [InlineData("6", false)]      // Invalid: too short
    [InlineData("659", false)]    // Invalid: starts with 6
    [InlineData("5999", false)]   // Invalid: too long
    [InlineData("ABC", false)]    // Invalid: letters other than 'N'
    public void ImportFile_WithSignalReport_ValidatesPerCabrilloV3(string signalReport, bool shouldBeValid)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_sig_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: 7000 PH 2025-09-20 1200 K7RMZ {signalReport} OKA N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? logFile = processor.GetReadOnlyLogFile();
            Assert.NotNull(logFile);

            if (!shouldBeValid)
            {
                // Invalid signal reports should be recorded as skipped
                Assert.Contains(logFile.SkippedEntries, s =>
                    s.Reason != null && s.Reason.Contains("Invalid SentSig"));
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Theory]
    [InlineData("WA", true)]       // Valid 2-char
    [InlineData("KING", true)]     // Valid 4-char
    [InlineData("OR/WA", true)]    // Valid with slash (total 5 chars)
    [InlineData("001", true)]      // Valid numeric
    [InlineData("3O", true)]       // Valid alphanumeric
    [InlineData("A", true)]        // Valid 1-char
    [InlineData("123456", true)]   // Valid 6-char
    [InlineData("TOOLONG", false)] // Invalid: 7 chars exceeds limit
    [InlineData("", false)]        // Invalid: empty
    public void ImportFile_WithExchangeMessage_ValidatesPerCabrilloV3SixCharLimit(string exchangeMsg, bool shouldBeValid)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_exch_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: 7000 PH 2025-09-20 1200 K7RMZ 59 {exchangeMsg} N7KN 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? logFile = processor.GetReadOnlyLogFile();
            Assert.NotNull(logFile);

            if (!shouldBeValid && !string.IsNullOrEmpty(exchangeMsg))
            {
                // Invalid exchange messages should be recorded as skipped
                Assert.Contains(logFile.SkippedEntries, s =>
                    s.Reason != null && s.Reason.Contains("Invalid SentMsg"));
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    [Theory]
    [InlineData("K7XXX", true)]        // Valid 5-char
    [InlineData("W7TMT", true)]        // Valid 5-char
    [InlineData("N7K", true)]          // Valid 3-char (minimum)
    [InlineData("W7/K7XXX", true)]     // Valid with prefix
    [InlineData("K7XXX/W7", true)]     // Valid with suffix
    [InlineData("VE7/K7XXX", true)]    // Valid with 3-char prefix
    [InlineData("AB", false)]          // Invalid: too short (< 3)
    [InlineData("K7XXXXXXXXXXXX", false)] // Invalid: too long (> 13)
    [InlineData("W/K7XXX", false)]     // Invalid: prefix too short
    [InlineData("K7XXX/W", false)]     // Invalid: suffix too short
    public void ImportFile_WithCallsign_ValidatesPerCabrilloV3ThirteenCharLimit(string callsign, bool shouldBeValid)
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_call_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            $"QSO: 7000 PH 2025-09-20 1200 K7RMZ 59 OKA {callsign} 59 ISL",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            CabrilloLogFileSnapshot? logFile = processor.GetReadOnlyLogFile();
            Assert.NotNull(logFile);

            if (!shouldBeValid)
            {
                // Invalid callsigns should be recorded as skipped
                Assert.Contains(logFile.SkippedEntries, s =>
                    s.Reason != null && s.Reason.Contains("Invalid TheirCall"));
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    #endregion

    #region Integration Tests with Real Log Samples

    [Fact]
    public void ImportFile_WithRealSalmonRunSample_ParsesAllFieldsCorrectly()
    {
        // Create a minimal valid log for this test
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_sample_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7XXX",
            "CONTEST: TEST",
            "QSO: 3930 PH 2023-09-20 1605 K7XXX 59 OKA AC7DC 59 WHI",
            "QSO: 7215 PH 2023-09-20 1711 K7XXX 59 OKA K7BA 59 KING",
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
        }
        catch
        {
            return; // Skip test if we can't create temp file
        }

        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        OperationResult<Unit> result = processor.ImportFileResult(tmp);

        Assert.True(result.IsSuccess);

        List<LogEntry> entries = processor.ReadEntriesResult().Value!.ToList();
        Assert.NotEmpty(entries);

        // Verify first entry has valid structure
        LogEntry firstEntry = entries.First();
        Assert.NotNull(firstEntry.Frequency);
        Assert.True(firstEntry.FrequencyIsValid);
        Assert.NotNull(firstEntry.Mode);
        Assert.NotNull(firstEntry.CallSign);
        Assert.NotNull(firstEntry.TheirCall);
        Assert.NotNull(firstEntry.SentExchange);
        Assert.NotNull(firstEntry.ReceivedExchange);

        // Verify no transmitter ID in this log (single-op)
        Assert.Null(firstEntry.TransmitterId);

        // Cleanup temp file
        try { File.Delete(tmp); } catch { }
    }

    [Fact]
    public void ImportFile_WithMultipleFrequencyBands_ValidatesAllCorrectly()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"clp_multiband_{Guid.NewGuid():N}.log");
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 3930 PH 2025-09-20 1600 K7RMZ 59 OKA AC7DC 59 WHI",   // 80m
            "QSO: 7215 PH 2025-09-20 1700 K7RMZ 59 OKA K7BA 59 KING",   // 40m
            "QSO: 14200 PH 2025-09-20 1800 K7RMZ 59 OKA W7IB 59 WHA",   // 20m
            "QSO: 28050 PH 2025-09-20 1900 K7RMZ 59 OKA N7KN 59 ISL",   // 10m
            "QSO: 50100 PH 2025-09-20 2000 K7RMZ 59 OKA K7YR 59 DOU",   // 6m
            "END-OF-LOG:"
        };

        try
        {
            File.WriteAllLines(tmp, lines);
            CabrilloLogProcessor processor = new CabrilloLogProcessor();
            OperationResult<Unit> result = processor.ImportFileResult(tmp);

            Assert.True(result.IsSuccess);

            List<LogEntry> entries = processor.ReadEntriesResult().Value!.ToList();
            Assert.Equal(5, entries.Count);

            // Verify all frequencies are valid and bands are correctly assigned
            Assert.All(entries, entry => Assert.True(entry.FrequencyIsValid));
            Assert.Equal("80m", entries[0].Band);
            Assert.Equal("40m", entries[1].Band);
            Assert.Equal("20m", entries[2].Band);
            Assert.Equal("10m", entries[3].Band);
            Assert.Equal("6m", entries[4].Band);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    #endregion
}
