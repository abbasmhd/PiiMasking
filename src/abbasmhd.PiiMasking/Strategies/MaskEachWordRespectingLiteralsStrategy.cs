using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Word-level masking with configured literal substrings preserved (see <see cref="PiiMaskingSettings.LiteralWordMaskSeparators"/>).
/// </summary>
internal sealed class MaskEachWordRespectingLiteralsStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.MaskEachWordRespectingLiterals;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        PiiStringMasking.MaskEachWordRespectingLiterals(
            value,
            settings.LiteralWordMaskSeparators,
            settings.MaskSuffix,
            marker.LeaveRemainderUnmaskedAfterLiterals);
}
