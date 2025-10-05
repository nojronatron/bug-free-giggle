using System;
using System.IO;
using System.Threading.Tasks;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ExportCommandHandler : ICommandHandler
{
    public string Name => "export";
    public string? HelpText => "Export current in-memory log to <filepath>.log";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: export <path>");
            return;
        }

        string path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

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

            OperationResult<Unit> res = ctx.Processor.ExportFileResult(path);
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
