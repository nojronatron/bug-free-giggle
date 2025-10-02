# ContestLogProcessor.Console

A small command-line front-end for parsing, editing and scoring Cabrillo-format contest logs (Salmon Run rules).

## Prerequisites

- `dotnet` (SDK) targeting .NET 9

## Quick usage

There are two modes: interactive (recommended for ad-hoc editing) and non-interactive (single-command reports).

### Interactive mode

Start the app in an interactive shell:

```pwsh
dotnet run --project ContestLogProcessor.Console -- --interactive
```

Common interactive commands (type `help` after launching):

- `import <path>`        — Import a Cabrillo `.log` file into memory
- `view [pageSize]`      — View loaded entries (raw lines)
- `add`                  — Add a new QSO entry interactively
- `duplicate <id>`       — Duplicate an entry (optionally change SentSig/SentMsg/TheirCall)
- `filter <expr>`        — Search entries
- `filter-dupe <expr>`   — Search entries and optionally duplicate selected/all matches
- `help`                 — Show available interactive commands
- `score`                — Score the loaded log and print a brief report
- `export <newfile>`     — Export the in-memory log to a `.log` file
- `exit`                 — Leave interactive mode

### Non-interactive mode

Run single commands directly from your shell. Examples:

Import a file (no report):

```pwsh
dotnet run --project ContestLogProcessor.Console -- --import "C:\path\to\my.log" --list
```

Score a file and print a report (includes skipped-entry summary):

```pwsh
dotnet run --project ContestLogProcessor.Console -- --score "C:\path\to\my.log"
```

## Non-interactive Options

- `--debug`               Enable debug/stack trace output for troubleshooting
- `-i, --import <logfile>` Import a Cabrillo `.log` file
- `-e, --export <file>`   Export current in-memory log to a file
- `-l, --list`            List loaded entries (raw lines)
- `--interactive`         Start interactive shell
- `--score <logfile>`     Score a Cabrillo `.log` file and print a brief report

## Skipped entries report

When you run `score` (interactive or non-interactive) the report includes a "Skipped entries" section. It prints a total count and up to 10 sample items showing:

- line number (when available)
- reason (for example: "X-QSO (ignored)", "Unsupported Mode", "Unknown or invalid band/frequency", etc.)
- raw QSO line (indented)

If you need a full machine-readable dump of skipped items, consider piping output or exporting the log and inspecting `CabrilloLogFile.SkippedEntries` via a small helper script.

## Notes

- The console app uses the library in `ContestLogProcessor.Lib` for parsing and scoring.
- Use absolute paths when running from different working directories.

## Feedback & contributing

If you'd like the console to provide more export options (JSON/CSV dump of skipped items) or extra output formats, open an issue or submit a PR.
