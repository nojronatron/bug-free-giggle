using System;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class SourceLineNumberTests
{
    [Fact]
    public void ImportFile_SetsSourceLineNumber_PerParsedQso()
    {
        string[] lines = new[]
        {
            "START-OF-LOG: 1",
            "CREATED-BY: UnitTest",
            "QSO: 14000 CW 2023-09-26 2100 K7XXX 001 WA 59",
            "QSO: 7000 CW 2023-09-27 1200 TEST 123",
            "END-OF-LOG:"
        };

        var tmp = Path.Combine(Path.GetTempPath(), "sln_test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(tmp, lines);

            var processor = new CabrilloLogProcessor();
            processor.ImportFile(tmp);

            var entries = processor.ReadEntries().ToList();
            Assert.Equal(2, entries.Count);

            // The first QSO was on line 3 (1-based), the second on line 4
            var first = entries[0];
            var second = entries[1];

            Assert.Equal(3, first.SourceLineNumber);
            Assert.Equal(4, second.SourceLineNumber);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
