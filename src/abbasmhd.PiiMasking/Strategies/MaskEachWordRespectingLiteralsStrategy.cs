using System.Text;
using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Strategies;

/// <summary>
/// Literals-aware word masking implementation (duplicated per strategy file by design).
/// </summary>
internal static class EachWordRespectingLiteralsMaskingOperations
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

    internal static string? MaskEachWordRespectingLiterals(string? value, IReadOnlyList<string>? literalSeparators, string? maskSuffix = null) =>
        MaskEachWordRespectingLiterals(value, literalSeparators, maskSuffix, leaveRemainderUnmasked: false);

    internal static string? MaskEachWordRespectingLiterals(
        string? value,
        IReadOnlyList<string>? literalSeparators,
        string? maskSuffix,
        bool leaveRemainderUnmasked)
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
