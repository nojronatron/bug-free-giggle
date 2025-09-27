# Project Overview

This project library will examine a supplied Cabrillo formatted amateur radio log file and provide a brief report of statistics, and enable making bulk edits such as adding entries for a "second county" of a county-line expedition.

## Folder Structure

- `/docs`: Contains solution-wide documentation.
- `/ContestLogProcessor.Console`: A .NET Console application useful for command-line operation.
- `/ContestLogProcessor.Lib`: The core, shared abstractions and implementations needed by the Console and other Projects (to be added later).
- `/ContestLogProcessor.Unittest`: XUnit test project. Add a directory to match the Project to be tested for example: `/Lib` for `ContestLogProcessor.Lib` unittests, etc.
- `/LogExamples`: Folder contains log file examples and should not be directly imported or copied to code nor checked in to git repository.

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
- Use collection expression syntax
- If-then-else statements always use braces around their code block
- Use explicit types instead of var: `Backpack EmptyGenericBackpack = new();` and `Student? FoundStudent = _entries.Find(match);`
- Always include a blank line before and after for() and foreach() iterator blocks to improve readability in dense files

## Libraries and Frameworks

- Use .NET 9
- Only Blazor or ASP.NET Core projects should utilize HTML, CSS, and JavaScript
- Always ask before adding, updating, or removing NuGet packages
- When writing unit tests use XUnit
- Use a NuGet Package like Moq to enabling mocking only when necessary to implement and run a unit test
- The Solution will need to be deployed to Docker containers for portabilities

## Architecture Guidelines

- Projects should depend on ContentLogProcessor.Lib for core functionality
- Projects should provide their own functionality that directly relates to their purpose and not depend on other projects besides .Lib
- Projects should utilize Logging features (i.e. `<ILogger>`) and have a way to log output
- ContestingProcessor.Lib is the core processing component and other projects are for UI, testing, or other purposes
- Each project should have a Docker container, except for ContestingProcessor.Lib which is a dependency to each of the other projects
- The Unittest Project should NOT be Dockerized

### Exception Handling

- The Console should catch and handle Exceptions, returning a message to the user with information on what went wrong and how to fix it with altered user input
- Design Classes so that Exceptions can be avoided
- Use try-catch-finally blocks to recover from Exceptions, including Cancellation/Asynchronous Exceptions
- Exceptions that must be returned to the caller should be done using `throw` keyword so that the Stack Track is maintained
- Use Finally block to clean-up resources

### ContestingProcessor.Console

- Should NOT output logging information unless a debug argument is included on the command line
- Should output logging information to the standard output (console screen) when a debug argument is included
- Should accept input in a human readable and concise format such as: `-debug` to add logging, `-add`, `-update`, `-remove` to perform operations on the data in memory, `-export` to dump data in memory to a file, etc

### ContestLogProcessor.Lib

- Prefer synchronous functions over asynchronous
- Methods that provide API access to the Library should be public
- Methods that support functionality and processing should not be publicly accessible by dependant projects
- Abstraction should be used to hide complexity from calling methods while enabling functionality

### ContestLogProcessor.Unittest

- XUnit test project for the entire Solution
- Follow the Folder Structure instructions
- Unittests should be concise, focused on method functionality
- Focus unittests on specific, basic functional goal
- Avoid writing extensing edge-case unittests
- Use data in `./LogExamples` directory to derive realistic test case data but do not copy the data exactly

### Cabrillo Log File Standards

- A Tag is surrounded by `<` and `>:` with a following space and then the data associated with the tag
- Each line must start with a Tag, followed by data, and ends with a newline character (LF or CRLF)
- Tags other than QSO and END-OF-LOG can be in any order
- Required Tags: START-OF-LOG, END-OF-LOG
- Common Tags: CALLSIGN, CONTEST:{string}, CATEGORY-{string}, CERTIFICATE, CLAIMED-SCORE, CLUB, CREATED-BY, EMAIL, GRID-LOCATOR, LOCATION, NAME, ADDRESS, ADDRESS-{string}, OPERATORS, OFFTIME, SOAPBOX, and X-{string}
- Special Tag `END-OF-LOG` followed by a newline character (LF or CRLF) indicating the end of the log and no more reading or parsing is necessary
- Special Tab `X-QSO` indicates the following qso data will be ignored by an upstream log processor
- Tag QSO indicates the following line is an ILogEntry type
- Once QSO Tags are encountered there will only be QSO Tags until Tag END-OF-LOG is encountered
- Each QSO Tag contains data separated by at least one whitespace character

#### Cabrillo Log QSO Tag Standards

QSO data format is made up of Frequency, Mode, Date, Time, Call, and Exchange. Definitions of each are below.

Frequency:

- Frequency in whole numbers, numbers with a decimal point and the letter `G`, whole numbers and the letter `G`, or the word `LIGHT`
- Max length: 7 characters
- No leading zeros
- Right-aligned
- Whole Numbers

Mode:

- Max length: 2 characters
- `PH`, `CW`, `FM`, `RY`, `DG`

Date:

- UTC date in yyyy-MM-dd format

Time:

- UTC time in HHmm format

Call:

- A string of alpha-numeric characters and an optional `/` up to 15 characters long

Exch:

- 5 elements
- 1st element: up to 3 whole numbers
- 2nd element: up to 5 alpha characters
- 3rd element: a string of up to 15 alpha-numeric characters including an optional `/`
- 4th element: up to 3 whole numbers
- 5th element: up to 5 alpha characters

## User Interactions

### ContestingProcessor.Console

- Executable run without any arguments returns text explaining how to use the app arguments
- Executable run with -?, -h, or --help returns text explaining how to use the app arguments
- Executable run with -i or --import and a relative or absolute filepath will try to load the file into memory using CabrilloLogProcessor
- Executable run with -e or --export and a relative or absolute filepath will try to export stored log data to the filepath argument using CabrilloLogProcessor
- Executable run with -l or --list will try to display stored log data to the Console

