# Project Overview

This project library will examine a supplied Cabrillo formatted amateur radio log file and provide a brief report of statistics, and enable making bulk edits such as adding entries for a "second county" of a county-line expedition.

## Folder Structure

- `/docs`: Contains solution-wide documentation.
- `/ContestLogProcessor.Console`: A .NET Console application useful for command-line operation.
- `/ContestLogProcessor.Lib`: The core, shared abstractions and implementations needed by the Console and other Projects (to be added later).
- `/ContestLogProcessor.Unittest`: xUnit test project. Add a directory to match the project to be tested (for example: `/Lib` for `ContestLogProcessor.Lib` unit tests).
- `/LogExamples`: Folder contains log file examples and should not be directly imported or copied to code nor checked in to git repository.
- `/Rules`: Additional specifications and descriptions of contest rules, cabrillo specifications, and special handling rules.

When it is time to create additional projects in this Solution:

- Follow the same naming pattern as existing projects, using "Website" for an ASP.NET Core webapp, "WebClient" for WASM, "Server" for an API server, etc

## References

- [Salmon Run Cabrillo Log Standards](../Rules/README_SalmonRunCabrilloLogStandards.md)
- [Preserve Data Import Order Assertions](../Rules/README_PreserveDataImportOrderAssertions.md)
- [Salmon Run Scoring Rules](../Rules/README_SalmonRunScoringRules.md)
- [Salmon Run Scoring](../Rules/README_SalmonRunScoring.md)

## Coding Standards 

- Use C# 13
- Name variables, methods, classes (etc) with PascalCase
- Use descriptive naming conventions that simply describe purpose of variables, methods, classes (etc)
- Code should be modular
- Prefer to use LINQ for filter, sort, and selection of data
- Use Collections with CRUD-like operations so data can be manipulated
- Prefer to use Interfaces over Abstract classes
- Library and functional code should be written so it is easy to unittest
- Web and Desktop UI Code should be written so it is easy to test with a Framework like Playwright
- Classes, Interfaces, and Structs shall be written in their own code files
- Prefer file-scoped namespaces: `namespace ContestLogProcessor.Lib;`
- Use object initializer syntax with data: `Student Student1 = new Student { FirstName = "Anony", LastName = "Mouse" };`
- Use empty initialization syntax when no initialization data is needed: `Car Subaru = new();`
- Use collection expression syntax `List<string> = []` (supported in C# 12 and later)
- If-then-else statements always use braces around their code blocks
- Use explicit types instead of var: `Backpack EmptyGenericBackpack = new();` and `Student? FoundStudent = _entries.Find(match);`

## Libraries and Frameworks

- Use .NET 9
- Only Blazor or ASP.NET Core projects should utilize HTML, CSS, and JavaScript
- Always ask before adding, updating, or removing NuGet packages
- When writing unit tests use xUnit
- Use a NuGet package like Moq to enable mocking only when necessary to implement and run a unit test
- The solution may be deployed to Docker containers for portability

## Architecture Guidelines

- Projects should depend on ContestLogProcessor.Lib for core functionality
- Projects should provide their own functionality that directly relates to their purpose and not depend on other projects besides `ContestLogProcessor.Lib`
- Projects should utilize Logging features (i.e. `<ILogger>`) and have a way to log output
- ContestLogProcessor.Lib is the core processing component and other projects are for UI, testing, or other purposes
- Each project should have a Docker container, except for `ContestLogProcessor.Lib` which is a dependency of the other projects
- The Unittest Project should NOT be Dockerized

### Exception Handling

- The Console should catch and handle exceptions, returning a message to the user with information on what went wrong and how to fix it with altered user input
- Design classes so that exceptions can be avoided where possible
- Use try-catch-finally blocks to recover from exceptions, including cancellation/asynchronous exceptions
- Exceptions that must be returned to the caller should be thrown using the `throw` keyword so that the stack trace is maintained
	- Use a finally block to clean up resources

### ContestLogProcessor.Console

- Should NOT output logging information unless a debug argument is included on the command line
- Should output logging information to the standard output (console screen) when a debug argument is included
- Should accept input in a human-readable and concise format such as: `-debug` to add logging, `-add`, `-update`, `-remove` to perform operations on the data in memory, `-export` to dump data in memory to a file, etc

### ContestLogProcessor.Lib

- Prefer synchronous functions over asynchronous
- Methods that provide API access to the Library should be public
- Methods that support functionality and processing should not be publicly accessible by dependent projects
- Abstraction should be used to hide complexity from calling methods while enabling functionality

### ContestLogProcessor.Unittest

- xUnit test project for the entire solution
- Follow the folder structure instructions
- Unit tests should be concise, focused on method functionality
- Focus unit tests on specific, basic functional goals
- Avoid writing extensive edge-case unit tests
- Use data in `./LogExamples` directory to derive realistic test case data but do not copy the data exactly

## User Interactions

### ContestLogProcessor.Console

#### Non-interactive terminal execution

Usage: ContestLogProcessor.Console [options]

Options:

- `--debug`                 Enable debug output
- `-i, --import <logfile>`  Import a Cabrillo .log file
- `-e, --export <newfile>`  Export current data to a .log file
- `-l, --list`              List loaded entries (raw lines)
- `--interactive`           Start an interactive session
- `--score <logfile>`       Score a Cabrillo .log file and print a brief report (lists any non-compliant log entries too)

#### Interactive terminal execution features

- `help`         Show available interactive commands
- `import`       Import a Cabrillo `.log` file into memory
- `export`       Export current in-memory log to `<filepath>.log` (prompts on overwrite)
- `view [pageSize]` View loaded log entries in canonical format; optional page size and simple navigation
- `add`          Add a new log entry interactively (TheirCall required)
- `duplicate`    Duplicate entries by index or by filter (supports `--index <n>` or `--filter "text"`)
- `filter`       Search entries and list matches (read-only)
- `filter-dupe`  Search entries and optionally duplicate selected/all matches
- `score`        Score the currently loaded log and show a brief report (also available non-interactively via `--score <file>`)
- `exit`         Exit interactive session
