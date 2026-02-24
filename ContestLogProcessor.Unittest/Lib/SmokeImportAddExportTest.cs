using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class SmokeImportAddExportTest
{
    [Fact]
    public async Task ImportHeaders_AddEntry_ExportSucceeds()
    {
        // Locate test data file from TestData
        string path = LocateTestData("K7XXX_HeadersOnly_Smoke.txt");

        // Prepare processor and import via handler
        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        TestConsole importConsole = new TestConsole(new string?[] { });
        CommandContext importCtx = new CommandContext(proc, importConsole, debug: false);
        InteractiveShell importShell = new InteractiveShell(importCtx);
        importShell.RegisterHandler(new ImportCommandHandler());

        await importShell.ExecuteCommandAsync(new[] { "import", path });

        // Ensure imported (the sample file is headers-only and may contain no QSO entries)
        Assert.Contains(importConsole.Outputs, o => o.Contains("Imported:"));
        // It's acceptable for a headers-only file to have zero entries; we'll add one below before exporting.

        // Add an entry
        TestConsole addConsole = new TestConsole(new string?[] { "2025-09-30", "1200", "20", "PH", "K7SMOKE", "N0SMK", "599", "RRR" });
        CommandContext addCtx = new CommandContext(proc, addConsole, debug: false);
        AddCommandHandler addHandler = new AddCommandHandler();
        await addHandler.HandleAsync(new[] { "add" }, addCtx);

        // Now export to a temp file
        string outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, "smoke-export.log");

        TestConsole exportConsole = new TestConsole(new string?[] { "y" });
        CommandContext exportCtx = new CommandContext(proc, exportConsole, debug: false);
        ExportCommandHandler exportHandler = new ExportCommandHandler();
        await exportHandler.HandleAsync(new[] { "export", outPath }, exportCtx);

        string output = string.Join('\n', exportConsole.Outputs);
        Assert.Contains("Exported:", output);

        Assert.True(File.Exists(outPath));

        // Clean up
        try { File.Delete(outPath); Directory.Delete(outDir); } catch { }
    }

    private static string LocateTestData(string fileName)
    {
        string baseDir = System.AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();

        // First try a few common relative locations near the test assembly
        string[] candidates = new[] {
            Path.Combine(baseDir, "Lib", "TestData", fileName),
            Path.Combine(baseDir, "TestData", fileName),
            Path.Combine(baseDir, fileName)
        };
        foreach (string c in candidates)
        {
            if (File.Exists(c))
            {
                return c;
            }
        }

        // Walk up parents to find the repository root or the Unittest project folder
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        int depth = 0;
        while (dir != null && depth < 10)
        {
            // If this directory contains the solution file, check TestData under the Unittest project
            string slnProbe = Path.Combine(dir.FullName, "ContestLogProcessor.sln");
            if (File.Exists(slnProbe))
            {
                string repoProbe = Path.Combine(dir.FullName, "ContestLogProcessor.Unittest", "Lib", "TestData", fileName);
                if (File.Exists(repoProbe))
                {
                    return repoProbe;
                }
            }

            // Check sibling layout patterns
            string probe = Path.Combine(dir.FullName, "ContestLogProcessor.Unittest", "Lib", "TestData", fileName);
            if (File.Exists(probe))
            {
                return probe;
            }

            // Also check within this directory for a Lib/TestData path
            string localProbe = Path.Combine(dir.FullName, "Lib", "TestData", fileName);
            if (File.Exists(localProbe))
            {
                return localProbe;
            }

            dir = dir.Parent;
            depth++;
        }

        // Final fallback: bounded recursive search from baseDir
        try
        {
            string? found = Directory.GetFiles(baseDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(found))
            {
                return found;
            }
        }
        catch { }

        throw new FileNotFoundException($"Test data file not found: {fileName}");
    }
}
