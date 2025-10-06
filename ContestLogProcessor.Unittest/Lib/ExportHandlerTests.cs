using System.IO;
using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class ExportHandlerTests
{
    [Fact]
    public async Task Export_HappyPath_WritesFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        string outPath = Path.Combine(tmpDir, "testout.log");

        try
        {
            var console = new TestConsole(new string?[] { });
            var proc = new CabrilloLogProcessor();
            // add a sample entry so export writes something
            var r = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(r.IsSuccess);

            var ctx = new CommandContext(proc, console, debug: false);
            var handler = new ExportCommandHandler();

            await handler.HandleAsync(new[] { "export", outPath }, ctx);

            Assert.True(File.Exists(outPath));
            string output = string.Join('\n', console.Outputs);
            Assert.Contains("Exported:", output);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Export_MissingExtension_NoAppend_FileCreatedAsGiven()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        string outPath = Path.Combine(tmpDir, "noext");

        try
        {
            var console = new TestConsole(new string?[] { });
            var proc = new CabrilloLogProcessor();
            var _created = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(_created.IsSuccess);
            var ctx = new CommandContext(proc, console, debug: false);
            var handler = new ExportCommandHandler();

            await handler.HandleAsync(new[] { "export", outPath }, ctx);

            // The processor appends .log when exporting, so the physical file will have .log appended
            string actualPath = outPath.EndsWith(".log", System.StringComparison.OrdinalIgnoreCase) ? outPath : outPath + ".log";
            Assert.True(File.Exists(actualPath));
            string output = string.Join('\n', console.Outputs);
            Assert.Contains("Exported:", output);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Export_ExistingFile_CancelledOnNo()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        string outPath = Path.Combine(tmpDir, "dupfile.log");
        File.WriteAllText(outPath, "existing");

        try
        {
            var console = new TestConsole(new string?[] { "n" });
            var proc = new CabrilloLogProcessor();
            var _created2 = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(_created2.IsSuccess);
            var ctx = new CommandContext(proc, console, debug: false);
            var handler = new ExportCommandHandler();

            await handler.HandleAsync(new[] { "export", Path.Combine(tmpDir, "dupfile") }, ctx);

            string output = string.Join('\n', console.Outputs);
            Assert.Contains("Export cancelled.", output);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Export_ExistingFile_OverwriteOnYes()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        string outPath = Path.Combine(tmpDir, "dupfile.log");
        File.WriteAllText(outPath, "existing");

        try
        {
            var console = new TestConsole(new string?[] { "y" });
            var proc = new CabrilloLogProcessor();
            proc.CreateEntry(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            var ctx = new CommandContext(proc, console, debug: false);
            var handler = new ExportCommandHandler();

            await handler.HandleAsync(new[] { "export", Path.Combine(tmpDir, "dupfile") }, ctx);

            string output = string.Join('\n', console.Outputs);
            Assert.Contains("Exported:", output);
            // file should exist (overwritten)
            Assert.True(File.Exists(outPath));
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Export_BadPath_ShowsError()
    {
        string badPath = "?:\\\0\"<>|";

        var console = new TestConsole(new string?[] { });
        var proc = new CabrilloLogProcessor();
        var ctx = new CommandContext(proc, console, debug: false);
        var handler = new ExportCommandHandler();

        await handler.HandleAsync(new[] { "export", badPath }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("Export failed:", output);
    }
}
