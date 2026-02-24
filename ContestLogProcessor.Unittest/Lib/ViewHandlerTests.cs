using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class ViewHandlerTests
{
    [Fact]
    public async Task View_HappyPath_PrintsPage()
    {
        TestConsole console = new TestConsole(new string?[] { "q" });
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        // Add a few entries
        for (int i = 0; i < 5; i++) { OperationResult<LogEntry> r = proc.CreateEntryResult(new LogEntry { CallSign = "K7" + i, TheirCall = "N0" + i }); Assert.True(r.IsSuccess); }

        CommandContext ctx = new CommandContext(proc, console, debug: false);
        ViewCommandHandler handler = new ViewCommandHandler();

        await handler.HandleAsync(new[] { "view" }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("Showing page 1", output);
    }

    [Fact]
    public async Task View_NoEntries_PrintsNoEntriesMessage()
    {
        TestConsole console = new TestConsole(new string?[] { });
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        CommandContext ctx = new CommandContext(proc, console, debug: false);
        ViewCommandHandler handler = new ViewCommandHandler();

        await handler.HandleAsync(new[] { "view" }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("(no entries loaded)", output);
    }
}
