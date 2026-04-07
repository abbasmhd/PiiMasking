using Microsoft.Extensions.Options;

namespace PiiMasking;

/// <summary>
/// Default <see cref="IOptionsMonitor{T}"/> when tests or hosts omit configuration binding.
/// </summary>
public sealed class PiiMaskingFallbackOptionsMonitor : IOptionsMonitor<PiiMaskingSettings>
{
    private static readonly PiiMaskingSettings Default = new();

    public PiiMaskingSettings CurrentValue => Default;

    public PiiMaskingSettings Get(string? name) => Default;

    public IDisposable OnChange(Action<PiiMaskingSettings, string?> listener) => EmptyDisposable.Instance;

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}
