using System;
using System.IO;
using System.Threading.Tasks;

using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ExportCommandHandler : ICommandHandler
{
    public string Name => "export";
    public string? HelpText => "Export current in-memory log to <filepath>.log [--use-band]";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: export [--use-band] <path>");
            return;
        }

        // Support an optional flag --use-band which tells the exporter to prefer the Band token
        // in the frequency slot when exporting (for example "40m"). The flag may appear before
        // or after the path; remove it from the path construction when present.
        bool useBand = false;
        System.Collections.Generic.List<string> pathParts = new System.Collections.Generic.List<string>();
        for (int i = 1; i < parts.Length; i++)
        {
            if (string.Equals(parts[i], "--use-band", StringComparison.OrdinalIgnoreCase))
            {
                useBand = true;
                continue;
            }
            pathParts.Add(parts[i]);
        }

        if (pathParts.Count == 0)
        {
            ctx.Console.WriteLine("Usage: export [--use-band] <path>");
            return;
        }

        string path = string.Join(' ', pathParts).Trim('"');

        // Normalize for overwrite check
        string checkPath = path;
        if (!checkPath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            checkPath += ".log";
        }

        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(checkPath))
            {
                ctx.Console.Write($"File '{checkPath}' already exists. Overwrite? (y/N): ");
                string? ans = await ctx.Console.ReadLineAsync();
                if (!string.Equals(ans, "y", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(ans, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Console.WriteLine("Export cancelled.");
                    return;
                }
            }

            OperationResult<Unit> res = ctx.Processor.ExportFileResult(path, useCanonicalFormat: true, useBandToken: useBand);
            if (res.IsSuccess)
            {
                ctx.Console.WriteLine($"Exported: {path}");
            }
            else
            {
                ctx.Console.WriteLine($"Export failed: {res.ErrorMessage}");
                if (ctx.Debug && res.Diagnostic != null) ctx.Console.WriteLine(res.Diagnostic.ToString());
            }
        }
        catch (Exception ex)
        {
            ctx.Console.WriteLine($"Export failed: {ex.Message}");
            if (ctx.Debug) ctx.Console.WriteLine(ex.ToString());
        }

        await Task.CompletedTask;
    }
}
