using System;

namespace ContestLogProcessor.Lib.Formatters
{
    /// <summary>
    /// Formatter contract for producing string representations of a <see cref="LogEntry"/>.
    /// Implementations should avoid throwing from TryFormat and instead return false on failure.
    /// </summary>
    public interface ILogEntryFormatter
    {
        /// <summary>
        /// Short name identifying the formatter (e.g. "cabrillo", "adif").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Try to format the provided <paramref name="entry"/> into a string.
        /// Returns true on success and sets <paramref name="formatted"/>; otherwise returns false.
        /// An optional <paramref name="logger"/> may be provided to receive diagnostic messages.
        /// </summary>
        bool TryFormat(LogEntry entry, out string formatted, Action<string>? logger = null);

        /// <summary>
        /// Format the provided entry or throw when formatting fails. Convenience wrapper.
        /// </summary>
        string Format(LogEntry entry);
    }
}
