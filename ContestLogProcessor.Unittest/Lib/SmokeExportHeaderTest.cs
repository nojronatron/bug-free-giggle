using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class SmokeExportHeaderTest
{
    [Fact]
    public async Task Export_AfterAddWithoutHeaders_PrintsExportFailed()
    {
        // Arrange: simulate interactive add then export via handlers
        var console = new TestConsole(new string?[] { /* unused inputs for handlers */ });
        var proc = new CabrilloLogProcessor();
        var ctx = new CommandContext(proc, console, debug: false);

        // Use AddCommandHandler to create an entry directly (call with parts but override inputs via TestConsole)
        var addConsole = new TestConsole(new string?[] { "2025-09-30", "1200", "20", "PH", "K7SMOKE", "N0SMK", "599", "RRR" });
        var addCtx = new CommandContext(proc, addConsole, debug: false);
        var addHandler = new AddCommandHandler();
        await addHandler.HandleAsync(new[] { "add" }, addCtx);

        // Now attempt export using ExportCommandHandler
        var exportConsole = new TestConsole(new string?[] { });
        var exportCtx = new CommandContext(proc, exportConsole, debug: false);
        var exportHandler = new ExportCommandHandler();
        await exportHandler.HandleAsync(new[] { "export", "somepath.log" }, exportCtx);

        // Assert
        string output = string.Join('\n', exportConsole.Outputs);
        Assert.Contains("Export failed:", output);
    }
}
