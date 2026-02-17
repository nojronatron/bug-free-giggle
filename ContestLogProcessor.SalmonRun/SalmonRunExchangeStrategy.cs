using System;
using System.Text.RegularExpressions;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.SalmonRun;

/// <summary>
/// Salmon Run exchange strategy for validating location-based exchanges.
/// Per Salmon Run rules: Exchange consists of signal report + location (1-5 chars).
/// Examples: "59 KING", "599 OR", "5NN WHI"
/// </summary>
public class SalmonRunExchangeStrategy : IContestExchangeStrategy
{
    private static readonly Regex SignalReportRegex = new Regex(
        @"^(?:[1-5][0-9]{1,2}|[1-5][nN]{1,2})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex LocationRegex = new Regex(
        @"^[A-Za-z0-9]{1,5}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public string ContestId => "SALMON-RUN";

    public OperationResult<bool> ValidateSentExchange(string? sentSig, string? sentMsg)
    {
        return ValidateExchangeInternal(sentSig, sentMsg, "sent");
    }

    public OperationResult<bool> ValidateReceivedExchange(string? receivedSig, string? receivedMsg)
    {
        return ValidateExchangeInternal(receivedSig, receivedMsg, "received");
    }

    public OperationResult<bool> ValidateExchange(Exchange exchange)
    {
        if (exchange == null)
        {
            return OperationResult.Failure<bool>(
                "Exchange cannot be null",
                ResponseStatus.BadFormat);
        }

        OperationResult<bool> sentResult = ValidateSentExchange(exchange.SentSig, exchange.SentMsg);
        if (!sentResult.IsSuccess)
        {
            return sentResult;
        }

        OperationResult<bool> receivedResult = ValidateReceivedExchange(exchange.ReceivedSig, exchange.ReceivedMsg);
        if (!receivedResult.IsSuccess)
        {
            return receivedResult;
        }

        return OperationResult.Success(true);
    }

    public string[] GetRequiredFields()
    {
        return new[] { "Signal Report", "Location" };
    }

    public string GetExchangeFormatDescription()
    {
        return "Salmon Run exchange: Signal report (2-3 chars: 59, 599, 5NN) + Location (1-5 chars: county abbreviation or state/province code). Examples: '59 KING', '599 OR', '5NN WHI'";
    }

    private OperationResult<bool> ValidateExchangeInternal(string? sig, string? msg, string direction)
    {
        // Validate signal report
        if (string.IsNullOrWhiteSpace(sig))
        {
            return OperationResult.Failure<bool>(
                $"Salmon Run {direction} signal report cannot be null or empty",
                ResponseStatus.BadFormat);
        }

        if (!SignalReportRegex.IsMatch(sig.Trim()))
        {
            return OperationResult.Failure<bool>(
                $"Salmon Run {direction} signal report '{sig}' is invalid. Must be 2-3 characters matching pattern: [1-5][0-9]{{1,2}} or [1-5][nN]{{1,2}}",
                ResponseStatus.BadFormat);
        }

        // Validate location
        if (string.IsNullOrWhiteSpace(msg))
        {
            return OperationResult.Failure<bool>(
                $"Salmon Run {direction} location cannot be null or empty",
                ResponseStatus.BadFormat);
        }

        string location = msg.Trim();
        if (location.Length < 1 || location.Length > 5)
        {
            return OperationResult.Failure<bool>(
                $"Salmon Run {direction} location '{msg}' must be 1-5 characters",
                ResponseStatus.BadFormat);
        }

        if (!LocationRegex.IsMatch(location))
        {
            return OperationResult.Failure<bool>(
                $"Salmon Run {direction} location '{msg}' contains invalid characters. Must be alphanumeric only",
                ResponseStatus.BadFormat);
        }

        return OperationResult.Success(true);
    }
}
