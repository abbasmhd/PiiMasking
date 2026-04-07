using abbasmhd.PiiMasking.Strategies;

namespace abbasmhd.PiiMasking;

/// <summary>
/// Built-in masking rules shared by <see cref="PiiMaskingPropertyStringTransform"/> and JSON fallback paths.
/// </summary>
internal static class PiiMaskingKernel
{
    private static readonly IPiiMaskingStrategy[] Strategies =
    [
        new MaskEmailStrategy(),
        new MaskEachWordRespectingLiteralsStrategy(),
        new MaskEachWordStrategy(),
        new MaskSegmentStrategy(),
    ];

    public static string ApplyBuiltin(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings)
    {
        foreach (var strategy in Strategies)
        {
            if (strategy.CanHandle(marker))
            {
                return strategy.Mask(value, marker, settings) ?? value;
            }
        }

        return value;
    }
}
