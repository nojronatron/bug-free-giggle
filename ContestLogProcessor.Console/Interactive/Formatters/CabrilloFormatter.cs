using System;
using ContestLogProcessor.Lib;
using ContestLogProcessor.Lib.Formatters;
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

    /// <summary>
    /// Try to format the provided <paramref name="entry"/> using the named formatter
    /// registered in <see cref="FormatterRegistry"/> (for example "adif" or "cabrillo").
    /// </summary>
    public static bool TryFormat(LogEntry entry, string formatterName, out string formatted)
    {
        if (entry == null) { formatted = string.Empty; return false; }
        if (string.IsNullOrWhiteSpace(formatterName)) { formatted = string.Empty; return false; }

        if (FormatterRegistry.TryGet(formatterName, out ILogEntryFormatter? formatter))
        {
            return formatter!.TryFormat(entry, out formatted);
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Try to format the provided <paramref name="entry"/> using the named formatter
    /// and an optional <see cref="IConsole"/> used as a simple logger for diagnostic messages.
    /// </summary>
    public static bool TryFormat(LogEntry entry, string formatterName, out string formatted, IConsole? console)
    {
        Action<string>? logger = console is null ? null : new Action<string>(s => console.WriteLine(s));
        if (entry == null) { formatted = string.Empty; return false; }
        if (string.IsNullOrWhiteSpace(formatterName)) { formatted = string.Empty; return false; }

        if (FormatterRegistry.TryGet(formatterName, out ILogEntryFormatter? formatter))
        {
            return formatter!.TryFormat(entry, out formatted, logger);
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Convenience: format or throw. Attempts to find the named formatter and call its
    /// <c>Format</c> method. Throws <see cref="ArgumentException"/> if the formatter is not found.
    /// </summary>
    public static string FormatOrThrow(LogEntry entry, string formatterName)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrWhiteSpace(formatterName)) throw new ArgumentException("formatterName is required", nameof(formatterName));

        if (FormatterRegistry.TryGet(formatterName, out ILogEntryFormatter? formatter))
        {
            return formatter!.Format(entry);
        }

        throw new ArgumentException($"Formatter '{formatterName}' not found", nameof(formatterName));
    }
}
