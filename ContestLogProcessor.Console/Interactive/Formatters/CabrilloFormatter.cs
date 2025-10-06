using System;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive.Formatters;

/// <summary>
/// Small helper helpers for producing Cabrillo-formatted lines from LogEntry instances
/// in a safe, exception-tolerant manner.
/// </summary>
public static class CabrilloFormatter
{
    /// <summary>
    /// Try to produce a Cabrillo-formatted line from the provided <paramref name="entry"/>.
    /// This method catches exceptions thrown by <see cref="LogEntry.ToCabrilloLine"/> and
    /// returns <c>false</c> with an empty string when formatting fails.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <param name="line">When successful, contains the Cabrillo-formatted line; otherwise empty.</param>
    /// <returns><c>true</c> when formatting succeeded; otherwise <c>false</c>.</returns>
    public static bool TrySafeToCabrillo(LogEntry entry, out string line)
    {
        if (entry == null)
        {
            line = string.Empty;
            return false;
        }

        try
        {
            line = entry.ToCabrilloLine();
            return true;
        }
        catch (Exception)
        {
            line = string.Empty;
            return false;
        }
    }
}
