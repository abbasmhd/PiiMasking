namespace PiiMasking.Bypass;

/// <summary>
/// Decides whether PII masking is skipped for the current user/request. Registered providers are consulted once per request; the first <c>true</c> wins.
/// A provider failure never disables masking: evaluation continues with remaining providers (fail-closed).
/// </summary>
public interface IPiiMaskingBypassProvider
{
    /// <summary>
    /// Returns true when masking should be skipped for the current user/request.
    /// </summary>
    bool ShouldBypass(PiiMaskingBypassContext context);
}
