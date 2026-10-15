using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiiMasking.Bypass;

namespace PiiMasking.AspNetCore;

/// <summary>
/// Evaluates the policy configured in <see cref="PiiMaskingBypassSettings.Policy"/> for the current user and stores the result on <see cref="PiiMaskingBypassContext.PolicyEvaluationSucceeded"/>; <see cref="PolicyBypassProvider"/> turns a successful result into a masking bypass.
/// </summary>
internal sealed class PolicyBypassProvider : IPiiMaskingBypassProvider
{
    public bool ShouldBypass(PiiMaskingBypassContext context) => context.PolicyEvaluationSucceeded == true;
}

/// <summary>
/// Captures the current user and request services into the ambient <see cref="PiiMaskingBypassContextAccessor"/> so masking is skipped for users matched by a bypass source.
/// Evaluates <see cref="PiiMaskingBypassSettings.Policy"/> (when configured) once per request. Register with <c>AddPiiMaskingBypass()</c> and place after <c>UseAuthentication</c>/<c>UseAuthorization</c>.
/// </summary>
public sealed class PiiMaskingBypassMiddleware(
    RequestDelegate next,
    IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
    PiiMaskingBypassContextAccessor bypassContextAccessor,
    ILogger<PiiMaskingBypassMiddleware>? logger = null)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly IOptionsMonitor<PiiMaskingSettings> _piiMaskingSettings = piiMaskingSettings ?? throw new ArgumentNullException(nameof(piiMaskingSettings));
    private readonly PiiMaskingBypassContextAccessor _bypassContextAccessor = bypassContextAccessor ?? throw new ArgumentNullException(nameof(bypassContextAccessor));
    private readonly ILogger? _logger = logger;

    public async Task InvokeAsync(HttpContext context, IAuthorizationService? authorizationService = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var policyName = _piiMaskingSettings.CurrentValue.Bypass?.Policy;
        bool? policyEvaluationSucceeded = null;
        if (!string.IsNullOrEmpty(policyName) && authorizationService is not null)
        {
            try
            {
                var result = await authorizationService.AuthorizeAsync(context.User, resource: null, policyName);
                policyEvaluationSucceeded = result.Succeeded;
            }
            catch (Exception exception)
            {
                // Fail-closed: a policy evaluation failure keeps masking enabled.
                policyEvaluationSucceeded = false;
                _logger?.LogWarning(
                    exception,
                    "PiiMasking bypass policy '{PolicyName}' evaluation failed; masking remains enabled.",
                    policyName);
            }
        }

        _bypassContextAccessor.Context = new PiiMaskingBypassContext
        {
            User = context.User,
            RequestServices = context.RequestServices,
            PolicyEvaluationSucceeded = policyEvaluationSucceeded,
        };

        try
        {
            await _next(context);
        }
        finally
        {
            _bypassContextAccessor.Context = null;
        }
    }
}
