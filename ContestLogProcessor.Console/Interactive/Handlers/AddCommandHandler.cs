using System.Threading.Tasks;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class AddCommandHandler : ICommandHandler
{
    public string Name => "add";
    public string? HelpText => "Add a new log entry interactively (TheirCall required)";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        // Prompt for basic fields. Keep prompts simple and synchronous via IConsole abstraction.
        ctx.Console.Write("Date (yyyy-MM-dd) [optional]: ");
        string? dateStr = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("Time (HHmm) [optional]: ");
        string? timeStr = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("Frequency [optional]: ");
        string? frequency = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("Mode [optional]: ");
        string? mode = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("Callsign [optional]: ");
        string? call = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("TheirCall (required): ");
        string? theirCall = await ctx.Console.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(theirCall))
        {
            ctx.Console.WriteLine("TheirCall is required. Aborting.");
            return;
        }

        // Exchanges
        ctx.Console.Write("Sent exchange (space-separated): ");
        string? sentEx = await ctx.Console.ReadLineAsync();

        ctx.Console.Write("Received exchange (space-separated): ");
        string? recvEx = await ctx.Console.ReadLineAsync();

        // Parse date/time if provided
        System.DateTime qsoDateTime = System.DateTime.MinValue;
        if (!string.IsNullOrWhiteSpace(dateStr) && !string.IsNullOrWhiteSpace(timeStr))
        {
            System.DateTime.TryParse(dateStr + " " + timeStr, out qsoDateTime);
        }

        LogEntry newEntry = new LogEntry
        {
            RawLine = null,
            Frequency = string.IsNullOrWhiteSpace(frequency) ? null : frequency,
            Mode = string.IsNullOrWhiteSpace(mode) ? null : mode,
            QsoDateTime = qsoDateTime,
            CallSign = string.IsNullOrWhiteSpace(call) ? null : call,
            TheirCall = theirCall
        };

        if (!string.IsNullOrWhiteSpace(sentEx))
        {
            newEntry.SentExchange = new Exchange { SentSig = sentEx };
        }

        if (!string.IsNullOrWhiteSpace(recvEx))
        {
            newEntry.ReceivedExchange = new Exchange { ReceivedMsg = recvEx };
        }

        try
        {
            LogEntry stored = ctx.Processor.CreateEntry(newEntry);
            ctx.Console.WriteLine($"Added entry with Id: {stored.Id}");
        }
        catch (System.Exception ex)
        {
            ctx.Console.WriteLine($"Failed to add entry: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}
