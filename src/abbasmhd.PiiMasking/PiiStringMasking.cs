using System.Text;

namespace abbasmhd.PiiMasking;

/// <summary>
/// Masks string values for safe display (e.g. API responses, logs).
/// The mask portion defaults to <see cref="DefaultMaskSuffix"/>; override via <see cref="PiiMaskingSettings.MaskSuffix"/>.
/// Values that already contain the configured suffix are returned unchanged (after trim) so already-masked strings are not double-masked.
/// </summary>
public static class PiiStringMasking
{
    /// <summary>
    /// Default mask suffix when none is supplied or configuration is empty.
    /// </summary>
    public const string DefaultMaskSuffix = "****";

    private static string ResolveSuffix(string? maskSuffix) =>
        string.IsNullOrEmpty(maskSuffix) ? DefaultMaskSuffix : maskSuffix;

    /// <summary>
    /// Masks a single segment (e.g. a name).
    /// If the trimmed value already contains <paramref name="maskSuffix"/>, it is returned as-is.
    /// Returns <c>null</c> when <paramref name="value"/> is <c>null</c>; empty string stays empty.
    /// </summary>
    public static string? MaskSegment(string? value, string? maskSuffix = null)
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

    /// <summary>
    /// Masks only the local part (name) of an email; the domain after <c>@</c> is left unchanged.
    /// If the trimmed value already contains <paramref name="maskSuffix"/> anywhere, the whole address is returned as-is.
    /// Returns <c>null</c> when <paramref name="value"/> is <c>null</c>; empty string stays empty.
    /// </summary>
    public static string? MaskEmail(string? value, string? maskSuffix = null)
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

    /// <summary>
    /// Masks each whitespace-separated word with <see cref="MaskSegment"/>.
    /// </summary>
    public static string? MaskEachWord(string? value, string? maskSuffix = null)
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

    /// <summary>
    /// Like <see cref="MaskEachWord"/> but leaves configured literal substrings unmasked (matched case-insensitively); the matched text is taken from the source.
    /// When <paramref name="literalSeparators"/> is null, empty, or contains only empty entries, or when no separator appears in the value, behaves like <see cref="MaskEachWord"/>.
    /// </summary>
    public static string? MaskEachWordRespectingLiterals(string? value, IReadOnlyList<string>? literalSeparators, string? maskSuffix = null) =>
        MaskEachWordRespectingLiterals(value, literalSeparators, maskSuffix, leaveRemainderUnmasked: false);

    /// <inheritdoc cref="MaskEachWordRespectingLiterals(string?, IReadOnlyList{string}?, string?)"/>
    /// <param name="leaveRemainderUnmasked">When true, trailing text after the last literal is not masked.</param>
    public static string? MaskEachWordRespectingLiterals(
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
            suffix,
            part => MaskEachWord(part, suffix),
            leaveRemainderUnmasked);
    }

    private static string ApplyMaskingRespectingLiterals(
        string trimmed,
        List<string> separators,
        string suffix,
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

    private static bool ContainsMaskSuffix(string s, string suffix) =>
        suffix.Length > 0 && s.Contains(suffix, StringComparison.Ordinal);

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
