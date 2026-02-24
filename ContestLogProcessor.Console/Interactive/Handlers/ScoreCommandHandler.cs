using System;
using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;
using ContestLogProcessor.WinterFieldDay;
using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ScoreCommandHandler : ICommandHandler
{
    public string Name => "score";

    public string? HelpText => "Score the currently loaded log and show a brief report";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        ILogProcessor processor = ctx.Processor;
        IConsole console = ctx.Console;

        try
        {
            OperationResult<IEnumerable<LogEntry>> readOp = processor.ReadEntriesResult();
            if (!readOp.IsSuccess)
            {
                console.WriteLine($"Operation failed: {readOp.ErrorMessage}");
                if (ctx.Debug && readOp.Diagnostic != null) console.WriteLine(readOp.Diagnostic.ToString());
                return;
            }

            List<LogEntry> entries = readOp.Value!.ToList();
            if (entries.Count == 0)
            {
                console.WriteLine("No entries loaded. Import a log first using: import <path>");
                return;
            }

            CabrilloLogFile log = new CabrilloLogFile();

            // Try to access the full log file with headers if processor supports it
            if (processor is CabrilloLogProcessor cabrilloProcessor)
            {
                CabrilloLogFileSnapshot? snapshot = cabrilloProcessor.GetReadOnlyLogFile();
                if (snapshot != null)
                {
                    // Copy all headers from the snapshot
                    foreach (KeyValuePair<string, string> header in snapshot.Headers)
                    {
                        log.Headers[header.Key] = header.Value;
                    }
                }
            }

            // Fallback: Try to infer CALLSIGN from entries when header access isn't available
            if (!log.Headers.ContainsKey("CALLSIGN"))
            {
                string? inferred = entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
                if (!string.IsNullOrWhiteSpace(inferred)) log.Headers["CALLSIGN"] = inferred!;
            }

            log.Entries = entries;

            // Set up contest services (simplified approach for console app)
            ContestRegistry registry = new ContestRegistry();
            ContestDetector detector = new ContestDetector(registry);

            OperationResult<string> contestDetection = detector.DetectContestType(log);
            if (!contestDetection.IsSuccess)
            {
                console.WriteLine($"Contest detection failed: {contestDetection.ErrorMessage}");
                if (ctx.Debug && contestDetection.Diagnostic != null) console.WriteLine(contestDetection.Diagnostic.ToString());
                return;
            }

            string contestType = contestDetection.Value!;
            console.WriteLine($"Detected contest: {contestType}");

            if (contestType == "SALMON-RUN")
            {
                SalmonRunScoringService svc = new SalmonRunScoringService();
                OperationResult<SalmonRunScoreResult> scoreOp = svc.CalculateScore(log);
                if (!scoreOp.IsSuccess)
                {
                    console.WriteLine($"Scoring failed: {scoreOp.ErrorMessage}");
                    if (ctx.Debug && scoreOp.Diagnostic != null) console.WriteLine(scoreOp.Diagnostic.ToString());
                    return;
                }

                SalmonRunScoreResult res = scoreOp.Value!;
                PrintSalmonRunScore(res, console);
            }
            else if (contestType == "WFD")
            {
                WinterFieldDayScoringService svc = new WinterFieldDayScoringService();
                OperationResult<WinterFieldDayScoreResult> scoreOp = svc.CalculateScore(log);
                if (!scoreOp.IsSuccess)
                {
                    console.WriteLine($"Scoring failed: {scoreOp.ErrorMessage}");
                    if (ctx.Debug && scoreOp.Diagnostic != null) console.WriteLine(scoreOp.Diagnostic.ToString());
                    return;
                }

                WinterFieldDayScoreResult res = scoreOp.Value!;
                PrintWinterFieldDayScore(res, console);
            }
            else
            {
                console.WriteLine($"Scoring not implemented for contest type: {contestType}");
                return;
            }
        }
        catch (Exception ex)
        {
            console.WriteLine($"Scoring failed: {ex.Message}");
            if (ctx.Debug) console.WriteLine(ex.ToString());
        }

        await Task.CompletedTask;
    }

    private static void PrintSalmonRunScore(SalmonRunScoreResult res, IConsole console)
    {
        string headerBorder = "+----------------------------------------+";
        string headerTitle = "|          Salmon Run Score Report      |";
        console.WriteLine(headerBorder);
        console.WriteLine(headerTitle);
        console.WriteLine(headerBorder);
        console.WriteLine($" Final score : {res.FinalScore}");
        console.WriteLine($" QSO points  : {res.QsoPoints}");
        console.WriteLine($" Multiplier   : {res.Multiplier}");
        console.WriteLine($" W7DX bonus   : {res.W7DxBonusPoints}");
        console.WriteLine("------------------------------------------");

        // Use the same helper formatting from Program.cs style by printing simple lists
        console.WriteLine($" Washington Counties : {res.UniqueWashingtonCounties.Count}");
        console.WriteLine($" US States           : {res.UniqueUSStates.Count}");
        console.WriteLine($" Canadian Provinces  : {res.UniqueCanadianProvinces.Count}");
        console.WriteLine($" DXCC Entities       : {res.UniqueDxccEntities.Count} / 10");

        console.WriteLine("------------------------------------------");
        console.WriteLine($" Skipped entries: {res.SkippedEntries.Count}");
        int show = Math.Min(10, res.SkippedEntries.Count);
        for (int i = 0; i < show; i++)
        {
            SkippedEntryInfo s = res.SkippedEntries[i];
            foreach (string outLine in ReportRenderer.FormatSkippedEntry(s))
            {
                console.WriteLine(outLine);
            }
        }

        console.WriteLine(headerBorder);
    }

    private static void PrintWinterFieldDayScore(WinterFieldDayScoreResult res, IConsole console)
    {
        string headerBorder = "+----------------------------------------+";
        string headerTitle = "|       Winter Field Day Score Report   |";
        console.WriteLine(headerBorder);
        console.WriteLine(headerTitle);
        console.WriteLine(headerBorder);
        console.WriteLine($" Final score : {res.FinalScore}");
        console.WriteLine($" QSO points  : {res.QsoPoints}");
        console.WriteLine($" Phone QSOs  : {res.PhoneQsos} x 1pt = {res.PhoneQsos}");
        console.WriteLine($" CW/Digital  : {res.CwDigitalQsos} x 2pts = {res.CwDigitalQsos * 2}");
        console.WriteLine("------------------------------------------");

        console.WriteLine($" Station Categories : {res.UniqueStationCategories.Count}");
        console.WriteLine($" Locations          : {res.UniqueLocations.Count}");

        console.WriteLine("------------------------------------------");
        console.WriteLine($" Skipped entries: {res.SkippedEntries.Count}");
        int show = Math.Min(10, res.SkippedEntries.Count);
        for (int i = 0; i < show; i++)
        {
            SkippedEntryInfo s = res.SkippedEntries[i];
            foreach (string outLine in ReportRenderer.FormatSkippedEntry(s))
            {
                console.WriteLine(outLine);
            }
        }

        console.WriteLine(headerBorder);
    }
}
