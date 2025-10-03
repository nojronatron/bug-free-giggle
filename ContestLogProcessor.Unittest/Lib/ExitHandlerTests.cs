using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Unittest.Lib;
using ContestLogProcessor.Lib;
using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ExitHandlerTests
    {
        [Fact]
        public async System.Threading.Tasks.Task ExitHandler_RequestsExit()
        {
            var proc = new CabrilloLogProcessor();
            var console = new TestConsole(new string?[] { });
            var ctx = new CommandContext(proc, console, false);

            var handler = new ExitCommandHandler();
            await handler.HandleAsync(new[] { "exit" }, ctx);

            Assert.True(ctx.ExitRequested);
            Assert.Contains(console.Outputs, o => o.Contains("Exiting interactive session."));
        }

        [Fact]
        public async System.Threading.Tasks.Task Shell_Stops_On_ExitHandler()
        {
            var proc = new CabrilloLogProcessor();
            var console = new TestConsole(new string?[] { });
            var ctx = new CommandContext(proc, console, false);

            InteractiveShell shell = new InteractiveShell(ctx);
            shell.RegisterHandler(new ExitCommandHandler());

            // Execute the exit command via the shell
            bool cont = await shell.ExecuteCommandAsync(new[] { "exit" });

            // ExecuteCommandAsync returns false when exit was requested
            Assert.False(cont);
            Assert.True(ctx.ExitRequested);
            Assert.Contains(console.Outputs, o => o.Contains("Exiting interactive session."));
        }
    }
}
