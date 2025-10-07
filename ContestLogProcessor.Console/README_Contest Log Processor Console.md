# ContestLogProcessor.Console

A small command-line front-end for parsing, editing and scoring Cabrillo-format contest logs (Salmon Run rules).

## Prerequisites

- DotNET (SDK) targeting .NET 9

Docker (optional): `docker run --rm -it --name contestlog_local_run contestlogprocessor:local --interactive`

## Quick usage

There are two modes:

- Interactive: Recommended for ad-hoc editing
- Non-interactive: Single-command reports

Docker: See [Docker](#docker) later in this readme.

## Docker

Build

```pwsh
docker build -t contestlogprocessor:local -f "ContestLogProcessor.Console/Dockerfile" .
```

### Drive Sharing

In Windows, Docker Desktop must be configured to allow this (Settings -> Resources -> File Sharing).

Path style: Use forward slashes `/` instead of back slashes `\` for the best file-pathing experience.

Quoted Paths: If a path contains a space, surround the path in quotations.

#### To Write Files Back To The Host

This project's Dockerfile uses a non-root USER (`ARG APP_UID=1000` then `USER $APP_UID`). This could block export (wriging) files in some cases.

- Prefer mounting with appropriate permissions
- Linux: Build the Container with a UID matching the host using `--build-arg APP_UID=$(id -u)`
- Map a narrow directory tree (or single directory) rather than an entire repo or volume/drive

Interactive Import Example:

```pwsh
docker run --rm -it -v 'C:/ContestLogs:/data/logs:ro' contestlogprocessor:local --interactive
# inside interactive CLI:
import /data/logs/test-cabrillo.log
```

Non-Interactive Import Example:

```pwsh
docker run --rm -v 'C:/ContestLogs:/data/logs:ro' contestlogprocessor:local --import /data/logs/test-cabrillo.log
```

Alternative Copy Into Running Container Example:

```pwsh
docker cp 'C:/ContestLogs/cabrillo.log' <containerId>:/tmp/test-cabrillo.log
# then inside container:
import /tmp/test-cabrillo.log
```

### Interactive Shell - Docker

Start the app in an interactive shell:

```pwsh
dotnet run --project ContestLogProcessor.Console -- --interactive
```

When running within a Docker container you might need to mount a drive or copy files.

```pwsh
# Host folder (adjust your path)
$hostFolder = 'C:/ContestLogs'

# Run interactive container with the host folder mounted read-only at /data/logs
docker run --rm -it -v "$hostFolder:/data/logs:ro" contestlogprocessor:local --interactive

import /data/logs/test-cabrillo.log
```

Copy a file into a started container:

```pwsh
# get container id (if running detached)
docker ps

# copy the host file into /tmp inside the container
docker cp 'F:/Ham Contest Logs/cabrillo.log' <containerId>:/tmp/test-cabrillo.log
```

List files inside the container (good for troubleshooting):

```pwsh
docker run --rm -it -v 'F:/Ham Contest Logs:/data/logs:ro' contestlogprocessor:local sh -c "ls -la /data/logs"
```

### Non-Interactive Shell - Docker

One-shot:

```pwsh
docker run --rm -v 'F:/Ham Contest Logs:/data/logs:ro' contestlogprocessor:local --import /data/logs/test-cabrillo.log
```

Copy a file into a started container:

```pwsh
# get container id (if running detached)
docker ps

# copy the host file into /tmp inside the container
docker cp 'F:/Ham Contest Logs/cabrillo.log' <containerId>:/tmp/test-cabrillo.log
```

### Interactive mode

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
