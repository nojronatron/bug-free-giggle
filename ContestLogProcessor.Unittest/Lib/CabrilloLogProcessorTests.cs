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
    public void ImportFile_ParsesLogFileCorrectly()
    {
        var processor = new CabrilloLogProcessor();
        processor.ImportFile(SampleLogPath);
        var entries = processor.ReadEntries().ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.CallSign)));
    }

    [Fact]
    public void ReadEntries_FilterOrderPaging_Works()
    {
        var processor = new CabrilloLogProcessor();
        processor.ImportFile(SampleLogPath);
        var cw = processor.ReadEntries(filter: e => e.Mode == "CW").ToList();
        Assert.All(cw, e => Assert.Equal("CW", e.Mode));

        var ordered = processor.ReadEntries(orderBy: e => e.QsoDateTime).ToList();
        Assert.True(ordered.SequenceEqual(ordered.OrderBy(e => e.QsoDateTime)));

        var paged = processor.ReadEntries(orderBy: e => e.QsoDateTime, skip: 1, take: 1).ToList();
        Assert.True(paged.Count <= 1);
    }

    [Fact]
    public void CreateGetUpdateDelete_AndEvents_Work()
    {
        var processor = new CabrilloLogProcessor();

        // Track events
        var added = new List<LogEntry>();
        var updated = new List<LogEntry>();
        var deletedIds = new List<string>();

        processor.EntryAdded += (_, e) => added.Add(e);
        processor.EntryUpdated += (_, e) => updated.Add(e);
        processor.EntryDeleted += (_, id) => deletedIds.Add(id);

        // Create
        var newEntry = new LogEntry
        {
            Frequency = "14000",
            Mode = "CW",
            QsoDateTime = DateTime.UtcNow,
            CallSign = "UNITTEST",
            SentExchange = new Exchange { SentSig = "001", SentMsg = "WA", TheirCall = "K7XXX" },
            TheirCall = "K7XXX"
        };

    var createdResult = processor.CreateEntryResult(newEntry);
    Assert.True(createdResult.IsSuccess);
    var created = createdResult.Value;
    Assert.NotNull(created);
    Assert.False(string.IsNullOrWhiteSpace(created.Id));
    Assert.Contains(added, a => a.Id == created.Id);

        // Read by id
        var fetched = processor.GetEntryById(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("UNITTEST", fetched.CallSign);

        // Update
        bool updatedResult = processor.UpdateEntry(created.Id, e => e.Band = "20m");
        Assert.True(updatedResult);
        Assert.Contains(updated, u => u.Id == created.Id && u.Band == "20m");

        // Delete
        bool deleted = processor.DeleteEntry(created.Id);
        Assert.True(deleted);
        Assert.Contains(deletedIds, id => id == created.Id);
        Assert.Null(processor.GetEntryById(created.Id));
    }

    [Fact]
    public void Integration_ImportModifyExport_CanonicalFormatting()
    {
        var processor = new CabrilloLogProcessor();
        processor.ImportFile(SampleLogPath);

        var entries = processor.ReadEntries(orderBy: e => e.QsoDateTime).ToList();
        Assert.NotEmpty(entries);

        // pick a parsed token to check for in exported canonical output
        var sentSig = entries.Select(e => e.SentExchange?.SentSig).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        // Create a new entry to ensure CRUD is exercised before export
        var newEntry = new LogEntry
        {
            Frequency = "7000",
            Mode = "CW",
            QsoDateTime = DateTime.UtcNow,
            CallSign = "INTEG",
            SentExchange = new Exchange { SentSig = "123", SentMsg = "TX" },
            TheirCall = "INTEG"
        };

    var createdResult = processor.CreateEntryResult(newEntry);
    Assert.True(createdResult.IsSuccess);
    var created = createdResult.Value;
    processor.UpdateEntry(created.Id, e => e.Band = "40m");

        var exportFile = ExportPath + "_crud_integ.log";
        if (File.Exists(exportFile)) File.Delete(exportFile);

        processor.ExportFile(ExportPath + "_crud_integ");
        Assert.True(File.Exists(exportFile));

        var lines = File.ReadAllLines(exportFile);
        Assert.Contains(lines, l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(sentSig))
        {
            Assert.Contains(lines, l => l.Contains(sentSig));
        }

        // clean up
        File.Delete(exportFile);
    }

    [Fact]
    public void ImportFile_ThrowsForMissingFile()
    {
        var processor = new CabrilloLogProcessor();
        var missing = Path.Combine(Path.GetTempPath(), "no-such-file-" + Guid.NewGuid() + ".log");
        Assert.Throws<FileNotFoundException>(() => processor.ImportFile(missing));
    }

    [Fact]
    public void ImportFile_ToleratesMalformedLines()
    {
        var processor = new CabrilloLogProcessor();
        var tmp = Path.Combine(Path.GetTempPath(), "malformed_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, new[]
            {
                "START-OF-LOG: 1",
                "CREATED-BY: UnitTest",
                // valid QSO
                "QSO: 14000 CW 2025-09-26 2100 K7RMZ 001 WA 59",
                // malformed lines
                "THIS IS NOT A TAG LINE",
                "QSO: BAD DATA",
                // another valid QSO
                "QSO: 7000 CW 2025-09-27 1200 TEST 123"
            });

            // Should not throw
            processor.ImportFile(tmp);

            var entries = processor.ReadEntries().ToList();
            Assert.NotEmpty(entries);
            // Ensure at least the valid callsigns were parsed
            Assert.Contains(entries, e => string.Equals(e.CallSign, "K7RMZ", StringComparison.OrdinalIgnoreCase) || string.Equals(e.CallSign, "TEST", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
