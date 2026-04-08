using PiiMasking.Strategies;

namespace PiiMasking;

/// <summary>
/// Built-in masking rules shared by <see cref="PiiMaskingPropertyStringTransform"/> and JSON fallback paths.
/// </summary>
/// <remarks>
/// Strategy evaluation order is significant and intentional. Strategies are evaluated in this order:
/// <list type="number">
/// <item><term>Email Strategy</term><description>If <see cref="PiiMaskingAttribute.AsEmail"/> is true, masks only the local part of email addresses.</description></item>
/// <item><term>MaskEachWordRespectingLiterals Strategy</term><description>If <see cref="PiiMaskingAttribute.MaskEachWordRespectingLiterals"/> is true, respects configured literal separators.</description></item>
/// <item><term>MaskEachWord Strategy</term><description>If <see cref="PiiMaskingAttribute.MaskEachWord"/> is true, masks each whitespace-separated word.</description></item>
/// <item><term>Segment Strategy (Fallback)</term><description>Applied by default, masks the entire value as a single segment.</description></item>
/// </list>
/// The order ensures that more specific rules (email, literals) are evaluated before generic ones (each word, segment).
/// This prevents accidentally applying a less appropriate strategy to a value.
/// </remarks>
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
