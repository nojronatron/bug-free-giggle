namespace ContestLogProcessor.Console.Interactive;

public interface IConsole
{
    System.Threading.Tasks.Task<string?> ReadLineAsync();
    void WriteLine(string? text);
    void Write(string? text);
}
