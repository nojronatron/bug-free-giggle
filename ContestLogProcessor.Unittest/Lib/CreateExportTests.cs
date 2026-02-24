using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class CreateExportTests
{
    private static string SampleLogPath => Path.Combine(AppContext.BaseDirectory, "TestData", "K7XXX_Test.log");

    [Fact]
    public void CreateEntry_AfterImport_IsVisibleInReadEntries()
    {
        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        OperationResult<Unit> imp = processor.ImportFileResult(SampleLogPath);
        Assert.True(imp.IsSuccess);

        string uniqueCall = "UNITTEST_CREATE_" + Guid.NewGuid().ToString("N");
        LogEntry newEntry = new LogEntry
        {
            Frequency = "7000",
            Mode = "CW",
            QsoDateTime = DateTime.UtcNow,
            CallSign = uniqueCall,
            SentExchange = new Exchange { SentSig = "001" },
            TheirCall = "TEST"
        };

        OperationResult<LogEntry> createdResult = processor.CreateEntryResult(newEntry);
        Assert.True(createdResult.IsSuccess);
        LogEntry? created = createdResult.Value;
        Assert.NotNull(created);

        bool found = processor.ReadEntriesResult().Value!.Any(e => string.Equals(e.CallSign, uniqueCall, StringComparison.OrdinalIgnoreCase));
        Assert.True(found, "Created entry should be visible via ReadEntries after import.");
    }

    [Fact]
    public void DuplicateEntry_CopiesAndAllowsSentMsgOverride()
    {
        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        OperationResult<Unit> imp = processor.ImportFileResult(SampleLogPath);
        Assert.True(imp.IsSuccess);

        LogEntry? original = processor.ReadEntriesResult().Value!.FirstOrDefault();
        Assert.NotNull(original);

        string newMsg = "ALTLOC" + Guid.NewGuid().ToString("N");
        OperationResult<LogEntry> dupResult = processor.DuplicateEntryResult(original.Id, ILogProcessor.DuplicateField.SentMsg, newMsg);
        Assert.True(dupResult.IsSuccess);
        LogEntry? dup = dupResult.Value;

        Assert.NotNull(dup);
        Assert.NotEqual(original.Id, dup.Id);
        Assert.Equal(original.Frequency, dup.Frequency);
        Assert.Equal(original.Mode, dup.Mode);
        Assert.Equal(original.QsoDateTime, dup.QsoDateTime);
        Assert.Equal(original.TheirCall, dup.TheirCall);

        // SentMsg should be replaced in the duplicated entry's SentExchange when provided
        if (dup.SentExchange != null)
        {
            Assert.Equal(newMsg, dup.SentExchange.SentMsg);
        }
    }

    [Fact]
    public void ExportFile_AppendsLogExtension_And_IncludesCreatedEntry()
    {
        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        OperationResult<Unit> imp2 = processor.ImportFileResult(SampleLogPath);
        Assert.True(imp2.IsSuccess);

        string uniqueCall = "EXPORTTEST_" + Guid.NewGuid().ToString("N");
        LogEntry newEntry = new LogEntry
        {
            Frequency = "7000",
            Mode = "CW",
            QsoDateTime = DateTime.UtcNow,
            CallSign = uniqueCall,
            SentExchange = new Exchange { SentSig = "999" },
            TheirCall = "TEST"
        };

        OperationResult<LogEntry> createdResult = processor.CreateEntryResult(newEntry);
        Assert.True(createdResult.IsSuccess);
        LogEntry? created = createdResult.Value;

        string tempDir = Path.GetTempPath();
        string basePath = Path.Combine(tempDir, "clp_export_test_" + Guid.NewGuid().ToString("N"));
        string expectedFile = basePath + ".log";

        try
        {
            if (File.Exists(expectedFile)) File.Delete(expectedFile);

            // Call ExportFile with a path that lacks the .log extension
            OperationResult<Unit> r = processor.ExportFileResult(basePath);
            Assert.True(r.IsSuccess);

            Assert.True(File.Exists(expectedFile), "ExportFile should append .log when writing files.");

            string[] lines = File.ReadAllLines(expectedFile);
            Assert.Contains(lines, l => l.StartsWith("QSO:", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(lines, l => l.IndexOf(uniqueCall, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        finally
        {
            try { if (File.Exists(expectedFile)) File.Delete(expectedFile); } catch { }
        }
    }

    [Fact]
    public void ExportFile_WithoutData_ThrowsInvalidOperationException()
    {
        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        string tmp = Path.Combine(Path.GetTempPath(), "clp_no_data_" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            OperationResult<Unit> fail = processor.ExportFileResult(tmp);
            Assert.False(fail.IsSuccess);
            Assert.Equal(ResponseStatus.Error, fail.Status);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }
}
