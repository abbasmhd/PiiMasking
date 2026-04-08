using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace PiiMasking;

/// <summary>
/// Registers <see cref="PiiMaskingSettings"/> and <see cref="IPiiMaskedPropertyStringTransform"/>.
/// Register <see cref="IPiiMaskingPropertyContributor"/> and <see cref="IPiiMaskingExecutionStrategy"/> implementations with <c>AddSingleton</c> to extend masking (contributors first, then strategies selected by <see cref="PiiMaskingAttribute.Mode"/>, then built-ins).
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
        });

        services.TryAddSingleton<IPiiMaskedPropertyStringTransform, PiiMaskingPropertyStringTransform>();
        return services;
    }
}
