using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class CabrilloLogProcessorTests
{
    private const string SampleLogPath = "..\\..\\..\\..\\TestData\\K7XXX_Test.log";
    private const string ExportPath = "..\\..\\..\\..\\TestData\\TestExport";

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
        processor.ExportFile(ExportPath);
        Assert.True(File.Exists(exportFile));
        var lines = File.ReadAllLines(exportFile);
        Assert.Contains(lines, l => l.StartsWith("QSO:"));
        File.Delete(exportFile);
    }
}
