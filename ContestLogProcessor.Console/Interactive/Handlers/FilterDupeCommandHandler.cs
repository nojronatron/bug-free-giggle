using System.Linq;
using System.Threading.Tasks;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class FilterDupeCommandHandler : ICommandHandler
{
    public string Name => "filter-dupe";
    public string? HelpText => "Search entries and optionally duplicate selected/all matches";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: filter-dupe <text>");
            return;
        }

        string filter = string.Join(' ', parts, 1, parts.Length - 1);

            OperationResult<IEnumerable<LogEntry>> readOp = ctx.Processor.ReadEntriesResult();
            if (!readOp.IsSuccess)
            {
                ctx.Console.WriteLine($"Operation failed: {readOp.ErrorMessage}");
                if (ctx.Debug && readOp.Diagnostic != null) ctx.Console.WriteLine(readOp.Diagnostic.ToString());
                return;
            }

            System.Collections.Generic.List<LogEntry> all = readOp.Value!.ToList();
            System.Collections.Generic.List<LogEntry> matches = all.Where(e =>
                (!string.IsNullOrWhiteSpace(e.CallSign) && e.CallSign.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(e.RawLine) && e.RawLine.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                (ContestLogProcessor.Lib.Formatters.CabrilloFormatter.TrySafeToCabrillo(e, out string line) && line.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

        if (matches.Count == 0)
        {
            ctx.Console.WriteLine("No matches found for filter.");
            return;
        }

        ctx.Console.WriteLine($"Found {matches.Count} matches. List:");
        for (int i = 0; i < matches.Count; i++)
        {
            if (ContestLogProcessor.Lib.Formatters.CabrilloFormatter.TrySafeToCabrillo(matches[i], out string outLine))
            {
                ctx.Console.WriteLine($"[{i}] {outLine}");
            }
            else
            {
                ctx.Console.WriteLine($"[{i}] {matches[i].RawLine ?? matches[i].CallSign ?? "(no data)"}");
            }
        }

        ctx.Console.WriteLine("Enter index to duplicate, 'all' to duplicate all matches, or 'cancel': ");
        string? choice = await ctx.Console.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(choice))
        {
            ctx.Console.WriteLine("Cancelled.");
            return;
        }

    System.Collections.Generic.List<string> targets = new System.Collections.Generic.List<string>();
        if (choice.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            targets.AddRange(matches.Select(m => m.Id));
        }
        else if (int.TryParse(choice, out int chosen) && chosen >= 0 && chosen < matches.Count)
        {
            targets.Add(matches[chosen].Id);
        }
        else
        {
            ctx.Console.WriteLine("Unknown selection. Cancelled.");
            return;
        }

        if (targets.Count == 0)
        {
            ctx.Console.WriteLine("No targets selected.");
            return;
        }

        ctx.Console.WriteLine("Choose field to change on duplicate (leave blank for no change):");
        ctx.Console.WriteLine("  1) SentSig");
        ctx.Console.WriteLine("  2) SentMsg");
        ctx.Console.WriteLine("  3) TheirCall");
        ctx.Console.WriteLine("Enter 1/2/3 or press Enter to skip: ");
        string? fld = await ctx.Console.ReadLineAsync();

        ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None;
        if (!string.IsNullOrWhiteSpace(fld))
        {
            field = fld.Trim() switch
            {
                "1" => ILogProcessor.DuplicateField.SentSig,
                "2" => ILogProcessor.DuplicateField.SentMsg,
                "3" => ILogProcessor.DuplicateField.TheirCall,
                _ => ILogProcessor.DuplicateField.None
            };
        }

        string? newValue = null;
        if (field != ILogProcessor.DuplicateField.None)
        {
            ctx.Console.WriteLine($"Enter new value for {field}: ");
            newValue = await ctx.Console.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(newValue))
            {
                ctx.Console.WriteLine("No new value provided. Cancelled.");
                return;
            }
        }

        foreach (string targetId in targets)
        {
            OperationResult<LogEntry> res = ctx.Processor.DuplicateEntryResult(targetId, field, newValue);
            if (res.IsSuccess && res.Value != null)
            {
                ctx.Console.WriteLine("Duplicated entry.");
            }
            else
            {
                ctx.Console.WriteLine($"Duplicate failed: {res.ErrorMessage}");
                if (ctx.Debug && res.Diagnostic != null) ctx.Console.WriteLine($"Diagnostic: {res.Diagnostic}");
            }
        }

        await Task.CompletedTask;
    }
}
