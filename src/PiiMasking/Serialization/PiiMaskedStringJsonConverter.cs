using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PiiMasking.Serialization;

/// <summary>
/// Writes masked strings when <see cref="PiiMaskingSettings.Enabled"/> is true; reads plain strings from JSON.
/// Uses <see cref="IPiiMaskedPropertyStringTransform"/> when a <see cref="PropertyInfo"/> is supplied (MVC / modifier path) so contributors apply.
/// </summary>
public sealed class PiiMaskedStringJsonConverter : JsonConverter<string>
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _settings;
    private readonly PiiMaskingAttribute _marker;
    private readonly IPiiMaskedPropertyStringTransform? _transform;
    private readonly PropertyInfo? _property;

    /// <summary>
    /// Creates a converter that applies masking via <paramref name="propertyStringTransform"/> (includes contributors).
    /// </summary>
    public PiiMaskedStringJsonConverter(
        IOptionsMonitor<PiiMaskingSettings> settings,
        IPiiMaskedPropertyStringTransform propertyStringTransform,
        PropertyInfo property,
        PiiMaskingAttribute marker)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _transform = propertyStringTransform ?? throw new ArgumentNullException(nameof(propertyStringTransform));
        _property = property ?? throw new ArgumentNullException(nameof(property));
        _marker = marker ?? throw new ArgumentNullException(nameof(marker));
    }

    /// <summary>
    /// Creates a converter using built-in rules only (no <see cref="IPiiMaskingPropertyContributor"/>). Prefer the overload with <see cref="IPiiMaskedPropertyStringTransform"/> when wiring DI.
    /// </summary>
    public PiiMaskedStringJsonConverter(IOptionsMonitor<PiiMaskingSettings> settings, PiiMaskingAttribute marker)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _marker = marker ?? throw new ArgumentNullException(nameof(marker));
        _transform = null;
        _property = null;
    }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString();

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (!_settings.CurrentValue.Enabled)
        {
            writer.WriteStringValue(value);
            return;
        }

        string masked;
        if (_transform is not null && _property is not null)
        {
            masked = _transform.Transform(_property, value);
        }
        else
        {
            masked = PiiMaskingKernel.ApplyBuiltin(value, _marker, _settings.CurrentValue);
        }

        writer.WriteStringValue(masked);
    }
}
