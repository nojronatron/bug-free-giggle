using System;
using Xunit;
using ContestLogProcessor.Lib;
using ContestLogProcessor.WinterFieldDay;

namespace ContestLogProcessor.Unittest.WinterFieldDay;

/// <summary>
/// Tests for WinterFieldDayExchangeParser focusing on robust exchange parsing
/// while completely ignoring signal report content in signal fields.
/// Test cases based on examples from test-wfd-simple.log.
/// </summary>
public class WinterFieldDayExchangeParserTests
{
    private readonly WinterFieldDayExchangeParser _parser = new();

    #region Sent Exchange Parsing Tests

    [Theory]
    [InlineData("", "1O OH", 1, 'O', "OH")]
    [InlineData("5NN", "1O OH", 1, 'O', "OH")]
    [InlineData("599", "1O OH", 1, 'O', "OH")]
    [InlineData("59", "1O OH", 1, 'O', "OH")]
    [InlineData(null, "1O OH", 1, 'O', "OH")]
    [InlineData("INVALID_SIGNAL", "1O OH", 1, 'O', "OH")]
    public void ParseSentExchange_ValidExchangeWithVariousSignals_ExtractsCorrectWfdInfo(
        string sentSig, string sentMsg, int expectedCategory, char expectedClass, string expectedLocation)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal($"{sentMsg}", result.Value!.RawExchange);
        Assert.Equal(expectedCategory, result.Value.Category);
        Assert.Equal(expectedClass, result.Value.Class);
        Assert.Equal(expectedLocation, result.Value.Location);
    }

    [Theory]
    [InlineData("", "2H IL", 2, 'H', "IL")]
    [InlineData("5NN", "14I LA", 14, 'I', "LA")]
    [InlineData("555", "2M WTX", 2, 'M', "WTX")]
    [InlineData("", "1O EMA", 1, 'O', "EMA")]
    [InlineData("", "1M LAX", 1, 'M', "LAX")]
    [InlineData("", "2H MN", 2, 'H', "MN")]
    public void ParseSentExchange_TestLogExamples_ExtractsCorrectWfdInfo(
        string sentSig, string sentMsg, int expectedCategory, char expectedClass, string expectedLocation)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(expectedCategory, result.Value!.Category);
        Assert.Equal(expectedClass, result.Value.Class);
        Assert.Equal(expectedLocation, result.Value.Location);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("5NN", "")]
    [InlineData("599", null)]
    [InlineData("", null)]
    public void ParseSentExchange_EmptyOrNullMessage_ReturnsFailure(string sentSig, string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Exchange message cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "1O")]
    [InlineData("5NN", "ONLY_ONE_PART")]
    [InlineData("599", "TOO MANY PARTS HERE")]
    public void ParseSentExchange_InvalidPartCount_ReturnsFailure(string sentSig, string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("must contain exactly 2 parts", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "INVALID OH")]
    [InlineData("5NN", "99X OH")]
    [InlineData("599", "1Z OH")]
    public void ParseSentExchange_InvalidCategoryClass_ReturnsFailure(string sentSig, string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("does not match required format", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "1O TOOLONG")]
    [InlineData("5NN", "1O 123456")]
    [InlineData("599", "1O @#$")]
    public void ParseSentExchange_InvalidLocation_ReturnsFailure(string sentSig, string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("does not match required format", result.ErrorMessage);
    }

    #endregion

    #region Received Exchange Parsing Tests

    [Theory]
    [InlineData("", "2H IL", 2, 'H', "IL")]
    [InlineData("5NN", "14I LA", 14, 'I', "LA")]
    [InlineData("555", "2M WTX", 2, 'M', "WTX")]
    [InlineData("", "1O EMA", 1, 'O', "EMA")]
    [InlineData("", "1M LAX", 1, 'M', "LAX")]
    [InlineData("", "2H MN", 2, 'H', "MN")]
    [InlineData(null, "1H GA", 1, 'H', "GA")]
    [InlineData("INVALID_SIGNAL", "3I CT", 3, 'I', "CT")]
    public void ParseReceivedExchange_ValidExchangeWithVariousSignals_ExtractsCorrectWfdInfo(
        string receivedSig, string receivedMsg, int expectedCategory, char expectedClass, string expectedLocation)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal($"{receivedMsg}", result.Value!.RawExchange);
        Assert.Equal(expectedCategory, result.Value.Category);
        Assert.Equal(expectedClass, result.Value.Class);
        Assert.Equal(expectedLocation, result.Value.Location);
    }

    [Theory]
    [InlineData("1O OH", 1, 'O', "OH")]
    [InlineData("2H IL", 2, 'H', "IL")]
    [InlineData("14I LA", 14, 'I', "LA")]
    [InlineData("2M WTX", 2, 'M', "WTX")]
    [InlineData("1M LAX", 1, 'M', "LAX")]
    [InlineData("99H DX", 99, 'H', "DX")]
    public void ParseReceivedExchange_AllValidClassTypes_ExtractsCorrectInfo(
        string receivedMsg, int expectedCategory, char expectedClass, string expectedLocation)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange("", receivedMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(expectedCategory, result.Value!.Category);
        Assert.Equal(expectedClass, result.Value.Class);
        Assert.Equal(expectedLocation, result.Value.Location);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("5NN", "")]
    [InlineData("555", null)]
    [InlineData("", null)]
    public void ParseReceivedExchange_EmptyOrNullMessage_ReturnsFailure(string receivedSig, string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("Exchange message cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "2H")]
    [InlineData("5NN", "ONLY_ONE_PART")]
    [InlineData("555", "TOO MANY PARTS HERE")]
    public void ParseReceivedExchange_InvalidPartCount_ReturnsFailure(string receivedSig, string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("must contain exactly 2 parts", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "INVALID IL")]
    [InlineData("5NN", "100X LA")]
    [InlineData("555", "2Z WTX")]
    public void ParseReceivedExchange_InvalidCategoryClass_ReturnsFailure(string receivedSig, string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("does not match required format", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "2H TOOLONG")]
    [InlineData("5NN", "14I 123456")]
    [InlineData("555", "2M @#$")]
    public void ParseReceivedExchange_InvalidLocation_ReturnsFailure(string receivedSig, string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResponseStatus.BadFormat, result.Status);
        Assert.Contains("does not match required format", result.ErrorMessage);
    }

    #endregion

    #region Signal Field Independence Tests

    [Theory]
    [InlineData("", "1O OH")]
    [InlineData("59", "1O OH")]
    [InlineData("5NN", "1O OH")]
    [InlineData("599", "1O OH")]
    [InlineData("555", "1O OH")]
    [InlineData("COMPLETELY_INVALID", "1O OH")]
    [InlineData("123ABC", "1O OH")]
    [InlineData(null, "1O OH")]
    public void ParseSentExchange_SignalFieldIndependence_SameResults(string sentSig, string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange(sentSig, sentMsg);

        // Assert - All should succeed with identical results regardless of signal field content
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.Category);
        Assert.Equal('O', result.Value.Class);
        Assert.Equal("OH", result.Value.Location);
    }

    [Theory]
    [InlineData("", "14I LA")]
    [InlineData("59", "14I LA")]
    [InlineData("5NN", "14I LA")]
    [InlineData("599", "14I LA")]
    [InlineData("555", "14I LA")]
    [InlineData("COMPLETELY_INVALID", "14I LA")]
    [InlineData("123ABC", "14I LA")]
    [InlineData(null, "14I LA")]
    public void ParseReceivedExchange_SignalFieldIndependence_SameResults(string receivedSig, string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange(receivedSig, receivedMsg);

        // Assert - All should succeed with identical results regardless of signal field content
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(14, result.Value!.Category);
        Assert.Equal('I', result.Value.Class);
        Assert.Equal("LA", result.Value.Location);
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("1h oh")] // lowercase class
    [InlineData("1i oh")] // lowercase class
    [InlineData("1m oh")] // lowercase class
    [InlineData("1o oh")] // lowercase class
    public void ParseSentExchange_LowercaseClass_HandlesCorrectly(string sentMsg)
    {
        // Act
        OperationResult<WfdInfoSent> result = _parser.ParseSentExchange("", sentMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.Category);
        Assert.True(char.IsUpper(result.Value.Class), "Class should be normalized to uppercase");
    }

    [Theory]
    [InlineData("1O oh")] // lowercase location
    [InlineData("1O OH")] // uppercase location
    [InlineData("1O Oh")] // mixed case location
    public void ParseReceivedExchange_LocationCaseHandling_NormalizesCorrectly(string receivedMsg)
    {
        // Act
        OperationResult<WfdInfoReceived> result = _parser.ParseReceivedExchange("", receivedMsg);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal("OH", result.Value!.Location); // Should be normalized to uppercase
    }

    #endregion
}