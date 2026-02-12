namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-TIME Cabrillo v3 header field.
/// Represents the maximum operating time permitted in the contest.
/// </summary>
public enum CategoryTime
{
    /// <summary>6 hours maximum operating time</summary>
    SixHours,
    
    /// <summary>8 hours maximum operating time</summary>
    EightHours,
    
    /// <summary>12 hours maximum operating time</summary>
    TwelveHours,
    
    /// <summary>24 hours maximum operating time</summary>
    TwentyFourHours
}

/// <summary>
/// Extension methods for CategoryTime enum.
/// </summary>
public static class CategoryTimeExtensions
{
    /// <summary>
    /// Convert CategoryTime enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryTime categoryTime)
    {
        return categoryTime switch
        {
            CategoryTime.SixHours => "6-HOURS",
            CategoryTime.EightHours => "8-HOURS",
            CategoryTime.TwelveHours => "12-HOURS",
            CategoryTime.TwentyFourHours => "24-HOURS",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryTime))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryTime enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryTime categoryTime)
    {
        categoryTime = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "6-HOURS" => SetValue(out categoryTime, CategoryTime.SixHours),
            "8-HOURS" => SetValue(out categoryTime, CategoryTime.EightHours),
            "12-HOURS" => SetValue(out categoryTime, CategoryTime.TwelveHours),
            "24-HOURS" => SetValue(out categoryTime, CategoryTime.TwentyFourHours),
            _ => false
        };

        static bool SetValue(out CategoryTime ct, CategoryTime value)
        {
            ct = value;
            return true;
        }
    }

    /// <summary>
    /// Get all valid Cabrillo format strings.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return new[] { "6-HOURS", "8-HOURS", "12-HOURS", "24-HOURS" };
    }
}
