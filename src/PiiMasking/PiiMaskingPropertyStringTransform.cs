using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiiMasking.Bypass;

namespace PiiMasking;

/// <summary>
/// Applies <see cref="PiiMaskingAttribute"/> using <see cref="IOptionsMonitor{T}"/> of <see cref="PiiMaskingSettings"/> (for custom outbound JSON pipelines and MVC JSON).
/// Skips masking entirely when a registered <see cref="IPiiMaskingBypassProvider"/> matches the current user (roles, claims, policy, or a custom source such as a database flag).
/// Otherwise runs registered <see cref="IPiiMaskingPropertyContributor"/> instances first, then <see cref="IPiiMaskingExecutionStrategy"/> when <see cref="PiiMaskingAttribute.Mode"/> is set, then built-in rules.
/// </summary>
public sealed class PiiMaskingPropertyStringTransform : IPiiMaskedPropertyStringTransform
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings;
    private readonly IReadOnlyList<IPiiMaskingPropertyContributor> _contributors;
    private readonly IReadOnlyList<IPiiMaskingExecutionStrategy> _executionStrategies;
    private readonly PiiMaskingBypassContextAccessor? _bypassContextAccessor;
    private readonly IReadOnlyList<IPiiMaskingBypassProvider>? _bypassProviders;
    private readonly ILogger? _logger;

    public PiiMaskingPropertyStringTransform(
        IOptionsMonitor<PiiMaskingSettings> settings,
        IEnumerable<IPiiMaskingPropertyContributor>? contributors = null,
        IEnumerable<IPiiMaskingExecutionStrategy>? executionStrategies = null,
        PiiMaskingBypassContextAccessor? bypassContextAccessor = null,
        IEnumerable<IPiiMaskingBypassProvider>? bypassProviders = null,
        ILogger<PiiMaskingPropertyStringTransform>? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _contributors = contributors?.ToList() ?? [];
        _executionStrategies = executionStrategies?.ToList() ?? [];
        _bypassContextAccessor = bypassContextAccessor;
        _bypassProviders = bypassProviders?.ToList();
        _logger = logger;
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

        if (PiiMaskingBypassEvaluator.ShouldBypass(_bypassContextAccessor, _bypassProviders, _logger))
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
