using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Default segment masking (first two characters pattern + suffix).
/// </summary>
internal sealed class MaskSegmentStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => true;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        PiiStringMasking.MaskSegment(value, settings.MaskSuffix);
}
