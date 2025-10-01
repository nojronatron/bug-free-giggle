using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive;

public class InteractiveShell
{
    private readonly Dictionary<string, ICommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICommandContext _ctx;

    public InteractiveShell(ICommandContext ctx)
    {
        _ctx = ctx;
    }

    public void RegisterHandler(ICommandHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _handlers[handler.Name] = handler;
    }

    public async Task RunAsync()
    {
        _ctx.Console.WriteLine("Entering interactive mode. Type 'help' for available commands.");

        while (true)
        {
            _ctx.Console.Write("> ");
            string? line = await _ctx.Console.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = SplitArgs(line);
            string cmd = parts[0];

            if (string.Equals(cmd, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(cmd, "quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (_handlers.TryGetValue(cmd, out ICommandHandler? handler))
            {
                try
                {
                    await handler.HandleAsync(parts, _ctx);
                }
                catch (Exception ex)
                {
                    _ctx.Console.WriteLine($"Error: {ex.Message}");
                    if (_ctx.Debug) _ctx.Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                _ctx.Console.WriteLine($"Unknown command: {cmd}");
            }
        }
    }

    private static string[] SplitArgs(string line)
    {
        // Very small splitter: split on spaces, respecting double quotes
        var parts = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuote = false;
        foreach (char c in line)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (!inQuote && char.IsWhiteSpace(c))
            {
                if (sb.Length > 0) { parts.Add(sb.ToString()); sb.Clear(); }
                continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) parts.Add(sb.ToString());
        return parts.ToArray();
    }
}
