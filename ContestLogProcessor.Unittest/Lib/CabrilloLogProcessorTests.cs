using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class CabrilloLogProcessorTests
{
    private static string SampleLogPath => Path.Combine(AppContext.BaseDirectory, "TestData", "K7XXX_Test.log");
    private static string ExportPath => Path.Combine(AppContext.BaseDirectory, "TestData", "TestExport");

    [Fact]
    public void ReadFile_ParsesLogFileCorrectly()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        var entries = processor.GetEntries().ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.CallSign)));
    }

    [Fact]
    public void GetEntries_FiltersAndSortsCorrectly()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        var filtered = processor.GetEntries(e => e.Mode == "CW").ToList();
        Assert.All(filtered, e => Assert.Equal("CW", e.Mode));
        var sorted = processor.GetEntries(orderBy: e => e.QsoDateTime).ToList();
        Assert.True(sorted.SequenceEqual(sorted.OrderBy(e => e.QsoDateTime)));
    }

    [Fact]
    public void DuplicateEntry_CreatesCopyWithEdits()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        var originalCount = processor.GetEntries().Count();
        processor.DuplicateEntry(e => e.CallSign == "K7XXX", e => e.CallSign = "TESTCALL");
        var entries = processor.GetEntries().ToList();
        Assert.Equal(originalCount + 1, entries.Count);
        Assert.Contains(entries, e => e.CallSign == "TESTCALL");
    }

    [Fact]
    public void UpdateEntry_UpdatesEntryCorrectly()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        processor.UpdateEntry(e => e.CallSign == "K7XXX", e => e.Band = "TESTBAND");
        var entries = processor.GetEntries().ToList();
        Assert.Contains(entries, e => e.Band == "TESTBAND");
    }

    [Fact]
    public void ExportFile_WritesLogFileCorrectly()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        var exportFile = ExportPath + ".log";

        if (File.Exists(exportFile))
        {
            File.Delete(exportFile);
        }

        processor.ExportFile(ExportPath); // default uses canonical format
        Assert.True(File.Exists(exportFile));
        var lines = File.ReadAllLines(exportFile);
        Assert.Contains(lines, l => l.StartsWith("QSO:"));
        // Check canonical structure: the first QSO line should contain frequency, mode and date/time tokens
        var firstQso = lines.FirstOrDefault(l => l.StartsWith("QSO:"));
        Assert.NotNull(firstQso);
        Assert.Contains(" ", firstQso); // at least some tokens after QSO:
        File.Delete(exportFile);
    }

    [Fact]
    public void ReadFile_PopulatesExchangeFieldsAndRoundTripExport()
    {
        var processor = new CabrilloLogProcessor();
        processor.ReadFile(SampleLogPath);
        var entries = processor.GetEntries().ToList();
        Assert.NotEmpty(entries);

        // Verify at least one entry has an exchange populated (SentSig at minimum)
        Assert.Contains(entries, e => e.SentExchange != null && !string.IsNullOrWhiteSpace(e.SentExchange.SentSig));

        // Try a round-trip export and ensure we get a QSO line in the output, and canonical includes SentSig
        var exportFile = ExportPath + "_roundtrip.log";

        if (File.Exists(exportFile))
        {
            File.Delete(exportFile);
        }
        
        processor.ExportFile(ExportPath + "_roundtrip");
        Assert.True(File.Exists(exportFile));
        var lines = File.ReadAllLines(exportFile);
        Assert.Contains(lines, l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase));

        // Ensure at least one exported QSO contains a parsed SentSig token
        var sentSig = entries.Select(e => e.SentExchange?.SentSig).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        Assert.False(string.IsNullOrWhiteSpace(sentSig));
        Assert.Contains(lines, l => l.Contains(sentSig));
        File.Delete(exportFile);
    }
}
