namespace ContestLogProcessor.Lib;

/// <summary>
/// Represents valid CATEGORY-STATION values per Cabrillo v3 specification.
/// </summary>
public enum CategoryStation
{
    Distributed,
    Fixed,
    Mobile,
    Portable,
    Rover,
    RoverLimited,
    RoverUnlimited,
    Expedition,
    Hq,
    School,
    Explorer
}

public static class CategoryStationExtensions
{
    /// <summary>
    /// Parse a Cabrillo CATEGORY-STATION string into the corresponding enum value.
    /// Returns true if the string matches a valid station category; otherwise false.
    /// </summary>
    public static bool TryParse(string value, out CategoryStation result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        string normalized = value.Trim().ToUpperInvariant();
        bool success = normalized switch
        {
            "DISTRIBUTED" => SetValue(CategoryStation.Distributed, out result),
            "FIXED" => SetValue(CategoryStation.Fixed, out result),
            "MOBILE" => SetValue(CategoryStation.Mobile, out result),
            "PORTABLE" => SetValue(CategoryStation.Portable, out result),
            "ROVER" => SetValue(CategoryStation.Rover, out result),
            "ROVER-LIMITED" => SetValue(CategoryStation.RoverLimited, out result),
            "ROVER-UNLIMITED" => SetValue(CategoryStation.RoverUnlimited, out result),
            "EXPEDITION" => SetValue(CategoryStation.Expedition, out result),
            "HQ" => SetValue(CategoryStation.Hq, out result),
            "SCHOOL" => SetValue(CategoryStation.School, out result),
            "EXPLORER" => SetValue(CategoryStation.Explorer, out result),
            _ => SetValue(default, out result, false)
        };
        return success;

        static bool SetValue(CategoryStation val, out CategoryStation outVal, bool ok = true)
        {
            outVal = val;
            return ok;
        }
    }

    /// <summary>
    /// Convert a CategoryStation enum value to its Cabrillo v3 string representation.
    /// </summary>
    public static string ToCabrilloString(this CategoryStation station)
    {
        return station switch
        {
            CategoryStation.Distributed => "DISTRIBUTED",
            CategoryStation.Fixed => "FIXED",
            CategoryStation.Mobile => "MOBILE",
            CategoryStation.Portable => "PORTABLE",
            CategoryStation.Rover => "ROVER",
            CategoryStation.RoverLimited => "ROVER-LIMITED",
            CategoryStation.RoverUnlimited => "ROVER-UNLIMITED",
            CategoryStation.Expedition => "EXPEDITION",
            CategoryStation.Hq => "HQ",
            CategoryStation.School => "SCHOOL",
            CategoryStation.Explorer => "EXPLORER",
            _ => throw new ArgumentOutOfRangeException(nameof(station), station, "Unknown CategoryStation value")
        };
    }

    /// <summary>
    /// Get all valid Cabrillo CATEGORY-STATION string values.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return [
            "DISTRIBUTED",
            "FIXED",
            "MOBILE",
            "PORTABLE",
            "ROVER",
            "ROVER-LIMITED",
            "ROVER-UNLIMITED",
            "EXPEDITION",
            "HQ",
            "SCHOOL",
            "EXPLORER"
        ];
    }
}
