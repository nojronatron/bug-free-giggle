namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-MODE Cabrillo v3 header field.
/// Represents the operating mode category.
/// </summary>
public enum CategoryMode
{
    /// <summary>CW (Continuous Wave/Morse code)</summary>
    CW,
    
    /// <summary>Digital modes</summary>
    Digi,
    
    /// <summary>FM (Frequency Modulation)</summary>
    FM,
    
    /// <summary>RTTY (Radio Teletype)</summary>
    RTTY,
    
    /// <summary>SSB (Single Sideband voice)</summary>
    SSB,
    
    /// <summary>Mixed modes</summary>
    Mixed
}

/// <summary>
/// Extension methods for CategoryMode enum.
/// </summary>
public static class CategoryModeExtensions
{
    /// <summary>
    /// Convert CategoryMode enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryMode categoryMode)
    {
        return categoryMode switch
        {
            CategoryMode.CW => "CW",
            CategoryMode.Digi => "DIGI",
            CategoryMode.FM => "FM",
            CategoryMode.RTTY => "RTTY",
            CategoryMode.SSB => "SSB",
            CategoryMode.Mixed => "MIXED",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryMode))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryMode enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryMode categoryMode)
    {
        categoryMode = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "CW" => SetValue(out categoryMode, CategoryMode.CW),
            "DIGI" => SetValue(out categoryMode, CategoryMode.Digi),
            "FM" => SetValue(out categoryMode, CategoryMode.FM),
            "RTTY" => SetValue(out categoryMode, CategoryMode.RTTY),
            "SSB" => SetValue(out categoryMode, CategoryMode.SSB),
            "MIXED" => SetValue(out categoryMode, CategoryMode.Mixed),
            _ => false
        };

        static bool SetValue(out CategoryMode cm, CategoryMode value)
        {
            cm = value;
            return true;
        }
    }

    /// <summary>
    /// Get all valid Cabrillo format strings.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return new[] { "CW", "DIGI", "FM", "RTTY", "SSB", "MIXED" };
    }
}
