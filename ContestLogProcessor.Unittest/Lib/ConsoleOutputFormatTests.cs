using System.Diagnostics;
using System.Text;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ConsoleOutputFormatTests
    {
        [Fact]
        public void ScoreReport_LabelFormatting_IsCorrect()
        {
            string project = "f:\\Projects\\Ham Contest Log Processor\\ContestLogProcessor\\ContestLogProcessor.Console\\ContestLogProcessor.Console.csproj";
            string logfile = "f:\\Projects\\Ham Contest Log Processor\\ContestLogProcessor\\ContestLogProcessor.Unittest\\Lib\\TestData\\K7XXX_Test_WithDX.log";

            // Run the already-built console DLL directly to avoid triggering a build during the test run.
            string dllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project) ?? string.Empty, "bin", "Release", "net9.0", "ContestLogProcessor.Console.dll");

            ProcessStartInfo psi = new ProcessStartInfo("dotnet")
            {
                // When invoking dotnet <dll> we pass the application args directly (no extra '--' separator)
                Arguments = $"\"{dllPath}\" \"--score\" \"{logfile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process p = Process.Start(psi)!;
            string outp = p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit(10000);

            // Ensure the process exited normally
            Assert.True(p.ExitCode == 0, $"Console run failed. ExitCode={p.ExitCode}. StdErr:\n{err}");

            // Check label formats - exact strings expected
            Assert.Contains("Washington Counties (39):", outp);
            Assert.Contains("US States (5):", outp);
            Assert.Contains("Canadian Provinces (1):", outp);
            Assert.Contains("DXCC Entities (10 / 10):", outp);
        }
    }
}
