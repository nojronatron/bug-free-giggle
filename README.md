# Contest Log Processor README

This project was created with the goal of processing ham radio communication logs prior to submitting to a contest sponsor.

A vast majority of code was was written by GitHub Copilot in Agent mode, using GPT-4.1 and GPT-5mini LLMs. Nojronatron directly wrote the initial solution structure, instruction files for GH Copilot, and some README files (or portions thereof).

## Features

- Interactive and non-interactive consoles for human review or scripted use
- Supports Salmon Run (WA State QSO Party 2025) and Winter Field Day (2026)
- Contest detection via valid headers
- Full Cabrillo v.3 support (import, view, score, export)
- Minimal ADIF support (import only)
- Malformed log entry tracking
- Format (Cabrillo v.3) validation on import

See [Change Log](changelog.md) for additional details.

## Status

[![Format and Build](https://github.com/nojronatron/bug-free-giggle/actions/workflows/format-and-build.yml/badge.svg)](https://github.com/nojronatron/bug-free-giggle/actions/workflows/format-and-build.yml)

## Publish Bits For Use

Prerequisites:

- dotnet 10 sdk or newer
- Windows 10/11, or Ubuntu 22.x/24.x

Process:

1. Clone repo to your target environment: `git clone https://github.com/nojronatron/bug-free-giggle.git`
2. Change directory to repo root: `cd ContestLogProcessor`
3. Build project: `dotnet build ContestLogProcessor.slnx -c Debug -v Detailed`
4. Run all tests: `dotnet test .\ContestLogProcessor.slnx -c Debug`
5. Deploy published executable: `dotnet publish ContestLogProcessor.Console\ContestLogProcessor.Console.csproj -c Release --self-contained --output {target-directory} -v Detailed`

## Usage

Interactive and non-interactive mode have identical features.

Use interactive mode for manually importing, viewing, scoring, and exporting a cabrillo v.3 file.

Use non-interactive mode to simply score a cab v.3 log, or to script a set of actions for autonomous processing.

### Non-Interactive Terminal

.\ContestLogProcessor.Console.exe --help

Description:
  ContestLogProcessor CLI - parse, edit and export Cabrillo v3 ham contest logs.

Usage:
  ContestLogProcessor.Console [options]

Options:
  `--debug`                 Enable debug output
  `-i, --import <logfile>`  Import a Cabrillo .log file
  `-e, --export <newfile>`  Export current data to a .log file
  `-l, --list`              List loaded entries (raw lines)
  `--interactive`           Start an interactive session
  `--score <logfile>`       Score a Cabrillo .log file and print a brief report
  `--version`               Show version information
  `-?, -h, --help`          Show help and usage information

## Usage - Interactive Terminal

.\ContestLogProcessor.Console.exe --interactive --help

Description:
  ContestLogProcessor CLI - parse, edit and export Cabrillo v3 ham contest logs.

Usage:
  ContestLogProcessor.Console [options]

Options:
  `--debug`                 Enable debug output
  `-i, --import <logfile>`  Import a Cabrillo .log file
  `-e, --export <newfile>`  Export current data to a .log file
  `-l, --list`              List loaded entries (raw lines)
  `--interactive`           Start an interactive session
  `--score <logfile>`       Score a Cabrillo .log file and print a brief report
  `--version`               Show version information
  `-?, -h, --help`          Show help and usage information

## Future

- [ ] Link other README files from this one
- [ ] Improve scoring for Salmon Run event
- [ ] View `filter` output using band labels instead of frequencies
- [ ] Add ability to sort `view` or `filter` output by specific log entry token(s) (at least one, up to 2)
- [ ] Add `--whatif` flag to explin what would be done, so user can decide whether to execute command chain or not
- [ ] Enable non-interactive mode to accept and process chained commands for example `import "<filepath>" --score --export --use-band "<path>/<filename>"`
- [ ] Allow user to overwrite score in imported log using scoring calculation from this utility
- [ ] Make edit operations transactional so a previous entry can be reverted with a simple 'undo' flag
- [ ] Full ADIF logfile support
