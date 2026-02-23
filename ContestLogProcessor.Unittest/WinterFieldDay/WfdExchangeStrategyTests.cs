using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;
using Xunit;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

public class WfdExchangeStrategyTests
{
    private readonly WfdExchangeStrategy _strategy;

    public WfdExchangeStrategyTests()
    {
        _strategy = new WfdExchangeStrategy();
    }

    [Fact]
    public void ContestId_ReturnsCorrectIdentifier()
    {
        Assert.Equal("WFD", _strategy.ContestId);
    }

    [Theory]
    [InlineData("59", "3O WA")]
    [InlineData("599", "1I CT")]
    [InlineData("5NN", "2M OR")]
    [InlineData("5nn", "10h AL")]
    [InlineData("159", "1I BC")]
    public void ValidateSentExchange_WithValidData_ReturnsSuccess(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData("59", "3O WA")]
    [InlineData("599", "1I CT")]
    [InlineData("5NN", "2M OR")]
    [InlineData("5nn", "5o TX")]
    [InlineData("159", "12H AZ")]
    public void ValidateReceivedExchange_WithValidData_ReturnsSuccess(string sig, string msg)
    {
        var result = _strategy.ValidateReceivedExchange(sig, msg);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData(null, "3O WA")]
    [InlineData("", "3O WA")]
    [InlineData("   ", "3O WA")]
    public void ValidateSentExchange_WithNullOrEmptySignal_ReturnsSuccess(string? sig, string msg)
    {
        // Signal reports are optional in WFD (not scored)
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData("59", null)]
    [InlineData("59", "")]
    [InlineData("59", "   ")]
    public void ValidateSentExchange_WithNullOrEmptyMessage_ReturnsFailure(string sig, string? msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("exchange message cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("09", "3O WA")]    // Invalid: starts with 0
    [InlineData("6", "3O WA")]     // Invalid: too short
    [InlineData("659", "3O WA")]   // Invalid: starts with 6
    [InlineData("5999", "3O WA")]  // Invalid: too long
    [InlineData("ABC", "3O WA")]   // Invalid: letters other than N
    public void ValidateSentExchange_WithInvalidSignalReport_ReturnsFailure(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("signal report", result.ErrorMessage);
        Assert.Contains("invalid", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", "3O")]         // Invalid: missing location
    [InlineData("59", "3O WA NV")]   // Invalid: too many parts
    [InlineData("59", "WA")]         // Invalid: only one part
    public void ValidateSentExchange_WithInvalidPartCount_ReturnsFailure(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("exactly 2 parts", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", "3X WA")]      // Invalid: X is not valid class
    [InlineData("59", "O WA")]       // Invalid: missing category number
    [InlineData("59", "100O WA")]    // Invalid: too many digits
    [InlineData("59", "3 WA")]       // Invalid: missing class letter
    public void ValidateSentExchange_WithInvalidCategoryClass_ReturnsFailure(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("category+class", result.ErrorMessage);
        Assert.Contains("invalid", result.ErrorMessage);
    }

    [Theory]
    [InlineData("59", "3O TOOLONG")]  // Invalid: location too long
    [InlineData("59", "3O W-A")]      // Invalid: location has dash
    [InlineData("59", "3O W A")]      // Invalid: embedded space treated as separate part
    public void ValidateSentExchange_WithInvalidLocation_ReturnsFailure(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void ValidateExchange_WithValidExchange_ReturnsSuccess()
    {
        var exchange = new Exchange
        {
            SentSig = "59",
            SentMsg = "3O WA",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "1I CT"
        };

        var result = _strategy.ValidateExchange(exchange);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void ValidateExchange_WithNullExchange_ReturnsFailure()
    {
        var result = _strategy.ValidateExchange(null!);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Exchange cannot be null", result.ErrorMessage);
    }

    [Fact]
    public void ValidateExchange_WithInvalidSentData_ReturnsFailure()
    {
        var exchange = new Exchange
        {
            SentSig = "INVALID",
            SentMsg = "3O WA",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "1A CT"
        };

        var result = _strategy.ValidateExchange(exchange);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void ValidateExchange_WithInvalidReceivedData_ReturnsFailure()
    {
        var exchange = new Exchange
        {
            SentSig = "59",
            SentMsg = "3O WA",
            TheirCall = "K7XXX",
            ReceivedSig = "599",
            ReceivedMsg = "INVALID"
        };

        var result = _strategy.ValidateExchange(exchange);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
    }

    [Fact]
    public void GetRequiredFields_ReturnsCorrectFields()
    {
        string[] fields = _strategy.GetRequiredFields();
        
        Assert.Equal(4, fields.Length);
        Assert.Contains("Signal Report", fields);
        Assert.Contains("Category", fields);
        Assert.Contains("Class", fields);
        Assert.Contains("Location", fields);
    }

    [Fact]
    public void GetExchangeFormatDescription_ReturnsNonEmptyString()
    {
        string description = _strategy.GetExchangeFormatDescription();
        
        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Contains("Winter Field Day", description);
        Assert.Contains("3O", description);
        Assert.Contains("WA", description);
    }

    [Theory]
    [InlineData("59", "1H WA")]    // Home
    [InlineData("59", "2I OR")]    // Indoor
    [InlineData("59", "3O CT")]    // Outdoor
    [InlineData("59", "4M AZ")]    // Mobile
    [InlineData("59", "1h wa")]    // Lowercase valid
    public void ValidateSentExchange_WithAllValidClasses_ReturnsSuccess(string sig, string msg)
    {
        var result = _strategy.ValidateSentExchange(sig, msg);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }
}
