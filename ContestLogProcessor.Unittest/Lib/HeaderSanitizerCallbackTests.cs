using System;
using System.Collections.Generic;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class HeaderSanitizerCallbackTests
    {
        [Fact]
        public void ImportFile_SanitizesHeaderAndInvokesWarningCallback()
        {
            // Arrange: create a temporary cabrillo file with a long header containing a suspicious substring
            string tempPath = System.IO.Path.GetTempFileName();
            try
            {
                string[] lines = new[]
                {
                    "START-OF-LOG: 3.0",
                    // Make a header value longer than 13 chars and include a suspicious pattern
                    "NAME: This user tried to run select * from users on import",
                    "CREATED-BY: Test",
                    "END-OF-LOG:",
                };
                System.IO.File.WriteAllLines(tempPath, lines);

                // Capture warnings
                List<string> warnings = new List<string>();
                Action<string> capture = msg => warnings.Add(msg);

                var proc = new CabrilloLogProcessor(capture);

                // Act
                var imp = proc.ImportFileResult(tempPath);
                Assert.True(imp.IsSuccess);

                // Assert
                // The header should be present but sanitized in the read-only snapshot
                CabrilloLogFileSnapshot? snapshot = proc.GetReadOnlyLogFile();
                Assert.NotNull(snapshot);
                Assert.True(snapshot.Headers.ContainsKey("NAME"));
                string nameVal = snapshot.GetHeader("NAME") ?? string.Empty;
                // The suspicious substring should have been masked (asterisks)
                Assert.DoesNotContain("select * from", nameVal, StringComparison.OrdinalIgnoreCase);

                // The warning callback should have been invoked at least once
                Assert.NotEmpty(warnings);
                Assert.Contains(warnings, w => w.Contains("Sanitized header", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                try { System.IO.File.Delete(tempPath); } catch { }
            }
        }
    }
}
