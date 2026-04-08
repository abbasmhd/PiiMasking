namespace PiiMasking.Strategies;

/// <summary>
/// Each-word masking operations forwarding to shared <see cref="MaskingOperationsBase"/>.
/// </summary>
internal static class EachWordMaskingOperations
{
    internal static string ResolveSuffix(string? maskSuffix) =>
        MaskingOperationsBase.ResolveSuffix(maskSuffix);

    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        MaskingOperationsBase.ContainsMaskSuffix(s, suffix);

    internal static string? MaskSegment(string? value, string? maskSuffix = null) =>
        MaskingOperationsBase.MaskSegment(value, maskSuffix);

    /// <summary>
    /// Masks each whitespace-separated word with segment rules (first two characters + suffix).
    /// </summary>
    internal static string? MaskEachWord(string? value, string? maskSuffix = null)
    {
        var suffix = MaskingOperationsBase.ResolveSuffix(maskSuffix);
        if (value is null)
        {
            return null;
        }

        if (value.Length == 0)
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (MaskingOperationsBase.ContainsMaskSuffix(trimmed, suffix))
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
