using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

using ContestLogProcessor.Lib;

// Default page size for interactive view paging. Adjust here to change default across the program.
const int DefaultPageSize = 10;

// Helper to print a label and comma-separated list wrapped to the ASCII header width.
static void PrintWrappedList(string label, System.Collections.Generic.ICollection<string> items, int innerWidth = 40, int indent = 2, bool showCountOnLabelRight = true, string? countDisplay = null)
{
    // Print the label and count on its own line. By default the count is printed to the right of the label
    // as " {label} : {count}". Consumers may pass showCountOnLabelRight=false and optionally a custom
    // countDisplay string to instead render the label as " {label} ({countDisplay}):" per the report style.
    if (showCountOnLabelRight)
    {
        Console.WriteLine($" {label} : {items.Count}");
    }
    else
    {
        string display = countDisplay ?? items.Count.ToString();
        Console.WriteLine($" {label} ({display}):");
    }

    if (items.Count == 0) return;

    string joined = string.Join(", ", items);
    int available = Math.Max(10, innerWidth - indent); // ensure we have some room
    string prefix = new string(' ', indent);

    string remaining = joined.Trim();
    while (!string.IsNullOrEmpty(remaining))
    {
        if (remaining.Length <= available)
        {
            Console.WriteLine(prefix + remaining);
            break;
        }

        // Try to break at the last ", " before the available limit so we don't split tokens
        int cut = remaining.LastIndexOf(", ", available);
        int take;
        if (cut == -1)
        {
            // Nothing to split on - force break
            take = available;
        }
        else
        {
            take = cut + 2; // include the comma and following space
        }

        string part = remaining.Substring(0, take).TrimEnd();
        Console.WriteLine(prefix + part);
        remaining = remaining.Substring(take).TrimStart();
    }
}

Option<bool> debugOption = new Option<bool>("--debug", description: "Enable debug output");
Option<string?> importOption = new Option<string?>(new[] { "-i", "--import" }, description: "Import a Cabrillo .log file");
importOption.ArgumentHelpName = "logfile";

Option<string?> exportOption = new Option<string?>(new[] { "-e", "--export" }, description: "Export current data to a .log file");
exportOption.ArgumentHelpName = "newfile";
Option<bool> listOption = new Option<bool>(new[] { "-l", "--list" }, description: "List loaded entries (raw lines)");
Option<bool> interactiveOption = new Option<bool>("--interactive", description: "Start an interactive session");
Option<string?> scoreOption = new Option<string?>("--score", description: "Score a Cabrillo .log file and print a brief report");
scoreOption.ArgumentHelpName = "logfile";

RootCommand root = new RootCommand("ContestLogProcessor CLI - parse, edit and export Cabrillo v3 ham contest logs. Use --score <logfile> to score a Cabrillo .log file non-interactively.")
{
    debugOption,
    importOption,
    exportOption,
    listOption,
    interactiveOption,
    scoreOption
};

// Non-interactive bulk update command for scripting: update or duplicate all entries from a logfile
Option<string> bulkFileOption = new Option<string>(new[] { "-f", "--file" }, description: "Input Cabrillo .log file path") { IsRequired = true };
Option<string> bulkFieldOption = new Option<string>(new[] { "-F", "--field" }, description: "Field to update: SentSig | SentMsg | TheirCall") { IsRequired = true };
Option<string> bulkValueOption = new Option<string>(new[] { "-v", "--value" }, description: "New value to set for the selected field") { IsRequired = true };
Option<bool> bulkDuplicateOption = new Option<bool>(new[] { "-d", "--duplicate" }, description: "If set, duplicate every entry and apply the change to the duplicate; otherwise update entries in-place") { IsRequired = false };

Command bulkCmd = new Command("bulk-update", "Perform a non-interactive bulk update or bulk-duplicate of a Cabrillo log file and write the result to a new .log file with timestamp")
{
    bulkFileOption,
    bulkFieldOption,
    bulkValueOption,
    bulkDuplicateOption
};

bulkCmd.SetHandler((string file, string field, string value, bool duplicate, bool debug) =>
{
    try
    {
        if (!File.Exists(file))
        {
            Console.WriteLine($"File not found: {file}");
            return;
        }

        CabrilloLogProcessor proc = new CabrilloLogProcessor();
        proc.ImportFile(file);

        // Map field string to enum
        if (string.IsNullOrWhiteSpace(field))
        {
            Console.WriteLine("Field must be specified.");
            return;
        }
            List<LogEntry> entries = proc.ReadEntries().ToList();

            // Map entries to the original input file QSO line numbers so messages avoid exposing internal Ids.
            string[] allLines = File.ReadAllLines(file);
            List<int> qsoLineNumbers = new List<int>();
            for (int i = 0; i < allLines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(allLines[i]) && allLines[i].TrimStart().StartsWith("QSO:", StringComparison.OrdinalIgnoreCase))
                {
                    // store 1-based file line numbers
                    qsoLineNumbers.Add(i + 1);
                }
            }

            // Build mapping of entry id -> src file line number (best-effort, relies on import order matching QSO lines order)
            Dictionary<string, int> entryLineMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                int lineNo = (i < qsoLineNumbers.Count) ? qsoLineNumbers[i] : -1;
                entryLineMap[entries[i].Id] = lineNo;
            }

        ILogProcessor.DuplicateField df = field.Trim().ToLowerInvariant() switch
        {
            "sentsig" => ILogProcessor.DuplicateField.SentSig,
            "sentmsg" => ILogProcessor.DuplicateField.SentMsg,
            "theircall" => ILogProcessor.DuplicateField.TheirCall,
            _ => ILogProcessor.DuplicateField.None
        };

        if (df == ILogProcessor.DuplicateField.None)
        {
            Console.WriteLine($"Unknown field: {field}. Allowed values: SentSig, SentMsg, TheirCall");
            return;
        }

        if (!duplicate)
        {
            // Update all entries in-place. Abort on any error.
            foreach (LogEntry e in entries)
            {
                bool ok = proc.UpdateEntry(e.Id, entry =>
                {
                    switch (df)
                    {
                        case ILogProcessor.DuplicateField.SentSig:
                            if (entry.SentExchange == null) entry.SentExchange = new Exchange();
                            entry.SentExchange.SentSig = value;
                            break;
                        case ILogProcessor.DuplicateField.SentMsg:
                            if (entry.SentExchange == null) entry.SentExchange = new Exchange();
                            entry.SentExchange.SentMsg = value;
                            break;
                        case ILogProcessor.DuplicateField.TheirCall:
                            entry.TheirCall = value;
                            break;
                    }
                });

                if (!ok)
                {
                        int ln = entryLineMap.ContainsKey(e.Id) ? entryLineMap[e.Id] : -1;
                        if (ln > 0)
                        {
                            Console.WriteLine($"Failed to update entry at input file line {ln}. Aborting.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to update entry (unknown source line). Aborting.");
                        }
                    return;
                }
            }
        }
        else
        {
            // Duplicate each existing entry and apply the change to the duplicate
            foreach (LogEntry e in entries)
            {
                proc.DuplicateEntry(e.Id, df, value);
            }
        }

        // Build output filename in same directory with timestamp
        string dir = Path.GetDirectoryName(file) ?? ".";
        string baseName = Path.GetFileNameWithoutExtension(file);
        string stamp = DateTime.Now.ToString("ddMMM-HHmm");
        string outPath = Path.Combine(dir, $"{baseName}-{stamp}.log");

        // Ensure directory exists
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        proc.ExportFile(outPath);
        Console.WriteLine($"Wrote output file: {outPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Bulk update failed: {ex.Message}");
        if (debug) Console.WriteLine(ex.ToString());
    }
}, bulkFileOption, bulkFieldOption, bulkValueOption, bulkDuplicateOption, debugOption);

root.AddCommand(bulkCmd);

root.SetHandler(async (bool debug, string? import, string? export, bool list, bool interactive, string? score) =>
{
    CabrilloLogProcessor processor = new CabrilloLogProcessor();

    // If interactive, start an interactive command loop and return
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

        // Non-interactive score option: import the given file and print the score report
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

                    // Build minimal CabrilloLogFile for scoring
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

                    // Print same formatted report as interactive mode
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

                    int innerWidth = Math.Max(10, headerBorder.Length - 2); // exclude the border chars
                    PrintWrappedList("Washington Counties", res.UniqueWashingtonCounties, innerWidth, 2, false, res.UniqueWashingtonCounties.Count.ToString());
                    PrintWrappedList("US States", res.UniqueUSStates, innerWidth, 2, false, res.UniqueUSStates.Count.ToString());
                    PrintWrappedList("Canadian Provinces", res.UniqueCanadianProvinces, innerWidth, 2, false, res.UniqueCanadianProvinces.Count.ToString());
                    PrintWrappedList("DXCC Entities", res.UniqueDxccEntities, innerWidth, 2, false, $"{res.UniqueDxccEntities.Count} / 10");

                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine($" Skipped entries: {res.SkippedEntries.Count}");
                    int show = Math.Min(10, res.SkippedEntries.Count);
                    for (int i = 0; i < show; i++)
                    {
                        SkippedEntryInfo s = res.SkippedEntries[i];
                        Console.WriteLine($"  - Line {s.SourceLineNumber ?? -1}: {s.Reason}");
                        if (!string.IsNullOrWhiteSpace(s.RawLine))
                        {
                            Console.WriteLine("     " + s.RawLine);
                        }
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

    Dictionary<string, Func<string[], Task>> commands =
        new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
    {
        { "help", async parts => {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  import <path>           - Import a Cabrillo .log file into memory");
            Console.WriteLine("  add                     - Add a new log entry interactively (TheirCall is required)");
            Console.WriteLine("  view [pageSize]         - View loaded log entries in canonical format; optional page size");
            Console.WriteLine("  filter <text>           - List entries matching text; you can pick one to duplicate");
            Console.WriteLine("  duplicate --filter \"text\" - Duplicate selected entry(s). The console will prompt");
            Console.WriteLine("  export <filepath>       - Export current in-memory log to <filepath>.log");
            Console.WriteLine("  score                   - Score the currently loaded log and show a brief report");
            Console.WriteLine("  exit                    - Exit interactive session");
            Console.WriteLine("  help                    - Show this help message");
            await Task.CompletedTask;
        }},

        { "filter", async parts => {
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: filter <text>");
                return;
            }

            string filter = string.Join(' ', parts, 1, parts.Length - 1);

            System.Collections.Generic.List<LogEntry> matches = processor.ReadEntries().Where(e =>
                (!string.IsNullOrWhiteSpace(e.CallSign) && e.CallSign.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(e.RawLine) && e.RawLine.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                e.ToCabrilloLine().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine("No matches found for filter.");
                return;
            }

            Console.WriteLine($"Found {matches.Count} matches. List:");
            for (int i = 0; i < matches.Count; i++)
            {
                Console.WriteLine($"[{i}] {matches[i].ToCabrilloLine()}");
            }

            Console.Write("Enter index to duplicate, 'all' to duplicate all matches, or 'cancel': ");
            string? choice = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(choice))
            {
                Console.WriteLine("Cancelled.");
                return;
            }

            // Build targets list of entry Ids to duplicate
            List<string> targets = new List<string>();

            if (choice.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                targets.AddRange(matches.Select(m => m.Id));
            }
            else if (int.TryParse(choice, out int chosen) && chosen >= 0 && chosen < matches.Count)
            {
                targets.Add(matches[chosen].Id);
            }
            else
            {
                Console.WriteLine("Unknown selection. Cancelled.");
                return;
            }

            if (targets.Count == 0)
            {
                Console.WriteLine("No targets selected.");
                return;
            }

            // Prompt for field and new value once (match duplicate command behavior)
            Console.WriteLine("Choose field to change on duplicate (leave blank for no change):");
            Console.WriteLine("  1) SentSig");
            Console.WriteLine("  2) SentMsg");
            Console.WriteLine("  3) TheirCall");
            Console.Write("Enter 1/2/3 or press Enter to skip: ");
            string? fld = Console.ReadLine();
            ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None;
            if (!string.IsNullOrWhiteSpace(fld))
            {
                field = fld.Trim() switch
                {
                    "1" => ILogProcessor.DuplicateField.SentSig,
                    "2" => ILogProcessor.DuplicateField.SentMsg,
                    "3" => ILogProcessor.DuplicateField.TheirCall,
                    _ => ILogProcessor.DuplicateField.None
                };
            }

            string? newValue = null;
            if (field != ILogProcessor.DuplicateField.None)
            {
                Console.Write($"Enter new value for {field}: ");
                newValue = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(newValue))
                {
                    Console.WriteLine("No new value provided. Cancelled.");
                    return;
                }
            }

            foreach (string targetId in targets)
            {
                try
                {
                    processor.DuplicateEntry(targetId, field, newValue);
                    Console.WriteLine("Duplicated entry.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Duplicate failed: {ex.Message}");
                }
            }
            await Task.CompletedTask;
        }},

        { "import", async parts => {
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: import <path>");
                return;
            }

            string path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"File not found: {path}");
                    return;
                }
                processor.ImportFile(path);
                Console.WriteLine($"Imported: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import failed: {ex.Message}");
                if (debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            await Task.CompletedTask;
        }},

        { "add", async parts => {
            // Interactive add - prompt user for basic QSO fields
            Console.WriteLine("Adding a new log entry. Press Enter to skip optional fields.");

            Console.Write("Frequency: ");
            string? frequency = Console.ReadLine();

            Console.Write("Mode: ");
            string? mode = Console.ReadLine();

            Console.Write("Date (yyyy-MM-dd): ");
            string? dateStr = Console.ReadLine();

            Console.Write("Time (HHmm): ");
            string? timeStr = Console.ReadLine();

            // If a log file is loaded and contains a CALLSIGN header, use it as the default my-call.
            string? call = null;
            if (processor.TryGetHeader("CALLSIGN", out string? headerCall) && !string.IsNullOrWhiteSpace(headerCall))
            {
                call = headerCall;
                Console.WriteLine($"Using CALLSIGN from loaded log headers: {call}");
            }
            else
            {
                Console.Write("CallSign: ");
                call = Console.ReadLine();
            }

            // TheirCall is required for interactive adds — prompt until a non-empty value is provided.
            string? theirCallInput = null;
            while (true)
            {
                Console.Write("TheirCall: ");
                theirCallInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(theirCallInput)) break;
                Console.WriteLine("TheirCall is required for added entries. Please enter the other station's callsign.");
            }

            DateTime qsoDateTime = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(dateStr) && !string.IsNullOrWhiteSpace(timeStr))
            {
                DateTime.TryParse(dateStr + " " + timeStr, out qsoDateTime);
            }

            // Simple exchanges as single strings — the library can parse them further if needed
            Console.Write("Sent exchange (space-separated tokens): ");
            string? sentEx = Console.ReadLine();

            Console.Write("Received exchange (space-separated tokens): ");
            string? recvEx = Console.ReadLine();

            // Build LogEntry
            ContestLogProcessor.Lib.LogEntry newEntry = new ContestLogProcessor.Lib.LogEntry
            {
                RawLine = null,
                Frequency = string.IsNullOrWhiteSpace(frequency) ? null : frequency,
                Mode = string.IsNullOrWhiteSpace(mode) ? null : mode,
                QsoDateTime = qsoDateTime,
                CallSign = string.IsNullOrWhiteSpace(call) ? null : call,
                TheirCall = string.IsNullOrWhiteSpace(theirCallInput) ? null : theirCallInput
            };

            if (!string.IsNullOrWhiteSpace(sentEx))
            {
                newEntry.SentExchange = new ContestLogProcessor.Lib.Exchange { SentSig = sentEx };
            }

            if (!string.IsNullOrWhiteSpace(recvEx))
            {
                newEntry.ReceivedExchange = new ContestLogProcessor.Lib.Exchange { ReceivedMsg = recvEx };
            }

            try
            {
                ContestLogProcessor.Lib.LogEntry stored = processor.CreateEntry(newEntry);
                Console.WriteLine($"Added entry with Id: {stored.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add entry: {ex.Message}");
            }

            await Task.CompletedTask;
        }},

        { "view", async parts => {
            try
            {
				// Optional page size: `view 50` sets page size to 50
				int pageSize = DefaultPageSize;
                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsed))
                {
                    pageSize = Math.Max(1, parsed);
                }

                List<LogEntry> entries = processor.ReadEntries().ToList();
                int total = entries.Count;
                if (total == 0)
                {
                    Console.WriteLine("(no entries loaded)");
                    return;
                }

                int totalPages = (int)Math.Ceiling(total / (double)pageSize);
                int currentPage = 1;

                void PrintPage(int page)
                {
                    Console.WriteLine($"Showing page {page} of {totalPages} (page size {pageSize}) - total entries: {total}");
                    int start = (page - 1) * pageSize;
                    int end = Math.Min(start + pageSize, total);
                    for (int idx = start; idx < end; idx++)
                    {
                        LogEntry e = entries[idx];
                        try
                        {
                            Console.WriteLine(e.ToCabrilloLine());
                        }
                        catch
                        {
                            Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
                        }
                    }
                }

                PrintPage(currentPage);

                while (true)
                {
                    Console.Write("[n]ext, [p]rev, [#] goto page, [a]ll, [q]uit > ");
                    string? nav = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(nav))
                    {
                        continue;
                    }
                    nav = nav.Trim();

                    if (nav.Equals("n", StringComparison.OrdinalIgnoreCase) || nav.Equals("next", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentPage < totalPages)
                        {
                            currentPage++;
                            PrintPage(currentPage);
                        }
                        else
                        {
                            Console.WriteLine("Already at last page.");
                        }
                        continue;
                    }

                    if (nav.Equals("p", StringComparison.OrdinalIgnoreCase) || nav.Equals("prev", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentPage > 1)
                        {
                            currentPage--;
                            PrintPage(currentPage);
                        }
                        else
                        {
                            Console.WriteLine("Already at first page.");
                        }
                        continue;
                    }

                    if (nav.Equals("a", StringComparison.OrdinalIgnoreCase) || nav.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
						// Print all entries after current page
						for (int idx = 0; idx < total; idx++)
                        {
                            LogEntry e = entries[idx];
                            try
                            {
                                Console.WriteLine(e.ToCabrilloLine());
                            }
                            catch
                            {
                                Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
                            }
                        }
                        continue;
                    }

                    if (nav.Equals("q", StringComparison.OrdinalIgnoreCase) || nav.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

					// Try to parse a page number
					if (int.TryParse(nav, out int gotoPage))
                    {
                        if (gotoPage >= 1 && gotoPage <= totalPages)
                        {
                            currentPage = gotoPage;
                            PrintPage(currentPage);
                        }
                        else
                        {
                            Console.WriteLine($"Page out of range (1-{totalPages}).");
                        }
                        continue;
                    }

                    Console.WriteLine("Unknown navigation command. Use n/p/#/a/q or 'help'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"View failed: {ex.Message}");
                if (debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            await Task.CompletedTask;
        }},

        { "score", async parts => {
            // Score the currently loaded log using SalmonRunScoringService
            try
            {
                // Ensure we have entries loaded
                List<LogEntry> entries = processor.ReadEntries().ToList();
                if (entries.Count == 0)
                {
                    Console.WriteLine("No entries loaded. Import a log first using: import <path>");
                    return;
                }

                // Build a minimal CabrilloLogFile with at least the CALLSIGN header (required by the scoring service)
                CabrilloLogFile log = new CabrilloLogFile();
                if (processor.TryGetHeader("CALLSIGN", out string? call) && !string.IsNullOrWhiteSpace(call))
                {
                    log.Headers["CALLSIGN"] = call!;
                }
                else
                {
                    // If the loaded file didn't include a CALLSIGN header, try to infer from the first entry's CallSign
                    string? inferred = entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CallSign))?.CallSign;
                    if (!string.IsNullOrWhiteSpace(inferred))
                    {
                        log.Headers["CALLSIGN"] = inferred!;
                    }
                }

                log.Entries = entries;

                // Run scoring service
                SalmonRunScoringService svc = new SalmonRunScoringService();
                SalmonRunScoreResult res = svc.CalculateScore(log);

                // Nicely format output with minimal ASCII art
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

                // Multiplier breakdown
                int innerWidth = Math.Max(10, headerBorder.Length - 2);
                PrintWrappedList("Washington Counties", res.UniqueWashingtonCounties, innerWidth, 2, false, res.UniqueWashingtonCounties.Count.ToString());

                PrintWrappedList("US States", res.UniqueUSStates, innerWidth, 2, false, res.UniqueUSStates.Count.ToString());

                PrintWrappedList("Canadian Provinces", res.UniqueCanadianProvinces, innerWidth, 2, false, res.UniqueCanadianProvinces.Count.ToString());

                PrintWrappedList("DXCC Entities", res.UniqueDxccEntities, innerWidth, 2, false, $"{res.UniqueDxccEntities.Count} / 10");

                // Skipped entries summary (show up to 10)
                Console.WriteLine("------------------------------------------");
                Console.WriteLine($" Skipped entries: {res.SkippedEntries.Count}");
                int show = Math.Min(10, res.SkippedEntries.Count);
                for (int i = 0; i < show; i++)
                {
                    SkippedEntryInfo s = res.SkippedEntries[i];
                    Console.WriteLine($"  - Line {s.SourceLineNumber ?? -1}: {s.Reason}");
                    if (!string.IsNullOrWhiteSpace(s.RawLine))
                    {
                        Console.WriteLine("     " + s.RawLine);
                    }
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

            await Task.CompletedTask;
        }},

        { "duplicate", async parts => {
            // Modes:
            // duplicate --index <n>
            // duplicate --filter "text"

            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: duplicate --index <n> | --filter \"text\"");
                return;
            }

            List<string> targets = new List<string>();

            if (parts[1].Equals("--index", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
            {
                if (!int.TryParse(parts[2], out int idx))
                {
                    Console.WriteLine("Invalid index.");
                    return;
                }

                System.Collections.Generic.List<LogEntry> all = processor.ReadEntries(orderBy: e => e.QsoDateTime).ToList();
                if (idx < 0 || idx >= all.Count)
                {
                    Console.WriteLine($"Index out of range (0-{all.Count - 1}).");
                    return;
                }
                targets.Add(all[idx].Id);
            }
            else if (parts[1].Equals("--filter", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
            {
                string filter = parts[2];
                System.Collections.Generic.List<LogEntry> matches = processor.ReadEntries().Where(e =>
                    (!string.IsNullOrWhiteSpace(e.CallSign) && e.CallSign.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(e.RawLine) && e.RawLine.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    e.ToCabrilloLine().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();

                if (matches.Count == 0)
                {
                    Console.WriteLine("No matches found for filter.");
                    return;
                }

                Console.WriteLine($"Found {matches.Count} matches. List:");
                for (int i = 0; i < matches.Count; i++)
                {
                    Console.WriteLine($"[{i}] {matches[i].ToCabrilloLine()}");
                }

                Console.Write("Enter index to duplicate, 'all' to duplicate all matches, or 'cancel': ");
                string? choice = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(choice))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }

                if (choice.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    targets.AddRange(matches.Select(m => m.Id));
                }
                else if (int.TryParse(choice, out int chosen) && chosen >= 0 && chosen < matches.Count)
                {
                    targets.Add(matches[chosen].Id);
                }
                else
                {
                    Console.WriteLine("Unknown selection. Cancelled.");
                    return;
                }
            }
            else
            {
                // raw id selection is not supported — require --index or --filter to pick entries
                Console.WriteLine("Use --index <n> or --filter \"text\" to select entries to duplicate.");
                return;
            }

            if (targets.Count == 0)
            {
                Console.WriteLine("No targets selected.");
                return;
            }

            // Show a short summary of what will be duplicated (help the user identify targets)
            if (targets.Count == 1)
            {
                try
                {
                    LogEntry? original = processor.GetEntryById(targets[0]);
                    if (original != null)
                    {
                        Console.WriteLine("About to duplicate the following entry:");
                        try { Console.WriteLine(original.ToCabrilloLine()); }
                        catch { Console.WriteLine(original.RawLine ?? original.CallSign ?? "(no data)"); }
                    }
                }
                catch { /* ignore lookup errors here */ }
            }
            else
            {
                Console.WriteLine($"About to duplicate {targets.Count} entries.");
                int listCount = Math.Min(10, targets.Count);
                for (int i = 0; i < listCount; i++)
                {
                    try
                    {
                        LogEntry? o = processor.GetEntryById(targets[i]);
                        if (o != null)
                        {
                            try { Console.WriteLine($"[{i}] {o.ToCabrilloLine()}"); }
                            catch { Console.WriteLine($"[{i}] {o.RawLine ?? o.CallSign ?? "(no data)"}"); }
                        }
                    }
                    catch { }
                }
                if (targets.Count > listCount)
                {
                    Console.WriteLine($"...and {targets.Count - listCount} more entries (not shown)");
                }
            }

            // Prompt for field and new value once
            Console.WriteLine("Choose field to change on duplicate (leave blank for no change):");
            Console.WriteLine("  1) SentSig");
            Console.WriteLine("  2) SentMsg");
            Console.WriteLine("  3) TheirCall");
            Console.Write("Enter 1/2/3 or press Enter to skip: ");
            string? fld = Console.ReadLine();
            ILogProcessor.DuplicateField field = ILogProcessor.DuplicateField.None;
            if (!string.IsNullOrWhiteSpace(fld))
            {
                field = fld.Trim() switch
                {
                    "1" => ILogProcessor.DuplicateField.SentSig,
                    "2" => ILogProcessor.DuplicateField.SentMsg,
                    "3" => ILogProcessor.DuplicateField.TheirCall,
                    _ => ILogProcessor.DuplicateField.None
                };
            }

            string? newValue = null;
            if (field != ILogProcessor.DuplicateField.None)
            {
                Console.Write($"Enter new value for {field}: ");
                newValue = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(newValue))
                {
                    Console.WriteLine("No new value provided. Cancelled.");
                    return;
                }
            }

            foreach (string targetId in targets)
            {
                try
                {
                    processor.DuplicateEntry(targetId, field, newValue);
                    Console.WriteLine("Duplicated entry.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Duplicate failed: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }},

        { "export", async parts => {
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: export <path>");
                return;
            }

            string path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

            // Normalize filename to ensure .log extension is used for overwrite checks
            string checkPath = path;
            if (!checkPath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                checkPath += ".log";
            }

            try
            {
				// Ensure directory exists
				string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // If file exists (after applying .log extension), confirm overwrite
                if (File.Exists(checkPath))
                {
                    Console.Write($"File '{checkPath}' already exists. Overwrite? (y/N): ");
                    string? ans = Console.ReadLine()?.Trim();
                    if (!string.Equals(ans, "y", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ans, "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Export cancelled.");
                        return;
                    }
                }

                processor.ExportFile(path);
                Console.WriteLine($"Exported: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
                if (debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            await Task.CompletedTask;
        }},

        { "exit", async parts => {
			// Signal termination by throwing a special exception handled below
			await Task.CompletedTask;
            throw new OperationCanceledException();
        }}
    };

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        string[] parts = SplitArguments(input);
        string cmd = parts.Length > 0 ? parts[0] : string.Empty;

        if (!commands.TryGetValue(cmd, out Func<string[], Task>? handler))
        {
            Console.WriteLine($"Unknown command: {cmd}. Type 'help' to see available commands.");
            continue;
        }

        try
        {
            await handler(parts);
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

// Very small argument splitter that respects quoted paths
static string[] SplitArguments(string input)
{
    List<string> parts = new List<string>();
    System.Text.StringBuilder current = new System.Text.StringBuilder();
    bool inQuotes = false;

    foreach (char c in input)
    {
        if (c == '"')
        {
            inQuotes = !inQuotes;
            continue;
        }

        if (char.IsWhiteSpace(c) && !inQuotes)
        {
            if (current.Length > 0)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            continue;
        }

        current.Append(c);
    }

    if (current.Length > 0)
    {
        parts.Add(current.ToString());
    }

    return parts.ToArray();
}
