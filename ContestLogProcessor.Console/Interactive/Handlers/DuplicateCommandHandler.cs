using System.Threading.Tasks;
using System.Linq;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class DuplicateCommandHandler : ICommandHandler
{
    public string Name => "duplicate";

    public string? HelpText => "Duplicate entries by index or by filter";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: duplicate --index <n> | --filter \"text\"");
            return;
        }

        System.Collections.Generic.List<string> targets = new System.Collections.Generic.List<string>();

        if (parts[1].Equals("--index", System.StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
        {
            if (!int.TryParse(parts[2], out int idx))
            {
                ctx.Console.WriteLine("Invalid index.");
                return;
            }

            System.Collections.Generic.List<ContestLogProcessor.Lib.LogEntry> all = ctx.Processor.ReadEntries(orderBy: e => e.QsoDateTime).ToList();
            if (idx < 0 || idx >= all.Count)
            {
                ctx.Console.WriteLine($"Index out of range (0-{all.Count - 1}).");
                return;
            }
            targets.Add(all[idx].Id);
        }
        else if (parts[1].Equals("--filter", System.StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
        {
            string filter = parts[2];
            System.Collections.Generic.List<ContestLogProcessor.Lib.LogEntry> matches = ctx.Processor.ReadEntries().Where(e =>
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

            ctx.Console.Write("Enter index to duplicate, 'all' to duplicate all matches, or 'cancel': ");
            string? choice = await ctx.Console.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(choice))
            {
                ctx.Console.WriteLine("Cancelled.");
                return;
            }

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
        }
        else
        {
            ctx.Console.WriteLine("Use --index <n> or --filter \"text\" to select entries to duplicate.");
            return;
        }

        if (targets.Count == 0)
        {
            ctx.Console.WriteLine("No targets selected.");
            return;
        }

        // Show brief summary
        if (targets.Count == 1)
        {
            try
            {
                ContestLogProcessor.Lib.LogEntry? original = ctx.Processor.GetEntryById(targets[0]);
                if (original != null)
                {
                    ctx.Console.WriteLine("About to duplicate the following entry:");
                    try { ctx.Console.WriteLine(original.ToCabrilloLine()); }
                    catch { ctx.Console.WriteLine(original.RawLine ?? original.CallSign ?? "(no data)"); }
                }
            }
            catch { }
        }
        else
        {
            ctx.Console.WriteLine($"About to duplicate {targets.Count} entries.");
            int listCount = System.Math.Min(10, targets.Count);
            for (int i = 0; i < listCount; i++)
            {
                try
                {
                    ContestLogProcessor.Lib.LogEntry? o = ctx.Processor.GetEntryById(targets[i]);
                    if (o != null)
                    {
                        try { ctx.Console.WriteLine($"[{i}] {o.ToCabrilloLine()}"); }
                        catch { ctx.Console.WriteLine($"[{i}] {o.RawLine ?? o.CallSign ?? "(no data)"}"); }
                    }
                }
                catch { }
            }
            if (targets.Count > listCount)
            {
                ctx.Console.WriteLine($"...and {targets.Count - listCount} more entries (not shown)");
            }
        }

        // Prompt for field and new value once
        ctx.Console.WriteLine("Choose field to change on duplicate (leave blank for no change):");
        ctx.Console.WriteLine("  1) SentSig");
        ctx.Console.WriteLine("  2) SentMsg");
        ctx.Console.WriteLine("  3) TheirCall");
        ctx.Console.Write("Enter 1/2/3 or press Enter to skip: ");
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
            ctx.Console.Write($"Enter new value for {field}: ");
            newValue = await ctx.Console.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(newValue))
            {
                ctx.Console.WriteLine("No new value provided. Cancelled.");
                return;
            }
        }

        foreach (string targetId in targets)
        {
            try
            {
                ctx.Processor.DuplicateEntry(targetId, field, newValue);
                ctx.Console.WriteLine("Duplicated entry.");
            }
            catch (System.Exception ex)
            {
                ctx.Console.WriteLine($"Duplicate failed: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }
}
