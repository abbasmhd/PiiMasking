using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Masks only the local part of an email; domain after <c>@</c> is unchanged.
/// </summary>
internal sealed class MaskEmailStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.AsEmail;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        PiiStringMasking.MaskEmail(value, settings.MaskSuffix);
}
