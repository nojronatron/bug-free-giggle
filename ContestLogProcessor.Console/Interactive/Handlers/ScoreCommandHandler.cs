using System;
using ContestLogProcessor.Lib;
using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ScoreCommandHandler : ICommandHandler
{
    public string Name => "score";

    public string? HelpText => "Score the currently loaded log and show a brief report";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        CabrilloLogProcessor processor = ctx.Processor;
        IConsole console = ctx.Console;

        try
        {
            List<LogEntry> entries = processor.ReadEntries().ToList();
            if (entries.Count == 0)
            {
                console.WriteLine("No entries loaded. Import a log first using: import <path>");
                return;
            }

            CabrilloLogFile log = new CabrilloLogFile();
            if (processor.TryGetHeader("CALLSIGN", out string? call) && !string.IsNullOrWhiteSpace(call))
            {
                log.Headers["CALLSIGN"] = call!;
            }
            else
            {
                string? inferred = entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
                if (!string.IsNullOrWhiteSpace(inferred)) log.Headers["CALLSIGN"] = inferred!;
            }

            log.Entries = entries;

            SalmonRunScoringService svc = new SalmonRunScoringService();
            SalmonRunScoreResult res = svc.CalculateScore(log);

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

            int innerWidth = Math.Max(10, headerBorder.Length - 2);
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
                console.WriteLine($"  - Line {s.SourceLineNumber ?? -1}: {s.Reason}");
                if (!string.IsNullOrWhiteSpace(s.RawLine))
                {
                    console.WriteLine("     " + s.RawLine);
                }
            }

            console.WriteLine(headerBorder);
        }
        catch (Exception ex)
        {
            console.WriteLine($"Scoring failed: {ex.Message}");
            if (ctx.Debug) console.WriteLine(ex.ToString());
        }

        await Task.CompletedTask;
    }
}
