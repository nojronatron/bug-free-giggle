using System.Threading.Tasks;

namespace ContestLogProcessor.Console.Interactive;

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
