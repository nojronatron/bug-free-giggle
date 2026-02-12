using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using ContestLogProcessor.Lib;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.SalmonRun;
using ContestLogProcessor.WinterFieldDay;

// Minimal CLI entrypoint. Command implementations live in handler classes under
// ContestLogProcessor.Console.Interactive. Program.cs only wires up the root command
// and provides a simple interactive bootstrap that forwards input to the interactive
// shell handlers.

ServiceCollection services = new ServiceCollection();
ConfigureServices(services);
ServiceProvider serviceProvider = services.BuildServiceProvider();

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
    ILogProcessor processor = new CabrilloLogProcessor();

    if (interactive)
    {
        await RunInteractive(processor, debug, serviceProvider);
        return;
    }

    try
    {
        if (!string.IsNullOrWhiteSpace(import))
        {
            OperationResult<Unit> importResult = processor.ImportFileResult(import);
            if (importResult.IsSuccess)
            {
                Console.WriteLine($"Imported: {import}");
            }
            else
            {
                Console.WriteLine($"Import failed: {importResult.ErrorMessage}");
                if (debug && importResult.Diagnostic != null) Console.WriteLine(importResult.Diagnostic.ToString());
            }
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
                            ILogProcessor scProc = new CabrilloLogProcessor();
                            OperationResult<Unit> importRes = scProc.ImportFileResult(score);
                            if (!importRes.IsSuccess)
                            {
                                Console.WriteLine($"Import failed: {importRes.ErrorMessage}");
                                if (debug && importRes.Diagnostic != null) Console.WriteLine(importRes.Diagnostic.ToString());
                                return;
                            }

                            CabrilloLogFile log = new CabrilloLogFile();
                            OperationResult<IEnumerable<LogEntry>> readRes = scProc.ReadEntriesResult();
                            if (!readRes.IsSuccess)
                            {
                                Console.WriteLine($"Failed to read entries: {readRes.ErrorMessage}");
                                if (debug && readRes.Diagnostic != null) Console.WriteLine(readRes.Diagnostic.ToString());
                                return;
                            }

                            List<LogEntry> entriesForScore = readRes.Value!.ToList();
                            string? inferred = entriesForScore.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
                            if (!string.IsNullOrWhiteSpace(inferred)) log.Headers["CALLSIGN"] = inferred!;

                            // Get headers from the processor for contest detection
                            if (scProc is CabrilloLogProcessor cabrilloProc)
                            {
                                if (cabrilloProc.TryGetHeader("CONTEST", out string? contestHeader))
                                {
                                    log.Headers["CONTEST"] = contestHeader!;
                                }
                                if (cabrilloProc.TryGetHeader("CALLSIGN", out string? callsignHeader) && !string.IsNullOrWhiteSpace(callsignHeader))
                                {
                                    log.Headers["CALLSIGN"] = callsignHeader!;
                                }
                            }

                            log.Entries = entriesForScore;

                            // Detect contest type and get appropriate scoring service
                            IContestDetector detector = serviceProvider.GetRequiredService<IContestDetector>();
                            IContestRegistry registry = serviceProvider.GetRequiredService<IContestRegistry>();

                            OperationResult<string> contestDetection = detector.DetectContestType(log);
                            if (!contestDetection.IsSuccess)
                            {
                                Console.WriteLine($"Contest detection failed: {contestDetection.ErrorMessage}");
                                if (debug && contestDetection.Diagnostic != null) Console.WriteLine(contestDetection.Diagnostic.ToString());
                                return;
                            }

                            string contestType = contestDetection.Value!;
                            Console.WriteLine($"Detected contest: {contestType}");

                            if (contestType == "SALMON-RUN")
                            {
                                SalmonRunScoringService svc = serviceProvider.GetRequiredService<SalmonRunScoringService>();
                                OperationResult<SalmonRunScoreResult> scoreOp = svc.CalculateScore(log);
                                if (!scoreOp.IsSuccess)
                                {
                                    Console.WriteLine($"Scoring failed: {scoreOp.ErrorMessage}");
                                    if (debug && scoreOp.Diagnostic != null) Console.WriteLine(scoreOp.Diagnostic.ToString());
                                    return;
                                }

                                SalmonRunScoreResult res = scoreOp.Value!;
                                PrintSalmonRunScore(res);
                            }
                            else if (contestType == "WFD")
                            {
                                WinterFieldDayScoringService svc = serviceProvider.GetRequiredService<WinterFieldDayScoringService>();
                                OperationResult<WinterFieldDayScoreResult> scoreOp = svc.CalculateScore(log);
                                if (!scoreOp.IsSuccess)
                                {
                                    Console.WriteLine($"Scoring failed: {scoreOp.ErrorMessage}");
                                    if (debug && scoreOp.Diagnostic != null) Console.WriteLine(scoreOp.Diagnostic.ToString());
                                    return;
                                }

                                WinterFieldDayScoreResult res = scoreOp.Value!;
                                PrintWinterFieldDayScore(res);
                            }
                            else
                            {
                                Console.WriteLine($"Scoring not implemented for contest type: {contestType}");
                                return;
                            }
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
            OperationResult<IEnumerable<LogEntry>> readRes = processor.ReadEntriesResult();
            if (!readRes.IsSuccess)
            {
                Console.WriteLine($"Failed to read entries: {readRes.ErrorMessage}");
                if (debug && readRes.Diagnostic != null) Console.WriteLine(readRes.Diagnostic.ToString());
            }
            else
            {
                foreach (LogEntry e in readRes.Value!)
                {
                    Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(export))
        {
            OperationResult<Unit> res = processor.ExportFileResult(export);
            if (res.IsSuccess)
            {
                Console.WriteLine($"Exported: {export}");
            }
            else
            {
                Console.WriteLine($"Export failed: {res.ErrorMessage}");
                if (debug && res.Diagnostic != null) Console.WriteLine(res.Diagnostic.ToString());
            }
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

static async Task RunInteractive(ILogProcessor processor, bool debug, ServiceProvider serviceProvider)
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

static void PrintSalmonRunScore(SalmonRunScoreResult res)
{
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

static void PrintWinterFieldDayScore(WinterFieldDayScoreResult res)
{
    string headerBorder = "+----------------------------------------+";
    string headerTitle = "|       Winter Field Day Score Report   |";
    Console.WriteLine(headerBorder);
    Console.WriteLine(headerTitle);
    Console.WriteLine(headerBorder);
    Console.WriteLine($" Final score : {res.FinalScore}");
    Console.WriteLine($" QSO points  : {res.QsoPoints}");
    Console.WriteLine($" Phone QSOs  : {res.PhoneQsos} x 1pt = {res.PhoneQsos}");
    Console.WriteLine($" CW/Digital  : {res.CwDigitalQsos} x 2pts = {res.CwDigitalQsos * 2}");
    Console.WriteLine("------------------------------------------");
    
    int innerWidth = Math.Max(10, headerBorder.Length - 2);
    ReportRenderer.PrintWrappedList("Station Categories", res.UniqueStationCategories, innerWidth, 2, false, res.UniqueStationCategories.Count.ToString());
    ReportRenderer.PrintWrappedList("Locations", res.UniqueLocations, innerWidth, 2, false, res.UniqueLocations.Count.ToString());
    
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

static void ConfigureServices(ServiceCollection services)
{
    // Register contest registry and detection services
    services.AddSingleton<IContestRegistry, ContestRegistry>();
    services.AddSingleton<IContestDetector, ContestDetector>();
    
    // Register contest-specific services using their bootstrap extensions
    services.RegisterSalmonRunContest();
    services.RegisterWinterFieldDayContest();
}
