using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive;

public class CommandContext : ICommandContext
{
    public CommandContext(CabrilloLogProcessor processor, IConsole console, bool debug)
    {
        Processor = processor;
        Console = console;
        Debug = debug;
    }

    public CabrilloLogProcessor Processor { get; }
    public bool Debug { get; }
    public IConsole Console { get; }

    private bool _exitRequested = false;

    public void RequestExit()
    {
        _exitRequested = true;
    }

    public bool ExitRequested => _exitRequested;
}
