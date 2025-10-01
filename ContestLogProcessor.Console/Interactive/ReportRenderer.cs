using System;
using System.Collections.Generic;

namespace ContestLogProcessor.Console.Interactive
{
    internal static class ReportRenderer
    {
        // Helper to print a label and comma-separated list wrapped to the ASCII header width.
        public static void PrintWrappedList(string label, ICollection<string> items, int innerWidth = 40, int indent = 2, bool showCountOnLabelRight = true, string? countDisplay = null)
        {
            if (showCountOnLabelRight)
            {
                System.Console.WriteLine($" {label} : {items.Count}");
            }
            else
            {
                string display = countDisplay ?? items.Count.ToString();
                System.Console.WriteLine($" {label} ({display}):");
            }

            if (items.Count == 0) return;

            string joined = string.Join(", ", items);
            int available = Math.Max(10, innerWidth - indent);
            string prefix = new string(' ', indent);

            string remaining = joined.Trim();
            while (!string.IsNullOrEmpty(remaining))
            {
                if (remaining.Length <= available)
                {
                    System.Console.WriteLine(prefix + remaining);
                    break;
                }

                int cut = remaining.LastIndexOf(", ", available);
                int take;
                if (cut == -1)
                {
                    take = available;
                }
                else
                {
                    take = cut + 2;
                }

                string part = remaining.Substring(0, take).TrimEnd();
                System.Console.WriteLine(prefix + part);
                remaining = remaining.Substring(take).TrimStart();
            }
        }
    }
}
