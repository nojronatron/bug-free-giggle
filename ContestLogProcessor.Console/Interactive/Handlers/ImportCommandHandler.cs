using System.Threading.Tasks;
using System.IO;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ImportCommandHandler : ICommandHandler
{
    public string Name => "import";

    public string? HelpText => "Import a Cabrillo .log file into memory";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        if (parts.Length < 2)
        {
            ctx.Console.WriteLine("Usage: import <path>");
            return;
        }

        string path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

        try
        {
            if (!File.Exists(path))
            {
                ctx.Console.WriteLine($"File not found: {path}");
                return;
            }
            ctx.Processor.ImportFile(path);
            ctx.Console.WriteLine($"Imported: {path}");
        }
        catch (System.Exception ex)
        {
            ctx.Console.WriteLine($"Import failed: {ex.Message}");
            if (ctx.Debug) ctx.Console.WriteLine(ex.ToString());
        }

        await Task.CompletedTask;
    }
}
