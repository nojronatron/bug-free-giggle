using System;
using System.Collections.Generic;
using System.Linq;

namespace ContestLogProcessor.Lib.Formatters
{
    /// <summary>
    /// Simple registry for available <see cref="ILogEntryFormatter"/> implementations.
    /// UI projects may register custom formatters at startup.
    /// </summary>
    public static class FormatterRegistry
    {
        private static readonly List<ILogEntryFormatter> _formatters = new List<ILogEntryFormatter>();

        static FormatterRegistry()
        {
            // Register built-in formatters
            Register(new CabrilloEntryFormatter());
            Register(new AdifFormatter());
        }

        public static void Register(ILogEntryFormatter formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            lock (_formatters)
            {
                if (!_formatters.Exists(f => string.Equals(f.Name, formatter.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    _formatters.Add(formatter);
                }
            }
        }

        public static IReadOnlyList<ILogEntryFormatter> GetAll()
        {
            lock (_formatters) { return _formatters.ToArray(); }
        }

        public static bool TryGet(string name, out ILogEntryFormatter? formatter)
        {
            if (string.IsNullOrWhiteSpace(name)) { formatter = null; return false; }
            lock (_formatters)
            {
                formatter = _formatters.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                return formatter != null;
            }
        }
    }
}
