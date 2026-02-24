namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-POWER Cabrillo v3 header field.
/// Indicates the transmitter power category.
/// </summary>
public enum CategoryPower
{
    /// <summary>High power (typically >150W)</summary>
    High,

    /// <summary>Low power (typically 5W-150W)</summary>
    Low,

    /// <summary>QRP (low power, typically ≤5W)</summary>
    QRP
}

/// <summary>
/// Extension methods for CategoryPower enum.
/// </summary>
public static class CategoryPowerExtensions
{
    /// <summary>
    /// Convert CategoryPower enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryPower categoryPower)
    {
        return categoryPower switch
        {
            CategoryPower.High => "HIGH",
            CategoryPower.Low => "LOW",
            CategoryPower.QRP => "QRP",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryPower))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryPower enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryPower categoryPower)
    {
        categoryPower = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "HIGH" => SetValue(out categoryPower, CategoryPower.High),
            "LOW" => SetValue(out categoryPower, CategoryPower.Low),
            "QRP" => SetValue(out categoryPower, CategoryPower.QRP),
            _ => false
        };

        static bool SetValue(out CategoryPower cp, CategoryPower value)
        {
            cp = value;
            return true;
        }
    }

    /// <summary>
    /// Get all valid Cabrillo format strings.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return new[] { "HIGH", "LOW", "QRP" };
    }
}
