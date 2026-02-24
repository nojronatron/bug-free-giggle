using ContestLogProcessor.Lib;
using ContestLogProcessor.SalmonRun;

using Xunit;

namespace ContestLogProcessor.Unittest.SalmonRun;

public class SalmonRunExchangeStrategyTests
{
    private readonly SalmonRunExchangeStrategy _strategy;

    public SalmonRunExchangeStrategyTests()
    {
        _strategy = new SalmonRunExchangeStrategy();
    }

    [Fact]
    public void ContestId_ReturnsCorrectIdentifier()
    {
        Assert.Equal("SALMON-RUN", _strategy.ContestId);
    }

    [Theory]
    [InlineData("59", "KING")]
    [InlineData("599", "WHI")]
    [InlineData("5NN", "OR")]
    [InlineData("5nn", "wa")]
    [InlineData("159", "BC")]
    public void ValidateSentExchange_WithValidData_ReturnsSuccess(string sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData("59", "KING")]
    [InlineData("599", "OR")]
    [InlineData("5NN", "WHI")]
    [InlineData("5nn", "JEFF")]
    [InlineData("159", "B")]
    public void ValidateReceivedExchange_WithValidData_ReturnsSuccess(string sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateReceivedExchange(sig, msg);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData(null, "KING")]
    [InlineData("", "KING")]
    [InlineData("   ", "KING")]
    public void ValidateSentExchange_WithNullOrEmptySignal_ReturnsFailure(string? sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("signal report cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", null)]
    [InlineData("59", "")]
    [InlineData("59", "   ")]
    public void ValidateSentExchange_WithNullOrEmptyLocation_ReturnsFailure(string sig, string? msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("location cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("09", "KING")]    // Invalid: starts with 0
    [InlineData("6", "KING")]     // Invalid: too short
    [InlineData("659", "KING")]   // Invalid: starts with 6
    [InlineData("5999", "KING")]  // Invalid: too long
    [InlineData("ABC", "KING")]   // Invalid: letters other than N
    public void ValidateSentExchange_WithInvalidSignalReport_ReturnsFailure(string sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("signal report", result.ErrorMessage);
        Assert.Contains("invalid", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", "TOOLONG")]   // Invalid: 7 characters
    [InlineData("59", "ABCDEF")]    // Invalid: 6 characters (max is 5)
    [InlineData("59", "")]          // Invalid: empty
    public void ValidateSentExchange_WithInvalidLocationLength_ReturnsFailure(string sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("location", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", "K-7")]       // Invalid: contains dash
    [InlineData("59", "KI NG")]     // Invalid: contains space
    [InlineData("59", "K@NG")]      // Invalid: contains special char
    public void ValidateSentExchange_WithInvalidLocationCharacters_ReturnsFailure(string sig, string msg)
    {
        OperationResult<bool> result = _strategy.ValidateSentExchange(sig, msg);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("invalid characters", result.ErrorMessage);
    }

    [Fact]
    public void ValidateExchange_WithValidExchange_ReturnsSuccess()
    {
        Exchange exchange = new Exchange
        {
            SentSig = "59",
            SentMsg = "KING",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "OR"
        };

        OperationResult<bool> result = _strategy.ValidateExchange(exchange);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void ValidateExchange_WithNullExchange_ReturnsFailure()
    {
        OperationResult<bool> result = _strategy.ValidateExchange(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Exchange cannot be null", result.ErrorMessage);
    }

    [Fact]
    public void ValidateExchange_WithInvalidSentData_ReturnsFailure()
    {
        Exchange exchange = new Exchange
        {
            SentSig = "INVALID",
            SentMsg = "KING",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "OR"
        };

        OperationResult<bool> result = _strategy.ValidateExchange(exchange);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void ValidateExchange_WithInvalidReceivedData_ReturnsFailure()
    {
        Exchange exchange = new Exchange
        {
            SentSig = "59",
            SentMsg = "KING",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "TOOLONG"
        };

        OperationResult<bool> result = _strategy.ValidateExchange(exchange);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void GetRequiredFields_ReturnsCorrectFields()
    {
        string[] fields = _strategy.GetRequiredFields();

        Assert.Equal(2, fields.Length);
        Assert.Contains("Signal Report", fields);
        Assert.Contains("Location", fields);
    }

    [Fact]
    public void GetExchangeFormatDescription_ReturnsNonEmptyString()
    {
        string description = _strategy.GetExchangeFormatDescription();

        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Contains("Salmon Run", description);
        Assert.Contains("59", description);
        Assert.Contains("KING", description);
    }
}
