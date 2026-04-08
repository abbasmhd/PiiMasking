namespace PiiMasking.Strategies;

/// <summary>
/// Email masking operations forwarding to shared <see cref="MaskingOperationsBase"/>.
/// </summary>
internal static class EmailMaskingOperations
{
    internal static string ResolveSuffix(string? maskSuffix) =>
        MaskingOperationsBase.ResolveSuffix(maskSuffix);

    internal static bool ContainsMaskSuffix(string s, string suffix) =>
        MaskingOperationsBase.ContainsMaskSuffix(s, suffix);

    internal static string? MaskSegment(string? value, string? maskSuffix = null) =>
        MaskingOperationsBase.MaskSegment(value, maskSuffix);

    /// <summary>
    /// Masks only the local part of an email; the domain after <c>@</c> is unchanged.
    /// </summary>
    internal static string? MaskEmail(string? value, string? maskSuffix = null)
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
