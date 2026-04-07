using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using abbasmhd.PiiMasking;
using abbasmhd.PiiMasking.Serialization;

namespace abbasmhd.PiiMasking.AspNetCore;

/// <summary>
/// Wires PII string masking for MVC <see cref="JsonOptions"/> using <see cref="PiiMaskingSettings"/> and <see cref="IPiiMaskedPropertyStringTransform"/>.
/// </summary>
public sealed class PiiMaskingMvcJsonPostConfigure(
    IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
    IPiiMaskedPropertyStringTransform propertyStringTransform)
    : IPostConfigureOptions<JsonOptions>
{
    private readonly IOptionsMonitor<PiiMaskingSettings> _piiMaskingSettings = piiMaskingSettings;
    private readonly IPiiMaskedPropertyStringTransform _propertyStringTransform = propertyStringTransform;

    public void PostConfigure(string? name, JsonOptions options)
    {
        options.JsonSerializerOptions.AddPiiMaskingJsonModifier(_piiMaskingSettings, _propertyStringTransform);
    }
}
