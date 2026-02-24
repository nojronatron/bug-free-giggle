using System.Collections;

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

        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        OperationResult<Unit> imp = proc.ImportFileResult(tmp);
        Assert.True(imp.IsSuccess);

        // Take a snapshot and mutate it
        CabrilloLogFileSnapshot? snap = proc.GetReadOnlyLogFile();
        Assert.NotNull(snap);

        // Attempt to mutate snapshot headers - should not be possible because Headers is read-only
        bool headerMutationRaised = false;
        try
        {
            IDictionary? dict = snap!.Headers as IDictionary;
            if (dict != null)
            {
                dict["CALLSIGN"] = "MUTATED";
            }
        }
        catch (NotSupportedException)
        {
            headerMutationRaised = true;
        }

        // Attempt to mutate skipped entries item if present - entries themselves are clones so mutation should not affect internal state
        if (snap.SkippedEntries != null && snap.SkippedEntries.Count > 0)
        {
            snap.SkippedEntries[0].Reason = "MUTATED-REASON";
        }

        // Verify processor internal state unchanged via TryGetHeader and fresh snapshot
        Assert.True(proc.TryGetHeader("CALLSIGN", out string? origCall));
        Assert.Equal("K7XXX", origCall);

        CabrilloLogFileSnapshot? snap2 = proc.GetReadOnlyLogFile();
        Assert.NotNull(snap2);
        Assert.Equal("K7XXX", snap2!.GetHeader("CALLSIGN"));
        Assert.True(headerMutationRaised == true || snap2.GetHeader("CALLSIGN") == "K7XXX");

        // If there were skipped entries, ensure they were not mutated in the processor's snapshot
        if (snap2.SkippedEntries != null && snap2.SkippedEntries.Count > 0)
        {
            Assert.NotEqual("MUTATED-REASON", snap2.SkippedEntries[0].Reason);
        }

        File.Delete(tmp);
    }
}
