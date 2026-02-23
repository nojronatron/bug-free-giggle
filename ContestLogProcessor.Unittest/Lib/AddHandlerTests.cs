using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

using Xunit;

namespace ContestLogProcessor.Unittest.Lib;

public class AddHandlerTests
{
    [Fact]
    public async Task Add_HappyPath_AddsEntryAndPrintsId()
    {
        // Arrange
        TestConsole testConsole = new TestConsole(
            // Responses in order: date, time, frequency, mode, callsign, theirCall, sentEx, recvEx
            new[] { "2025-09-30", "1200", "20", "PH", "K7TEST", "N0CALL", "599", "RRR" }
        );

        CabrilloLogProcessor processor = new CabrilloLogProcessor();
        CommandContext ctx = new CommandContext(processor, testConsole, debug: false);
        AddCommandHandler handler = new AddCommandHandler();

        // Act
        await handler.HandleAsync(new string[] { "add" }, ctx);

        // Assert
        IEnumerable<LogEntry> entries = processor.ReadEntriesResult().Value!;
        Assert.Single(entries);
        string consoleOutput = string.Join('\n', testConsole.Outputs);
        Assert.Contains("Added entry with Id:", consoleOutput);
    }
}
