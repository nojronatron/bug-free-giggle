namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-OVERLAY Cabrillo v3 header field.
/// Represents special overlay categories for contest classification.
/// </summary>
public enum CategoryOverlay
{
    /// <summary>Classic category</summary>
    Classic,
    
    /// <summary>Rookie category for new operators</summary>
    Rookie,
    
    /// <summary>Tribander/Wires category</summary>
    TbWires,
    
    /// <summary>Youth category for young operators</summary>
    Youth,
    
    /// <summary>Novice/Technician category</summary>
    NoviceTech,
    
    /// <summary>Young Ladies category</summary>
    YL
}

/// <summary>
/// Extension methods for CategoryOverlay enum.
/// </summary>
public static class CategoryOverlayExtensions
{
    /// <summary>
    /// Convert CategoryOverlay enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryOverlay categoryOverlay)
    {
        return categoryOverlay switch
        {
            CategoryOverlay.Classic => "CLASSIC",
            CategoryOverlay.Rookie => "ROOKIE",
            CategoryOverlay.TbWires => "TB-WIRES",
            CategoryOverlay.Youth => "YOUTH",
            CategoryOverlay.NoviceTech => "NOVICE-TECH",
            CategoryOverlay.YL => "YL",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryOverlay))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryOverlay enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryOverlay categoryOverlay)
    {
        categoryOverlay = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "CLASSIC" => SetValue(out categoryOverlay, CategoryOverlay.Classic),
            "ROOKIE" => SetValue(out categoryOverlay, CategoryOverlay.Rookie),
            "TB-WIRES" => SetValue(out categoryOverlay, CategoryOverlay.TbWires),
            "YOUTH" => SetValue(out categoryOverlay, CategoryOverlay.Youth),
            "NOVICE-TECH" => SetValue(out categoryOverlay, CategoryOverlay.NoviceTech),
            "YL" => SetValue(out categoryOverlay, CategoryOverlay.YL),
            _ => false
        };

        static bool SetValue(out CategoryOverlay co, CategoryOverlay value)
        {
            co = value;
            return true;
        }
    }

    /// <summary>
    /// Get all valid Cabrillo format strings.
    /// </summary>
    public static string[] GetAllValidValues()
    {
        return new[] { "CLASSIC", "ROOKIE", "TB-WIRES", "YOUTH", "NOVICE-TECH", "YL" };
    }
}
