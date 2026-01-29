# Contest Log Processor README

This project was created with the goal of processing ham radio communication logs prior to submitting to a contest sponsor.

A vast majority of code was was written by GitHub Copilot in Agent mode, using GPT-4.1 and GPT-5mini LLMs. Nojronatron directly wrote the initial solution structure, instruction files for GH Copilot, and some README files (or portions thereof).

## Features

v2.0.0 (28 January 2026)

- Add ability to introduce other contests
- Modularize contest log registration
- Detect contest using provided header information
- Add Winter Field Day processor and validation logic
- Update README
- Add full Cabrillo v3.x (late 2025) definition
- Enable importing ADIF file (but is not validated or processed at all)

v1.0.0 (7 October 2025)

- Interactive console and non-interactive console interfaces, both with with on-screen help
- Support Cabrillo formatted logs
- Tolerant of many log format errors
- On-screen feedback and `-debug` flag for deeper textual feedback on exceptional problems
- Import log for Salmon Run contest and view, edit, score, and export the log (as Cabrillo)
- Configurable pagination when viewing imported, edited log entries
- Console app is Docker-ready
- Malformed log entry tracking
- Log entry import sanitization protects against raw input attack vectors
- Use defensive copy and snapshot techniques to protect imported data from accidental change

## Future

- [ ] Link other README files from this one
- [ ] Improve scoring for Salmon Run event
- [ ] Separate Salmon Run scoring and rules from the main processing functions, enabling other contests to be processed in similar ways
- [ ] View `filter` output using band labels instead of frequencies
- [ ] Add ability to sort `view` or `filter` output by specific log entry token(s) (at least one, up to 2)
- [ ] Add `--whatif` flag to explin what would be done, so user can decide whether to execute command chain or not
- [ ] Update unit tests to ensure coverage: Invalid Tokens, Frequency parsing and Band mapping are correct
- [ ] Expand frequency table to cover all amateur bands
- [ ] Expand band table to cover all amateur bands
- [ ] Enable non-interactive mode to accept and process chained commands for example `import "<filepath>" --score --export --use-band "<path>/<filename>"`
- [ ] Allow user to overwrite score in imported log using scoring calculation from this utility
- [ ] Make edit operations transactional so a previous entry can be reverted with a simple 'undo' flag
- [ ] Full ADIF logfile support (Untested: can output log entries in adif)
