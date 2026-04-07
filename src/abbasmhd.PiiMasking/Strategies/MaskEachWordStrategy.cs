using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Each-word masking implementation (duplicated per strategy file by design).
/// </summary>
internal static class EachWordMaskingOperations
{
    internal static string ResolveSuffix(string? maskSuffix) =>
        string.IsNullOrEmpty(maskSuffix) ? PiiMaskingSettings.DefaultMaskSuffix : maskSuffix;

    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        suffix.Length > 0 && s.Contains(suffix, StringComparison.Ordinal);

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

    internal static string? MaskEachWord(string? value, string? maskSuffix = null)
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

        var trimmed = value.Trim();
        if (ContainsMaskSuffix(trimmed, suffix))
        {
            return trimmed;
        }

        var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = MaskSegment(parts[i], suffix) ?? string.Empty;
        }

        return string.Join(' ', parts);
    }
}

/// <summary>
/// Masks each whitespace-separated word with segment rules.
/// </summary>
internal sealed class MaskEachWordStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.MaskEachWord;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        EachWordMaskingOperations.MaskEachWord(value, settings.MaskSuffix);
}
