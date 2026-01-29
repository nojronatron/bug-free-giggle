namespace ContestLogProcessor.Lib;

/// <summary>
/// Interface for parsing contest-specific exchange information from Cabrillo v3 QSO data.
/// Contest implementations provide concrete parsers for their specific exchange formats.
/// </summary>
/// <typeparam name="TSent">Contest-specific type implementing IInfoSent</typeparam>
/// <typeparam name="TReceived">Contest-specific type implementing IInfoReceived</typeparam>
public interface IExchangeParser<TSent, TReceived>
    where TSent : IInfoSent
    where TReceived : IInfoReceived
{
    /// <summary>
    /// Parse the sent exchange information from raw exchange string.
    /// Returns OperationResult with parsed exchange or failure with validation errors.
    /// </summary>
    OperationResult<TSent> ParseSentExchange(string sentSig, string sentMsg);

    /// <summary>
    /// Parse the received exchange information from raw exchange string.
    /// Returns OperationResult with parsed exchange or failure with validation errors.
    /// </summary>
    OperationResult<TReceived> ParseReceivedExchange(string receivedSig, string receivedMsg);
}