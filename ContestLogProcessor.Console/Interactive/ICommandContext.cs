using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive;

public interface ICommandContext
{
    ILogProcessor Processor { get; }
    bool Debug { get; }
    IConsole Console { get; }
    void RequestExit();
    bool ExitRequested { get; }
}
