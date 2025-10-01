using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Unittest.Lib.TestHelpers;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class HelpHandlerTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Help_Lists_Handlers()
        {
            var proc = new CabrilloLogProcessor();
            var console = new TestConsole(new string?[] { });
            var ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new FilterCommandHandler());
            shell.RegisterHandler(new FilterDupeCommandHandler());
            shell.RegisterHandler(new HelpCommandHandler(shell));

            // Execute help via the shell
            await shell.ExecuteCommandAsync(new[] { "help" });

            // Expect that outputs contain 'filter' and 'filter-dupe'
            Assert.Contains(console.Outputs, o => o.Contains("filter"));
            Assert.Contains(console.Outputs, o => o.Contains("filter-dupe"));
        }
    }
}
