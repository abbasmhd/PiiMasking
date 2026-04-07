using System.Reflection;
using Microsoft.Extensions.Options;

namespace abbasmhd.PiiMasking;

/// <summary>
/// Applies <see cref="PiiMaskingAttribute"/> using <see cref="IOptionsMonitor{T}"/> of <see cref="PiiMaskingSettings"/> (for custom outbound JSON pipelines and MVC JSON).
/// Runs registered <see cref="IPiiMaskingPropertyContributor"/> instances first, then built-in rules.
/// </summary>
public sealed class PiiMaskingPropertyStringTransform : IPiiMaskedPropertyStringTransform
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings;
    private readonly IReadOnlyList<IPiiMaskingPropertyContributor> _contributors;

    public PiiMaskingPropertyStringTransform(
        IOptionsMonitor<PiiMaskingSettings> settings,
        IEnumerable<IPiiMaskingPropertyContributor>? contributors = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _contributors = (contributors ?? Array.Empty<IPiiMaskingPropertyContributor>()).ToList();
    }

    /// <inheritdoc />
    public string Transform(PropertyInfo property, string value)
    {
        var marker = property.GetCustomAttribute<PiiMaskingAttribute>(inherit: true);
        if (marker is null)
        {
            return value;
        }

        var current = _settings.CurrentValue;
        if (!current.Enabled)
        {
            return value;
        }

        foreach (var contributor in _contributors)
        {
            var contributed = contributor.TryMask(property, value, marker, current);
            if (contributed is not null)
            {
                return contributed;
            }
        }

        return PiiMaskingKernel.ApplyBuiltin(value, marker, current);
    }
}
