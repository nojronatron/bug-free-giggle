using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class OnlyBandsImportTest
{
    [Fact]
    public void ImportFile_BandOnlyLog_MapsBandToFrequencyAndMarksValid()
    {
        // Locate the test project root by walking parent directories until we find the Unittest project folder
        string dir = AppContext.BaseDirectory;
        DirectoryInfo? d = new DirectoryInfo(dir);
        DirectoryInfo? projectRoot = null;
        while (d != null)
        {
            if (string.Equals(d.Name, "ContestLogProcessor.Unittest", StringComparison.OrdinalIgnoreCase))
            {
                projectRoot = d;
                break;
            }
            d = d.Parent;
        }
        if (projectRoot == null) throw new InvalidOperationException("Could not locate test project directory to copy TestData file.");

        // Copy the test data file from the project TestData folder to a temp location since running tests
        // under bin/Debug won't automatically include the file. This ensures the test is hermetic.
        string source = Path.Combine(projectRoot.FullName, "Lib", "TestData", "K7XXX_Test_OnlyBands.log");
        string tmp = Path.Combine(Path.GetTempPath(), "clp_onlybands_" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            File.Copy(source, tmp);
            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            OperationResult<Unit> imp = proc.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            List<LogEntry> entries = proc.ReadEntriesResult().Value!.ToList();
            Assert.NotEmpty(entries);

            // Check a few entries to ensure mapping
            LogEntry first = entries.First();
            Assert.Equal("6m", first.Band);
            Assert.True(first.FrequencyIsValid);
            Assert.Equal("50000", first.Frequency); // normalized to kHz

            // ensure some other band mapped
            LogEntry last = entries.Last();
            Assert.Equal("10m", last.Band);
            Assert.True(last.FrequencyIsValid);
            Assert.Equal("28000", last.Frequency);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }
}
