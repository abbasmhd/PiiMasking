namespace PiiMasking;

/// <summary>
/// Named masking rule selected when <see cref="PiiMaskingAttribute.Mode"/> matches <see cref="Name"/>.
/// Register implementations with <c>services.AddSingleton&lt;IPiiMaskingExecutionStrategy, YourType&gt;()</c> after <see cref="PiiMaskingServiceCollectionExtensions.AddPiiMasking"/>.
/// </summary>
public interface IPiiMaskingExecutionStrategy
{
    /// <summary>
    /// Stable identifier; must match <see cref="PiiMaskingAttribute.Mode"/> (ordinal, case-sensitive).
    /// For <c>[PiiMasking(Mode = ...)]</c> use a compile-time constant equal to this value. If you name that constant <c>Name</c>, implement this member with explicit interface syntax so it does not collide with the const (see unit tests).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Masks <paramref name="value"/> when this strategy is selected. <paramref name="value"/> is non-null.
    /// </summary>
    string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings);
}
