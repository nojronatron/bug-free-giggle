using ContestLogProcessor.Lib;
using Xunit;
using System.IO;

namespace ContestLogProcessor.Unittest.Lib
{
    public class ImportEdgeCaseTests
    {
        [Fact]
        public void Import_NonExistentFile_Throws()
        {
            var proc = new CabrilloLogProcessor();
            string fake = Path.Combine(Path.GetTempPath(), "this-file-does-not-exist-xyz.log");
            var res = proc.ImportFileResult(fake);
            Assert.False(res.IsSuccess);
            Assert.Equal(ResponseStatus.NotFound, res.Status);
        }

        [Fact]
        public void Import_EmptyFile_LoadsNoEntries()
        {
            var proc = new CabrilloLogProcessor();
            string tmp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmp, string.Empty);
                var res = proc.ImportFileResult(tmp);
                Assert.True(res.IsSuccess);
                var read = proc.ReadEntriesResult();
                Assert.True(read.IsSuccess);
                Assert.Empty(read.Value!.ToList());
            }
            finally
            {
                File.Delete(tmp);
            }
        }
    }
}
