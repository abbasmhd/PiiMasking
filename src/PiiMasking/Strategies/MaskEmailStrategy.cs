using PiiMasking;

namespace PiiMasking.Strategies;

/// <summary>
/// Email masking implementation (duplicated per strategy file by design).
/// </summary>
internal static class EmailMaskingOperations
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

    internal static string? MaskEmail(string? value, string? maskSuffix = null)
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

        var at = trimmed.IndexOf('@');
        if (at < 0)
        {
            return MaskSegment(trimmed, suffix);
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        return MaskSegment(local, suffix) + "@" + domain;
    }
}

/// <summary>
/// Masks only the local part of an email; domain after <c>@</c> is unchanged.
/// </summary>
internal sealed class MaskEmailStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.AsEmail;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        EmailMaskingOperations.MaskEmail(value, settings.MaskSuffix);
}
