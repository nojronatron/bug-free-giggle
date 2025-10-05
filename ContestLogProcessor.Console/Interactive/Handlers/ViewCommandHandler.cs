using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Handlers;

public class ViewCommandHandler : ICommandHandler
{
    public string Name => "view";
    public string? HelpText => "View loaded log entries in canonical format; optional page size";

    public async Task HandleAsync(string[] parts, ICommandContext ctx)
    {
        try
        {
            int pageSize = 10;
            if (parts.Length >= 2 && int.TryParse(parts[1], out int parsed))
            {
                pageSize = Math.Max(1, parsed);
            }

            OperationResult<IEnumerable<LogEntry>> readOp = ctx.Processor.ReadEntriesResult();
            if (!readOp.IsSuccess)
            {
                ctx.Console.WriteLine($"Operation failed: {readOp.ErrorMessage}");
                if (ctx.Debug && readOp.Diagnostic != null) ctx.Console.WriteLine(readOp.Diagnostic.ToString());
                return;
            }

            List<LogEntry> entries = readOp.Value!.ToList();
            int total = entries.Count;
            if (total == 0)
            {
                ctx.Console.WriteLine("(no entries loaded)");
                return;
            }

            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            int currentPage = 1;

            void PrintPage(int page)
            {
                ctx.Console.WriteLine($"Showing page {page} of {totalPages} (page size {pageSize}) - total entries: {total}");
                int start = (page - 1) * pageSize;
                int end = Math.Min(start + pageSize, total);
                for (int idx = start; idx < end; idx++)
                {
                    LogEntry e = entries[idx];
                    try
                    {
                        ctx.Console.WriteLine(e.ToCabrilloLine());
                    }
                    catch
                    {
                        ctx.Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
                    }
                }
            }

            PrintPage(currentPage);

            while (true)
            {
                ctx.Console.Write("[n]ext, [p]rev, [#] goto page, [a]ll, [q]uit > ");
                string? nav = await ctx.Console.ReadLineAsync();
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
                        ctx.Console.WriteLine("Already at last page.");
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
                        ctx.Console.WriteLine("Already at first page.");
                    }
                    continue;
                }

                if (nav.Equals("a", StringComparison.OrdinalIgnoreCase) || nav.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    for (int idx = 0; idx < total; idx++)
                    {
                        LogEntry e = entries[idx];
                        try
                        {
                            ctx.Console.WriteLine(e.ToCabrilloLine());
                        }
                        catch
                        {
                            ctx.Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
                        }
                    }
                    continue;
                }

                if (nav.Equals("q", StringComparison.OrdinalIgnoreCase) || nav.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (int.TryParse(nav, out int gotoPage))
                {
                    if (gotoPage >= 1 && gotoPage <= totalPages)
                    {
                        currentPage = gotoPage;
                        PrintPage(currentPage);
                    }
                    else
                    {
                        ctx.Console.WriteLine($"Page out of range (1-{totalPages}).");
                    }
                    continue;
                }

                ctx.Console.WriteLine("Unknown navigation command. Use n/p/#/a/q or 'help'.");
            }
        }
        catch (Exception ex)
        {
            ctx.Console.WriteLine($"View failed: {ex.Message}");
            if (ctx.Debug) ctx.Console.WriteLine(ex.ToString());
        }

        await Task.CompletedTask;
    }
}
