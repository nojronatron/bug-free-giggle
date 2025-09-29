using System;
using System.IO;
using System.Linq;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class ParseExchangeTests
{
    [Fact]
    public void ParseExchange_SentMsgAndTheirCall_AreParsedCorrectly()
    {
        // Arrange - create a minimal Cabrillo log containing the QSO line of interest
        string tmpDir = Path.GetTempPath();
        string filePath = Path.Combine(tmpDir, "clp_parse_exchange_test_" + Guid.NewGuid().ToString("N") + ".log");
        string[] lines = new[] {
            "START-OF-LOG: 3.0",
            "CALLSIGN: K7RMZ",
            "QSO: 3930 PH 2025-09-20 1605 K7RMZ 59 OKA AC7DC 59 WHI",
            "END-OF-LOG:" };

        try
        {
            File.WriteAllLines(filePath, lines);

            var processor = new CabrilloLogProcessor();
            processor.ImportFile(filePath);

            var entry = processor.ReadEntries().FirstOrDefault();
            Assert.NotNull(entry);

            // Verify the sent exchange SentMsg parsed as "OKA" and TheirCall parsed as "AC7DC"
            Assert.NotNull(entry.SentExchange);
            Assert.Equal("OKA", entry.SentExchange.SentMsg);
            Assert.Equal("AC7DC", entry.TheirCall);
        }
        finally
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
        }
    }
}
