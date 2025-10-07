using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class DuplicateCommandHandlerFailureTests
{
    private class FailingReadProcessor : ILogProcessor
    {
        public event EventHandler<string>? EntryDeleted;

    OperationResult<Unit> ILogProcessor.ImportFileResult(string filePath) => OperationResult.Failure<Unit>("not implemented");
    OperationResult<Unit> ILogProcessor.ExportFileResult(string filePath, bool useCanonicalFormat, bool useBandToken) => OperationResult.Failure<Unit>("not implemented");

        OperationResult<LogEntry> ILogProcessor.CreateEntryResult(LogEntry entry) => OperationResult.Failure<LogEntry>("not implemented");

        OperationResult<LogEntry> ILogProcessor.DuplicateEntryResult(string id, ILogProcessor.DuplicateField field, string? newValue) => OperationResult.Failure<LogEntry>("not implemented");

        OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesResult(Func<LogEntry, bool>? filter, Func<LogEntry, object>? orderBy, int? skip, int? take)
        {
            return OperationResult.Failure<IEnumerable<LogEntry>>("Read failed", ResponseStatus.Error, new InvalidOperationException("boom"));
        }
        OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesByBandResult(string band, Func<LogEntry, object>? orderBy, int? skip, int? take)
            => OperationResult.Failure<IEnumerable<LogEntry>>("not implemented");
        OperationResult<LogEntry> ILogProcessor.GetEntryByIdResult(string id) => OperationResult.Failure<LogEntry>("not implemented");

        OperationResult<Unit> ILogProcessor.UpdateEntryResult(string id, Action<LogEntry> editAction) => OperationResult.Failure<Unit>("not implemented");
        OperationResult<Unit> ILogProcessor.DeleteEntryResult(string id) => OperationResult.Failure<Unit>("not implemented");
    }

    [Fact]
    public async Task DuplicateHandler_ReadEntriesFailure_PrintsError()
    {
        FailingReadProcessor proc = new FailingReadProcessor();
        TestConsole console = new TestConsole(Array.Empty<string?>());
        CommandContext ctx = new CommandContext(proc, console, debug: true);
        DuplicateCommandHandler handler = new DuplicateCommandHandler();

        await handler.HandleAsync(new[] { "duplicate", "--filter", "x" }, ctx);

        Assert.Contains(console.Outputs, o => o.Contains("Operation failed: Read failed"));
    }

    private class NotFoundGetProcessor : ILogProcessor
    {
        public event EventHandler<string>? EntryDeleted;
    OperationResult<Unit> ILogProcessor.ImportFileResult(string filePath) => OperationResult.Failure<Unit>("not implemented");
    OperationResult<Unit> ILogProcessor.ExportFileResult(string filePath, bool useCanonicalFormat, bool useBandToken) => OperationResult.Failure<Unit>("not implemented");

        OperationResult<LogEntry> ILogProcessor.CreateEntryResult(LogEntry entry) => OperationResult.Failure<LogEntry>("not implemented");

        OperationResult<LogEntry> ILogProcessor.DuplicateEntryResult(string id, ILogProcessor.DuplicateField field, string? newValue) => OperationResult.Failure<LogEntry>("not implemented");

    OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesResult(Func<LogEntry, bool>? filter, Func<LogEntry, object>? orderBy, int? skip, int? take) => OperationResult.Success<IEnumerable<LogEntry>>(new List<LogEntry>());
    OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesByBandResult(string band, Func<LogEntry, object>? orderBy, int? skip, int? take) => OperationResult.Success<IEnumerable<LogEntry>>(new List<LogEntry>());

        OperationResult<LogEntry> ILogProcessor.GetEntryByIdResult(string id) => OperationResult.Failure<LogEntry>("not found", ResponseStatus.NotFound);

        OperationResult<Unit> ILogProcessor.UpdateEntryResult(string id, Action<LogEntry> editAction) => OperationResult.Failure<Unit>("not implemented");

        OperationResult<Unit> ILogProcessor.DeleteEntryResult(string id) => OperationResult.Failure<Unit>("not implemented");
    }

    [Fact]
    public async Task DuplicateHandler_GetEntryNotFound_PrintsNotFound()
    {
        NotFoundGetProcessor proc = new NotFoundGetProcessor();
        TestConsole console = new TestConsole(new string[] { "cancel" });
        CommandContext ctx = new CommandContext(proc, console, debug: false);
        DuplicateCommandHandler handler = new DuplicateCommandHandler();

        // Simulate --filter path where no matches are found (read returns empty list)
        await handler.HandleAsync(new[] { "duplicate", "--filter", "x" }, ctx);

        // Should contain 'No matches found' because read returned empty list
        Assert.Contains(console.Outputs, o => o.Contains("No matches found for filter."));
    }

    private class FailingDuplicateProcessor : ILogProcessor
    {
        public event EventHandler<string>? EntryDeleted;
    OperationResult<Unit> ILogProcessor.ImportFileResult(string filePath) => OperationResult.Failure<Unit>("not implemented");
    OperationResult<Unit> ILogProcessor.ExportFileResult(string filePath, bool useCanonicalFormat, bool useBandToken) => OperationResult.Failure<Unit>("not implemented");

        OperationResult<LogEntry> ILogProcessor.CreateEntryResult(LogEntry entry) => OperationResult.Failure<LogEntry>("not implemented");

        OperationResult<LogEntry> ILogProcessor.DuplicateEntryResult(string id, ILogProcessor.DuplicateField field, string? newValue)
            => OperationResult.Failure<LogEntry>("duplicate failed", ResponseStatus.Error, new InvalidOperationException("dupboom"));

        OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesResult(Func<LogEntry, bool>? filter, Func<LogEntry, object>? orderBy, int? skip, int? take)
            => OperationResult.Success<IEnumerable<LogEntry>>(new List<LogEntry> { new LogEntry { Id = "1", CallSign = "K" } });
        OperationResult<IEnumerable<LogEntry>> ILogProcessor.ReadEntriesByBandResult(string band, Func<LogEntry, object>? orderBy, int? skip, int? take)
            => OperationResult.Success<IEnumerable<LogEntry>>(new List<LogEntry> { new LogEntry { Id = "1", CallSign = "K" } });

        OperationResult<LogEntry> ILogProcessor.GetEntryByIdResult(string id) => OperationResult.Success(new LogEntry { Id = id, CallSign = "K" });

        OperationResult<Unit> ILogProcessor.UpdateEntryResult(string id, Action<LogEntry> editAction) => OperationResult.Failure<Unit>("not implemented");

        OperationResult<Unit> ILogProcessor.DeleteEntryResult(string id) => OperationResult.Failure<Unit>("not implemented");
    }

    [Fact]
    public async Task DuplicateHandler_DuplicateFails_PrintsErrorAndDiagnosticWhenDebug()
    {
        FailingDuplicateProcessor proc = new FailingDuplicateProcessor();
        TestConsole console = new TestConsole(new string[] { "", "" });
        CommandContext ctx = new CommandContext(proc, console, debug: true);
        DuplicateCommandHandler handler = new DuplicateCommandHandler();

        // Choose index 0 path
        await handler.HandleAsync(new[] { "duplicate", "--index", "0" }, ctx);

        Assert.Contains(console.Outputs, o => o.Contains("Duplicate failed: duplicate failed"));
        Assert.Contains(console.Outputs, o => o.Contains("dupboom"));
    }
}
