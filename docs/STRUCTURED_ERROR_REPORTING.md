# Structured Error Reporting

## Overview

The contest scoring system now supports hierarchical, structured error reporting through enhanced `SkippedEntryInfo` objects. This provides better categorization, severity levels, and detailed diagnostic information for entries that cannot be scored.

## Key Components

### ErrorCategory Enum

Categorizes errors into hierarchical groups for filtering and reporting:

- **General**: Unspecified errors
- **FileFormat**: Log file structure issues (missing Cabrillo markers, headers)
- **MissingData**: Required fields are missing or empty
- **Validation**: Data validation failures (invalid modes, bands, callsigns)
- **Exchange**: Exchange information parsing or validation errors
- **Duplicate**: Duplicate QSO entries
- **Excluded**: Explicitly excluded entries (X-QSO)
- **DateTime**: Date/time parsing or validation errors
- **ContestRules**: Contest-specific rule violations

### ErrorSeverity Enum

Defines severity levels for prioritization:

- **Info** (0): Informational - entry processed, notable characteristics (e.g., X-QSO)
- **Warning** (1): Issues that are expected or recoverable (e.g., duplicates)
- **Error** (2): Entry cannot be scored due to validation/data issues
- **Critical** (3): Fundamental issues preventing processing (e.g., missing Cabrillo markers)

### Enhanced SkippedEntryInfo

The `SkippedEntryInfo` class now includes:

**Backward-compatible fields:**
- `SourceLineNumber`: Line number in source file (1-based)
- `Reason`: Human-readable error message
- `RawLine`: The raw line that was skipped

**New structured fields:**
- `Category`: Error category (defaults to General)
- `Severity`: Error severity (defaults to Error)
- `FieldName`: Specific field that caused the error (e.g., "Mode", "SentExchange")
- `InvalidValue`: The invalid value that was encountered
- `ExpectedFormat`: Description of expected format or value
- `Details`: List of additional diagnostic details
- `RuleReference`: Contest-specific rule identifier (e.g., "WFD-ONE-CONTACT-PER-STATION-BAND-MODE")

## WinterFieldDay Implementation

The WinterFieldDayScoringService uses helper methods to create structured errors:

### Helper Methods

```csharp
// X-QSO entries (excluded by operator)
CreateExcludedEntryError(LogEntry entry)
// Category: Excluded, Severity: Info

// Eligibility validation failures (parsed from error message)
CreateEligibilityError(LogEntry entry, string errorMessage)
// Category: MissingData, Validation, or Exchange
// Severity: Error
// Automatically populates FieldName, InvalidValue, ExpectedFormat

// Duplicate contacts
CreateDuplicateError(LogEntry entry, string theirCall, string band, string mode)
// Category: Duplicate, Severity: Warning
// Includes details: Station, Band, Mode
// RuleReference: "WFD-ONE-CONTACT-PER-STATION-BAND-MODE"

// Exchange parsing failures
CreateExchangeError(LogEntry entry, string reason, bool isSentExchange, 
                    string invalidValue, List<string> details)
// Category: Exchange, Severity: Error
// FieldName: "SentExchange" or "ReceivedExchange"
// ExpectedFormat: WFD exchange format description
```

## Usage Examples

### Filtering Errors by Category

```csharp
WinterFieldDayScoreResult result = scoringService.CalculateScore(log).Value;

// Get all validation errors
var validationErrors = result.SkippedEntries
    .Where(e => e.Category == ErrorCategory.Validation)
    .ToList();

// Get all exchange-related errors
var exchangeErrors = result.SkippedEntries
    .Where(e => e.Category == ErrorCategory.Exchange)
    .ToList();

// Get duplicates only
var duplicates = result.SkippedEntries
    .Where(e => e.Category == ErrorCategory.Duplicate)
    .ToList();
```

### Filtering by Severity

```csharp
// Critical errors only
var critical = result.SkippedEntries
    .Where(e => e.Severity == ErrorSeverity.Critical)
    .ToList();

// Errors and critical (exclude warnings and info)
var serious = result.SkippedEntries
    .Where(e => e.Severity >= ErrorSeverity.Error)
    .ToList();

// Warnings and above
var actionable = result.SkippedEntries
    .Where(e => e.Severity >= ErrorSeverity.Warning)
    .ToList();
```

### Detailed Error Reporting

```csharp
foreach (SkippedEntryInfo error in result.SkippedEntries)
{
    Console.WriteLine($"Line {error.SourceLineNumber}: {error.Reason}");
    Console.WriteLine($"  Category: {error.Category}");
    Console.WriteLine($"  Severity: {error.Severity}");
    
    if (error.FieldName != null)
        Console.WriteLine($"  Field: {error.FieldName}");
    
    if (error.InvalidValue != null)
        Console.WriteLine($"  Invalid Value: {error.InvalidValue}");
    
    if (error.ExpectedFormat != null)
        Console.WriteLine($"  Expected: {error.ExpectedFormat}");
    
    if (error.Details.Any())
    {
        Console.WriteLine($"  Details:");
        foreach (string detail in error.Details)
            Console.WriteLine($"    - {detail}");
    }
    
    if (error.RuleReference != null)
        Console.WriteLine($"  Rule: {error.RuleReference}");
}
```

### Hierarchical Grouping

```csharp
// Group by category for summary reporting
var errorsByCategory = result.SkippedEntries
    .GroupBy(e => e.Category)
    .OrderBy(g => g.Key)
    .Select(g => new 
    {
        Category = g.Key,
        Count = g.Count(),
        Errors = g.ToList()
    });

foreach (var group in errorsByCategory)
{
    Console.WriteLine($"\n{group.Category} ({group.Count} entries):");
    foreach (var error in group.Errors)
    {
        Console.WriteLine($"  Line {error.SourceLineNumber}: {error.Reason}");
    }
}
```

## Backward Compatibility

The structured error reporting system is fully backward compatible:

- Existing code that only uses `Reason`, `SourceLineNumber`, and `RawLine` continues to work
- Default values are provided for new fields (Category: General, Severity: Error)
- All existing tests pass without modification
- New structured fields are optional and can be gradually adopted

## Future Enhancements

Potential areas for expansion:

1. **Additional Categories**: Add more specific categories as new contest types are supported
2. **Error Recovery**: Provide suggestions for fixing common errors
3. **Scoring Impact**: Track how many points were lost due to each error type
4. **Batch Reporting**: Generate CSV/JSON reports of all errors with full structured data
5. **Interactive Mode**: Allow users to review and potentially correct errors during interactive scoring

## Implementation Notes

- Helper methods in `WinterFieldDayScoringService` encapsulate error creation logic
- `CreateEligibilityError` intelligently parses error messages to populate structured fields
- Duplicate errors include detailed breakdown (Station, Band, Mode) in Details collection
- All error helpers are `private static` to emphasize they are service-specific factories
