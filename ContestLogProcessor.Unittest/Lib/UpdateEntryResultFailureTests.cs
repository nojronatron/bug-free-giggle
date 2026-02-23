using System;
using System.Linq;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class UpdateEntryResultFailureTests
    {
        [Fact]
        public void UpdateEntryResult_NonExistentId_ReturnsNotFound()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            // Attempt to update an id that does not exist
            string missing = Guid.NewGuid().ToString();
            var result = proc.UpdateEntryResult(missing, e => e.Band = "20m");
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.NotFound, result.Status);
            Assert.Contains(missing, result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UpdateEntryResult_EditActionThrowsArgumentException_ReturnsBadFormat()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            LogEntry entry = new LogEntry
            {
                QsoDateTime = DateTime.UtcNow,
                Frequency = "7000",
                Mode = "PH",
                CallSign = "UNITTEST",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                TheirCall = "K7XXX"
            };

            var created = proc.CreateEntryResult(entry);
            Assert.True(created.IsSuccess);

            var result = proc.UpdateEntryResult(created.Value.Id, e => throw new ArgumentException("bad"));
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.BadFormat, result.Status);
            Assert.NotNull(result.Diagnostic);
            Assert.IsType<ArgumentException>(result.Diagnostic);
        }

        [Fact]
        public void UpdateEntryResult_EditActionThrowsArgumentNullException_ReturnsBadFormat()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            LogEntry entry = new LogEntry
            {
                QsoDateTime = DateTime.UtcNow,
                Frequency = "7000",
                Mode = "PH",
                CallSign = "UNITTEST",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                TheirCall = "K7XXX"
            };

            var created = proc.CreateEntryResult(entry);
            Assert.True(created.IsSuccess);

            var result = proc.UpdateEntryResult(created.Value.Id, e => throw new ArgumentNullException("x"));
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.BadFormat, result.Status);
            Assert.NotNull(result.Diagnostic);
            Assert.IsType<ArgumentNullException>(result.Diagnostic);
        }

        [Fact]
        public void UpdateEntryResult_EditActionThrowsOperationCanceledException_IsPropagated()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            LogEntry entry = new LogEntry
            {
                QsoDateTime = DateTime.UtcNow,
                Frequency = "7000",
                Mode = "PH",
                CallSign = "UNITTEST",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                TheirCall = "K7XXX"
            };

            var created = proc.CreateEntryResult(entry);
            Assert.True(created.IsSuccess);

            Assert.Throws<OperationCanceledException>(() => proc.UpdateEntryResult(created.Value.Id, e => throw new OperationCanceledException()));
        }

        [Fact]
        public void UpdateEntryResult_EditActionThrowsGenericException_ReturnsErrorWithDiagnostic()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            LogEntry entry = new LogEntry
            {
                QsoDateTime = DateTime.UtcNow,
                Frequency = "7000",
                Mode = "PH",
                CallSign = "UNITTEST",
                SentExchange = new Exchange { SentSig = "59", SentMsg = "OKA" },
                TheirCall = "K7XXX"
            };

            var created = proc.CreateEntryResult(entry);
            Assert.True(created.IsSuccess);

            var result = proc.UpdateEntryResult(created.Value.Id, e => throw new InvalidOperationException("boom"));
            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseStatus.Error, result.Status);
            Assert.NotNull(result.Diagnostic);
            Assert.IsType<InvalidOperationException>(result.Diagnostic);
            Assert.Equal("boom", result.Diagnostic.Message);
        }
    }
}

