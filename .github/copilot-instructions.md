# Project Overview

This project library will examine a supplied Cabrillo formatted amateur radio log file and provide a brief report of statistics, and enable making bulk edits such as adding entries for a "second county" of a county-line expedition.

## Folder Structure

- `/docs`: Contains solution-wide documentation.
- `/ContestLogProcessor.Console`: A .NET Console application that processes user input and commands.
- `/ContestLogProcessor.Lib`: The core, shared abstractions and implementations.
- `/ContestLogProcessor.Unittest`: xUnit test project. Add a directory to match the project to be tested (for example: `/Lib` for `ContestLogProcessor.Lib` unit tests).
- `/LogExamples`: Folder contains log file examples and should not be directly imported or copied to code nor checked in to git repository.
- `/Rules`: Additional specifications and descriptions of contest rules, cabrillo specifications, and special handling rules.

When it is time to create additional projects in this Solution:

- Follow the same naming pattern as existing projects, using "Website" for an ASP.NET Core webapp, "WebClient" for WASM, "Server" for an API server, etc

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
- Use collection expression syntax supported in C# 12 and later
- Always add braces around If-then-else statement code blocks
- Use explicit types instead of var: `Backpack EmptyGenericBackpack = new();` and `Student? FoundStudent = _entries.Find(match);`
- Lambdas should use descriptive naming conventions including any arguments:
  - Avoid highly abbreviated or abstract naming conventions: `Entry entries.Where(e => e.Length >= 13)`
  - Prefer representative argument naming: `public List<Entry> Get(string name)` and `Enumerable<Entry> redCars = cars.Where(car => car.Red)`

## Libraries and Frameworks

- Use .NET 9
- Only Blazor or ASP.NET Core projects should utilize HTML, CSS, and JavaScript
- Always ask before adding, updating, or removing NuGet packages
- When writing unit tests use xUnit
- Use a NuGet package like Moq to enable mocking only when necessary to implement and run a unit test
- Projects within the solution (except the Unittest project) may be deployed to Docker containers for portability

## Architecture Guidelines

- Projects should depend on ContestLogProcessor.Lib for core functionality
- Projects should provide their own functionality that directly relates to their purpose and not depend on other projects besides `ContestLogProcessor.Lib`
- Projects should utilize Logging features (i.e. `<ILogger>`) and have a way to log output
- ContestLogProcessor.Lib is the core processing component and other projects are for UI, testing, or other purposes
- Each project should have a Docker container, except for `ContestLogProcessor.Lib` which is a dependency of the other projects

### Exception handling and Result contract

This project follows a consistent, C#-idiomatic approach to error handling: public library APIs return an immutable
OperationResult<T> for recoverable failures; exceptions are reserved for programmer/runtime errors and truly
unexpected conditions. For operations that are conceptually void, use OperationResult<Unit> (a small Unit/Empty type).

Key policies (high level):

- Validate early. Design public APIs and classes so that invalid inputs are detected and rejected before doing work.

- Recoverable vs programmer/runtime errors:
  - Recoverable errors: return a failure OperationResult<T>. The OperationResult carries a machine-friendly
    ResponseStatus, a concise user-facing ErrorMessage, and an optional Diagnostic (Exception) for logging.
  - Programmer/runtime errors (null reference, failed invariants, corrupted internal state): throw an exception.
    These indicate bugs that should be fixed rather than normal runtime conditions.

- Void-like operations: return OperationResult<Unit> rather than throwing for expected, recoverable error conditions.
  This keeps the public API surface consistent and easy to compose.

- Catch only what you can handle. Avoid blanket catches of System.Exception in library code; instead let
  higher-level callers (for example the Console app) catch and decide how to convert to OperationResult or log.

- Converting exceptions to OperationResult: do this at well-defined boundaries. When you catch an exception to
  convert it into a failure OperationResult, populate ErrorMessage (user-facing) and Diagnostic (developer-facing)
  so callers can log the diagnostic when appropriate (for example when a --debug flag is set).

- Rethrow vs wrap:
  - If you only need to propagate the same exception, rethrow with `throw;` to preserve the original stack trace.
  - If you must add contextual information, wrap the original exception as the InnerException of a new exception
    that contains the extra context. Be explicit in comments that the stack trace will reflect the wrapping point.
  - If you need to preserve the original stack trace while adding context, consider `ExceptionDispatchInfo.Capture(ex).Throw()`
    (use sparingly and document intent).

- Cancellation semantics:
  - Prefer to propagate OperationCanceledException (do not silently convert it into a generic failure). If a
    particular API chooses to surface cancellation as an OperationResult with a ResponseStatus.Cancelled, document it
    clearly and consistently across the API surface.

- Logging responsibility:
  - Library code should not write directly to console or user-facing streams. Return diagnostics in the
    OperationResult and let the calling application (Console) decide how and when to log diagnostic details.
  - Diagnostic (Exception) data contained in OperationResult is intended for logs only and must not be shown to end users
    unless an explicit debug flag is set.

See [Result object specification](../ContestLogProcessor.Lib/Docs/ResultObjectDefinition.md) for exact fields,
invariants, and usage examples.

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

### Code Examples Section
Add a dedicated section with more code examples:

```csharp
// Preferred: Explicit types
List<string> callsigns = ["K7XXX", "W7TMT"];
LogEntry entry = new() { Call = "K7XXX" };

// Preferred: Descriptive lambda parameters
var validEntries = entries.Where(logEntry => logEntry.IsValid);

// Preferred: File-scoped namespace
namespace ContestLogProcessor.Lib;

// Preferred: Object Initializer syntax
Student Student1 = new Student { FirstName = "Anony", LastName = "Mouse" };

// Preferred: Empty Initializer syntax when no arguments are needed
Car Subaru = new();

// Preferred: Collection Expression syntax
List<Car> Cars = []

// Preferred: Use explicit types instead of var
Backpack EmptyGenericBackpack = new();

// Preferred: Nullable explicit type instead of var
Student? FoundStudent = _entries.Find(match);
```