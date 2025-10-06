using System;
using ContestLogProcessor.Lib;
using ContestLogProcessor.Console.Interactive;

namespace ContestLogProcessor.Console.Interactive.Formatters;

/// <summary>
/// Tiny adapter in the Console project that forwards Cabrillo formatting requests
/// to the library formatter. Keep this small so we can add UI-specific behavior
/// later (colors, progress, etc.) without touching the library.
/// </summary>
public static class CabrilloFormatter
{
    /// <summary>
    /// Forwarding call to the library's <c>ContestLogProcessor.Lib.Formatters.CabrilloFormatter.TrySafeToCabrillo</c>.
    /// </summary>
    public static bool TrySafeToCabrillo(LogEntry entry, out string line)
        => ContestLogProcessor.Lib.Formatters.CabrilloFormatter.TrySafeToCabrillo(entry, out line);

    /// <summary>
    /// Forwarding call that accepts an <see cref="IConsole"/>. In the future this
    /// adapter can use the console reference to apply colors or other UI-specific
    /// behavior; for now it just provides a logger callback that writes messages
    /// to <c>console.WriteLine</c> when provided.
    /// </summary>
    public static bool TrySafeToCabrillo(LogEntry entry, out string line, IConsole? console)
    {
        Action<string>? logger = console is null ? null : new Action<string>(s => console.WriteLine(s));
        return ContestLogProcessor.Lib.Formatters.CabrilloFormatter.TrySafeToCabrillo(entry, out line, logger);
    }
}
