namespace PiiMasking;

/// <summary>
/// Applies <see cref="PiiMaskingAttribute"/> rules to a string (aligned with <see cref="Serialization.PiiMaskedStringJsonConverter"/> for JSON write behavior).
/// </summary>
public static class PiiMaskingTextFormatter
{
    /// <summary>
    /// Formats <paramref name="value"/> for outbound serialization when PII masking is enabled.
    /// Uses built-in rules only; <see cref="PiiMaskingAttribute.Mode"/> and <see cref="IPiiMaskingExecutionStrategy"/> are not applied here.
    /// </summary>
    /// <param name="maskSuffix">From <see cref="PiiMaskingSettings.MaskSuffix"/>; null uses <see cref="PiiMaskingSettings.DefaultMaskSuffix"/>.</param>
    /// <param name="literalWordMaskSeparators">From <see cref="PiiMaskingSettings.LiteralWordMaskSeparators"/> when using <see cref="PiiMaskingAttribute.MaskEachWordRespectingLiterals"/>.</param>
    public static string Apply(
        string value,
        PiiMaskingAttribute marker,
        bool piiMaskingEnabled,
        string? maskSuffix = null,
        IReadOnlyList<string>? literalWordMaskSeparators = null)
    {
        if (!piiMaskingEnabled)
        {
            return value;
        }

        var settings = new PiiMaskingSettings
        {
            MaskSuffix = string.IsNullOrWhiteSpace(maskSuffix)
                ? PiiMaskingSettings.DefaultMaskSuffix
                : maskSuffix,
            LiteralWordMaskSeparators = literalWordMaskSeparators is null
                ? []
                : literalWordMaskSeparators as string[] ?? literalWordMaskSeparators.ToArray(),
        };

        return PiiMaskingKernel.ApplyBuiltin(value, marker, settings);
    }
}
