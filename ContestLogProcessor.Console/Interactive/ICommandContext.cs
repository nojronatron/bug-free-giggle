using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Console.Interactive;

public interface ICommandContext
{
    CabrilloLogProcessor Processor { get; }
    bool Debug { get; }
    IConsole Console { get; }
}
