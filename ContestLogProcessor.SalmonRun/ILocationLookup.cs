namespace ContestLogProcessor.SalmonRun;

public interface ILocationLookup
{
    // Try to match a Washington county abbreviation or name. Returns canonical abbreviation (e.g. "ADA").
    bool TryMatchWashingtonCounty(string token, out string abbreviation);

    // Try to match a US state or territory abbreviation (e.g. "WA", "CA"). Returns abbreviation.
    bool TryMatchUSState(string token, out string abbreviation);

    // Try to match a Canadian province/territory abbreviation (e.g. "BC", "ON"). Returns abbreviation.
    bool TryMatchCanadianProvince(string token, out string abbreviation);

    // Try to match a DXCC entity abbreviation. Returns abbreviation as-listed (preserve slashes where present).
    bool TryMatchDxcc(string token, out string abbreviation);
}