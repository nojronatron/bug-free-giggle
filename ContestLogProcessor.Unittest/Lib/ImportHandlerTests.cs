using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ImportHandlerTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Import_Loads_File_And_Reports()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            TestConsole console = new TestConsole(new string?[] { });
            CommandContext ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new ImportCommandHandler());

            string path = FilterHandlerTests_LocateTestData("K7XXX_Test_WithDX.log");

            await shell.ExecuteCommandAsync(new[] { "import", path });

            // Expect console output contains 'Imported:' and processor has entries
            Assert.Contains(console.Outputs, o => o.Contains("Imported:"));
            Assert.True(proc.ReadEntriesResult().Value!.ToList().Count > 0);
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
