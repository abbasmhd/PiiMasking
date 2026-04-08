namespace PiiMasking.Strategies;

/// <summary>
/// Segment masking forwarding to shared <see cref="MaskingOperationsBase"/>.
/// </summary>
internal static class SegmentMaskingOperations
{
    internal static string ResolveSuffix(string? maskSuffix) =>
        MaskingOperationsBase.ResolveSuffix(maskSuffix);

    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        MaskingOperationsBase.ContainsMaskSuffix(s, suffix);

    /// <summary>
    /// Masks a single segment (e.g. a name).
    /// </summary>
    internal static string? MaskSegment(string? value, string? maskSuffix = null) =>
        MaskingOperationsBase.MaskSegment(value, maskSuffix);
}

/// <summary>
/// Default segment masking (first two characters pattern + suffix).
/// </summary>
internal sealed class MaskSegmentStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => true;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        SegmentMaskingOperations.MaskSegment(value, settings.MaskSuffix);
}
