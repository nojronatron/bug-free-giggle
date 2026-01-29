namespace ContestLogProcessor.Lib;

/// <summary>
/// Marker interface for contest-specific sent exchange information.
/// Contest implementations should create concrete types implementing this interface.
/// </summary>
public interface IInfoSent
{
    /// <summary>
    /// Return the raw exchange string as it appears in the log file.
    /// </summary>
    string RawExchange { get; }
}