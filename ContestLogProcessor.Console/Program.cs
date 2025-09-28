using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

using ContestLogProcessor.Lib;

// Default page size for interactive view paging. Adjust here to change default across the program.
const int DefaultPageSize = 10;

Option<bool> debugOption = new Option<bool>("--debug", description: "Enable debug output");
Option<string?> importOption = new Option<string?>(new[] { "-i", "--import" }, description: "Import a Cabrillo .log file");
Option<string?> exportOption = new Option<string?>(new[] { "-e", "--export" }, description: "Export current data to a .log file");
Option<bool> listOption = new Option<bool>(new[] { "-l", "--list" }, description: "List loaded entries (raw lines)");
Option<bool> interactiveOption = new Option<bool>("--interactive", description: "Start an interactive session");

RootCommand root = new RootCommand("ContestLogProcessor CLI")
{
    debugOption,
    importOption,
    exportOption,
    listOption,
    interactiveOption
};

root.SetHandler(async (bool debug, string? import, string? export, bool list, bool interactive) =>
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
}, debugOption, importOption, exportOption, listOption, interactiveOption);

return await root.InvokeAsync(args);

static async Task RunInteractive(CabrilloLogProcessor processor, bool debug)
{
    Console.WriteLine("Entering interactive mode. Type 'help' for available commands.");

    Dictionary<string, Func<string[], Task>> commands =
        new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
    {
        { "help", async parts => {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  import <path>   - Import a Cabrillo .log file into memory");
            Console.WriteLine("  add             - Add a new log entry interactively");
            Console.WriteLine("  view            - View all loaded log entries (canonical format)");
            Console.WriteLine("  export <path>   - Export current in-memory log to a Cabrillo .log file");
            Console.WriteLine("  exit            - Exit interactive session");
            Console.WriteLine("  help            - Show this help message");
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
