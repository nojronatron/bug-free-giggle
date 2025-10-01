using ContestLogProcessor.Console.Interactive;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContestLogProcessor.Unittest.Lib.TestHelpers;

public class TestConsole : IConsole
{
    private readonly Queue<string?> _inputs = new();
    public readonly List<string> Outputs = new();

    public TestConsole(IEnumerable<string?> inputs)
    {
        foreach (var s in inputs) _inputs.Enqueue(s);
    }

    public Task<string?> ReadLineAsync()
    {
        if (_inputs.Count == 0) return Task.FromResult<string?>(null);
        return Task.FromResult(_inputs.Dequeue());
    }

    public void WriteLine(string? text)
    {
        Outputs.Add(text ?? string.Empty);
    }

    public void Write(string? text)
    {
        // Write without newline - add as-is
        Outputs.Add(text ?? string.Empty);
    }
}
