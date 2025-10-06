using System;
using System.Text;

namespace ContestLogProcessor.Lib.Formatters
{
    /// <summary>
    /// Very small ADIF formatter for a single LogEntry record.
    /// This does not produce a full ADIF file header; it returns an ADIF record line for the entry.
    /// </summary>
    public class AdifFormatter : ILogEntryFormatter
    {
        public string Name => "adif";

        public string Format(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!TryFormat(entry, out string line, null)) throw new InvalidOperationException("Failed to format ADIF record.");
            return line;
        }

        public bool TryFormat(LogEntry entry, out string formatted, Action<string>? logger = null)
        {
            formatted = string.Empty;
            if (entry == null) return false;

            try
            {
                StringBuilder sb = new StringBuilder();
                // ADIF simple fields: CALL, QSO_DATE, TIME_ON, FREQ, MODE, RST_SENT, RST_RCVD, GRIDSQUARE (not used)
                void AppendField(string tag, string? value)
                {
                    if (string.IsNullOrWhiteSpace(value)) return;
                    string v = value.Trim();
                    sb.AppendFormat("<{0}:{1}>{2}", tag, v.Length, v);
                }

                AppendField("CALL", entry.CallSign);
                if (entry.QsoDateTime != DateTime.MinValue)
                {
                    AppendField("QSO_DATE", entry.QsoDateTime.ToString("yyyyMMdd"));
                    AppendField("TIME_ON", entry.QsoDateTime.ToString("HHmm"));
                }
                AppendField("FREQ", entry.Frequency);
                AppendField("MODE", entry.Mode);
                if (entry.SentExchange != null) AppendField("RST_SENT", entry.SentExchange.SentSig);
                if (entry.ReceivedExchange != null) AppendField("RST_RCVD", entry.ReceivedExchange.ReceivedSig);

                sb.Append("<EOR>");
                formatted = sb.ToString();
                return true;
            }
            catch (Exception ex)
            {
                try { logger?.Invoke($"ADIF format failed for entry {entry.Id}: {ex}"); } catch { }
                formatted = string.Empty;
                return false;
            }
        }
    }
}
