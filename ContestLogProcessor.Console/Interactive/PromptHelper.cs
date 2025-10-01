using System;
using System.Collections.Generic;
using System.Text;

namespace ContestLogProcessor.Console.Interactive
{
    internal static class PromptHelper
    {
        // Very small argument splitter that respects quoted paths
        public static string[] SplitArguments(string input)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return parts.ToArray();
        }
    }
}
