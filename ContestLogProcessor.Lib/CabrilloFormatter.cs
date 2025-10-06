using System;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Lib.Formatters;

/// <summary>
/// Small helper for producing Cabrillo-formatted lines from LogEntry instances
/// in a safe, exception-tolerant manner. Placed in the library so it can be used
/// by any project that references ContestLogProcessor.Lib.
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
