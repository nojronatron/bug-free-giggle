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

    // Execute a single command (by splitting args outside) using the registered handlers.
    // Returns true if a handler was found and executed, false otherwise.
    public async System.Threading.Tasks.Task<bool> ExecuteCommandAsync(string[] parts)
    {
        if (parts == null || parts.Length == 0) return false;
        string cmd = parts[0];
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
            return true;
        }

        return false;
    }

    // Expose registered handlers for helpers such as help command
    public System.Collections.Generic.IReadOnlyCollection<ICommandHandler> GetRegisteredHandlers()
    {
        return _handlers.Values;
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
    System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
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
