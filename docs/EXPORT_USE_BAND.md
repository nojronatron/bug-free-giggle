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
