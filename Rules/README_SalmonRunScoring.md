# SalmonRunScoringService (ContestLogProcessor.Lib)

This service calculates Salmon Run contest scores from a parsed Cabrillo log (`CabrilloLogFile`).

Public API:

- SalmonRunScoringService(ILocationLookup? lookup = null)
  - Optional dependency-injected `ILocationLookup` for lookup tables; defaults to in-memory implementation.

- SalmonRunScoreResult CalculateScore(CabrilloLogFile log)
  - Returns a `SalmonRunScoreResult` containing FinalScore, Multiplier, QsoPoints, W7DxBonusPoints, lists of unique multiplier categories, and a list of skipped entries with reasons.

Behavioral notes:

- Comparisons are case-insensitive and trimmed.
- Uniqueness for QSO points is determined by (TheirCall, Mode, Band).
- Multiplier uniqueness is determined by ReceivedMsg token only and follows the ordered processing of QSO lines (DateTime ascending, then source line number).
- Only eligible log entries (per rules in the project docs) are considered for scoring and multipliers.

Integration:

- This service consumes the existing `CabrilloLogFile` and `LogEntry` models included in the library.

## References

- [README - Salmon Run Scoring Rules](./README_SalmonRunScoringRules.md)
