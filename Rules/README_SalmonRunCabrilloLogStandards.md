# Cabrillo Log File Standards

Note: Cabrillo v3 creator created two pages of specification which should be considered authoritative: [Cabrillo v3 Headers](../docs/README_Cabrillo-Spec-v3-Header.md), [Cabrillo v3 QSO Data](../docs/README_Cabrillo-Spec-v3-QSO-Data.md).

The following was intended to help bootstrap parsing, validating, and scoring Salmon Run contest specifically.

## Table of Contents

- [Cabrillo Tags](#cabrillo-tags)
- [Cabrillo Log Standards For Salmon Run](#cabrillo-log-standards-for-salmon-run)

## Cabrillo Tags

- A Tag is a string surrounded by `<` and `>: ` (with a space)
- Tags will generally have data following them, although some tags can be empty
- Each line must start with a Tag, followed by data [see Cabrillo Log Standards For Salmon Run](#cabrillo-log-standards-for-salmon-run), and ends with a newline character
  - Usually newline is CRLF
  - LF is acceptable
- Required Tags for a valid logfile:
  - `START-OF-LOG:` Indicates the beginning of log data for parsing
  - `END-OF-LOG:` indicates a valid end of the log file and no more parsing is necessary
  - Logfile that has both of these tags is considered valid, however the tagged data within the log could be missing or invalid
- `X-QSO:` indicates the log entry on the same line will not be eligible for scoring
  - Lack of any `X-QSO:` tags has no special meaning
- `QSO:` indicates the beginning of a line of data that is expected to conform to a ILogEntry type
  - Only `QSO:` and `X-QSO:` tags are allowed between `CREATED-BY:` and `END-OF-LOG:`
  - Lack of any `QSO:` tags indicates no log entries have been added
- `QSO:` tag data format is expected to be: Frequency or Band, Mode, Date, Time, Callsign, SentSig, SentMsg, TheirCall, ReceivedSig, ReceivedMsg
  - Additional specification is provided in [LogEntry Data Format](#logentry-data-format) and [Exchange Data Format](#exchange-data-format)
- Header tags:
  - Appear between `START-OF-LOG:` and `CREATED-BY:` tags
  - Are defined for this application in [Header Tags Definition](#header-tags-definition)

## Cabrillo Log Standards For Salmon Run

Cabrillo logs contain tags in these categories, described in the following subsections:

- [Header Tags Definition](#header-tags-definition)
- [LogEntry Data Format](#logentry-data-format)
- [Exchange Data Format](#exchange-data-format)

Processing Tags and Tag Data are defined in [Tag Processing Rules](#tag-processing-rules).

### Header Tags Definition

String matching rule:

1. Trim leading and trailing whitespace.
2. Perform case insensitive comparison.

Usual Salmon Run Header Tags:

- `START-OF-LOG:`: Nullable string. Maximum length 12 characters. Must be first line of the log, indicating a valid beginning of Header tags. Value might be something like `3.0`, but should be treated as a string, not a floating-point number.
- `LOCATION:`: Nullable string, maximum length of 45.
- `CALLSIGN:`: Nullable string, up to 15 alpha-numeric characters including an optional '/' that is neither the first nor the last character in the string. RegEx: `/(?:[a-zA-Z0-9]{1,5}\/)?[a-zA-Z0-9]{1,5}/gm`
- `CLUB:`: Nullable string, up to 30 alpha-numeric characters including punctuation.
- `CONTEST:`: Nullable string, up to 32 alpha-numeric characters with optional `-` characters.
- `CATEGORY-OPERATOR:`: Nullable string. If not null, match one of `SINGLE-OP`, `MULTI-OP`, or `CHECKLOG`, case-insensitive.
- `CATEGORY-ASSISTED:`: Nullable string. If not null, must be one of 'assisted' or 'non-assisted' (trimmed, case insensitive)
- `CATEGORY-BAND:`: Nullable string. If not null, accept a string of up to 12 characters. Very basic RegEx: `/^ALL|VHF-3-BAND|VHF-FM-ONLY|Light|\d{3}|\d\.\d[gG]|\d{2,3}[gG]|\d{1,3}[mM]$/gm`
- `CATEGORY-MODE:`: Nullable string. If not null, accept a string matching 'CW', 'DIGI', 'FM', 'RTTY', 'SSB', or 'MIXED'.
- `CATEGORY-POWER:`: Nullable string. If not null, accept a string matching 'HIGH', 'LOW', or 'QRP'.
- `CATEGORY-STATION:`: Nullable string. If not null, accept a string matching 'DISTRIBUTED', 'FIXED', 'MOBILE', 'PORTABLE', 'ROVER', 'ROVER-LIMITED', 'ROVER-UNLIMITED', 'EXPEDITION', 'HQ', 'SCHOOL', or 'EXPLORER'.
- `CATEGORY-TIME:`: Nullable string. If not null, accept a RegEx match of `/^\d{1,2}-HOURS$/gm`
- `CATEGORY-TRANSMITTER:`: Nullable string. If not null, accept a string matching 'ONE', 'TWO', 'LIMITED', 'UNLIMITED', or 'SWL'.
- `CLAIMED-SCORE:`: Nullable string. If not empty, accept a RegEx match of up to 10 digits only: `/^\d{1,10}$/gm`
- `OPERATORS:`: Nullable string. If not empty, accept a RegEx match of up to 75 characters: `/@?(?:[a-zA-Z0-9]{1,5}\/)?[a-zA-Z0-9]{1,5}/gm`
- `NAME:`: Nullable string. If not empty, accepts up to 75 string-like characters.
- `ADDRESS:`: Nullable string. Maximum of up to 45 characters. Up to 6 `ADDRESS:` tags are allowed in the log.
- `ADDRESS-CITY:`: Nullable string. Maximum of up to 45 characters.
- `ADDRESS-STATE-PROVINCE:`: Nullable string. Maximum of up to 45 characters.
- `ADDRESS-POSTALCODE:`: Nullable string. Maximum of up to 45 characters.
- `ADDRESS-COUNTRY:`: Nullable string. Maximum of up to 45 characters.
- `GRID-LOCATOR:`: Nullable string. If not null, accept a string of up to 6 characters. RegEx `/^[a-zA-Z]{2}[0-9]{2}(?:[a-zA-Z]{2}){0,1}$/gm`
- `EMAIL:`: Nullable string. If not null, accept a string of up to 45 characters. RegEx `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/gm`
- `CREATED-BY:`: Nullable string. If not null, accept a string of up to 45 characters.
- `END-OF-LOG:`: Null-value. Used only as a tag to mark the end of log entries and end of logfile data.

Uncommon Salmon Run Header Tags:

- These can be missing from headers without impacting log processing.
- `CATEGORY-OPERATOR:`: Nullable string. If not null, accept a string matching 'SINGLE-OP', 'MULTI-OP', or 'CHECKLOG'.
- `CATEGORY-OVERLAY:`: Nullable string. Allow up to 12 characters in the class or word characters and an optional single '-'.
- `CERTIFICATE:`: Nullable string. Empty, 'yes', or 'no'.
- `SOAPBOX:`: Nullable string. Maximum character length:75. If there is one SOAPBOX header tag, retain it when imported and exported. If there are additional SOAPBOX tags with null or whitespace characters only, drop them. Accept up to 6 instances of this header tag.

### Tag Processing Rules

Missing Header Tags:

- Missing header tags that fall under 'Uncommon Salmon Run Header Tags' can be ignored and no warning or error is necessary. Processing continues normally.
- 'Usual Salmon Run Header Tags' must exist. Processing cannot continue if any Usual Salmon Run Header Tags are missing.

Tag Order:

- START-OF-LOG must appear before CREATED-BY and before END-OF-LOG
- Only tags between START-OF-LOG and END-OF-LOG should be evaluated
- Tags following CREATED-BY and before END-OF-LOG are considered LogEntry tags with data value or null as described elsewhere
- Tags following CREATED-BY and before END-OF-LOG must be consumed line-by-line, in ascending order as they appear in the source file
- Tags following START-OF-LOG and before CREATED-BY are considered Header Tags

Replace malicious looking strings in Tag fields with more than 13 characters:

- `select * from`: Replace matching substring characters with `*` characters when consuming the value.

### LogEntry Data Format

LogEntry tokens include the following properties and the [Exchange](#exchange-data-format) token:

Frequency:

- Multiple possible formats:
  - Whole numbers with an optional letter `G` (case invariant)
  - Floating-point (decimal) numbers with an optional letter `G` (case invariant)
  - The word `LIGHT` (case invariant)
- Max length: 7 characters
- No leading zeros
- Right-aligned

Band:

- Nullable string containing mixed numeric and alpha characters
- Regex: `/^(?:[0-9]{1,4}\.){0,1}[0-9]{1,4}(?:[mM]|[cC][mM]){1}$/gm`
- Related to Frequency token, see [SalmonRunScoring: Frequency and Band Tokens Scoring](./README_SalmonRunScoring.md)
- Defines a frequency range with a single label, see [SalmonRunScoring: Mapping Bands to Frequency Ranges](./README_SalmonRunScoring.md)
- RegEx match coupled with single-label match in list determines validity:
  - RegEx non-match => invalid Band token
- Blank or invalid Band tokens handling:
  - Frequency Token is valid: No Warning, no error, processing continues
  - Frequency Token is not valid: Warning only, does not stop processing, higher-level process will need to handle

Mode:

- Max length: 2 characters
  - `PH`, `CW`, `FM`, `RY`, `DG`
- Salmon Run specifically accepts only:
  - `PH`
  - `CW`
  - One instance of one or the other per log entry

Date:

- UTC date in yyyy-MM-dd format

Time:

- UTC time in HHmm format

Call:

- nullable string
- case insensitive
- up to 15 alphanumeric including optional '/' that is neither the first nor the last character in the string
- Regex: `/(?:[a-zA-Z0-9]{1,5}\/)?[a-zA-Z0-9]{1,5}/gm`

### Exchange Data Format

Exchange tokens are nullable strings with specific expected character lengths and some limitations of character types and formats.

SentSig, ReceivedSig:

- nullable string
- case insensitive
- 1 to 3 characters in length
- Regex: `/^[1-5][0-9]{1,2}|[1-5][nN]{1,2}$/gm`

SentMsg, ReceivedMsg:

- nullable string
- case insensitive
- 1 to 5 characters in length

ThierCall:

- nullable string
- case insensitive
- up to 15 alphanumeric including optional '/' that is neither the first nor the last character in the string
- Regex: `/^[a-zA-Z0-9]{1,5}\/[a-zA-Z0-9]{1,5}$/gm`
