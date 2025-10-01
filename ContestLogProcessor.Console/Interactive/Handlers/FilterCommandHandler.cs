using System.Linq;
using System.Threading.Tasks;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class FilterCommandHandler : ICommandHandler
{
    public string Name => "filter";
    public string? HelpText => "Search entries and list matches (read-only)";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: filter <text>");
            return;
        }

        string filter = string.Join(' ', parts, 1, parts.Length - 1);

        System.Collections.Generic.List<LogEntry> matches = ctx.Processor.ReadEntries().Where(e =>
            (!string.IsNullOrWhiteSpace(e.CallSign) && e.CallSign.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrWhiteSpace(e.RawLine) && e.RawLine.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
            e.ToCabrilloLine().IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0
        ).ToList();

        if (matches.Count == 0)
        {
            ctx.Console.WriteLine("No matches found for filter.");
            return;
        }

        ctx.Console.WriteLine($"Found {matches.Count} matches. List:");
        for (int i = 0; i < matches.Count; i++)
        {
            ctx.Console.WriteLine($"[{i}] {matches[i].ToCabrilloLine()}");
        }

        await System.Threading.Tasks.Task.CompletedTask;
    }
}
