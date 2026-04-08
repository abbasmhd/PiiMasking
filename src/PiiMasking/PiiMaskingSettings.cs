namespace PiiMasking;

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
    /// Example: with separators <c>[" on behalf of "]</c>, the value "John Doe on behalf of Jane Smith" becomes "Jo**** Do**** on behalf of Ja**** Sm****".
    /// Separator matching is case-insensitive, but the matched text from the source is copied as-is, preserving its original casing.
    /// </summary>
    public string[] LiteralWordMaskSeparators { get; set; } = [];
}
