using System.Reflection;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ReadEntriesResultFailureTests
    {
        [Fact]
        public void ReadEntriesResult_InternalNullEntry_ReturnsErrorWithNullReferenceDiagnostic()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();

            // Seed with at least one valid entry so internal list is created by CreateEntry
            OperationResult<LogEntry> created = proc.CreateEntryResult(new LogEntry { QsoDateTime = DateTime.UtcNow, Frequency = "7000", Mode = "PH", CallSign = "T", TheirCall = "K7X" });
            Assert.True(created.IsSuccess);

            // Use reflection to obtain the private _entries list and inject a null element to force a NullReferenceException during cloning
            FieldInfo? f = typeof(CabrilloLogProcessor).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            List<LogEntry?>? list = (List<LogEntry?>?)f.GetValue(proc);
            Assert.NotNull(list);

            // Insert a null so result.Select(e => e.Clone()) will throw
            list.Add(null);

            OperationResult<IEnumerable<LogEntry>> result = proc.ReadEntriesResult();
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.Error, result.Status);
            Assert.NotNull(result.Diagnostic);
            Assert.IsType<NullReferenceException>(result.Diagnostic);
        }
    }
}

