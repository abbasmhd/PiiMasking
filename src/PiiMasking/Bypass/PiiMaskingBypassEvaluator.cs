using Microsoft.Extensions.Logging;

namespace PiiMasking.Bypass;

/// <summary>
/// Evaluates registered <see cref="IPiiMaskingBypassProvider"/> instances against the ambient <see cref="PiiMaskingBypassContext"/>.
/// The decision is memoized once per context (i.e. once per request) so providers — including database-backed ones — are not invoked per serialized property.
/// Failures are fail-closed: a throwing provider never disables masking; evaluation continues with the remaining providers.
/// </summary>
internal static class PiiMaskingBypassEvaluator
{
    public static bool ShouldBypass(
        PiiMaskingBypassContextAccessor? bypassContextAccessor,
        IReadOnlyList<IPiiMaskingBypassProvider>? bypassProviders,
        ILogger? logger = null)
    {
        if (bypassProviders is not { Count: > 0 } || bypassContextAccessor is null)
        {
            return false;
        }

        var context = bypassContextAccessor.Context;
        if (context is null)
        {
            return false;
        }

        if (context.CachedDecision is { } decision)
        {
            return decision;
        }

        var hadFailure = false;
        foreach (var provider in bypassProviders)
        {
            try
            {
                if (provider.ShouldBypass(context))
                {
                    context.CachedDecision = true;
                    return true;
                }
            }
            catch (Exception exception)
            {
                hadFailure = true;
                logger?.LogWarning(
                    exception,
                    "PiiMasking bypass provider {ProviderType} failed; masking remains enabled for the current evaluation.",
                    provider.GetType().FullName);
            }
        }

        // Cache a negative decision only when every provider completed successfully;
        // after a failure, retry on the next property instead of poisoning the whole request.
        if (!hadFailure)
        {
            context.CachedDecision = false;
        }

        return false;
    }
}
