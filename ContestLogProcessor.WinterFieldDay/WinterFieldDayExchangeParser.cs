using System;
using System.Text.RegularExpressions;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Winter Field Day exchange parser for category+class+location format.
/// Validates exchange using regex patterns as specified: [0-9]{1,2}(?:H|I|O|M) and \w{1,5}
/// </summary>
public class WinterFieldDayExchangeParser : IExchangeParser<WfdInfoSent, WfdInfoReceived>
{
    private static readonly Regex CategoryClassRegex = new Regex(@"^([0-9]{1,2})([HIOM])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LocationRegex = new Regex(@"^\w{1,5}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public OperationResult<WfdInfoSent> ParseSentExchange(string sentSig, string sentMsg)
    {
        return ParseSentExchange(sentSig, sentMsg, (raw, category, classId, location) => 
            new WfdInfoSent(raw, category, classId, location));
    }

    public OperationResult<WfdInfoReceived> ParseReceivedExchange(string receivedSig, string receivedMsg)
    {
        return ParseReceivedExchange(receivedSig, receivedMsg, (raw, category, classId, location) => 
            new WfdInfoReceived(raw, category, classId, location));
    }

    private OperationResult<WfdInfoSent> ParseSentExchange(string sig, string msg, Func<string, int, char, string, WfdInfoSent> factory)
    {
        return ParseExchangeInternal(msg, factory);
    }

    private OperationResult<WfdInfoReceived> ParseReceivedExchange(string sig, string msg, Func<string, int, char, string, WfdInfoReceived> factory)
    {
        return ParseExchangeInternal(msg, factory);
    }

    private OperationResult<T> ParseExchangeInternal<T>(string msg, Func<string, int, char, string, T> factory)
    {
        if (string.IsNullOrWhiteSpace(msg))
        {
            return OperationResult.Failure<T>("Exchange message cannot be null or empty", ResponseStatus.BadFormat);
        }

        string[] parts = msg.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return OperationResult.Failure<T>(
                $"Winter Field Day exchange must contain exactly 2 parts (category+class and location), found {parts.Length} parts",
                ResponseStatus.BadFormat);
        }

        string categoryClassPart = parts[0];
        string locationPart = parts[1];

        // Validate category+class format: [0-9]{1,2}(?:H|I|O|M)
        Match categoryMatch = CategoryClassRegex.Match(categoryClassPart);
        if (!categoryMatch.Success)
        {
            return OperationResult.Failure<T>(
                $"Category+Class '{categoryClassPart}' does not match required format [0-9]{{1,2}}[HIOM]",
                ResponseStatus.BadFormat);
        }

        // Validate location format: \w{1,5}
        if (!LocationRegex.IsMatch(locationPart))
        {
            return OperationResult.Failure<T>(
                $"Location '{locationPart}' does not match required format \\w{{1,5}}",
                ResponseStatus.BadFormat);
        }

        // Parse category (number of transmitters)
        if (!int.TryParse(categoryMatch.Groups[1].Value, out int category) || category < 1)
        {
            return OperationResult.Failure<T>(
                $"Category '{categoryMatch.Groups[1].Value}' must be a positive integer",
                ResponseStatus.BadFormat);
        }

        // Parse class identifier
        char classId = char.ToUpper(categoryMatch.Groups[2].Value[0]);

        string rawExchange = msg.Trim();
        T result = factory(rawExchange, category, classId, locationPart.ToUpperInvariant());
        
        return OperationResult.Success(result);
    }
}