# Salmon Run Scoring Rules

## Table of Contents

- [Overview](#overview)
- [Code Requirements](#code-requirements)
- [Terminology](#terminology)
- [Normalization Rules](#normalization-rules)
- [Tie Breaker Rules](#tie-breaker-rules)
- [LogEntry Scoring Eligibility](#logentry-scoring-eligibility)
- [Mode Token QSO Points](#mode-token-qso-points)
- [Point Multiplier](#point-multiplier)
- [Special Bonus Station Points](#special-bonus-station-points)
- [Frequency and Band Tokens Scoring](#frequency-and-band-tokens-scoring)
- [Calculate Final Score](#calculate-final-score)
- [Washington State County Abbreviations](#washington-state-county-abbreviations)
- [US State and Territory Abbreviations](#us-state-and-territory-abbreviations)
- [ARRL Canadian Province And Territories Multipliers List](#arrl-canadian-province-and-territories-multipliers-list)
- [ARRL Registered DXCC Entities Abbreviations](#arrl-registered-dxcc-entities-abbreviations)

## Overview

The reason log entries exist is to accumulate "points", which are evaluated to determine an overall "score" in the Salmon Run Contest.

Participants submit their Cabrillo Logs to the Contest Sponsor who is responsible for calculating the top scoring entries in various categories (defined in the Cabrillo Log Header as "CATEGORY-*" but not used for calculating score value).

Total Score must be calculated using a set of rules that are described in the sections that follow.

## Code Requirements

- Implementing the Rules and Scoring should be done in a separate service Class within The ContestLogProcessor Project, providing methods and Properties that contain the static details and algorithm, only providing the functionality needed to implement the feature in ContestLogProcessor.
- Comparisons should be case-insensitive and done only after trimming whitespace.
- Normalization of tokens with punctuation (e.g. "3Y/B") should be compared verbatim.
- Store Lookup Tables in external resource files:
  - Allow dependency injection via an ILocationLookup provider to enable overriding for tests and updates.

## Terminology

- Mode: The LogEntry Mode Token, either `PH` or `CW`
- Band: The LogEntry Frequency Token
- Contacts: Every appearance of ThierCall Token is considered a Contact and is scored based on Mode Token value in the same LogEntry (Exchange).

## Normalization Rules

### Valid Strings Containing '/' Rule

Strings containing a '/' are valid when all of the following are true:

- String is from Callsign token, TheirCall token, or an [DXCC Entity Abbreviation](#arrl-registered-dxcc-entities-abbreviations)
- When comparing, one or both string contain a '/'
- The '/' is:
  - Not preceded or followed by a whitespace character
  - Neither the 1st character nor the last character in the string after whitespace trimming
  - Preceded by at least 1 letter or number
  - Followed by at least 1 letter or number

### Comparing Two String Values

1. Do not mutate values to be compared, use a locally scoped copies instead.
2. Trim leading and trailing whitespace from both values.
3. If both values to be compared contain '/', retain '/' for verbatim comparison [see Valid Strings Containing '/' Rule](#valid-strings-containing--rule).
4. Perform case-insensitive comparison.

### SentMsg and ReceivedMsg Values

Values will include:

- letters [a-zA-Z]
- numbers [0-9]
- an optional '/' character [see Valid Strings Containing '/' Rule](#valid-strings-containing--rule)

## Tie Breaker Rules

1. Earliest QsoDateTime UTC
2. LogEntry order
3. SourceLineNumber

## LogEntry Scoring Eligibility

Each LogEntry must meet these minimum requirements in order to be eligible for scoring:

- String comparison must be be done by trimming whitespace and using case insensitivity.
- Must not be empty or contain only whitespace:
  - Band
  - One of either Mode or Freq (if one is empty, use the non-empty value)
  - Call
  - TheirCall
  - ReceivedMsg
  - SentMsg
  - Mode
- Mode must match one of 'PH' or 'CW'.
- Call must match at least one Header 'CALLSIGN' entry value:
  - If CALLSIGN Header is missing, or there are no matches, return an error message requesting the header be updated so that 'at least one CALLSIGN matches at least on LogEntry Call field'. Stop processing the log.
- SentMsg must match one item within the list of abbreviations for [WA Counties](#washington-state-county-abbreviations), [US States](#us-state-and-territory-abbreviations), [Canadian Provinces](#arrl-canadian-province-and-territories-multipliers-list), or [DXCC Entities](#arrl-registered-dxcc-entities-abbreviations):
- LogEntries beginning with `X-QSO:` cannot be scored and are not eligible for scoring in any way.

An example regex to help determine eligibility of Call, TheirCall, SentMsg, and ReceivedMsg token values: `^[A-Za-z0-9]+(?:/[A-Za-z0-9]+)?$`

Note: SentSig and ReceivedSig tokens are required by Cabrillo spec, but are not considered part of determining scoring eligibility in this implementation.

### LogEntry Ineligibility Handling

Any LogEntry not meeting [these criteria](#logentry-scoring-eligibility) completely:

- Are not eligible for scoring and therefore must not be evaluated during the scoring algorithm processing.
- No errors will be thrown.
- Processing will continue.
- A summary of skipped log entries due to ineligibility should be provided to the caller so it can be reviewed and managed separately.

## Mode Token QSO Points

For every log entry with the following Mode Token, add the correlating number of points to the total score:

- `PH`: 2 points.
- `CW`: 3 points.
- For each unique (TheirCall, Mode, and within the same Band) triplet, count the Mode points once, even if multiple TheirCall tokens exist having the same Band.
- The first time the same TheirCall in a Band in each Mode (PH and CW) appears is eligible for points accumulation.
- Subsequent times TheirCall appears with the same Band and Mode as a previous LogEntry are not eligible for points accumulation and should not be counted.

Reference: [Frequency and Band Tokens Scoring](#frequency-and-band-tokens-scoring)

### Mode Token QSO Points Examples

Scores 2 points for PH Mode log entry within a band with a TheirCall and valid SentMsg and ReceivedMsg:

```text
QSO: 7265 PH 2023-09-21 0019 K7XXX 59 OKA N7UK 59 KITT
```

Scores 3 points for CW Mode log entry within a band:

```text
QSO: 7265 CW 2023-09-21 0019 K7XXX 59 OKA N7UK 59 KITT
```

Scores 5 points (2 for PH QSO with same TheirCall token and 3 for CW QSO with same TheirCall token):

```text
QSO: 7265 PH 2023-09-21 0019 K7XXX 59 OKA N7UK 59 KITT
QSO: 7073 CW 2023-09-21 0123 K7XXX 59 OKA N7UK 59 KITT
```

Scores 5 points (2 for first PH QSO with same TheirCall token at 0019, and 3 for first CW QSO with same TheirCall token at 0123):

```text
QSO: 7265 PH 2023-09-21 0019 K7XXX 59 OKA N7UK 59 KITT
QSO: 7073 CW 2023-09-21 0123 K7XXX 59 OKA N7UK 59 KITT
QSO: 7277 PH 2023-09-21 0345 K7XXX 59 OKA N7UK 59 KITT
QSO: 7071 CW 2023-09-21 0823 K7XXX 59 OKA N7UK 59 KITT
```

Scores 10 points (2 for each PH QSO with same TheirCall token on separate Frequency Token values, and 3 for each CW QSO with same TheirCall token on separate Frequency Token values):

```text
QSO: 7265 PH 2023-09-21 0019 K7XXX 59 OKA W7M 59 ADA
QSO: 7073 CW 2023-09-21 0123 K7XXX 59 OKA W7M 59 ADA
QSO: 3977 PH 2023-09-21 0345 K7XXX 59 OKA W7M 59 ADA
QSO: 3655 CW 2023-09-21 0823 K7XXX 59 OKA W7M 59 ADA
```

## Point Multiplier

Multiplier should compute only from LogEntries that eligible and valid:

- [Log Entry Scoring Eligibility](#logentry-scoring-eligibility).
- RecieivedMsg and SentMsg tokens are valid as detailed in [SentMsgToken and ReceivedMsgToken](#sentmsg-token-and-receivedmsg-token).

The formula for arriving at a value for "Multiplier" is detailed in subsection [Accumulate the Multiplier Value](#accumulate-the-multiplier-value).

The multiplier is used to increase the Mode Token QSO Points through multiplication in order to [Calculate the Final Score](#calculate-final-score).

### SentMsg Token and ReceivedMsg Token

String values in RecievedMsg identify eligibility of LogEntry to count toward a Multiplier Point.

Lookup tables of valid values are contained in the following sections:

- [Washington State County](#washington-state-county-abbreviations) abbreviations.
- [US State or Territory](#us-state-and-territory-abbreviations) abbreviations.
- [Canadian Province and Territory](#arrl-canadian-province-and-territories-multipliers-list) abbreviations.
- Non-US, Non-Canadian [DXCC Entries](#arrl-registered-dxcc-entities-abbreviations) abbreviations.

### Multiplier Eligibility

1. The LogEntry is valid according to [Log Entry Scoring Eligibility](#logentry-scoring-eligibility)
2. AND the LogEntry ReceivedMsg value has not be encountered before
3. AND one of the following returns a found match:
  A. the LogEntry ReceivedMsg matches one entry in Washington State County lookup table
  B. If not found, the LogEntry RecievedMsg matches one entry in US State or Territory lookup table.
  C. If not found, the LogEntry ReceivedMsg matches one entry in Canadian Province and Territoty lookup table.
  D. If not found, the LogEntry ReceivedMsg matches one entry in DXCC Entries lookup table.
4. Otherwise the LogEntry is not eligible to increment to Multiplier.

### Accumulate the Multiplier Value

Score 1 point for every unique instance of the following:

- [Washington Counties](#washington-state-county-abbreviations) based only on ReceivedMsg value.
- [US States](#us-state-and-territory-abbreviations) based only on ReceivedMsg value.
- [Canadian Provinces and Territories](#arrl-canadian-province-and-territories-multipliers-list) based only on ReceivedMsg value.
- [DXCC Entries](#arrl-registered-dxcc-entities-abbreviations) based only on ReceivedMsg value.

### Unique Multiplier Value Determination

Both apply only to determining uniqueness of multipliers:

- [Comparing](#comparing-two-string-values) ReceivedMsg token values and storing only one, such as in a Hash Map.
- Mode token and Frequency token values are not considered when determining uniqueness.

### Processing Flow

1. Create variables to store only unique entries across Mode token and Frequency Token:
  A. List of unique Washington County names that will store a copy of the LogEntry in its entirety.
  B. List of unique US State names (except Washington) that will store a copy of the LogEntry in its entirety.
  C. List of unique Canadian Province and Territory names that will store a copy of the LogEntry in its entirety
  D. List of unique DXCC Entities that will store a copy of the LogEntry in its entirety, up to a maximum List capacity of 10.
2. Iterate through all LogEntries, in Date/Time ascending order, and store items into the variables when the LogEntry is considered eligible for scoring, meeting the specified criteria.
3. Determine the multiplier value by counting the number of stored items in the following Lists:
  A. Unique Washington County names List.
  B. Unique US States names List.
  C. Unique Canadian Provinces and Territory names List.
  D. Unique DXCC Entities.
4. Return the multiplier value to the caller to use in the scoring algorithm.

## Special Bonus Station Points

Slightly different scoring rules apply to Special Bonus Station with TheirCall token value "W7DX":

- Only LogEntries that pass [Log Entry Scoring Eligibility](#logentry-scoring-eligibility) rules shall be evaluated to determine if TheirCall contains the value "W7DX".
  - If such as evaluation has already been done to the current LogEntry, that result should be respected.
- For each of up to two instances, add 500 points to the points accumulator.
- W7DX can only be counted a total of 2 times for a maximum of 1000 points.
- W7DX can only be scored once per Mode token (regardless of Band).
- When multiple W7DX entries appear in LogEntries with the same Mode Token, only the first is eligible for points accumulation for that Mode.

## Frequency and Band Tokens Scoring

The Cabrillo Specification defines a token named `freq`:

- A frequency represented in kilohertz (kHz), Megahertz (MHz), or Gigahertz (GHz).
- Interpreted (by humans) as the radio frequency (for example 14250 kHz) displayed on the radio at the time the LogEntry was created.
- 'freq' maps to `LogEntry.Frequency`
- Frequency Token value can be used to lookup the [Band](#mapping-bands-to-frequency-ranges)

The Cabrillo Specification does not define a token named `band`, but Salmon Run Rules mention it so it is defined here:

- Contains at least 1, but no more than 3 whole numbers
- Optionally contains the letter `M` (case insensitive)
- Band Token value can be used to lookup the [Frequency Range](#mapping-bands-to-frequency-ranges)

There are many entries in the Cabrillo "Explanation of Fields - Freq" documentation, but only a few of them apply to Salmon Run and are defined below, all of which should be implemented to determine scoring.

### Not Valid Frequency Tokens For Salmon Run Contest

- Values that contain frequency unit indicators (examples: 'Hz', 'kHz', 'MHz', 'GHz', etc).
- Values that contain the letter `G` or `M`, case-insensitive.
- Values that contain or are `LIGHT`.
- Values parsed to an integer value within the range of 55 to 1000.
- Values parsed to an integer value above 29999.
- Other non-numeric and non-word characters combinations.
- Empty or whitespace Frequency Token value.

### Valid Frequency Tokens For Salmon Run Contest

- Must be a parsable integer within the stated inclusive ranges:
  - 1800 kHz and 2000 kHz
  - 3500 kHz and 4000 kHz
  - 7000 kHz and 7300 kHz
  - 14000 kHz and 14350 kHz
  - 21000 kHz and 21450 kHz
  - 28000 kHz and 29700 kHz
  - 50000 kHz and 54000 kHz
- Frequency Token values within the same range are considered "the same" (same band) for points scoring purposes.
- Frequency Token Values that use floating-point values:
  - Frequency Token Values should be parsed as Integers for the purposes of determining Band validity.
  - Only the whole-number portion of the value will be considered when determining valid Band, as determined by truncating everything to the right of the decimal point, without rounding.

### Valid Band Tokens for Salmon Run Contest

- 160m
- 80m
- 40m
- 20m
- 15m
- 10m
- 6m

### Mapping Bands to Frequency Ranges

- 160m <-> 1800 kHz through 2000 kHz
- 80m <-> 3500 kHz through 4000 kHz
- 40m <-> 7000 kHz through 7300 kHz
- 20m <-> 14000 kHz through 14350 kHz
- 15m <-> 21000 kHz through 21450 kHz
- 10m <-> 28000 kHz through 29700 kHz
- 6m <-> 50000 kHz through 54000 kHz

When two frequencies are within range of one band, they are the same band:

```text
QSO: 7265 PH 2023-09-21 0019 K7XXX 59 OKA N7UK 59 KITT => 40m Band => 7000 kHz through 7300 kHz
QSO: 7073 CW 2023-09-21 0123 K7XXX 59 OKA N7UK 59 KITT => 40m Band => 7000 kHz through 7300 kHz
QSO: 3655 CW 2023-09-21 0823 K7XXX 59 OKA W7M 59 ADA => 80m Band => Freq range 3500 kHz through 4000 kHz
```

## Calculate Final Score

1. Sum the accumulated Mode token QSO Points.
1. Calculate the Multiplier value.
1. Multiply QSO Points by Multiplier to get Intermediate Score.
1. Add the Special Bonus Station Points value to Intermediate Score to get the Final Score.
1. Return Final Score value to the caller.

In short: `Final Score = ( rawPoints * multiplier ) + W7DX_bonus`

### Output

Once the final score is calculated, a custom object should be returned to the caller that contains:

- The Final Score, as an integer.
- The Multiplier, as an integer.
- The QSO Points, as an integer.
- Special Bonus Station Points, as an integer.
- List of unique Washington Counties by Name.
- List of unique US States by Name.
- List of unique Canadian Provinces and Territories by Name.
- List of unique DXCC Entities.
- List of items that could not be processed because they were unparseable or had the LogEntry element `X-QSO:`.

## Washington State County Abbreviations

| County Name  | Abbreviation |
| ------------ | ------------ |
| Adams        | ADA          |
| Asotin       | ASO          |
| Benton       | BEN          |
| Chelan       | CHE          |
| Clallam      | CLAL         |
| Clark        | CLAR         |
| Columbia     | COL          |
| Cowlitz      | COW          |
| Douglas      | DOU          |
| Ferry        | FER          |
| Franklin     | FRA          |
| Garfield     | GAR          |
| Grant        | GRAN         |
| Grays Harbor | GRAY         |
| Island       | ISL          |
| Jefferson    | JEFF         |
| King         | KING         |
| Kitsap       | KITS         |
| Kittitas     | KITT         |
| Klickitat    | KLI          |
| Lewis        | LEW          |
| Lincoln      | LIN          |
| Mason        | MAS          |
| Okanogan     | OKA          |
| Pacific      | PAC          |
| Pend Oreille | PEND         |
| Pierce       | PIE          |
| San Juan     | SAN          |
| Skagit       | SKAG         |
| Skamania     | SKAM         |
| Snohomish    | SNO          |
| Spokane      | SPO          |
| Stevens      | STE          |
| Thurston     | THU          |
| Wahkiakum    | WAH          |
| Walla Walla  | WAL          |
| Whatcom      | WHA          |
| Whitman      | WHI          |
| Yakima       | YAK          |

LogEntry ReceivedMsg comparison should be done after whitespace trimming and with case insensitivity.

## US State and Territory Abbreviations

| Name                     | Abbreviation |
| ------------------------ | ------------ |
| Alabama                  | AL           |
| Alaska                   | AK           |
| Arizona                  | AZ           |
| Arkansas                 | AR           |
| California               | CA           |
| Colorado                 | CO           |
| Connecticut              | CT           |
| Delaware                 | DE           |
| Florida                  | FL           |
| Georgia                  | GA           |
| Hawaii                   | HI           |
| Idaho                    | ID           |
| Illinois                 | IL           |
| Indiana                  | IN           |
| Iowa                     | IA           |
| Kansas                   | KS           |
| Kentucky                 | KY           |
| Louisiana                | LA           |
| Maine                    | ME           |
| Maryland                 | MD           |
| Massachusetts            | MA           |
| Michigan                 | MI           |
| Minnesota                | MN           |
| Mississippi              | MS           |
| Missouri                 | MO           |
| Montana                  | MT           |
| Nebraska                 | NE           |
| Nevada                   | NV           |
| New Hampshire            | NH           |
| New Jersey               | NJ           |
| New Mexico               | NM           |
| New York                 | NY           |
| North Carolina           | NC           |
| North Dakota             | ND           |
| Ohio                     | OH           |
| Oklahoma                 | OK           |
| Oregon                   | OR           |
| Pennsylvania             | PA           |
| Rhode Island             | RI           |
| South Carolina           | SC           |
| South Dakota             | SD           |
| Tennessee                | TN           |
| Texas                    | TX           |
| Utah                     | UT           |
| Vermont                  | VT           |
| Virginia                 | VA           |
| Washington               | WA           |
| West Virginia            | WV           |
| Wisconsin                | WI           |
| Wyoming                  | WY           |
| District of Columbia     | DC           |
| American Samoa           | AS           |
| Guam                     | GU           |
| Northern Mariana Islands | MP           |
| Puerto Rico              | PR           |
| U.S. Virgin Islands      | VI           |

## ARRL Canadian Province And Territories Multipliers List

| Name | Abbreviation |
| ---- | ------------ |
| Alberta | AB |
| British Columbia | BC |
| Labrador | LB |
| Manitoba | MB |
| New Brunswick | NB |
| Newfoundland | NF |
| Nova Scotia | NS |
| Northern Territories | NT |
| Nunavut | NU |
| Ontario | ON |
| Prince Edward Island | PE |
| Quebec | QC |
| Saskatchewan | SK |
| Yukon Territory | YT |

## ARRL Registered DXCC Entities Abbreviations

Note: For simplicity, only the abbreviations are listed here.

For comparison and point eligibility purposes:

- Existing '/' characters are considered necessary and should be preserved
- Comparisons should utilize [rules](#comparing-two-string-values)

1A
3A
3B6
3B8
3B9
3C
3C0
3D2
3DA
3V
3W
3X
3Y/B
3Y/P
4J
4L
4O
4S
4U1I
4U1U
4U1V
4W
4X
5A
5B
5H
5N
5R
5T
5U
5V
5W
5X
5Z
6W
6Y
7O
7P
7Q
7X
8P
8Q
8R
9A
9G
9H
9J
9K
9L
9M2
9M6
9N
9Q
9U
9V
9X
9Y
A2
A3
A4
A5
A6
A7
A9
AP
BS7
BV
BV9P
BY
C3
C5
C6
C9
CE
CE0X
CE0Y
CE0Z
CE9
CM
CN
CO
CP
CT
CT3
CU
CX
CY0
CY9
D2
D4
D6
DL
DU
E3
E4
EA
EA6
EA8
EA9
EI
EK
EL
EP
ER
ES
ET
EU
EX
EY
EZ
F
FG
FH
FJ
FK
FM
FO
FO/A
FO/M
FO0
FR
FR/G
FR/J
FR/T
FT/G
FT/J
FT/T
FT/W
FW
FY
GA
GD
GI
GJ
GM
GU
GW
H4
H40
HA
HB
HB0
HC
HC8
HH
HI
HK
HK0A
HK0M
HL
HM
HP
HR
HS
HV
HZ
I
IS
IS0
J2
J3
J5
J6
J7
J8
JA
JD1M
JD1O
JT
JW
JX
JY
K
KG4
KH0
KH1
KH2
KH3
KH4
KH5
KH5K
KH6
KH7K
KH8
KH9
KL
KP1
KP2
KP3
KP4
LA
LU
LX
LY
LZ
OA
OD
OE
OH
OH0
OJ0
OK
OM
ON
OX
OY
OZ
P2
P4
PA
PJ2
PJ4
PJ5
PJ7
PY
PY0F
PY0S
PY0T
R1F
S0
S2
S5
S7
S9
SM
SP
ST
SU
SV
SV/A
SV5
SV9
T2
T30
T31
T32
T33
T5
T7
T8
TA
TF
TG
TI
TI9
TJ
TK
TL
TN
TR
TT
TU
TY
TZ
UA
UA2
UA9
UK
UN
UR
V2
V3
V4
V5
V6
V7
V8
VE
VK
VK0H
VK0M
VK9C
VK9L
VK9M
VK9N
VK9W
VK9X
VP2E
VP2M
VP2V
VP5
VP6
VP6/D
VP8
VP8/G
VP8/H
VP8O
VP9
VU
VU4
VU7
XE
XF4
XT
XU
XW
XX9
XY
XZ
YA
YB
YI
YJ
YK
YL
YN
YO
YS
YT
YV
Z2
Z3
Z8
ZA
ZB
ZC4
ZD7
ZD8
ZD9
ZF
ZK3
ZL
ZL7
ZL8
ZL9
ZP
ZS
ZS8
