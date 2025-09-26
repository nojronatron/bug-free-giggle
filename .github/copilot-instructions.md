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
- Use object-oriented, type-safe coding standards
- Name variables, methods, classes (etc) with PascalCase
- Use descriptive names that are easy to read and convey the purpose of the variable, method, class (etc)
- Code should be modular
- Prefer to use LINQ for filter, sort, and selection of data
- Use Collections with CRUD-like operations so data can be manipulated
- Prefer to use Interfaces over Abstract classes
- Library and functional code should be written so it is easy to unittest
- Web and Desktop UI Code should be written so it is easy to test with a Framework like Playwright
- Write and store classes each in their own code files
- Interfaces should be written in their own code files
- Namespace declarations should end with a semicolon rather than enclose the code in braces
- Use object initializer syntax with data: `Student Student1 = new Student { FirstName = "Anony", LastName = "Mouse" };`
- Use empty initialization syntax when no initialization data is needed: `Car Subaru = new();`
- Use collection expression syntax like `List<string> MyStrings = [];` and `List<int> Numbers = [1, 2, 3];`
- If-else statements should use braces around their code block
- Avoid using Var unless it is absolutely necessary

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

### ContestingProcessor.Console

- Should NOT output logging information unless a debug argument is included on the command line
- Should output logging information to the standard output (console screen) when a debug argument is included
- Should accept input in a human readable and concise format such as: `-debug` to add logging, `-add`, `-update`, `-remove` to perform operations on the data in memory, `-export` to dump data in memory to a file, etc
- Exceptions thrown by ContestLogProcessor.Lib must be caught and handled gracefully, reporting back to the Console what happened

### ContestLogProcessor.Lib

- Exceptions should be caught and a custom Exception with information about what went wrong and why should be passed back to the calling function to be handled
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
