using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Unittest.Lib;
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

    // Ensure imported (the sample file is headers-only and may contain no QSO entries)
    Assert.Contains(importConsole.Outputs, o => o.Contains("Imported:"));
    // It's acceptable for a headers-only file to have zero entries; we'll add one below before exporting.

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

        // First try a few common relative locations near the test assembly
        string[] candidates = new[] {
            System.IO.Path.Combine(baseDir, "Lib", "TestData", fileName),
            System.IO.Path.Combine(baseDir, "TestData", fileName),
            System.IO.Path.Combine(baseDir, fileName)
        };
        foreach (string c in candidates)
        {
            if (System.IO.File.Exists(c)) return c;
        }

        // Walk up parents to find the repository root or the Unittest project folder
        var dir = new System.IO.DirectoryInfo(baseDir);
        int depth = 0;
        while (dir != null && depth < 10)
        {
            // If this directory contains the solution file, check TestData under the Unittest project
            string slnProbe = System.IO.Path.Combine(dir.FullName, "ContestLogProcessor.sln");
            if (System.IO.File.Exists(slnProbe))
            {
                string repoProbe = System.IO.Path.Combine(dir.FullName, "ContestLogProcessor.Unittest", "Lib", "TestData", fileName);
                if (System.IO.File.Exists(repoProbe)) return repoProbe;
            }

            // Check sibling layout patterns
            string probe = System.IO.Path.Combine(dir.FullName, "ContestLogProcessor.Unittest", "Lib", "TestData", fileName);
            if (System.IO.File.Exists(probe)) return probe;

            // Also check within this directory for a Lib/TestData path
            string localProbe = System.IO.Path.Combine(dir.FullName, "Lib", "TestData", fileName);
            if (System.IO.File.Exists(localProbe)) return localProbe;

            dir = dir.Parent;
            depth++;
        }

        // Final fallback: bounded recursive search from baseDir
        try
        {
            var found = System.IO.Directory.GetFiles(baseDir, fileName, System.IO.SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(found)) return found;
        }
        catch { }

        throw new System.IO.FileNotFoundException($"Test data file not found: {fileName}");
    }
}
