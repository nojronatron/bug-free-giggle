using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ExitHandlerTests
    {
        [Fact]
        public async Task ExitHandler_RequestsExit()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            TestConsole console = new TestConsole(new string?[] { });
            CommandContext ctx = new CommandContext(proc, console, false);

            ExitCommandHandler handler = new ExitCommandHandler();
            await handler.HandleAsync(new[] { "exit" }, ctx);

            Assert.True(ctx.ExitRequested);
            Assert.Contains(console.Outputs, o => o.Contains("Exiting interactive session."));
        }

        [Fact]
        public async Task Shell_Stops_On_ExitHandler()
        {
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            TestConsole console = new TestConsole(new string?[] { });
            CommandContext ctx = new CommandContext(proc, console, false);

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
