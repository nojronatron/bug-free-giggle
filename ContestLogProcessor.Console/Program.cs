using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ContestLogProcessor.Lib;

var debugOption = new Option<bool>("--debug", description: "Enable debug output");
var importOption = new Option<string?>(new[] {"-i","--import"}, description: "Import a Cabrillo .log file");
var exportOption = new Option<string?>(new[] {"-e","--export"}, description: "Export current data to a .log file");
var listOption = new Option<bool>(new[] {"-l","--list"}, description: "List loaded entries (raw lines)");
var interactiveOption = new Option<bool>("--interactive", description: "Start an interactive session");

var root = new RootCommand("ContestLogProcessor CLI")
{
	debugOption,
	importOption,
	exportOption,
	listOption,
	interactiveOption
};

root.SetHandler(async (bool debug, string? import, string? export, bool list, bool interactive) =>
{
	var processor = new CabrilloLogProcessor();

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
			foreach (var e in processor.ReadEntries())
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

	var commands = new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
	{
		{ "help", async parts => {
			Console.WriteLine("Available commands:");
			Console.WriteLine("  import <path>   - Import a Cabrillo .log file into memory");
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

			var path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

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

		{ "view", async parts => {
			try
			{
				var entries = processor.ReadEntries();
				int i = 0;
				foreach (var e in entries)
				{
					i++;
					try
					{
						// Prefer canonical Cabrillo line when available
						var line = e.ToCabrilloLine();
						Console.WriteLine(line);
					}
					catch
					{
						Console.WriteLine(e.RawLine ?? e.CallSign ?? "(no data)");
					}
				}
				if (i == 0)
				{
					Console.WriteLine("(no entries loaded)");
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

			var path = string.Join(' ', parts, 1, parts.Length - 1).Trim('"');

			try
			{
				// Ensure directory exists
				var dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				// If file exists, confirm overwrite
				if (File.Exists(path))
				{
					Console.Write($"File '{path}' already exists. Overwrite? (y/N): ");
					var ans = Console.ReadLine()?.Trim();
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
		var input = Console.ReadLine();
		if (string.IsNullOrWhiteSpace(input))
		{
			continue;
		}

		var parts = SplitArguments(input);
		var cmd = parts.Length > 0 ? parts[0] : string.Empty;

		if (!commands.TryGetValue(cmd, out var handler))
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
	var parts = new List<string>();
	var current = new System.Text.StringBuilder();
	bool inQuotes = false;

	foreach (var c in input)
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
