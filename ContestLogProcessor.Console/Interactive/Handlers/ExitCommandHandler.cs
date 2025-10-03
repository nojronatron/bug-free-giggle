using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ExitCommandHandler : ICommandHandler
{
    public string Name => "exit";

    public string? HelpText => "Exit the interactive session";

    public Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        ctx.Console.WriteLine("Exiting interactive session.");
        ctx.RequestExit();
        return Task.CompletedTask;
    }
}
