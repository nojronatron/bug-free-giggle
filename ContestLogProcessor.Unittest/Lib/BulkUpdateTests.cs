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
                "QSO: 3930 PH 2025-09-20 1605 K7XXX 59 OKA AC7DC 59 WHI",
                "QSO: 7000 PH 2025-09-20 1610 K7XXX 59 ABC AC7EF 59 WHI",
                "END-OF-LOG"
            };
            File.WriteAllLines(temp, lines);

            try
            {
                CabrilloLogProcessor proc = new CabrilloLogProcessor();
                var imp = proc.ImportFileResult(temp);
                Assert.True(imp.IsSuccess);

                var entries = proc.ReadEntriesResult().Value!.ToList();
                Assert.Equal(2, entries.Count);

                // Act: update SentMsg in-place
                foreach (var e in entries)
                {
                    var r = proc.UpdateEntryResult(e.Id, entry =>
                    {
                        if (entry.SentExchange == null) entry.SentExchange = new Exchange();
                        entry.SentExchange.SentMsg = "ZZZ";
                    });
                    Assert.True(r.IsSuccess, "UpdateEntryResult failed for an entry");
                }

                // Export to a temp output and re-import to verify
                string outFile = Path.Combine(Path.GetTempPath(), "bulk-inplace-test.log");
                var exportResult = proc.ExportFileResult(outFile);
                Assert.True(exportResult.IsSuccess);

                CabrilloLogProcessor verify = new CabrilloLogProcessor();
                var impVerify = verify.ImportFileResult(outFile);
                Assert.True(impVerify.IsSuccess);
                var vEntries = verify.ReadEntriesResult().Value!.ToList();
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
        public void Export_WritesEndOfLog_AsFinalLineWithNewlineBytes()
        {
            // Arrange: small synthetic log
            string temp = Path.GetTempFileName() + ".log";
            string[] lines = new[]
            {
                "START-OF-LOG: 3.0",
                "CALLSIGN: TEST",
                "QSO: 3930 PH 2025-09-20 1605 K7XXX 59 OKA AC7DC 59 WHI",
                "END-OF-LOG"
            };
            File.WriteAllLines(temp, lines);

            string outFile = Path.Combine(Path.GetTempPath(), "export-endoflog-test.log");
            try
            {
                if (File.Exists(outFile)) File.Delete(outFile);

                CabrilloLogProcessor proc = new CabrilloLogProcessor();
                var imp2 = proc.ImportFileResult(temp);
                Assert.True(imp2.IsSuccess);
                var exportRes = proc.ExportFileResult(outFile);
                Assert.True(exportRes.IsSuccess);

                byte[] data = File.ReadAllBytes(outFile);
                // Must end with CRLF on Windows (\r\n)
                Assert.True(data.Length >= 3, "Export file too small to contain END-OF-LOG and newline");

                byte cr = (byte)'\r';
                byte lf = (byte)'\n';
                Assert.Equal(cr, data[data.Length - 2]);
                Assert.Equal(lf, data[data.Length - 1]);

                // Check that the bytes immediately before CRLF spell "END-OF-LOG:"
                string tag = "END-OF-LOG:";
                byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
                int tagStart = data.Length - 2 - tagBytes.Length;
                Assert.True(tagStart >= 0, "File too short to contain tag before CRLF");
                for (int i = 0; i < tagBytes.Length; i++)
                {
                    Assert.Equal(tagBytes[i], data[tagStart + i]);
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
                try { File.Delete(outFile); } catch { }
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
                "QSO: 3930 PH 2025-09-20 1605 K7XXX 59 OKA AC7DC 59 WHI",
                "QSO: 7000 PH 2025-09-20 1610 K7XXX 59 ABC AC7EF 59 WHI",
                "END-OF-LOG"
            };
            File.WriteAllLines(temp, lines);

            try
            {
                CabrilloLogProcessor proc = new CabrilloLogProcessor();
                var imp3 = proc.ImportFileResult(temp);
                Assert.True(imp3.IsSuccess);

                var entries = proc.ReadEntriesResult().Value!.ToList();
                int originalCount = entries.Count;
                Assert.Equal(2, originalCount);

                // Act: duplicate every entry and change TheirCall to NEWCALL
                foreach (var e in entries)
                {
                    var r = proc.DuplicateEntryResult(e.Id, ILogProcessor.DuplicateField.TheirCall, "NEWCALL");
                    Assert.True(r.IsSuccess);
                }

                // Inspect in-memory entries to verify duplicates were added and had TheirCall updated
                var allInMemory = proc.ReadEntriesResult().Value!.ToList();
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
