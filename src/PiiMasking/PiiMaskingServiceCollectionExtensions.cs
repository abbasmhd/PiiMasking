using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PiiMasking.Bypass;

namespace PiiMasking;

/// <summary>
/// Registers <see cref="PiiMaskingSettings"/> and <see cref="IPiiMaskedPropertyStringTransform"/>.
/// Register <see cref="IPiiMaskingPropertyContributor"/> and <see cref="IPiiMaskingExecutionStrategy"/> implementations with <c>AddSingleton</c> to extend masking (contributors first, then strategies selected by <see cref="PiiMaskingAttribute.Mode"/>, then built-ins).
/// Register custom <see cref="IPiiMaskingBypassProvider"/> implementations (e.g. a database-flag check) to skip masking for selected users; built-in role and claim providers are registered automatically.
/// </summary>
public static class PiiMaskingServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="PiiMaskingSettings"/> from configuration section <see cref="PiiMaskingSettings.SectionName"/> and applies the same defaults as the reference host.
    /// </summary>
    public static IServiceCollection AddPiiMasking(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PiiMaskingSettings>(configuration.GetSection(PiiMaskingSettings.SectionName));
        services.PostConfigure<PiiMaskingSettings>(settings =>
        {
            var enablePiiMasking = configuration.GetSection(PiiMaskingSettings.SectionName)["Enabled"];
            if (string.IsNullOrEmpty(enablePiiMasking))
            {
                settings.Enabled = true;
            }

            if (string.IsNullOrWhiteSpace(settings.MaskSuffix))
            {
                settings.MaskSuffix = PiiMaskingSettings.DefaultMaskSuffix;
            }

            // Validate mask suffix length
            if (settings.MaskSuffix.Length > 20)
            {
                throw new InvalidOperationException(
                    $"PiiMaskingSettings.MaskSuffix must be 20 characters or fewer. Current length: {settings.MaskSuffix.Length}");
            }

            if (settings.LiteralWordMaskSeparators is { Length: > 0 })
            {
                settings.LiteralWordMaskSeparators = settings.LiteralWordMaskSeparators
                    .Where(static s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }

            if (settings.Bypass is { } bypass)
            {
                if (bypass.Roles is { Length: > 0 })
                {
                    bypass.Roles = bypass.Roles
                        .Where(static r => !string.IsNullOrWhiteSpace(r))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                }

                if (bypass.Claims is { Length: > 0 })
                {
                    bypass.Claims = bypass.Claims
                        .Where(static c => !string.IsNullOrWhiteSpace(c.Type))
                        .ToArray();
                }

                if (string.IsNullOrWhiteSpace(bypass.Policy))
                {
                    bypass.Policy = null;
                }
            }
        });

        services.TryAddSingleton<PiiMaskingBypassContextAccessor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPiiMaskingBypassProvider, RoleBypassProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPiiMaskingBypassProvider, ClaimBypassProvider>());
        services.TryAddSingleton<IPiiMaskedPropertyStringTransform, PiiMaskingPropertyStringTransform>();
        return services;
    }
}
