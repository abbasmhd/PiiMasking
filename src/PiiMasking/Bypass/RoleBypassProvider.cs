using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace PiiMasking.Bypass;

/// <summary>
/// Bypasses masking when the current user is in any role configured in <see cref="PiiMaskingBypassSettings.Roles"/>.
/// </summary>
internal sealed class RoleBypassProvider(IOptionsMonitor<PiiMaskingSettings> settings) : IPiiMaskingBypassProvider
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public bool ShouldBypass(PiiMaskingBypassContext context)
    {
        var roles = _settings.CurrentValue.Bypass?.Roles;
        if (roles is not { Length: > 0 } || context.User is null)
        {
            return false;
        }

        foreach (var role in roles)
        {
            if (context.User.IsInRole(role))
            {
                return true;
            }
        }

        return false;
    }
}
