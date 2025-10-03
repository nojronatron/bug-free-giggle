using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ContestLogProcessor.Lib;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;

// Minimal CLI entrypoint. Command implementations live in handler classes under
// ContestLogProcessor.Console.Interactive. Program.cs only wires up the root command
// and provides a simple interactive bootstrap that forwards input to the interactive
// shell handlers.

Option<bool> debugOption = new Option<bool>("--debug", description: "Enable debug output");
Option<string?> importOption = new Option<string?>(new[] { "-i", "--import" }, description: "Import a Cabrillo .log file") { ArgumentHelpName = "logfile" };
Option<string?> exportOption = new Option<string?>(new[] { "-e", "--export" }, description: "Export current data to a .log file") { ArgumentHelpName = "newfile" };
Option<bool> listOption = new Option<bool>(new[] { "-l", "--list" }, description: "List loaded entries (raw lines)");
Option<bool> interactiveOption = new Option<bool>("--interactive", description: "Start an interactive session");
Option<string?> scoreOption = new Option<string?>("--score", description: "Score a Cabrillo .log file and print a brief report") { ArgumentHelpName = "logfile" };

RootCommand root = new RootCommand("ContestLogProcessor CLI - parse, edit and export Cabrillo v3 ham contest logs. Use --score <logfile> to score a Cabrillo .log file non-interactively.")
{
    debugOption,
    importOption,
    exportOption,
    listOption,
    interactiveOption,
    scoreOption
};

root.SetHandler(async (bool debug, string? import, string? export, bool list, bool interactive, string? score) =>
{
    CabrilloLogProcessor processor = new CabrilloLogProcessor();

    if (interactive)
    {
        await RunInteractive(processor, debug);
        return;
    }

    try
    {
        if (!string.IsNullOrWhiteSpace(import))
        {
            processor.ImportFile(import);
            Console.WriteLine($"Imported: {import}");
        }

        if (!string.IsNullOrWhiteSpace(score))
        {
            if (!File.Exists(score))
            {
                Console.WriteLine($"File not found: {score}");
            }
            else
            {
                try
                {
                    CabrilloLogProcessor scProc = new CabrilloLogProcessor();
                    scProc.ImportFile(score);

                    CabrilloLogFile log = new CabrilloLogFile();
                    if (scProc.TryGetHeader("CALLSIGN", out string? call) && !string.IsNullOrWhiteSpace(call))
                    {
                        log.Headers["CALLSIGN"] = call!;
                    }
                    else
                    {
                        string? inferred = scProc.ReadEntries().FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
                        if (!string.IsNullOrWhiteSpace(inferred)) log.Headers["CALLSIGN"] = inferred!;
                    }

                    log.Entries = scProc.ReadEntries().ToList();

                    SalmonRunScoringService svc = new SalmonRunScoringService();
                    SalmonRunScoreResult res = svc.CalculateScore(log);

                    string headerBorder = "+----------------------------------------+";
                    string headerTitle = "|          Salmon Run Score Report      |";
                    Console.WriteLine(headerBorder);
                    Console.WriteLine(headerTitle);
                    Console.WriteLine(headerBorder);
                    Console.WriteLine($" Final score : {res.FinalScore}");
                    Console.WriteLine($" QSO points  : {res.QsoPoints}");
                    Console.WriteLine($" Multiplier   : {res.Multiplier}");
                    Console.WriteLine($" W7DX bonus   : {res.W7DxBonusPoints}");
                    Console.WriteLine("------------------------------------------");

                    int innerWidth = Math.Max(10, headerBorder.Length - 2);
                    ReportRenderer.PrintWrappedList("Washington Counties", res.UniqueWashingtonCounties, innerWidth, 2, false, res.UniqueWashingtonCounties.Count.ToString());
                    ReportRenderer.PrintWrappedList("US States", res.UniqueUSStates, innerWidth, 2, false, res.UniqueUSStates.Count.ToString());
                    ReportRenderer.PrintWrappedList("Canadian Provinces", res.UniqueCanadianProvinces, innerWidth, 2, false, res.UniqueCanadianProvinces.Count.ToString());
                    ReportRenderer.PrintWrappedList("DXCC Entities", res.UniqueDxccEntities, innerWidth, 2, false, $"{res.UniqueDxccEntities.Count} / 10");

                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine($" Skipped entries: {res.SkippedEntries.Count}");
                    int show = Math.Min(10, res.SkippedEntries.Count);
                    for (int i = 0; i < show; i++)
                    {
                        SkippedEntryInfo s = res.SkippedEntries[i];
                        foreach (string outLine in ReportRenderer.FormatSkippedEntry(s)) Console.WriteLine(outLine);
                    }
                    if (res.SkippedEntries.Count > show)
                    {
                        Console.WriteLine($"  ...and {res.SkippedEntries.Count - show} more skipped entries (not shown)");
                    }

                    Console.WriteLine(headerBorder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Scoring failed: {ex.Message}");
                    if (debug) Console.WriteLine(ex.ToString());
                }
            }
        }

        if (list)
        {
            foreach (LogEntry e in processor.ReadEntries())
            {
                Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
            }
        }

        if (!string.IsNullOrWhiteSpace(export))
        {
            processor.ExportFile(export);
            Console.WriteLine($"Exported: {export}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");

        if (debug)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    await Task.CompletedTask;
}, debugOption, importOption, exportOption, listOption, interactiveOption, scoreOption);

return await root.InvokeAsync(args);

static async Task RunInteractive(CabrilloLogProcessor processor, bool debug)
{
    Console.WriteLine("Entering interactive mode. Type 'help' for available commands.");

    InteractiveShell shell = new InteractiveShell(new CommandContext(processor, new SystemConsoleWrapper(), debug));
    shell.RegisterHandler(new FilterCommandHandler());
    shell.RegisterHandler(new FilterDupeCommandHandler());
    shell.RegisterHandler(new AddCommandHandler());
    shell.RegisterHandler(new ExportCommandHandler());
    shell.RegisterHandler(new ViewCommandHandler());
    shell.RegisterHandler(new ScoreCommandHandler());
    shell.RegisterHandler(new ImportCommandHandler());
    shell.RegisterHandler(new DuplicateCommandHandler());
    shell.RegisterHandler(new HelpCommandHandler(shell));
    shell.RegisterHandler(new ExitCommandHandler());

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        string[] parts = PromptHelper.SplitArguments(input);
        string cmd = parts.Length > 0 ? parts[0] : string.Empty;

        // Allow users to type 'exit' or 'quit' to leave the interactive session
        if (string.Equals(cmd, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(cmd, "quit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Exiting interactive session.");
            break;
        }

        try
        {
            bool handled = await shell.ExecuteCommandAsync(parts);
            if (!handled)
            {
                Console.WriteLine($"Unknown command: {cmd}. Type 'help' to see available commands.");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Exiting interactive session.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command failed: {ex.Message}");
            if (debug)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
