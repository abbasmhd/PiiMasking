using System.Text;

namespace PiiMasking.Strategies;

/// <summary>
/// Literals-aware word masking operations forwarding common operations to shared <see cref="MaskingOperationsBase"/>.
/// </summary>
internal static class EachWordRespectingLiteralsMaskingOperations
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

    /// <summary>
    /// Masks words while preserving configured literal substrings (case-insensitive matching with source casing preserved).
    /// </summary>
    internal static string? MaskEachWordRespectingLiterals(string? value, IReadOnlyList<string>? literalSeparators, string? maskSuffix = null) =>
        MaskEachWordRespectingLiterals(value, literalSeparators, maskSuffix, leaveRemainderUnmasked: false);

    /// <summary>
    /// Masks words while preserving configured literal substrings (case-insensitive matching with source casing preserved).
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <param name="literalSeparators">Substrings to preserve unmasked (matched case-insensitively).</param>
    /// <param name="maskSuffix">The suffix to append during masking.</param>
    /// <param name="leaveRemainderUnmasked">When true, text after the last matched literal is left unmasked.</param>
    internal static string? MaskEachWordRespectingLiterals(
        string? value,
        IReadOnlyList<string>? literalSeparators,
        string? maskSuffix,
        bool leaveRemainderUnmasked)
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

        var separators = NormalizeLiteralSeparators(literalSeparators);
        if (separators.Count == 0)
        {
            return MaskEachWord(trimmed, suffix);
        }

        return ApplyMaskingRespectingLiterals(
            trimmed,
            separators,
            part => MaskEachWord(part, suffix),
            leaveRemainderUnmasked);
    }

    private static string ApplyMaskingRespectingLiterals(
        string trimmed,
        List<string> separators,
        Func<string, string?> maskSegment,
        bool leaveRemainderUnmasked = false)
    {
        var sb = new StringBuilder();
        ReadOnlySpan<char> remaining = trimmed;
        while (remaining.Length > 0)
        {
            if (!TryFindFirstLiteral(remaining, separators, out var index, out var length))
            {
                if (leaveRemainderUnmasked)
                {
                    sb.Append(remaining);
                }
                else
                {
                    sb.Append(maskSegment(remaining.ToString()) ?? string.Empty);
                }

                break;
            }

            if (index > 0)
            {
                sb.Append(maskSegment(remaining[..index].ToString()) ?? string.Empty);
            }

            sb.Append(remaining.Slice(index, length));
            remaining = remaining[(index + length)..];
        }

        return sb.ToString();
    }

    private static List<string> NormalizeLiteralSeparators(IReadOnlyList<string>? literalSeparators)
    {
        if (literalSeparators is null || literalSeparators.Count == 0)
        {
            return [];
        }

        var list = new List<string>(literalSeparators.Count);
        foreach (var s in literalSeparators)
        {
            if (s.Length > 0)
            {
                list.Add(s);
            }
        }

        return list;
    }

    private static bool TryFindFirstLiteral(ReadOnlySpan<char> text, List<string> separators, out int index, out int length)
    {
        index = -1;
        length = 0;

        foreach (var sep in separators)
        {
            if (sep.Length == 0)
            {
                continue;
            }

            var found = text.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                continue;
            }

            if (index < 0 || found < index || (found == index && sep.Length > length))
            {
                index = found;
                length = sep.Length;
            }
        }

        return index >= 0;
    }
}

/// <summary>
/// Word-level masking with configured literal substrings preserved (see <see cref="PiiMaskingSettings.LiteralWordMaskSeparators"/>).
/// </summary>
internal sealed class MaskEachWordRespectingLiteralsStrategy : IPiiMaskingStrategy
{
    public bool CanHandle(PiiMaskingAttribute marker) => marker.MaskEachWordRespectingLiterals;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
        EachWordRespectingLiteralsMaskingOperations.MaskEachWordRespectingLiterals(
            value,
            settings.LiteralWordMaskSeparators,
            settings.MaskSuffix,
            marker.LeaveRemainderUnmaskedAfterLiterals);
}
