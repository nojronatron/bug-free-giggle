using System;
using System.IO;

using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class HeaderSanitizerTests
{
    [Fact]
    public void SanitizeHeaderValue_Masks_Malicious_Substring()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            // Header value contains a suspicious substring 'select * from' and is longer than 13 chars
            // Use a sanitizable header key (NAME) which our policy covers.
            File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCREATED-BY: innocent\r\nNAME: select * from users where id=1\r\nEND-OF-LOG:\r\n");

            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            var imp = proc.ImportFileResult(tmp);
            Assert.True(imp.IsSuccess);

            Assert.True(proc.TryGetHeader("NAME", out string? noteVal));
            Assert.NotNull(noteVal);
            // The suspicious substring should not appear verbatim
            Assert.DoesNotContain("select * from", noteVal!.ToLowerInvariant());
            // The sanitized value should contain masked characters ('*') and retain the trailing part
            Assert.Contains('*', noteVal);
            Assert.EndsWith(" users where id=1", noteVal);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public void SanitizeHeaderValue_Does_Not_Mask_Short_Benign_Values()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "START-OF-LOG: 3.0\r\nCREATED-BY: ShortName\r\nEND-OF-LOG:\r\n");

            CabrilloLogProcessor proc = new CabrilloLogProcessor();
            var imp2 = proc.ImportFileResult(tmp);
            Assert.True(imp2.IsSuccess);

            Assert.True(proc.TryGetHeader("CREATED-BY", out string? cb));
            Assert.Equal("ShortName", cb);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
