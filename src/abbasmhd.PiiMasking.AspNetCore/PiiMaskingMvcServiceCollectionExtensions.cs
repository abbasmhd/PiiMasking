using Microsoft.Extensions.DependencyInjection;

namespace abbasmhd.PiiMasking.AspNetCore;

/// <summary>
/// ASP.NET Core registration helpers for MVC JSON masking.
/// </summary>
public static class PiiMaskingMvcServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PiiMaskingMvcJsonPostConfigure"/> so MVC JSON serialization applies <see cref="PiiMaskingAttribute"/> on string properties.
    /// Call <see cref="PiiMasking.PiiMaskingServiceCollectionExtensions.AddPiiMasking"/> first so <see cref="PiiMaskingSettings"/> is configured.
    /// </summary>
    public static IServiceCollection AddPiiMaskingMvcJson(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.ConfigureOptions<PiiMaskingMvcJsonPostConfigure>();
        return services;
    }
}
