using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ReadEntriesResultFailureTests
    {
        [Fact]
        public void ReadEntriesResult_NullEntryInInternalList_ReturnsErrorWithDiagnostic()
        {
            var proc = new CabrilloLogProcessor();

            // Seed with at least one valid entry so internal list is created by CreateEntry
            var created = proc.CreateEntryResult(new LogEntry { QsoDateTime = DateTime.UtcNow, Frequency = "7000", Mode = "PH", CallSign = "T", TheirCall = "K7X" });
            Assert.True(created.IsSuccess);

            // Use reflection to obtain the private _entries list and inject a null element to force a NullReferenceException during cloning
            FieldInfo? f = typeof(CabrilloLogProcessor).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            var list = (List<LogEntry>?)f.GetValue(proc);
            Assert.NotNull(list);

            // Insert a null so result.Select(e => e.Clone()) will throw
            list.Add(null);

            var result = proc.ReadEntriesResult();
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.Error, result.Status);
            Assert.NotNull(result.Diagnostic);
            Assert.IsType<NullReferenceException>(result.Diagnostic);
        }
    }
}

