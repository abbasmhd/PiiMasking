using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace PiiMasking.Bypass;

/// <summary>
/// Bypasses masking when the current user holds any claim configured in <see cref="PiiMaskingBypassSettings.Claims"/>.
/// A requirement with an empty <see cref="PiiMaskingClaimBypassRequirement.Value"/> matches any value of its claim type.
/// </summary>
internal sealed class ClaimBypassProvider(IOptionsMonitor<PiiMaskingSettings> settings) : IPiiMaskingBypassProvider
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public bool ShouldBypass(PiiMaskingBypassContext context)
    {
        var requirements = _settings.CurrentValue.Bypass?.Claims;
        var user = context.User;
        if (requirements is not { Length: > 0 } || user is null)
        {
            return false;
        }

        foreach (var requirement in requirements)
        {
            if (string.IsNullOrEmpty(requirement.Type))
            {
                continue;
            }

            if (string.IsNullOrEmpty(requirement.Value))
            {
                if (user.HasClaim(claim => claim.Type == requirement.Type))
                {
                    return true;
                }
            }
            else if (user.HasClaim(requirement.Type, requirement.Value))
            {
                return true;
            }
        }

        return false;
    }
}
