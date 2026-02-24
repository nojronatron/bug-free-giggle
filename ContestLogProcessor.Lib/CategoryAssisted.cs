namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-ASSISTED Cabrillo v3 header field.
/// Indicates whether the operator used spotting assistance.
/// </summary>
public enum CategoryAssisted
{
    /// <summary>Non-assisted operation (no spotting assistance)</summary>
    NonAssisted,

    /// <summary>Assisted operation (spotting assistance allowed)</summary>
    Assisted
}

/// <summary>
/// Extension methods for CategoryAssisted enum.
/// </summary>
public static class CategoryAssistedExtensions
{
    /// <summary>
    /// Convert CategoryAssisted enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryAssisted categoryAssisted)
    {
        return categoryAssisted switch
        {
            CategoryAssisted.Assisted => "ASSISTED",
            CategoryAssisted.NonAssisted => "NON-ASSISTED",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryAssisted))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryAssisted enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryAssisted categoryAssisted)
    {
        categoryAssisted = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ASSISTED" => SetValue(out categoryAssisted, CategoryAssisted.Assisted),
            "NON-ASSISTED" => SetValue(out categoryAssisted, CategoryAssisted.NonAssisted),
            _ => false
        };

        static bool SetValue(out CategoryAssisted ca, CategoryAssisted value)
        {
            ca = value;
            return true;
        }
    }

    /// <summary>
    /// Get all valid Cabrillo format strings.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return new[] { "ASSISTED", "NON-ASSISTED" };
    }
}
