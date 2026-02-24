# Change Log

v2.0.0 (23 Feb 2026)

- Add ability to introduce other contests
- Modularize contest log registration
- Detect contest using provided header information
- Add Winter Field Day processor and validation logic
- Update README
- Add full Cabrillo v3.x (late 2025) definition
- Enable importing ADIF file (but is not validated or processed at all)
- Implement parsing of Cabrillo v3.x files as part of validation process
- Separate contest-specific scoring from log file validation to improve testing and simplify adding more contests in the future

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
