using System;
using System.IO;
using System.Linq;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class XQSOAndEndOfLogTests
    {
        [Fact]
        public void Import_Should_Mark_XQSO_Entries_As_Ignored()
        {
            string tmp = Path.GetTempFileName();
            try
            {
                string[] lines = new[]
                {
                    "START-OF-LOG: 3.0",
                    "CALLSIGN: K7RMZ",
                    "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
                    "X-QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN",
                    "END-OF-LOG:"
                };

                File.WriteAllLines(tmp, lines);

                CabrilloLogProcessor p = new CabrilloLogProcessor();
                var imp = p.ImportFileResult(tmp);
                Assert.True(imp.IsSuccess);

                List<LogEntry> entries = p.ReadEntriesResult().Value!.ToList();
                Assert.Equal(2, entries.Count);
                Assert.False(entries[0].IsXQso);
                Assert.True(entries[1].IsXQso);
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        [Fact]
        public void Import_Should_Stop_At_EndOfLog()
        {
            string tmp = Path.GetTempFileName();
            try
            {
                string[] lines = new[]
                {
                    "START-OF-LOG: 3.0",
                    "CALLSIGN: K7RMZ",
                    "QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL",
                    "END-OF-LOG:",
                    "QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN"
                };

                File.WriteAllLines(tmp, lines);

                CabrilloLogProcessor p = new CabrilloLogProcessor();
                var imp = p.ImportFileResult(tmp);
                Assert.True(imp.IsSuccess);

                List<LogEntry> entries = p.ReadEntriesResult().Value!.ToList();
                // only the first QSO should be present
                Assert.Single(entries);
                Assert.Equal("KD7JB", entries[0].TheirCall);
            }
            finally
            {
                File.Delete(tmp);
            }
        }
    }
}
