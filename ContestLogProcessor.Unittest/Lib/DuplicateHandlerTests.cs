using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class DuplicateHandlerTests
    {
        [Fact]
        public async Task Duplicate_ByIndex_HappyPath()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            string path = FilterHandlerTests_LocateTestData("K7XXX_Test_WithDX.log");
            OperationResult<Unit> imp = proc.ImportFileResult(path);
            Assert.True(imp.IsSuccess);

            // Before count
            int before = proc.ReadEntriesResult().Value!.ToList().Count;

            // Provide empty string to skip changing any field
            TestConsole console = new TestConsole(new string?[] { "" });
            CommandContext ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new DuplicateCommandHandler());

            await shell.ExecuteCommandAsync(new[] { "duplicate", "--index", "0" });

            int after = proc.ReadEntriesResult().Value!.ToList().Count;
            Assert.True(after > before, "After duplicating by index, entry count should increase.");
            Assert.Contains(console.Outputs, o => o.Contains("Duplicated entry") || o.Contains("Duplicated entry."));
        }

        [Fact]
        public async Task Duplicate_ByFilter_All_HappyPath()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            string path = FilterHandlerTests_LocateTestData("K7XXX_Test_WithDX.log");
            OperationResult<Unit> imp2 = proc.ImportFileResult(path);
            Assert.True(imp2.IsSuccess);

            int before = proc.ReadEntriesResult().Value!.ToList().Count;

            // Provide 'all' to duplicate all matches and '' to skip field change
            TestConsole console = new TestConsole(new string?[] { "all", "" });
            CommandContext ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new DuplicateCommandHandler());

            await shell.ExecuteCommandAsync(new[] { "duplicate", "--filter", "AC7DC" });

            int after = proc.ReadEntries().ToList().Count;
            Assert.True(after > before, "After duplicating by filter, entry count should increase.");
            Assert.Contains(console.Outputs, o => o.Contains("Duplicated entry"));
        }

        [Fact]
        public async Task Duplicate_Index_OutOfRange()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            string path = FilterHandlerTests_LocateTestData("K7XXX_Test_WithDX.log");
            OperationResult<Unit> imp3 = proc.ImportFileResult(path);
            Assert.True(imp3.IsSuccess);

            TestConsole console = new TestConsole(new string?[] { });
            CommandContext ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new DuplicateCommandHandler());

            await shell.ExecuteCommandAsync(new[] { "duplicate", "--index", "9999" });

            Assert.Contains(console.Outputs, o => o.Contains("Index out of range"));
        }

        private static string FilterHandlerTests_LocateTestData(string fileName)
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
}
