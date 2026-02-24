namespace ContestLogProcessor.Lib;

/// <summary>
/// Header fields that should be sanitized to mask suspicious substrings.
/// These fields may contain long user-supplied strings.
/// </summary>
[Flags]
public enum SanitizableHeader
{
    /// <summary>No headers</summary>
    None = 0,

    /// <summary>LOCATION header</summary>
    Location = 1 << 0,

    /// <summary>CALLSIGN header</summary>
    Callsign = 1 << 1,

    /// <summary>CLUB header</summary>
    Club = 1 << 2,

    /// <summary>NAME header</summary>
    Name = 1 << 3,

    /// <summary>ADDRESS header</summary>
    Address = 1 << 4,

    /// <summary>ADDRESS-CITY header</summary>
    AddressCity = 1 << 5,

    /// <summary>ADDRESS-POSTALCODE header</summary>
    AddressPostalcode = 1 << 6,

    /// <summary>ADDRESS-COUNTRY header</summary>
    AddressCountry = 1 << 7,

    /// <summary>EMAIL header</summary>
    Email = 1 << 8,

    /// <summary>CREATED-BY header</summary>
    CreatedBy = 1 << 9,

    /// <summary>SOAPBOX header</summary>
    Soapbox = 1 << 10
}

/// <summary>
/// Extension methods for SanitizableHeader enum.
/// </summary>
public static class SanitizableHeaderExtensions
{
    /// <summary>
    /// Try to parse a header key to SanitizableHeader enum.
    /// </summary>
    public static bool TryParse(string key, out SanitizableHeader header)
    {
        header = SanitizableHeader.None;
        if (string.IsNullOrWhiteSpace(key)) return false;

        string normalized = key.Trim().ToUpperInvariant();
        header = normalized switch
        {
            "LOCATION" => SanitizableHeader.Location,
            "CALLSIGN" => SanitizableHeader.Callsign,
            "CLUB" => SanitizableHeader.Club,
            "NAME" => SanitizableHeader.Name,
            "ADDRESS" => SanitizableHeader.Address,
            "ADDRESS-CITY" => SanitizableHeader.AddressCity,
            "ADDRESS-POSTALCODE" => SanitizableHeader.AddressPostalcode,
            "ADDRESS-COUNTRY" => SanitizableHeader.AddressCountry,
            "EMAIL" => SanitizableHeader.Email,
            "CREATED-BY" => SanitizableHeader.CreatedBy,
            "SOAPBOX" => SanitizableHeader.Soapbox,
            _ => SanitizableHeader.None
        };

        return header != SanitizableHeader.None;
    }

    /// <summary>
    /// Check if a header key should be sanitized.
    /// </summary>
    public static bool IsSanitizable(string key)
    {
        return TryParse(key, out SanitizableHeader header) && header != SanitizableHeader.None;
    }
}
