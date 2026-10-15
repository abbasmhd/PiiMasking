using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PiiMasking;
using PiiMasking.Bypass;

namespace PiiMasking.AspNetCore;

/// <summary>
/// Registers the masking bypass infrastructure: the ambient <see cref="PiiMaskingBypassContextAccessor"/> and <see cref="PolicyBypassProvider"/> (built-in role/claim providers come from <see cref="PiiMaskingServiceCollectionExtensions.AddPiiMasking"/>).
/// Combine with <see cref="PiiMaskingApplicationBuilderExtensions.UsePiiMaskingBypass"/> in the request pipeline.
/// </summary>
public static class PiiMaskingBypassServiceCollectionExtensions
{
    /// <summary>
    /// Enables masking bypass for the current application. Call after <see cref="PiiMaskingServiceCollectionExtensions.AddPiiMasking"/>; configure sources via <c>PiiMasking:Bypass</c> or by registering custom <see cref="IPiiMaskingBypassProvider"/> implementations.
    /// </summary>
    public static IServiceCollection AddPiiMaskingBypass(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<PiiMaskingBypassContextAccessor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPiiMaskingBypassProvider, PolicyBypassProvider>());
        return services;
    }
}

/// <summary>
/// Adds <see cref="PiiMaskingBypassMiddleware"/> to the request pipeline.
/// </summary>
public static class PiiMaskingApplicationBuilderExtensions
{
    /// <summary>
    /// Captures the current user for masking bypass decisions. Requires <c>AddPiiMaskingBypass()</c>. Place after <c>UseAuthentication</c>/<c>UseAuthorization</c> (or before endpoints that serialize masked DTOs).
    /// </summary>
    public static IApplicationBuilder UsePiiMaskingBypass(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<PiiMaskingBypassMiddleware>();
        return app;
    }
}
