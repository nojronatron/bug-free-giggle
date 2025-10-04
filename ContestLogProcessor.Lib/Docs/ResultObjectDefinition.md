# `OperationResult<T>` — Result object specification

This document defines the `OperationResult<T>` contract used across ContestLogProcessor.Lib public APIs. It is
intentionally small, immutable, and C#-idiomatic. The goals are:

- Provide a consistent, machine- and human-friendly return value for recoverable failures.
- Keep library code from writing directly to UI output; instead return diagnostics so callers decide how to log.
- Make caller behavior explicit: check `IsSuccess`, use `ErrorMessage` for users, and `Diagnostic` for logs.

Note: For operations that are conceptually void, use `OperationResult<Unit>` where `Unit` is a small placeholder type.

## Core types

- `OperationResult<T>` — Generic immutable result.
- `Unit` — Empty value type used when no payload is necessary.
- `ResponseStatus` — Enum used to classify results programmatically (Success, NotFound, BadFormat, OutOfRange, Cancelled, Error, Other).

## `OperationResult<T>` shape (guidelines)

Properties (read-only / immutable):

- `bool IsSuccess`
  - True only when the operation completed successfully.

- `T? Value`
  - Present when `IsSuccess == true`. For `OperationResult<Unit>` the `Value` will be `Unit.Instance`.

- `ResponseStatus Status`
  - Machine-friendly category of the outcome. Use `Success` for successful operations. Use other values to
    classify common failure types (e.g., `NotFound`, `BadFormat`, `OutOfRange`, `Cancelled`, `Error`, `Other`).

- `string? ErrorMessage`
  - Human-facing, concise message intended to be shown to users. Required when `IsSuccess == false`.

- `Exception? Diagnostic`
  - Optional developer-facing exception or diagnostic object for logs. This MUST NOT be displayed to users unless
    the caller explicitly enables debug-mode logging. Library code should populate this field when converting
    caught exceptions into `OperationResult` failures so the caller has enough context to log.

## Invariants and factory methods

- Exactly one of success or failure is true. Represent this via `IsSuccess` and derive `IsError` as `!IsSuccess`.
- Prefer static factory methods over public constructors to enforce invariants:
  - `OperationResult.Success(T value, ResponseStatus status = ResponseStatus.Success)`
  - `OperationResult.Failure(string errorMessage, ResponseStatus status = ResponseStatus.Error, Exception? diagnostic = null)`

## ResponseStatus usage

- Status is intended to be machine-readable. Use `ErrorMessage` for user text and `Status` for programmatic branching.
- Example: return `Status = ResponseStatus.NotFound` and `ErrorMessage = "Log file not found"` when a file is missing.

## Cancellation semantics

- Prefer to propagate `OperationCanceledException` (i.e., do not silently convert cancellation to a generic failure).
- If an API explicitly maps cancellation to an `OperationResult` outcome, use `ResponseStatus.Cancelled` and document
  this behavior on the API surface.

## When to throw vs return an `OperationResult`

- Return an `OperationResult` for recoverable, expected error conditions (invalid input, parse failure, resource not found).
- Throw exceptions for programmer/runtime errors that indicate bugs (null references, contract violations, corrupted state).
  Let higher-level code catch these, log diagnostics, and decide whether to convert to `OperationResult` as an API boundary.

## Logging and diagnostics

- Library code must not write to Console or other user-facing streams.
- Populate `Diagnostic` when material exception details would help the caller log a failure.
- The Console application (or other top-level runner) should decide whether to write `Diagnostic` to logs based on a
  debug flag.

## Thread-safety and immutability

- `OperationResult<T>` should be implemented as an immutable, thread-safe type so it can be passed between threads without
  additional synchronization.

## Minimal example (pseudo-prose)

- Caller receives `OperationResult<LogFile>` and checks `IsSuccess`.
  - If true: use `Value` and ignore `ErrorMessage`/`Diagnostic`.
  - If false: show `ErrorMessage` to user; if running in debug mode, log `Diagnostic` for more details.

## Backwards compatibility note

- If existing APIs currently throw for recoverable conditions, consider adding a small adapter/surface change so new
  code can use `OperationResult` while existing callers remain compatible. Prefer an explicit migration plan.

## Recommended helper types (implementation suggestion)

- `Unit`: an immutable, single-instance struct with `public static readonly Unit Instance`.
- `OperationResult`: consider implementing helper implicit conversions or extension helpers to simplify use in tests and code.

---

This document is a normative specification for `OperationResult<T>` used by the codebase. Use it as the canonical
reference when writing library APIs and when converting exceptions to failure results.
