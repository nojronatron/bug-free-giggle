# Export command: --use-band

Overview

The interactive `export` command supports an optional `--use-band` flag which tells the exporter
to prefer the entry's `Band` token in the first QSO slot (the Cabrillo `freq`/`band` position) when
writing exported Cabrillo `.log` files.

Why this exists

Some logs supply a canonical band token (for example `40m`) instead of or in addition to a numeric
frequency like `7073`. When `--use-band` is used the exporter will place the band token in the
frequency column so the exported lines show the band label clearly, for example:

Example exported line (with --use-band):

QSO: 40m PH 2025-09-26 2100 K7RMZ 59 OKA N7KN 59 ISL

Example exported line (without --use-band):

QSO: 7073 PH 2025-09-26 2100 K7RMZ 59 OKA N7KN 59 ISL

Notes

- If an entry lacks a `Band` token but has a numeric `Frequency`, the exporter will fall back to
  the frequency (for example `7000`) even when `--use-band` is provided.
- If the entry lacks both `Band` and a mappable `Frequency`, an empty token will be emitted in the
  first slot so token positions remain stable.
- The console `help` output includes the `export` command and its usage. In interactive mode the
  `help` command lists available handlers with brief help text; the `ExportCommandHandler` includes
  the `--use-band` guidance in its long help string.

Try it

In interactive mode:

> export --use-band C:\temp\my_export

Or non-interactive via the global option (CLI) using the `--export` option (note: the global option
currently does not accept the --use-band flag; use the interactive `export` command to toggle this)

```powershell
# start interactive session then run:
> export --use-band C:\temp\my_export
```

View command: --use-band

Overview

The interactive `view` command also supports an optional `--use-band` flag which affects how
entries are displayed on-screen. When present, `view` will prefer the entry's `Band` token for
the first QSO slot instead of the numeric `Frequency` token. This is purely a display option and
does not modify the in-memory entries or exported files unless you also use `export --use-band`.

Usage

In interactive mode you can pass `--use-band` before or after the optional page size. Examples:

```powershell
> view --use-band         # show first page using band tokens when available
> view 20 --use-band      # show pages of 20 entries using band tokens
> view 5                  # show pages of 5 entries (no band preference)
```

Example

Given a log entry with a numeric frequency token:

  Frequency: 14268
  Band: (not set)

If the entry maps to the 20m band, then `view --use-band` will display the QSO line with the
band token in the frequency slot, for example:

```text
QSO: 20m PH 2025-09-26 2100 K7RMZ 59 OKA N7KN 59 ISL
```

Without `--use-band` the same entry would display the numeric frequency in that position:

```text
QSO: 14268 PH 2025-09-26 2100 K7RMZ 59 OKA N7KN 59 ISL
```

Notes

- `view --use-band` only changes how entries are presented in interactive view; it does not
  alter the stored `LogEntry` objects.
- If an entry lacks a `Band` token but has a numeric `Frequency` that maps to a band, `view --use-band`
  will display the mapped band (for example `14268` -> `20m`).
- If neither Band nor a mappable Frequency is present, the display will show an empty token in the
  first slot to keep column positions stable.

