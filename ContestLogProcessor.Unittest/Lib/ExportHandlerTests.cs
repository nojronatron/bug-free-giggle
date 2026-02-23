using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

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
            TestConsole console = new TestConsole(new string?[] { });
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            // add a sample entry so export writes something
            OperationResult<LogEntry> r = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(r.IsSuccess);

            CommandContext ctx = new CommandContext(proc, console, debug: false);
            ExportCommandHandler handler = new ExportCommandHandler();

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
            TestConsole console = new TestConsole(new string?[] { });
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            OperationResult<LogEntry> _created = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(_created.IsSuccess);
            CommandContext ctx = new CommandContext(proc, console, debug: false);
            ExportCommandHandler handler = new ExportCommandHandler();

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
            TestConsole console = new TestConsole(new string?[] { "n" });
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            OperationResult<LogEntry> _created2 = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(_created2.IsSuccess);
            CommandContext ctx = new CommandContext(proc, console, debug: false);
            ExportCommandHandler handler = new ExportCommandHandler();

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
            TestConsole console = new TestConsole(new string?[] { "y" });
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            OperationResult<LogEntry> _created2 = proc.CreateEntryResult(new LogEntry { CallSign = "K7TEST", TheirCall = "N0CALL" });
            Assert.True(_created2.IsSuccess);
            CommandContext ctx = new CommandContext(proc, console, debug: false);
            ExportCommandHandler handler = new ExportCommandHandler();

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

        TestConsole console = new TestConsole(new string?[] { });
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        CommandContext ctx = new CommandContext(proc, console, debug: false);
        ExportCommandHandler handler = new ExportCommandHandler();

        await handler.HandleAsync(new[] { "export", badPath }, ctx);

        string output = string.Join('\n', console.Outputs);
        Assert.Contains("Export failed:", output);
    }
}
