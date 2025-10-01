using System.Threading.Tasks;
using Xunit;
using ContestLogProcessor.Console.Interactive;
using ContestLogProcessor.Console.Interactive.Handlers;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.Unittest.Lib;

public class AddHandlerTests
{
    [Fact]
    public async Task Add_HappyPath_AddsEntryAndPrintsId()
    {
        // Arrange
        var testConsole = new TestConsole(
            // Responses in order: date, time, frequency, mode, callsign, theirCall, sentEx, recvEx
            new[] { "2025-09-30", "1200", "20", "PH", "K7TEST", "N0CALL", "599", "RRR" }
        );

        var processor = new CabrilloLogProcessor();
        var ctx = new CommandContext(processor, testConsole, debug: false);
        var handler = new AddCommandHandler();

        // Act
        await handler.HandleAsync(new string[] { "add" }, ctx);

        // Assert
        var entries = processor.ReadEntries();
        Assert.Single(entries);
    string consoleOutput = string.Join('\n', testConsole.Outputs);
    Assert.Contains("Added entry with Id:", consoleOutput);
    }
}
