using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class DefensiveCopyTests
    {
        [Fact]
        public void ReadEntries_Returns_Defensive_Clones_And_GetEntryById_Returns_Clone()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();

            // Create an entry and get the stored id
            OperationResult<LogEntry> createdResult = proc.CreateEntryResult(new LogEntry { CallSign = "ORIGINAL", TheirCall = "T1" });
            Assert.True(createdResult.IsSuccess);
            LogEntry? created = createdResult.Value;
            Assert.NotNull(created);
            string id = created.Id;

            // Read via ReadEntries (returns clone). Mutating clone should not affect stored entry.
            LogEntry? cloneFromList = proc.ReadEntriesResult().Value!.FirstOrDefault();
            Assert.NotNull(cloneFromList);
            cloneFromList!.CallSign = "MUTATED_BY_CALLER";

            LogEntry? fresh = proc.GetEntryByIdResult(id).Value;
            Assert.NotNull(fresh);
            Assert.Equal("ORIGINAL", fresh!.CallSign);

            // Mutate clone returned by GetEntryById
            LogEntry? cloneById = proc.GetEntryByIdResult(id).Value;
            cloneById!.CallSign = "MUTATED_BY_ID";

            LogEntry? fresh2 = proc.GetEntryByIdResult(id).Value;
            Assert.Equal("ORIGINAL", fresh2!.CallSign);
        }

        [Fact]
        public void EntryAdded_Event_Receives_Snapshot_Mutation_Does_Not_Affect_Stored()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();

            LogEntry? eventEntry = null;
            proc.EntryAdded += (s, e) =>
            {
                // Event should receive a snapshot; mutate it and verify stored state is not affected afterwards
                eventEntry = e;
                e.CallSign = "CHANGED_IN_EVENT";
            };

            OperationResult<LogEntry> createdResult2 = proc.CreateEntryResult(new LogEntry { CallSign = "ORIGINAL", TheirCall = "T2" });
            Assert.True(createdResult2.IsSuccess);
            LogEntry? created2 = createdResult2.Value;
            Assert.NotNull(created2);
            Assert.NotNull(eventEntry);
            Assert.Equal("CHANGED_IN_EVENT", eventEntry.CallSign);

            // Stored value should remain original
            LogEntry? stored = proc.GetEntryByIdResult(created2.Id).Value;
            Assert.Equal("ORIGINAL", stored!.CallSign);
        }
    }
}
