using System;
using System.Text.RegularExpressions;
using ContestLogProcessor.Lib;

namespace ContestLogProcessor.WinterFieldDay;

/// <summary>
/// Winter Field Day exchange strategy for validating category+class+location exchanges.
/// Per WFD rules: Exchange consists of optional signal report + category (1-2 digits) + class (H/I/O/M) + location (1-5 chars).
/// Signal reports are informational only and not scored, so they may be omitted.
/// Examples: "59 3O WA", "1A CT", "5NN 2M OR", "3O WWA"
/// </summary>
public class WfdExchangeStrategy : IContestExchangeStrategy
{
    private static readonly Regex SignalReportRegex = new Regex(
        @"^(?:[1-5][0-9]{1,2}|[1-5][nN]{1,2})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex CategoryClassRegex = new Regex(
        @"^[0-9]{1,2}[HIOM]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex LocationRegex = new Regex(
        @"^\w{1,5}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public string ContestId => "WFD";

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
        return new[] { "Signal Report", "Category", "Class", "Location" };
    }

    public string GetExchangeFormatDescription()
    {
        return "Winter Field Day exchange: Signal report (optional: 59, 599, 5NN) + Category (1-2 digits) + Class (H/I/O/M) + Location (1-5 chars: ARRL/RAC section). Examples: '59 3O WA', '1A CT', '5NN 2M OR', '3O WWA'";
    }

    private OperationResult<bool> ValidateExchangeInternal(string? sig, string? msg, string direction)
    {
        // Signal reports are optional in WFD (not scored, informational only)
        // If a signal report is provided, validate it; otherwise skip validation
        if (!string.IsNullOrWhiteSpace(sig))
        {
            string trimmedSig = sig.Trim();
            if (!SignalReportRegex.IsMatch(trimmedSig))
            {
                return OperationResult.Failure<bool>(
                    $"WFD {direction} signal report '{sig}' is invalid. Must be 2-3 characters matching pattern: [1-5][0-9]{{1,2}} or [1-5][nN]{{1,2}}",
                    ResponseStatus.BadFormat);
            }
        }

        // Validate exchange message (category+class location)
        if (string.IsNullOrWhiteSpace(msg))
        {
            return OperationResult.Failure<bool>(
                $"WFD {direction} exchange message cannot be null or empty",
                ResponseStatus.BadFormat);
        }

        // Parse the exchange message into parts
        string[] parts = msg.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return OperationResult.Failure<bool>(
                $"WFD {direction} exchange must contain exactly 2 parts (category+class and location), found {parts.Length} parts in '{msg}'",
                ResponseStatus.BadFormat);
        }

        string categoryClassPart = parts[0];
        string locationPart = parts[1];

        // Validate category+class format (e.g., "3O", "1A", "2M")
        if (!CategoryClassRegex.IsMatch(categoryClassPart))
        {
            return OperationResult.Failure<bool>(
                $"WFD {direction} category+class '{categoryClassPart}' is invalid. Must be 1-2 digits followed by H, I, O, or M",
                ResponseStatus.BadFormat);
        }

        // Validate location format (e.g., "WA", "CT", "OR")
        if (!LocationRegex.IsMatch(locationPart))
        {
            return OperationResult.Failure<bool>(
                $"WFD {direction} location '{locationPart}' is invalid. Must be 1-5 word characters",
                ResponseStatus.BadFormat);
        }

        return OperationResult.Success(true);
    }
}
