using ContestLogProcessor.Lib;
using ContestLogProcessor.Lib.Formatters;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class FormatterTests
{
    [Fact]
    public void AdifFormatter_FormatsBasicEntry()
    {
        LogEntry e = new LogEntry { CallSign = "K7RMZ", Frequency = "14000", Mode = "CW", QsoDateTime = new DateTime(2025, 9, 26, 21, 0, 0, DateTimeKind.Utc) };
        AdifFormatter f = new AdifFormatter();
        bool ok = f.TryFormat(e, out string s);
        Assert.True(ok);
        Assert.Contains("<CALL:5>K7RMZ", s);
        Assert.Contains("<FREQ:5>14000", s);
        Assert.Contains("<EOR>", s);
    }

    [Fact]
    public void FormatterRegistry_Returns_CabrilloAndAdif()
    {
        IReadOnlyList<ILogEntryFormatter> all = FormatterRegistry.GetAll();
        Assert.Contains(all, f => string.Equals(f.Name, "cabrillo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(all, f => string.Equals(f.Name, "adif", StringComparison.OrdinalIgnoreCase));
        Assert.True(FormatterRegistry.TryGet("cabrillo", out ILogEntryFormatter? cab));
        Assert.NotNull(cab);
    }
}
