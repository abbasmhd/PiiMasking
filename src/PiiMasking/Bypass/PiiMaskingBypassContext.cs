using System.Security.Claims;

namespace PiiMasking.Bypass;

/// <summary>
/// Describes the current user/request used by <see cref="IPiiMaskingBypassProvider"/> implementations to decide whether masking is skipped.
/// </summary>
public sealed class PiiMaskingBypassContext
{
    /// <summary>
    /// Principal for the current user, or null when no user context is available.
    /// </summary>
    public ClaimsPrincipal? User { get; init; }

    /// <summary>
    /// Services scoped to the current request, or null when unavailable. Custom providers (e.g. database-backed) resolve scoped dependencies from here instead of injecting them.
    /// </summary>
    public IServiceProvider? RequestServices { get; init; }

    /// <summary>
    /// Result of evaluating <see cref="PiiMaskingBypassSettings.Policy"/> for the current user; null when not evaluated. Set by the PiiMasking.AspNetCore middleware.
    /// </summary>
    public bool? PolicyEvaluationSucceeded { get; init; }

    /// <summary>
    /// Memoized decision so providers run at most once per request (the transform is a singleton invoked per property). Internal use only.
    /// </summary>
    internal bool? CachedDecision { get; set; }
}
