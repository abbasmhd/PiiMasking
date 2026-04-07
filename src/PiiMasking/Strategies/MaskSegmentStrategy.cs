using PiiMasking;

namespace PiiMasking.Strategies;

/// <summary>
/// Segment masking implementation (duplicated per strategy file by design).
/// </summary>
internal static class SegmentMaskingOperations
{
    internal static string ResolveSuffix(string? maskSuffix) =>
        string.IsNullOrEmpty(maskSuffix) ? PiiMaskingSettings.DefaultMaskSuffix : maskSuffix;

    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        suffix.Length > 0 && s.Contains(suffix, StringComparison.Ordinal);

    /// <summary>
    /// Masks a single segment (e.g. a name).
    /// </summary>
    internal static string? MaskSegment(string? value, string? maskSuffix = null)
    {
        var suffix = ResolveSuffix(maskSuffix);
        if (value is null)
        {
            return null;
        }

        if (value.Length == 0)
        {
            return string.Empty;
        }

        var s = value.Trim();
        if (ContainsMaskSuffix(s, suffix))
        {
            return s;
        }

        if (s.Length <= 2)
        {
            return s + suffix;
        }

        var first = char.ToUpperInvariant(s[0]);
        var second = char.ToLowerInvariant(s[1]);
        return string.Concat(first, second, suffix);
    }
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
