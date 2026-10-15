namespace PiiMasking.Bypass;

/// <summary>
/// Configuration for masking bypass sources. All sources are opt-in: leave entries empty (or omit the <c>Bypass</c> section) to always mask.
/// </summary>
public sealed class PiiMaskingBypassSettings
{
    /// <summary>
    /// Role names for which masking is skipped (checked via <see cref="System.Security.Claims.ClaimsPrincipal.IsInRole(string)"/>).
    /// </summary>
    public string[] Roles { get; set; } = [];

    /// <summary>
    /// Claim requirements for which masking is skipped (any match wins).
    /// </summary>
    public PiiMaskingClaimBypassRequirement[] Claims { get; set; } = [];

    /// <summary>
    /// Name of an ASP.NET Core authorization policy; when it succeeds for the current user, masking is skipped.
    /// Evaluated by the PiiMasking.AspNetCore middleware (<see cref="PiiMasking.AspNetCore.PiiMaskingBypassMiddleware"/>); ignored outside ASP.NET Core.
    /// </summary>
    public string? Policy { get; set; }
}

/// <summary>
/// A claim type/value pair that grants a masking bypass. When <see cref="Value"/> is empty, any value of <see cref="Type"/> matches.
/// </summary>
public sealed class PiiMaskingClaimBypassRequirement
{
    /// <summary>Claim type to match (e.g. <c>permission</c>).</summary>
    public string Type { get; set; } = "";

    /// <summary>Claim value to match (e.g. <c>pii:read</c>). Empty means any value.</summary>
    public string Value { get; set; } = "";
}
