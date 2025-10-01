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
            Assert.Throws<FileNotFoundException>(() => proc.ImportFile(fake));
        }

        [Fact]
        public void Import_EmptyFile_LoadsNoEntries()
        {
            var proc = new CabrilloLogProcessor();
            string tmp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmp, string.Empty);
                proc.ImportFile(tmp);
                Assert.Equal(0, proc.ReadEntries().ToList().Count);
            }
            finally
            {
                File.Delete(tmp);
            }
        }
    }
}
