using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Masks each whitespace-separated word with segment rules.
/// </summary>
internal sealed class MaskEachWordStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.MaskEachWord;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        PiiStringMasking.MaskEachWord(value, settings.MaskSuffix);
}
