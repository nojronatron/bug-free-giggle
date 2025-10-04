using System;
using System.Collections.Generic;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib
{
    public class DeleteEntryResultTests
    {
        [Fact]
        public void DeleteEntryResult_RemovesExistingEntry_ReturnsSuccess()
        {
            var proc = new CabrilloLogProcessor();

            var createdResult = proc.CreateEntryResult(new LogEntry
            {
                Frequency = "7000",
                Mode = "PH",
                QsoDateTime = DateTime.UtcNow,
                CallSign = "DELTEST",
                SentExchange = new Exchange { SentSig = "599", SentMsg = "COL", TheirCall = "K7X" },
                TheirCall = "K7X"
            });

            Assert.True(createdResult.IsSuccess);
            var created = createdResult.Value;
            Assert.NotNull(created);

            var deletedIds = new List<string>();
            proc.EntryDeleted += (_, id) => deletedIds.Add(id);

            var result = proc.DeleteEntryResult(created.Id);
            Assert.True(result.IsSuccess);

            // Event should have been raised
            Assert.Contains(created.Id, deletedIds);

            // Entry should no longer be retrievable
            Assert.Null(proc.GetEntryById(created.Id));
        }

        [Fact]
        public void DeleteEntryResult_NonexistentId_ReturnsNotFound()
        {
            var proc = new CabrilloLogProcessor();

            string notFoundId = Guid.NewGuid().ToString();
            var result = proc.DeleteEntryResult(notFoundId);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.NotFound, result.Status);
            Assert.Null(result.Diagnostic);
        }
    }
}
