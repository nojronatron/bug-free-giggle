namespace ContestLogProcessor.Lib;

/// <summary>
/// Valid values for the CATEGORY-OPERATOR Cabrillo v3 header field.
/// Indicates the operator category (single-op, multi-op, or checklog).
/// </summary>
public enum CategoryOperator
{
    /// <summary>Single operator</summary>
    SingleOp,

    /// <summary>Multiple operators</summary>
    MultiOp,

    /// <summary>Check log (not for awards/competition)</summary>
    Checklog
}

/// <summary>
/// Extension methods for CategoryOperator enum.
/// </summary>
public static class CategoryOperatorExtensions
{
    /// <summary>
    /// Convert CategoryOperator enum value to Cabrillo format string.
    /// </summary>
    public static string ToCabrilloString(this CategoryOperator categoryOperator)
    {
        return categoryOperator switch
        {
            CategoryOperator.SingleOp => "SINGLE-OP",
            CategoryOperator.MultiOp => "MULTI-OP",
            CategoryOperator.Checklog => "CHECKLOG",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryOperator))
        };
    }

    /// <summary>
    /// Try to parse a Cabrillo format string to CategoryOperator enum.
    /// </summary>
    public static bool TryParse(string value, out CategoryOperator categoryOperator)
    {
        categoryOperator = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "SINGLE-OP" => SetValue(out categoryOperator, CategoryOperator.SingleOp),
            "MULTI-OP" => SetValue(out categoryOperator, CategoryOperator.MultiOp),
            "CHECKLOG" => SetValue(out categoryOperator, CategoryOperator.Checklog),
            _ => false
        };

        static bool SetValue(out CategoryOperator co, CategoryOperator value)
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
        return new[] { "SINGLE-OP", "MULTI-OP", "CHECKLOG" };
    }
}
