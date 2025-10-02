using System;
using System.IO;
using System.Linq;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class SnapshotImmutabilityTests
{
    [Fact]
    public void GetReadOnlyLogFile_Returns_Defensive_Copy()
    {
        string tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCALLSIGN: K7XXX\r\nCREATED-BY: Test\r\nQSO: 7265 PH 2025-09-20 1715 K7XXX 59 OKA N7UK 59 KITT\r\nEND-OF-LOG:\r\n");

        var proc = new CabrilloLogProcessor();
        proc.ImportFile(tmp);

        // Take a snapshot and mutate it
        var snap = proc.GetReadOnlyLogFile();
        Assert.NotNull(snap);

        // Mutate snapshot headers and skipped entries
        snap!.Headers["CALLSIGN"] = "MUTATED";
        if (snap.SkippedEntries != null && snap.SkippedEntries.Count > 0)
        {
            snap.SkippedEntries[0].Reason = "MUTATED-REASON";
        }

        // Verify processor internal state unchanged via TryGetHeader and fresh snapshot
        Assert.True(proc.TryGetHeader("CALLSIGN", out string? origCall));
        Assert.Equal("K7XXX", origCall);

        var snap2 = proc.GetReadOnlyLogFile();
        Assert.NotNull(snap2);
        Assert.Equal("K7XXX", snap2!.GetHeader("CALLSIGN"));

        // If there were skipped entries, ensure they were not mutated in the processor's snapshot
        if (snap2.SkippedEntries != null && snap2.SkippedEntries.Count > 0)
        {
            Assert.NotEqual("MUTATED-REASON", snap2.SkippedEntries[0].Reason);
        }

        File.Delete(tmp);
    }
}
