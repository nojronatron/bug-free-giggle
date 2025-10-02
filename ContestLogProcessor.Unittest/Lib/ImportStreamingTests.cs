using System;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class ImportStreamingTests
{
    [Fact]
    public void ImportFile_StopsAtEndOfLogAndIgnoresRemainingLines()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 3.0",
            "CREATED-BY: Test",
            // a valid QSO that should be parsed
            "QSO: 7265 PH 2025-09-20 1715 K7XXX 59 OKA N7UK 59 KITT",
            // end-of-log marker should stop further processing
            "END-OF-LOG:",
            // the following lines should be ignored
            "QSO: 7265 PH 2025-09-20 1716 K7XXX 59 OKA W7IB 59 WHA",
            "CREATED-BY: ShouldNotBeRead",
            "QSO: 7265 PH 2025-09-20 1717 K7XXX 59 OKA FAKER 59 FAK"
        };

        string tmp = Path.Combine(Path.GetTempPath(), "import_streaming_test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);

            var proc = new CabrilloLogProcessor();
            proc.ImportFile(tmp);

            // Only the QSO before END-OF-LOG should be imported
            var entries = proc.ReadEntries().ToList();
            Assert.Single(entries);

            var first = entries[0];
            Assert.Equal("K7XXX", first.CallSign);
            // SourceLineNumber should be 3 (1-based)
            Assert.Equal(3, first.SourceLineNumber);

            // Header CREATED-BY should be the first one, not the one after END-OF-LOG
            Assert.True(proc.TryGetHeader("CREATED-BY", out string? createdBy));
            Assert.Equal("Test", createdBy);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
