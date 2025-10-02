using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive;

/// <summary>
/// Provides a wrapper around the <see cref="System.Console"/> class, implementing the <see cref="IConsole"/> interface
/// for reading and writing text to the console in an asynchronous manner.
/// </summary>
public class SystemConsoleWrapper : IConsole
{
    public Task<string?> ReadLineAsync()
    {
        return Task.FromResult(System.Console.ReadLine());
    }

    public void WriteLine(string? text)
    {
        System.Console.WriteLine(text);
    }

    public void Write(string? text)
    {
        System.Console.Write(text);
    }
}
