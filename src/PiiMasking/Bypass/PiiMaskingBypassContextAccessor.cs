namespace PiiMasking.Bypass;

/// <summary>
/// Holds the <see cref="PiiMaskingBypassContext"/> for the logical async flow (AsyncLocal).
/// Registered as a singleton; the PiiMasking.AspNetCore middleware assigns the context at the start of each request so the singleton transform/converter can read it during serialization.
/// </summary>
public sealed class PiiMaskingBypassContextAccessor
{
    private static readonly AsyncLocal<PiiMaskingBypassContext?> Current = new();

    /// <summary>
    /// Context for the current async flow, or null when bypass is not active.
    /// </summary>
    public PiiMaskingBypassContext? Context
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
