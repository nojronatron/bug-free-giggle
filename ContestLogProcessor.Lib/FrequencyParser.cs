using System.Globalization;

namespace ContestLogProcessor.Lib;

public static class FrequencyParser
{
    public static bool TryParseFrequencyToken(string? token, out int frequencyKhz)
    {
        frequencyKhz = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        string t = token.Trim();

        // Disallow tokens containing unit indicators or letters G/M etc.
        if (t.IndexOfAny(new char[] { 'G', 'g', 'M', 'm', 'H', 'h', 'L', 'l' }) >= 0) return false;

        // Disallow the literal "LIGHT"
        if (string.Equals(t, "LIGHT", StringComparison.OrdinalIgnoreCase)) return false;

        // Try parse as double so we can truncate
        if (!double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double d)) return false;

        int parsed = (int)d; // truncate

        if (parsed >= 55 && parsed <= 1000) return false;
        if (parsed > 29999) return false;

        frequencyKhz = parsed;
        return true;
    }

    public static FrequencyBand GetBandForFrequency(int frequencyKhz)
    {
        if (frequencyKhz >= 1800 && frequencyKhz <= 2000) return FrequencyBand.M160;
        if (frequencyKhz >= 3500 && frequencyKhz <= 4000) return FrequencyBand.M80;
        if (frequencyKhz >= 7000 && frequencyKhz <= 7300) return FrequencyBand.M40;
        if (frequencyKhz >= 14000 && frequencyKhz <= 14350) return FrequencyBand.M20;
        if (frequencyKhz >= 21000 && frequencyKhz <= 21450) return FrequencyBand.M15;
        if (frequencyKhz >= 28000 && frequencyKhz <= 29700) return FrequencyBand.M10;
        if (frequencyKhz >= 50000 && frequencyKhz <= 54000) return FrequencyBand.M6;
        return FrequencyBand.Unknown;
    }
}
