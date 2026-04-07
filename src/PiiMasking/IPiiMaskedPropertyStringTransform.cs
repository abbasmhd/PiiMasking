using System.Reflection;

namespace PiiMasking;

/// <summary>
/// Hook for custom JSON serializers to apply the same masking as <see cref="PiiMaskingAttribute"/> / <see cref="Serialization.PiiMaskedStringJsonConverter"/>.
/// The default implementation <see cref="PiiMaskingPropertyStringTransform"/> runs <see cref="IPiiMaskingPropertyContributor"/> instances first, then built-in rules.
/// </summary>
public interface IPiiMaskedPropertyStringTransform
{
    /// <summary>
    /// Returns the string to write for JSON. <paramref name="value"/> is never null.
    /// </summary>
    string Transform(PropertyInfo property, string value);
}
