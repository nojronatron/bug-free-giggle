using System;
using System.IO;
using System.Linq;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class BulkUpdateTests
    {
        [Fact]
        public void BulkUpdate_InPlace_UpdatesSentMsgForAllEntries()
        {
            // Arrange: write a small synthetic cabrillo log with two QSO lines
            string temp = Path.GetTempFileName() + ".log";
            string[] lines = new[]
            {
                "START-OF-LOG: 3.0",
                "CALLSIGN: TEST",
                "QSO: 3930 PH 2025-09-20 1605 K7RMZ 59 OKA AC7DC 59 WHI",
                "QSO: 7000 PH 2025-09-20 1610 K7RMZ 59 ABC AC7EF 59 WHI",
                "END-OF-LOG"
            };
            File.WriteAllLines(temp, lines);

            try
            {
                CabrilloLogProcessor proc = new CabrilloLogProcessor();
                proc.ImportFile(temp);

                var entries = proc.ReadEntries().ToList();
                Assert.Equal(2, entries.Count);

                // Act: update SentMsg in-place
                foreach (var e in entries)
                {
                    bool ok = proc.UpdateEntry(e.Id, entry =>
                    {
                        if (entry.SentExchange == null) entry.SentExchange = new Exchange();
                        entry.SentExchange.SentMsg = "ZZZ";
                    });
                    Assert.True(ok, "UpdateEntry failed for an entry");
                }

                // Export to a temp output and re-import to verify
                string outFile = Path.Combine(Path.GetTempPath(), "bulk-inplace-test.log");
                proc.ExportFile(outFile);

                CabrilloLogProcessor verify = new CabrilloLogProcessor();
                verify.ImportFile(outFile);
                var vEntries = verify.ReadEntries().ToList();
                Assert.Equal(entries.Count, vEntries.Count);
                foreach (var ve in vEntries)
                {
                    Assert.NotNull(ve.SentExchange);
                    Assert.Equal("ZZZ", ve.SentExchange.SentMsg);
                }
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Fact]
        public void BulkUpdate_Duplicate_CreatesDuplicatesWithChangedTheirCall()
        {
            // Arrange: small synthetic log
            string temp = Path.GetTempFileName() + ".log";
            string[] lines = new[]
            {
                "START-OF-LOG: 3.0",
                "CALLSIGN: TEST",
                "QSO: 3930 PH 2025-09-20 1605 K7RMZ 59 OKA AC7DC 59 WHI",
                "QSO: 7000 PH 2025-09-20 1610 K7RMZ 59 ABC AC7EF 59 WHI",
                "END-OF-LOG"
            };
            File.WriteAllLines(temp, lines);

            try
            {
                CabrilloLogProcessor proc = new CabrilloLogProcessor();
                proc.ImportFile(temp);

                var entries = proc.ReadEntries().ToList();
                int originalCount = entries.Count;
                Assert.Equal(2, originalCount);

                // Act: duplicate every entry and change TheirCall to NEWCALL
                foreach (var e in entries)
                {
                    proc.DuplicateEntry(e.Id, ILogProcessor.DuplicateField.TheirCall, "NEWCALL");
                }

                // Inspect in-memory entries to verify duplicates were added and had TheirCall updated
                var allInMemory = proc.ReadEntries().ToList();
                Assert.True(allInMemory.Count >= originalCount * 2, "Expected total entries to increase by at least the original count");
                var changed = allInMemory.Where(x => string.Equals(x.TheirCall, "NEWCALL", StringComparison.OrdinalIgnoreCase)).ToList();
                Assert.True(changed.Count > 0, "Expected at least one changed entry with THEIRCALL set to NEWCALL");
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }
}
