using System.Threading.Tasks;

using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class SmokeExportHeaderTest
{
    [Fact]
    public async Task Export_AfterAddWithoutHeaders_PrintsExportFailed()
    {
        // Arrange: simulate interactive add then export via handlers
        TestConsole console = new TestConsole(new string?[] { /* unused inputs for handlers */ });
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        CommandContext ctx = new CommandContext(proc, console, debug: false);

        // Use AddCommandHandler to create an entry directly (call with parts but override inputs via TestConsole)
        TestConsole addConsole = new TestConsole(new string?[] { "2025-09-30", "1200", "20", "PH", "K7SMOKE", "N0SMK", "599", "RRR" });
        CommandContext addCtx = new CommandContext(proc, addConsole, debug: false);
        AddCommandHandler addHandler = new AddCommandHandler();
        await addHandler.HandleAsync(new[] { "add" }, addCtx);

        // Now attempt export using ExportCommandHandler
        TestConsole exportConsole = new TestConsole(new string?[] { });
        CommandContext exportCtx = new CommandContext(proc, exportConsole, debug: false);
        ExportCommandHandler exportHandler = new ExportCommandHandler();
        await exportHandler.HandleAsync(new[] { "export", "somepath.log" }, exportCtx);

        // Assert
        string output = string.Join('\n', exportConsole.Outputs);
        Assert.Contains("Export failed:", output);
    }
}
