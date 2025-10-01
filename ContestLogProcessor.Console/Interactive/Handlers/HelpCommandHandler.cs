using System.Threading.Tasks;
using System.Linq;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class HelpCommandHandler : ICommandHandler
{
    private readonly InteractiveShell? _shell;

    public HelpCommandHandler(InteractiveShell? shell = null)
    {
        _shell = shell;
    }

    public string Name => "help";

    public string? HelpText => "Show available interactive commands";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        // If an InteractiveShell instance was provided, use it to enumerate handlers
        if (_shell != null)
        {
            System.Collections.Generic.IReadOnlyCollection<ICommandHandler> handlers = _shell.GetRegisteredHandlers();
            ctx.Console.WriteLine("Available commands:");
            foreach (ICommandHandler h in handlers.OrderBy(h => h.Name, System.StringComparer.OrdinalIgnoreCase))
            {
                ctx.Console.WriteLine($"  {h.Name.PadRight(15)} - {h.HelpText}");
            }
            await Task.CompletedTask;
            return;
        }

        // Fallback: simple help
        ctx.Console.WriteLine("Available commands (help unavailable):");
        ctx.Console.WriteLine("  help - Show this message");
        await Task.CompletedTask;
    }
}
