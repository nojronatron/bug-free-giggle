namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents valid CATEGORY-TRANSMITTER values per Cabrillo v3 specification.
/// </summary>
public enum CategoryTransmitter
{
    One,
    Two,
    Limited,
    Unlimited,
    Swl
}

public static class CategoryTransmitterExtensions
{
    /// <summary>
    /// Parse a Cabrillo CATEGORY-TRANSMITTER string into the corresponding enum value.
    /// Returns true if the string matches a valid transmitter category; otherwise false.
    /// </summary>
    public static bool TryParse(string value, out CategoryTransmitter result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        string normalized = value.Trim().ToUpperInvariant();
        bool success = normalized switch
        {
            "ONE" => SetValue(CategoryTransmitter.One, out result),
            "TWO" => SetValue(CategoryTransmitter.Two, out result),
            "LIMITED" => SetValue(CategoryTransmitter.Limited, out result),
            "UNLIMITED" => SetValue(CategoryTransmitter.Unlimited, out result),
            "SWL" => SetValue(CategoryTransmitter.Swl, out result),
            _ => SetValue(default, out result, false)
        };
        return success;

        static bool SetValue(CategoryTransmitter val, out CategoryTransmitter outVal, bool ok = true)
        {
            outVal = val;
            return ok;
        }
    }

    /// <summary>
    /// Convert a CategoryTransmitter enum value to its Cabrillo v3 string representation.
    /// </summary>
    public static string ToCabrilloString(this CategoryTransmitter transmitter)
    {
        return transmitter switch
        {
            CategoryTransmitter.One => "ONE",
            CategoryTransmitter.Two => "TWO",
            CategoryTransmitter.Limited => "LIMITED",
            CategoryTransmitter.Unlimited => "UNLIMITED",
            CategoryTransmitter.Swl => "SWL",
            _ => throw new ArgumentOutOfRangeException(nameof(transmitter), transmitter, "Unknown CategoryTransmitter value")
        };
    }

    /// <summary>
    /// Get all valid Cabrillo CATEGORY-TRANSMITTER string values.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return [
            "ONE",
            "TWO",
            "LIMITED",
            "UNLIMITED",
            "SWL"
        ];
    }
}
