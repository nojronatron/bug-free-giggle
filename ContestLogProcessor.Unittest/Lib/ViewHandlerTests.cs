using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class ViewHandlerTests
{
    [Fact]
    public async Task View_HappyPath_PrintsPage()
    {
        var console = new TestConsole(new string?[] { "q" });
        var proc = new CabrilloLogProcessor();
        // Add a few entries
        for (int i = 0; i < 5; i++) { var r = proc.CreateEntryResult(new LogEntry { CallSign = "K7" + i, TheirCall = "N0" + i }); Assert.True(r.IsSuccess); }

        var ctx = new CommandContext(proc, console, debug: false);
        var handler = new ViewCommandHandler();

        await handler.HandleAsync(new[] { "view" }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("Showing page 1", output);
    }

    [Fact]
    public async Task View_NoEntries_PrintsNoEntriesMessage()
    {
        var console = new TestConsole(new string?[] { });
        var proc = new CabrilloLogProcessor();
        var ctx = new CommandContext(proc, console, debug: false);
        var handler = new ViewCommandHandler();

        await handler.HandleAsync(new[] { "view" }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("(no entries loaded)", output);
    }
}
