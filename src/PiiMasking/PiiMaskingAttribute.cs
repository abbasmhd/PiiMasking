namespace PiiMasking;

/// <summary>
/// Marks a string property for PII masking during JSON serialization when <see cref="PiiMaskingSettings.Enabled"/> is true.
/// Deserialization still receives plain values from JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PiiMaskingAttribute : Attribute
{
    /// <summary>
    /// When true, treats the value as an email (only the local part before @ is masked; the domain is unchanged).
    /// </summary>
    public bool AsEmail { get; init; }

    /// <summary>
    /// When true, masks each whitespace-separated word using segment rules (first two characters + <see cref="PiiMaskingSettings.MaskSuffix"/>), e.g. <c>Abe David</c> → <c>Ab**** Da****</c>.
    /// Ignored when <see cref="AsEmail"/> is true.
    /// </summary>
    public bool MaskEachWord { get; init; }

    /// <summary>
    /// When true, splits the value by <see cref="PiiMaskingSettings.LiteralWordMaskSeparators"/> (case-insensitive); each segment between literals is masked with word-level rules; the matched literal text is copied from the source.
    /// If no separators are configured or none match, behaves like <see cref="MaskEachWord"/>. Ignored when <see cref="AsEmail"/> is true. Takes precedence over <see cref="MaskEachWord"/> when both are set.
    /// </summary>
    public bool MaskEachWordRespectingLiterals { get; init; }

    /// <summary>
    /// When true together with <see cref="MaskEachWordRespectingLiterals"/>, text after the last matched literal in
    /// <see cref="PiiMaskingSettings.LiteralWordMaskSeparators"/> is copied to the output without masking (e.g. a fixed date suffix after <c> on </c>).
    /// </summary>
    public bool LeaveRemainderUnmaskedAfterLiterals { get; init; }
}
