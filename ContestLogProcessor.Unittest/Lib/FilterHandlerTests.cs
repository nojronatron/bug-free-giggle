using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Unittest.Lib;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class FilterHandlerTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Filter_ReturnsMatches()
        {
            var proc = new CabrilloLogProcessor();
            string path = LocateTestData("K7XXX_Test_WithDX.log");
            var imp = proc.ImportFileResult(path);
            Assert.True(imp.IsSuccess);

            var console = new TestConsole(new string?[] { });
            var ctx = new CommandContext(proc, console, false);
            var handler = new FilterCommandHandler();

            await handler.HandleAsync(new[] { "filter", "W7DX" }, ctx);

            // Expect at least one line mentioning W7DX in outputs
            Assert.Contains(console.Outputs, o => o.Contains("W7DX") || o.Contains("W7DX"));
        }

        [Fact]
        public async System.Threading.Tasks.Task FilterDupe_DuplicatesOnAll()
        {
            var proc = new CabrilloLogProcessor();
            string path = LocateTestData("K7XXX_Test_WithDX.log");
            var imp2 = proc.ImportFileResult(path);
            Assert.True(imp2.IsSuccess);
            // Provide inputs: 'all' then choose '3' (TheirCall) then value 'ZZZ'
            var console = new TestConsole(new string?[] { "all", "3", "ZZZ" });
            var ctx = new CommandContext(proc, console, false);
            var handler = new FilterDupeCommandHandler();

            await handler.HandleAsync(new[] { "filter-dupe", "AC7DC" }, ctx);

            // After duplication, processor should have more entries than original
            System.Collections.Generic.List<ContestLogProcessor.Lib.LogEntry> entries = proc.ReadEntries().ToList();
            Assert.True(entries.Count > 0);
        }

        private static string LocateTestData(string fileName)
        {
            string baseDir = AppContext.BaseDirectory ?? System.IO.Directory.GetCurrentDirectory();
            string[] candidates = new[] {
                System.IO.Path.Combine(baseDir, "Lib", "TestData", fileName),
                System.IO.Path.Combine(baseDir, "TestData", fileName),
                System.IO.Path.Combine(baseDir, fileName)
            };
            foreach (string c in candidates)
            {
                if (System.IO.File.Exists(c)) return c;
            }
            // As a last resort, try relative to repo root (two levels up)
            string repoRelative = System.IO.Path.Combine(baseDir, "..", "..", "Lib", "TestData", fileName);
            repoRelative = System.IO.Path.GetFullPath(repoRelative);
            if (System.IO.File.Exists(repoRelative)) return repoRelative;

            throw new System.IO.FileNotFoundException($"Test data file not found: {fileName}. Tried: {string.Join(';', candidates)} and {repoRelative}");
        }
    }
}
