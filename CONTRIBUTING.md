# Contributing — formatting & build checks

This repository uses `dotnet format` and solution builds in CI to enforce formatting and basic correctness. Follow the steps below to run the same checks locally before creating a PR.

## Recommended (preferred): use a local tool manifest

1. Create the tool manifest once (only necessary one time per repo):

```pwsh
# run from the repository root
dotnet new tool-manifest
```

1. Install `dotnet-format` into the local manifest (pin a version if you want reproducible behaviour):

```pwsh
dotnet tool install dotnet-format --version 9.*
```

1. Restore the tools (CI will do this for you):

```pwsh
dotnet tool restore
```

1. Run the formatter (this will rewrite files to match the repo settings):

```pwsh
dotnet format "ContestLogProcessor.sln"
```

1. Verify formatting only (useful for CI or pre-PR checks):

```pwsh
dotnet format --verify-no-changes
```

If `--verify-no-changes` fails locally, run the `dotnet format "ContestLogProcessor.sln"` command in step 4, review the changes, and commit them before creating a PR.

## Alternative (global install)

If you prefer a global tool install instead of a per-repo manifest, you can install the tool globally:

```pwsh
dotnet tool install -g dotnet-format --version 9.*
```

Then run the same `dotnet format` and `dotnet format --verify-no-changes` commands from the repo root.

## Build verification

Before opening a PR it's also helpful to make sure the solution builds:

```pwsh
dotnet build "ContestLogProcessor.sln" --configuration Release
```

## Tips

- Use the local tool manifest approach for reproducible tooling across contributors and CI.
- If your editor supports automatic formatting on save (for example, Visual Studio or VS Code with the C# extension), configure it to use the repository rules.
- Consider adding a lightweight pre-commit hook that runs `dotnet format --verify-no-changes` and `dotnet build` to catch issues before pushing.

## Important API behavior change

- The library now returns defensive snapshots (clones) from mutation and read APIs to avoid callers inadvertently mutating internal state.
- In particular, `CreateEntry(...)` and `DuplicateEntry(...)` return a snapshot of the stored entry rather than the live, stored instance. `ReadEntries()` and `GetEntryById()` also return clones.
If you need to modify an entry, mutate the clone you receive and then call `UpdateEntry(id, editAction)` to persist your changes. `UpdateEntry` is the supported mutation API and will apply validation/sanitization before updating internal state.

This change prevents accidental mutations and makes the library safer for concurrent and multi-consumer scenarios. If you have performance-sensitive internal callers that require live references, open an issue to discuss a documented ``unsafe`` fast-path API.

Additional migration details (new since last release):

- New snapshot type: `CabrilloLogFileSnapshot`
  - `GetReadOnlyLogFile()` now returns a `CabrilloLogFileSnapshot?` (or `null` when no file is loaded). The snapshot uses `IReadOnlyDictionary<string,string>` for headers and `IReadOnlyList<T>` for entries and skipped entries, and exposes a `GetHeader(string)` helper.
  - Snapshots are deep clones (entries are cloned, skipped entries copied) and are wrapped in read-only collections to prevent accidental mutation of internal state.

### Migration guidance

- Update callers that previously expected a mutable `CabrilloLogFile` to use the new snapshot type. Example:

```csharp
// old (mutable) usage - no longer returned by the library
// var file = proc.GetReadOnlyLogFile();

// new usage
CabrilloLogFileSnapshot? snap = proc.GetReadOnlyLogFile();
string? call = snap?.GetHeader("CALLSIGN");
var entries = snap?.Entries; // IReadOnlyList<LogEntry>
```

- If you only need a single header value prefer `GetHeader("KEY")` to avoid direct dictionary indexing that might be awkward with read-only wrappers.
- If you relied on mutating an object returned by `ReadEntries()` or `GetEntryById()` to persist changes, switch to `UpdateEntry(id, editAction)` to apply and persist edits.

### Backwards compatibility and deprecation policy

- The library intentionally changed the surface to return snapshots; there is not currently a deprecated shim that returns a live mutable `CabrilloLogFile`. This keeps the public API clear and avoids accidental downstream reliance on mutable references.
- If you need a staged deprecation (compile-time warnings) for large downstream codebases, open an issue and we can add an `[Obsolete]` shim that forwards to a snapshot-to-mutable adapter for a transition period.

### Performance note

- Creating a snapshot clones the data; for very large logs this has a cost. If callers need to repeatedly inspect very large logs, consider:
  - Reusing a single snapshot instance rather than calling `GetReadOnlyLogFile()` in tight loops.
  - Requesting an internal/unsafe fast-path (will be considered via issue) that returns a non-cloned view for trusted callers (this will be explicitly documented and gated).

Write your code accordingly and open an issue if you need help migrating large codebases.
