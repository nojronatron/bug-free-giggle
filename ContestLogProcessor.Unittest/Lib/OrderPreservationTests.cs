using System;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class OrderPreservationTests
{
    [Fact]
    public void CreateEntry_Inserts_By_QsoDateTime_Block()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 1",
            "CREATED-BY: UnitTest",
            "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
            "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN",
            "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA N7KN 59 ISL",
            "QSO: 7218 PH 2023-09-20 1717 K7XXX 59 OKA N7FCC/M 59 SKAG",
            "END-OF-LOG:"
        };

        var tmp = Path.Combine(Path.GetTempPath(), "order_test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            p.ImportFile(tmp);

            // Create a new entry with timestamp 2023-09-20 17:16 which should be inserted after the two 1716 entries
            var dt = DateTime.SpecifyKind(new DateTime(2023, 9, 20, 17, 16, 0), DateTimeKind.Utc);
            var newEntry = new LogEntry
            {
                QsoDateTime = dt,
                Frequency = "7218",
                Mode = "PH",
                CallSign = "K7XXX",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                TheirCall = "F4KE"
            };

            var createdResult = p.CreateEntryResult(newEntry);
            Assert.True(createdResult.IsSuccess);
            var created = createdResult.Value;

            var entries = p.ReadEntries().ToList();
            // Expect the new entry to be after the two 1716 entries, which are at indexes 1 and 2 (0-based list: 0->1715,1->1716,2->1716,3->1717)
            int idx = entries.FindIndex(e => e.Id == created.Id);
            Assert.Equal(3, idx); // 0:1715,1:1716,2:1716,3:new(1716),4:1717

            // Ensure SourceLineNumber for created entries is null
            Assert.Null(created.SourceLineNumber);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void DuplicateEntry_Inserts_Immediately_After_Source()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 1",
            "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
            "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN",
            "END-OF-LOG:"
        };

        var tmp = Path.Combine(Path.GetTempPath(), "dup_test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            p.ImportFile(tmp);

            var entriesBefore = p.ReadEntries().ToList();
            var source = entriesBefore[0];
            var dupResult = p.DuplicateEntryResult(source.Id, ILogProcessor.DuplicateField.SentMsg, "CHE");
            Assert.True(dupResult.IsSuccess);
            var dup = dupResult.Value;

            var entries = p.ReadEntries().ToList();
            int sourceIndex = entries.FindIndex(e => e.Id == source.Id);
            Assert.True(sourceIndex >= 0);
            Assert.Equal(sourceIndex + 1, entries.FindIndex(e => e.Id == dup.Id));

            // Duplicates should have null SourceLineNumber
            Assert.Null(dup.SourceLineNumber);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void UpdateEntry_DoesNot_Change_Position()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 1",
            "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
            "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN",
            "END-OF-LOG:"
        };

        var tmp = Path.Combine(Path.GetTempPath(), "update_test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            p.ImportFile(tmp);

            var entries = p.ReadEntries().ToList();
            var target = entries[1];
            int beforeIndex = entries.IndexOf(target);

            var updateResult = p.UpdateEntryResult(target.Id, e => { if (e.SentExchange != null) e.SentExchange.SentMsg = "CHE"; });
            Assert.True(updateResult.IsSuccess);

            var entriesAfter = p.ReadEntries().ToList();
            int afterIndex = entriesAfter.FindIndex(e => e.Id == target.Id);
            Assert.Equal(beforeIndex, afterIndex);
            Assert.Equal("CHE", entriesAfter[afterIndex].SentExchange?.SentMsg);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void Export_Preserves_InMemory_Order()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 1",
            "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
            "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN",
            "END-OF-LOG:"
        };

        var tmp = Path.Combine(Path.GetTempPath(), "export_test_" + Guid.NewGuid() + ".log");
        var outp = Path.Combine(Path.GetTempPath(), "export_out_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);
            var p = new CabrilloLogProcessor();
            p.ImportFile(tmp);

            // Duplicate first entry so order becomes: orig1, dup1, orig2
            var entriesBefore = p.ReadEntries().ToList();
            var dupResult2 = p.DuplicateEntryResult(entriesBefore[0].Id, ILogProcessor.DuplicateField.SentMsg, "CHE");
            Assert.True(dupResult2.IsSuccess);
            var dup = dupResult2.Value;

            p.ExportFile(Path.Combine(Path.GetTempPath(), "export_out_" + Guid.NewGuid()));
            // read the last created file by matching prefix
            var exported = Directory.GetFiles(Path.GetTempPath(), "export_out_*.log");
            Assert.NotEmpty(exported);
            var file = exported.OrderByDescending(f => File.GetLastWriteTimeUtc(f)).First();
            var outLines = File.ReadAllLines(file);

            // Create expected canonical lines from in-memory entries
            var mem = p.ReadEntries().ToList();

            // Extract exported QSO lines (lines that start with "QSO:") in order
            var exportedQsoLines = outLines.Where(l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Equal(mem.Count, exportedQsoLines.Count);
            for (int i = 0; i < mem.Count; i++)
            {
                Assert.Equal(mem[i].ToCabrilloLine(), exportedQsoLines[i]);
            }

            // final line should be END-OF-LOG:
            Assert.Equal("END-OF-LOG:", outLines.Last());
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            foreach (var f in Directory.GetFiles(Path.GetTempPath(), "export_out_*.log"))
            {
                try { File.Delete(f); } catch { }
            }
        }
    }
}
