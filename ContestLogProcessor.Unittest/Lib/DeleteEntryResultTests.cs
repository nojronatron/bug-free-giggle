using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class DeleteEntryResultTests
    {
        [Fact]
        public void DeleteEntryResult_RemovesExistingEntry_ReturnsSuccess()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();

            OperationResult<LogEntry> createdResult = proc.CreateEntryResult(new LogEntry
            {
                Frequency = "7000",
                Mode = "PH",
                QsoDateTime = DateTime.UtcNow,
                CallSign = "DELTEST",
                SentExchange = new Exchange { SentSig = "599", SentMsg = "COL", TheirCall = "K7X" },
                TheirCall = "K7X"
            });

            Assert.True(createdResult.IsSuccess);
            LogEntry? created = createdResult.Value;
            Assert.NotNull(created);

            List<string> deletedIds = new List<string>();
            proc.EntryDeleted += (_, id) => deletedIds.Add(id);

            OperationResult<Unit> result = proc.DeleteEntryResult(created.Id);
            Assert.True(result.IsSuccess);

            // Event should have been raised
            Assert.Contains(created.Id, deletedIds);

            // Entry should no longer be retrievable (use OperationResult API)
            Assert.False(proc.GetEntryByIdResult(created.Id).IsSuccess);
        }

        [Fact]
        public void DeleteEntryResult_NonexistentId_ReturnsNotFound()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();

            string notFoundId = Guid.NewGuid().ToString();
            OperationResult<Unit> result = proc.DeleteEntryResult(notFoundId);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.NotFound, result.Status);
            Assert.Null(result.Diagnostic);
        }
    }
}
