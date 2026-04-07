namespace PiiMasking.Strategies;

/// <summary>
/// Selects and applies one built-in masking rule for a <see cref="PiiMaskingAttribute"/>.
/// </summary>
internal interface IPiiMaskingStrategy
{
    /// <summary>
    /// Whether this strategy should run for the given attribute (evaluation order is defined by <see cref="PiiMaskingKernel"/>).
    /// </summary>
    bool CanHandle(PiiMaskingAttribute marker);

    /// <summary>
    /// Applies masking; <paramref name="value"/> is non-null when called from the kernel.
    /// </summary>
    string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings);
}
