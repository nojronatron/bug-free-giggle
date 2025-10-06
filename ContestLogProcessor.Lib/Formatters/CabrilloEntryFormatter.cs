using System;

namespace ContestLogProcessor.Lib.Formatters
{
    /// <summary>
    /// Adapter that exposes the existing Cabrillo formatting via the ILogEntryFormatter contract.
    /// </summary>
    public class CabrilloEntryFormatter : ILogEntryFormatter
    {
        public string Name => "cabrillo";

        public string Format(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return entry.ToCabrilloLine();
        }

        public bool TryFormat(LogEntry entry, out string formatted, Action<string>? logger = null)
        {
            return CabrilloFormatter.TrySafeToCabrillo(entry, out formatted, logger);
        }
    }
}
