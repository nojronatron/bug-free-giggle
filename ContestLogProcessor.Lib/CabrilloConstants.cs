namespace ContestLogProcessor.Lib;

/// <summary>
/// Constants used throughout Cabrillo log processing.
/// </summary>
public static class CabrilloConstants
{
    /// <summary>
    /// Valid values for CATEGORY-BAND Cabrillo v3 header field.
    /// Includes all standard frequency bands and special categories.
    /// </summary>
    public static readonly string[] CategoryBandValues = new[]
    {
        "ALL",
        "160M", "80M", "40M", "20M", "15M", "10M",
        "6M", "4M", "2M",
        "222", "432", "902",
        "1.2G", "2.3G", "3.4G", "5.7G", "10G", "24G", "47G", "75G", "122G", "134G", "241G",
        "LIGHT",
        "VHF-3-BAND", "VHF-FM-ONLY"
    };

    /// <summary>
    /// Suspicious patterns that may indicate malicious input in header values.
    /// Used for sanitization when header values exceed 13 characters.
    /// </summary>
    public static readonly string[] SuspiciousPatterns = new[]
    {
        "select * from",
        "drop table",
        "--",
        ";--",
        "exec ",
        "rm -rf",
        "curl ",
        "powershell -",
        "invoke-"
    };

    /// <summary>
    /// Date/time format patterns accepted when parsing QSO timestamps.
    /// Supports various Cabrillo date and time formatting conventions.
    /// </summary>
    public static readonly string[] DateTimeFormats = new[]
    {
        "yyyy-MM-dd HHmm",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd H:mm",
        "yyyy-MM-dd Hm",
        "yyyyMMdd HHmm"
    };

    /// <summary>
    /// Official Cabrillo frequency tokens mapped to their kHz values.
    /// Supports both legacy band tokens and modern frequency designations.
    /// </summary>
    public static readonly Dictionary<string, int> OfficialFrequencies = new(StringComparer.OrdinalIgnoreCase)
    {
        { "1800", 1800 },
        { "3500", 3500 },
        { "7000", 7000 },
        { "14000", 14000 },
        { "21000", 21000 },
        { "28000", 28000 },
        { "50", 50000 },
        { "70", 70000 },
        { "144", 144000 },
        { "222", 222000 },
        { "432", 432000 },
        { "902", 902000 },
        { "1.2G", 1200000 },
        { "2.3G", 2300000 },
        { "3.4G", 3400000 },
        { "5.7G", 5700000 },
        { "10G", 10000000 },
        { "24G", 24000000 },
        { "47G", 47000000 },
        { "75G", 75000000 },
        { "122G", 122000000 },
        { "134G", 134000000 },
        { "241G", 241000000 },
        { "LIGHT", 999999999 }
    };
}
