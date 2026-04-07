using System.Reflection;

namespace PiiMasking;

/// <summary>
/// Optional extension point: run before built-in <see cref="PiiMaskingAttribute"/> rules when masking is enabled.
/// Register with <c>services.AddSingleton&lt;IPiiMaskingPropertyContributor, YourType&gt;()</c> after <see cref="PiiMaskingServiceCollectionExtensions.AddPiiMasking"/>.
/// </summary>
public interface IPiiMaskingPropertyContributor
{
    /// <summary>
    /// Return a non-null string to use as the masked output. Return null to let the next contributor (if any) or built-in rules handle the value.
    /// </summary>
    /// <param name="property">The property being serialized.</param>
    /// <param name="value">Plain string; never null.</param>
    /// <param name="marker">The <see cref="PiiMaskingAttribute"/> on <paramref name="property"/>.</param>
    /// <param name="settings">Current masking settings (suffix, literals, etc.).</param>
    string? TryMask(PropertyInfo property, string value, PiiMaskingAttribute marker, PiiMaskingSettings settings);
}
