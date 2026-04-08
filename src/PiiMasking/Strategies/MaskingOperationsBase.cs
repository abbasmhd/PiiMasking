using PiiMasking;

namespace PiiMasking.Strategies;

/// <summary>
/// Shared masking operations used by all masking strategies.
/// </summary>
internal static class MaskingOperationsBase
{
    /// <summary>
    /// Minimum number of characters required before the first two characters are masked.
    /// </summary>
    private const int CharacterThresholdForMasking = 2;

    /// <summary>
    /// Resolves the mask suffix, using the default if the provided suffix is null or empty.
    /// </summary>
    internal static string ResolveSuffix(string? maskSuffix) =>
        string.IsNullOrEmpty(maskSuffix) ? PiiMaskingSettings.DefaultMaskSuffix : maskSuffix;

    /// <summary>
    /// Determines whether a string contains the mask suffix.
    /// </summary>
    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        suffix.Length > 0 && s.Contains(suffix, StringComparison.Ordinal);

    /// <summary>
    /// Masks a single segment by keeping the first two characters (with specific casing) and appending the mask suffix.
    /// Segments of 2 characters or fewer are returned as-is with the suffix appended.
    /// </summary>
    /// <param name="value">The value to mask; null returns null.</param>
    /// <param name="maskSuffix">The suffix to append; null uses the default suffix.</param>
    /// <returns>The masked segment, or null/empty if input is null/empty.</returns>
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

        if (s.Length <= CharacterThresholdForMasking)
        {
            return s + suffix;
        }

        var first = char.ToUpperInvariant(s[0]);
        var second = char.ToLowerInvariant(s[1]);
        return string.Concat(first, second, suffix);
    }
}
