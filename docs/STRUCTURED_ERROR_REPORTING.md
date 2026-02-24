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
- `ErrorCode`: Hierarchical error code (e.g., "WFD.EXCHANGE.MALFORMED", "WFD.RULES.INVALID_MODE")
- `Category`: Error category (defaults to General)
- `Severity`: Error severity (defaults to Error)
- `FieldName`: Specific field that caused the error (e.g., "Mode", "SentExchange")
- `InvalidValue`: The invalid value that was encountered
- `ExpectedFormat`: Description of expected format or value
- `Details`: List of additional diagnostic details
- `RuleReference`: Contest-specific rule identifier (e.g., "WFD-ONE-CONTACT-PER-STATION-BAND-MODE")

### Hierarchical Error Codes

Error codes follow the pattern: `CONTEST.CATEGORY.SPECIFIC`

**WFD Error Code Examples:**
- `WFD.EXCLUDED.X_QSO` - Entry explicitly marked as excluded
- `WFD.MISSING.FREQUENCY` - Missing frequency field
- `WFD.MISSING.MODE` - Missing mode field
- `WFD.MISSING.CALLSIGN` - Missing call sign
- `WFD.MISSING.THEIRCALL` - Missing their call sign
- `WFD.MISSING.SENT_EXCHANGE` - Missing sent exchange
- `WFD.MISSING.RECEIVED_EXCHANGE` - Missing received exchange
- `WFD.RULES.INVALID_MODE` - Mode not valid for WFD
- `WFD.RULES.CALLSIGN_MISMATCH` - Call sign doesn't match header
- `WFD.EXCHANGE.SENT_INVALID` - Sent exchange validation failed
- `WFD.EXCHANGE.RECEIVED_INVALID` - Received exchange validation failed
- `WFD.EXCHANGE.SENT_MALFORMED` - Sent exchange format is malformed
- `WFD.EXCHANGE.RECEIVED_MALFORMED` - Received exchange format is malformed
- `WFD.DUPLICATE.BAND_MODE_STATION` - Duplicate contact (same station, band, mode)

## WinterFieldDay Implementation

The WinterFieldDayScoringService uses dependency injection and helper methods to create structured errors:

### Dependency Injection

The service now accepts `WfdExchangeStrategy` through constructor injection:

```csharp
public WinterFieldDayScoringService(WfdExchangeStrategy exchangeStrategy)
{
    _exchangeStrategy = exchangeStrategy ?? throw new ArgumentNullException(nameof(exchangeStrategy));
    _exchangeParser = new WinterFieldDayExchangeParser();
}

// Legacy constructor for backward compatibility
public WinterFieldDayScoringService() : this(new WfdExchangeStrategy())
{
}
```

The exchange strategy is used for validation instead of manual parsing, providing consistent error messages and better separation of concerns.

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

### Filtering by Error Code

```csharp
WinterFieldDayScoreResult result = scoringService.CalculateScore(log).Value;

// Get all exchange-related errors
var exchangeErrors = result.SkippedEntries
    .Where(e => e.ErrorCode != null && e.ErrorCode.Contains(".EXCHANGE."))
    .ToList();

// Get all missing data errors
var missingDataErrors = result.SkippedEntries
    .Where(e => e.ErrorCode != null && e.ErrorCode.StartsWith("WFD.MISSING."))
    .ToList();

// Get specific error type
var invalidModeErrors = result.SkippedEntries
    .Where(e => e.ErrorCode == "WFD.RULES.INVALID_MODE")
    .ToList();

// Get all duplicate entries
var duplicates = result.SkippedEntries
    .Where(e => e.ErrorCode == "WFD.DUPLICATE.BAND_MODE_STATION")
    .ToList();
```

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
    
    if (error.ErrorCode != null)
        Console.WriteLine($"  Error Code: {error.ErrorCode}");
    
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

### Programmatic Error Handling

Error codes enable automated error handling and correction:

```csharp
foreach (SkippedEntryInfo error in result.SkippedEntries)
{
    switch (error.ErrorCode)
    {
        case "WFD.MISSING.MODE":
            // Attempt to infer mode from frequency
            TryInferMode(error);
            break;
            
        case "WFD.RULES.INVALID_MODE":
            // Log mode translation suggestions
            SuggestModeCorrection(error.InvalidValue);
            break;
            
        case "WFD.EXCHANGE.SENT_MALFORMED":
        case "WFD.EXCHANGE.RECEIVED_MALFORMED":
            // Queue for manual review
            QueueForManualReview(error);
            break;
            
        case "WFD.DUPLICATE.BAND_MODE_STATION":
            // Track duplicate patterns for analysis
            TrackDuplicatePattern(error);
            break;
            
        default:
            // Generic error handling
            LogError(error);
            break;
    }
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
- `CreateEligibilityError` intelligently parses error messages (format: "CODE|Message") to populate structured fields
- Duplicate errors include detailed breakdown (Station, Band, Mode) in Details collection
- All error helpers are `private static` to emphasize they are service-specific factories
- **Dependency Injection**: `WfdExchangeStrategy` is injected into `WinterFieldDayScoringService` for validation
- **Exchange Strategy**: The service uses `IContestExchangeStrategy` methods instead of manual parsing
- **Error Code Format**: `CONTEST.CATEGORY.SPECIFIC` enables hierarchical filtering and automated handling
- **Backward Compatibility**: Legacy constructor without parameters still works for existing code
