using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Unittest.Lib.TestHelpers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class SmokeImportAddExportTest
{
    [Fact]
    public async Task ImportHeaders_AddEntry_ExportSucceeds()
    {
        // Locate test data file from TestData
        string path = LocateTestData("K7XXX_HeadersOnly_Smoke.txt");

        // Prepare processor and import via handler
        var proc = new CabrilloLogProcessor();
        var importConsole = new TestConsole(new string?[] { });
        var importCtx = new CommandContext(proc, importConsole, debug: false);
        var importShell = new InteractiveShell(importCtx);
        importShell.RegisterHandler(new ImportCommandHandler());

        await importShell.ExecuteCommandAsync(new[] { "import", path });

        // Ensure imported
        Assert.Contains(importConsole.Outputs, o => o.Contains("Imported:"));
        Assert.True(proc.ReadEntries().Any());

        // Add an entry
        var addConsole = new TestConsole(new string?[] { "2025-09-30", "1200", "20", "PH", "K7SMOKE", "N0SMK", "599", "RRR" });
        var addCtx = new CommandContext(proc, addConsole, debug: false);
        var addHandler = new AddCommandHandler();
        await addHandler.HandleAsync(new[] { "add" }, addCtx);

        // Now export to a temp file
        string outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, "smoke-export.log");

        var exportConsole = new TestConsole(new string?[] { "y" });
        var exportCtx = new CommandContext(proc, exportConsole, debug: false);
        var exportHandler = new ExportCommandHandler();
        await exportHandler.HandleAsync(new[] { "export", outPath }, exportCtx);

        string output = string.Join('\n', exportConsole.Outputs);
        Assert.Contains("Exported:", output);

        Assert.True(File.Exists(outPath));

        // Clean up
        try { File.Delete(outPath); Directory.Delete(outDir); } catch { }
    }

    private static string LocateTestData(string fileName)
    {
        string baseDir = System.AppContext.BaseDirectory ?? System.IO.Directory.GetCurrentDirectory();
        string[] candidates = new[] {
            System.IO.Path.Combine(baseDir, "Lib", "TestData", fileName),
            System.IO.Path.Combine(baseDir, "TestData", fileName),
            System.IO.Path.Combine(baseDir, fileName)
        };
        foreach (string c in candidates)
        {
            if (System.IO.File.Exists(c)) return c;
        }
        string repoRelative = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "Lib", "TestData", fileName));
        if (System.IO.File.Exists(repoRelative)) return repoRelative;
        throw new System.IO.FileNotFoundException($"Test data file not found: {fileName}");
    }
}
