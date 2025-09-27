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
