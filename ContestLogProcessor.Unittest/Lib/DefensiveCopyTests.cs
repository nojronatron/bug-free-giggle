using System;
using System.Linq;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class DefensiveCopyTests
    {
        [Fact]
        public void ReadEntries_Returns_Defensive_Clones_And_GetEntryById_Returns_Clone()
        {
            var proc = new CabrilloLogProcessor();

            // Create an entry and get the stored id
            var createdResult = proc.CreateEntryResult(new LogEntry { CallSign = "ORIGINAL", TheirCall = "T1" });
            Assert.True(createdResult.IsSuccess);
            var created = createdResult.Value;
            string id = created.Id;

            // Read via ReadEntries (returns clone). Mutating clone should not affect stored entry.
            var cloneFromList = proc.ReadEntries().FirstOrDefault();
            Assert.NotNull(cloneFromList);
            cloneFromList!.CallSign = "MUTATED_BY_CALLER";

            var fresh = proc.GetEntryById(id);
            Assert.NotNull(fresh);
            Assert.Equal("ORIGINAL", fresh!.CallSign);

            // Mutate clone returned by GetEntryById
            var cloneById = proc.GetEntryById(id);
            cloneById!.CallSign = "MUTATED_BY_ID";

            var fresh2 = proc.GetEntryById(id);
            Assert.Equal("ORIGINAL", fresh2!.CallSign);
        }

        [Fact]
        public void EntryAdded_Event_Receives_Snapshot_Mutation_Does_Not_Affect_Stored()
        {
            var proc = new CabrilloLogProcessor();

            LogEntry? eventEntry = null;
            proc.EntryAdded += (s, e) =>
            {
                // Event should receive a snapshot; mutate it and verify stored state is not affected afterwards
                eventEntry = e;
                e.CallSign = "CHANGED_IN_EVENT";
            };

            var createdResult2 = proc.CreateEntryResult(new LogEntry { CallSign = "ORIGINAL", TheirCall = "T2" });
            Assert.True(createdResult2.IsSuccess);
            var created2 = createdResult2.Value;
            Assert.NotNull(eventEntry);
            Assert.Equal("CHANGED_IN_EVENT", eventEntry.CallSign);

            // Stored value should remain original
            var stored = proc.GetEntryById(created2.Id);
            Assert.Equal("ORIGINAL", stored!.CallSign);
        }
    }
}
