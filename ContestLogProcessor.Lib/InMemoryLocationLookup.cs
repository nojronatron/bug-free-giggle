using System.Globalization;

namespace ContestLogProcessor.Lib;

public class InMemoryLocationLookup : ILocationLookup
{
    private static readonly HashSet<string> _waCounties = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADA","ASO","BEN","CHE","CLAL","CLAR","COL","COW","DOU","FER","FRA","GAR","GRAN","GRAY","ISL","JEFF","KING","KITS","KITT","KLI","LEW","LIN","MAS","OKA","PAC","PEND","PIE","SAN","SKAG","SKAM","SNO","SPO","STE","THU","WAH","WAL","WHA","WHI","YAK"
    };

    private static readonly HashSet<string> _usStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT","VA","WA","WV","WI","WY","DC","AS","GU","MP","PR","VI"
    };

    private static readonly HashSet<string> _canada = new(StringComparer.OrdinalIgnoreCase)
    {
        "AB","BC","LB","MB","NB","NF","NS","NT","NU","ON","PE","QC","SK","YT"
    };

    // DXCC list - abbreviated; preserve slashes in entries
    private static readonly HashSet<string> _dxcc = new(StringComparer.OrdinalIgnoreCase)
    {
        // Partial list copied from rules document; include common forms used in lookups.
        // For brevity include a representative subset; this implementation can be expanded.
        "1A","3A","3B6","3B8","3B9","3C","3C0","3D2","3DA","3V","3W","3X","3Y/B","3Y/P","4J","4L","4O","4S","4U1I","4U1U","4U1V","4W","4X","5A","5B","5H","5N","5R","5T","5U","5V","5W","5X","5Z","6W","6Y","7O","7P","7Q","7X","8P","8Q","8R","9A","9G","9H","9J","9K","9L","9M2","9M6","9N","9Q","9U","9V","9X","9Y","A2","A3","A4","A5","A6","A7","A9","AP","BS7","BV","BV9P","BY","C3","C5","C6","C9","CE","CE0X","CE0Y","CE0Z","CE9","CM","CN","CO","CP","CT","CT3","CU","CX","CY0","CY9","D2","D4","D6","DL","DU","E3","E4","EA","EA6","EA8","EA9","EI","EK","EL","EP","ER","ES","ET","EU","EX","EY","EZ","F","FG","FH","FJ","FK","FM","FO","FO/A","FO/M","FO0","FR","FR/G","FR/J","FR/T","FT/G","FT/J","FT/T","FT/W","FW","FY","GA","GD","GI","GJ","GM","GU","GW","H4","H40","HA","HB","HB0","HC","HC8","HH","HI","HK","HK0A","HK0M","HL","HM","HP","HR","HS","HV","HZ","I","IS","IS0","J2","J3","J5","J6","J7","J8","JA","JD1M","JD1O","JT","JW","JX","JY","K","KG4","KH0","KH1","KH2","KH3","KH4","KH5","KH5K","KH6","KH7K","KH8","KH9","KL","KP1","KP2","KP3","KP4","LA","LU","LX","LY","LZ","OA","OD","OE","OH","OH0","OJ0","OK","OM","ON","OX","OY","OZ","P2","P4","PA","PJ2","PJ4","PJ5","PJ7","PY","PY0F","PY0S","PY0T","R1F","S0","S2","S5","S7","S9","SM","SP","ST","SU","SV","SV/A","SV5","SV9","T2","T30","T31","T32","T33","T5","T7","T8","TA","TF","TG","TI","TI9","TJ","TK","TL","TN","TR","TT","TU","TY","TZ","UA","UA2","UA9","UK","UN","UR","V2","V3","V4","V5","V6","V7","V8","VE","VK","VK0H","VK0M","VK9C","VK9L","VK9M","VK9N","VK9W","VK9X","VP2E","VP2M","VP2V","VP5","VP6","VP6/D","VP8","VP8/G","VP8/H","VP8O","VP9","VU","VU4","VU7","XE","XF4","XT","XU","XW","XX9","XY","XZ","YA","YB","YI","YJ","YK","YL","YN","YO","YS","YT","YV","Z2","Z3","Z8","ZA","ZB","ZC4","ZD7","ZD8","ZD9","ZF","ZK3","ZL","ZL7","ZL8","ZL9","ZP","ZS","ZS8"
    };

    public bool TryMatchWashingtonCounty(string token, out string abbreviation)
    {
        abbreviation = null!;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string t = token.Trim();
        if (_waCounties.Contains(t))
        {
            abbreviation = t.ToUpperInvariant();
            return true;
        }
        return false;
    }

    public bool TryMatchUSState(string token, out string abbreviation)
    {
        abbreviation = null!;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string t = token.Trim();
        if (_usStates.Contains(t))
        {
            abbreviation = t.ToUpperInvariant();
            return true;
        }
        return false;
    }

    public bool TryMatchCanadianProvince(string token, out string abbreviation)
    {
        abbreviation = null!;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string t = token.Trim();
        if (_canada.Contains(t))
        {
            abbreviation = t.ToUpperInvariant();
            return true;
        }
        return false;
    }

    public bool TryMatchDxcc(string token, out string abbreviation)
    {
        abbreviation = null!;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string t = token.Trim();
        if (_dxcc.Contains(t))
        {
            abbreviation = t.ToUpperInvariant();
            return true;
        }
        return false;
    }
}
