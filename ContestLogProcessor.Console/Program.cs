using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using ContestLogProcessor.Lib;

var debugOption = new Option<bool>("--debug", description: "Enable debug output");
var importOption = new Option<string?>(new[] {"-i","--import"}, description: "Import a Cabrillo .log file");
var exportOption = new Option<string?>(new[] {"-e","--export"}, description: "Export current data to a .log file");
var listOption = new Option<bool>(new[] {"-l","--list"}, description: "List loaded entries (raw lines)");

var root = new RootCommand("ContestLogProcessor CLI")
{
	debugOption,
	importOption,
	exportOption,
	listOption
};

root.SetHandler(async (bool debug, string? import, string? export, bool list) =>
{
	var processor = new CabrilloLogProcessor();
	try
	{
		if (!string.IsNullOrWhiteSpace(import))
		{
			processor.ReadFile(import);
			Console.WriteLine($"Imported: {import}");
		}

		if (list)
		{
			foreach (var e in processor.GetEntries())
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
}, debugOption, importOption, exportOption, listOption);

return await root.InvokeAsync(args);
