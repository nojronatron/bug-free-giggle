using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive;

public interface ICommandHandler
{
    string Name { get; }
    string? HelpText { get; }
    Task HandleAsync(string[] parts, ICommandContext ctx);
}
