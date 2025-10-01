# Preserve Data Import Order Test Case Assertions

This file exists to assist Github Copilot Agent in writing tests to confirm import-order is preserved, and insertions and edits are handled correctly when updating the memory store and exporting to file.

Use the headings below to design unit tests for Import, Add, Duplicate, Update, and Export.

Example data formatting: [Example Data to Import from file](#example-data-to-import-from-file) has additional whitespace characters, representing a realistic case for how the data is usually formatted.

## Table of Contents

- [Import](#import)
- [Export](#export)
- [Update Entry](#update-entry)
- [Create Entry](#create-entry)
- [Duplicate Entry](#duplicate-entry)
- [Log Entry Source Line Numbers](#log-entry-source-line-numbers)
- [Comparison in General](#comparison-in-general)
- [QSODateTime Stamps, Comparison](#qsodatetime-stamps-comparison)
- [Example Data to Import from file](#example-data-to-import-from-file)
- [Expected Export to File (No Edits)](#expected-export-to-file-no-edits)
- [Expected Export to File After All Duplicated With SentMsg CHE](#expected-export-to-file-after-all-duplicated-with-sentmsg-che)
- [Expected Export to File After Single Duplicated SentMsg CHE](#expected-export-to-file-after-single-duplicated-sentmsg-che)
- [Expected Export to File After Add Single Log Entry TheirCall F4KE](#expected-export-to-file-after-add-single-log-entry-theircall-f4ke)

## Import

- Import order must be respected on import.
- Import must parse date/time into a UTC DateTime and normalize seconds to zero.
- Imported or created log entries with missing or unparsable QsoDateTime should be imported with QsoDateTime equal to DateTime.MinValue.
- Malformed QSO lines in input should be tolerated. How to handle Exceptions and error conditions are explained in [copilot-instructions.md](../../../.github/copilot-instructions.md).

## Export

- Export to file must respect log entry order and accomodate AddEntry and DuplicateEntry rules and not re-sort them.

## Update Entry

- UpdateEntry only modifies SentSig, SentMsg, TheirCall, and does not relocate the item in the list.
- Explicit assertion: index of the entry (position) remains same before/after update. This should be consistent because it is not possible to edit QSODateTime using UpdateEntry.
- Tests must assert the index position of an entry in the in-memory ordering is identical before and after UpdateEntry.
- Malformed QSO lines in input should be tolerated. How to handle Exceptions and error conditions are explained in [copilot-instructions.md](../../../.github/copilot-instructions.md).

## Create Entry

- Create Entry specification has be adjusted due to a new understanding of requirements. Sorry about that. Assert this new behavior and change code accordingly.
- Newly created entries should have a SourceLineNumber set to `null`.
- Create Entry with a timestamp earlier than all other entries should place it at index 0.
- Create Entry must append new entry following all other entries with same QSODateTime [See QSODateTime Stamps, Comparisons](#qsodatetime-stamps-comparison) and before entries with later QSODateTime.
- Create Entry: Comparison should ensure forward-timeline order (Year, then Month, Day, Hour, and and finally Minute), and then insert the created entry after the last of that timeline block.
- See [Expected Export to File After Add Single](#expected-export-to-file-after-add-single-log-entry-theircall-f4ke).
- Malformed QSO lines in input should be tolerated. How to handle Exceptions and error conditions are explained in [copilot-instructions.md](../../../.github/copilot-instructions.md).
- For a new entry with QsoDateTime dt, insert it immediately after the last existing entry whose QsoDateTime equals dt. If no entries have QsoDateTime equal to dt, insert immediately before the first entry with QsoDateTime greater than dt. If dt is earlier than all entries, insert at index 0.
- When computing insertion point for CreateEntry, treat log entries produced by DuplicateEntry method as orginary entries when comparing QsoDateTime so the new entry is appended after the last such slot.

## Duplicate Entry

- A duplicated entry is considered occuring at the same time as the source entry, but inserted 1 index after the source entry.
- Duplicating the last log entry should append the duplicate at the end.
- Duplicated entry must immediately follow source entry, ahead of other entries including those with later QSODateTime.
- Assert duplicated entries have SourceLineNumber equal to `null`.

## Log Entry Source Line Numbers

- SourceLineNumber is set only for Log Entries parsed from the input file (1-based line numbers).
- Entries created in-memory by CreateEntry method or produced by DuplicateEntry method must have SourceLineNumber equal to `null`.
- Update Entry does _not_ change SourceLineNumber.
- Import should retain the original log entry lines, not for comparison, but for debugging and audit tracing purposes.

## Comparison in General

- Compare specific tokens to avoid whitespace issues.
- Import should trim whiltespace from log entries and parse log entry fields to tokenized and store them in memory for comparison, editing, and exporting.

## QSODateTime Stamps, Comparison

- DateTime Kind `UTC` is the expected input from imported logs and from created entries.
- Only hours and minutes are considered when comparing QSODateTimes. Seconds should not be considered.
- Imported data date-time format is expected to be `yyyy-MM-dd HHmm` (the most likely format).
- ImportFile method should retain parsing agility to include additional possible (but unlikely) formats of "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd Hm", and "yyyyMMdd HHmm"
- Same QSODateTime: Two entries are considered to share the same timestamp when their DateTime values are equal after normalizing to UTC and truncating seconds (equality on Year, Month, Day, Hour, and Minute).
- Tests must specific DateTime Kind as `UTC`, and ignore seconds.
- Duplicate Entry: Comparison is not necessary, see [All Duplicated](#expected-export-to-file-after-all-duplicated-with-sentmsg-che) and [Single Duplicated](#expected-export-to-file-after-single-duplicated-sentmsg-che)

## Example Data to Import from file

QSO:    7218 PH 2023-09-20 1715 K7XXX           59  OKA    KD7JB           59  COL                
QSO:    7218 PH 2023-09-20 1716 K7XXX           59  OKA    W7TMT           59  SAN                
QSO:    7218 PH 2023-09-20 1716 K7XXX           59  OKA    N7KN            59  ISL                
QSO:    7218 PH 2023-09-20 1717 K7XXX           59  OKA    N7FCC/M         59  SKAG               
QSO:    7218 PH 2023-09-20 1718 K7XXX           59  OKA    W7IB            59  WHA                

## Expected Export to File (No Edits)

QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1717 K7XXX 59 OKA N7FCC/M 59 SKAG
QSO: 7218 PH 2023-09-20 1718 K7XXX 59 OKA W7IB 59 WHA

## Expected Export to File After All Duplicated With SentMsg CHE

QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL
QSO: 7218 PH 2023-09-20 1715 K7XXX 59 CHE KD7JB 59 COL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 CHE W7TMT 59 SAN
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 CHE N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1717 K7XXX 59 OKA N7FCC/M 59 SKAG
QSO: 7218 PH 2023-09-20 1717 K7XXX 59 CHE N7FCC/M 59 SKAG
QSO: 7218 PH 2023-09-20 1718 K7XXX 59 OKA W7IB 59 WHA
QSO: 7218 PH 2023-09-20 1718 K7XXX 59 CHE W7IB 59 WHA

## Expected Export to File After Single Duplicated SentMsg CHE

QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 CHE N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1717 K7XXX 59 OKA N7FCC/M 59 SKAG
QSO: 7218 PH 2023-09-20 1718 K7XXX 59 OKA W7IB 59 WHA

## Expected Export to File After Add Single Log Entry TheirCall F4KE

QSO: 7218 PH 2023-09-20 1715 K7XXX 59 OKA KD7JB 59 COL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA W7TMT 59 SAN
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA N7KN 59 ISL
QSO: 7218 PH 2023-09-20 1716 K7XXX 59 OKA F4KE 59 FRA
QSO: 7218 PH 2023-09-20 1717 K7XXX 59 OKA N7FCC/M 59 SKAG
QSO: 7218 PH 2023-09-20 1718 K7XXX 59 OKA W7IB 59 WHA
