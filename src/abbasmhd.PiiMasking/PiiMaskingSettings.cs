namespace abbasmhd.PiiMasking;

/// <summary>
/// Controls masking of personally identifiable string values in JSON for properties marked with <see cref="PiiMaskingAttribute"/>.
/// </summary>
public sealed class PiiMaskingSettings
{
    public const string SectionName = "PiiMasking";

    /// <summary>
    /// Default mask suffix when none is supplied or configuration normalizes an empty value.
    /// </summary>
    public const string DefaultMaskSuffix = "****";

    /// <summary>
    /// When false, marked string properties serialize as plain values.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// String appended when masking segments (default <c>****</c>). Bound from configuration; empty values are normalized at startup.
    /// </summary>
    public string MaskSuffix { get; set; } = DefaultMaskSuffix;

    /// <summary>
    /// Substrings left unmasked (matched case-insensitively) when a property uses <see cref="PiiMaskingAttribute.MaskEachWordRespectingLiterals"/>.
    /// Text between matches is word-masked. Include leading/trailing spaces in entries when the source text has them (e.g. <c> on behalf of </c>).
    /// </summary>
    public string[] LiteralWordMaskSeparators { get; set; } = [];
}
