using System.Reflection;
using Microsoft.Extensions.Options;

namespace PiiMasking;

/// <summary>
/// Applies <see cref="PiiMaskingAttribute"/> using <see cref="IOptionsMonitor{T}"/> of <see cref="PiiMaskingSettings"/> (for custom outbound JSON pipelines and MVC JSON).
/// Runs registered <see cref="IPiiMaskingPropertyContributor"/> instances first, then <see cref="IPiiMaskingExecutionStrategy"/> when <see cref="PiiMaskingAttribute.Mode"/> is set, then built-in rules.
/// </summary>
public sealed class PiiMaskingPropertyStringTransform : IPiiMaskedPropertyStringTransform
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings;
    private readonly IReadOnlyList<IPiiMaskingPropertyContributor> _contributors;
    private readonly IReadOnlyList<IPiiMaskingExecutionStrategy> _executionStrategies;

    public PiiMaskingPropertyStringTransform(
        IOptionsMonitor<PiiMaskingSettings> settings,
        IEnumerable<IPiiMaskingPropertyContributor>? contributors = null,
        IEnumerable<IPiiMaskingExecutionStrategy>? executionStrategies = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _contributors = (contributors ?? Array.Empty<IPiiMaskingPropertyContributor>()).ToList();
        _executionStrategies = (executionStrategies ?? Array.Empty<IPiiMaskingExecutionStrategy>()).ToList();
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

        var named = PiiMaskingExecutionStrategyInvoker.TryApplyNamedStrategy(_executionStrategies, value, marker, current);
        if (named is not null)
        {
            return named;
        }

        return PiiMaskingKernel.ApplyBuiltin(value, marker, current);
    }
}
