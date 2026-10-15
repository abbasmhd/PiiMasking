using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using PiiMasking.Bypass;

namespace PiiMasking.Serialization;

/// <summary>
/// Applies <see cref="PiiMaskedStringJsonConverter"/> to properties marked with <see cref="PiiMaskingAttribute"/>.
/// </summary>
public static class PiiMaskingJsonTypeInfoModifier
{
    /// <summary>
    /// Registers the PII masking modifier. <paramref name="propertyStringTransform"/> should resolve from DI (includes <see cref="IPiiMaskingPropertyContributor"/> and <see cref="IPiiMaskingExecutionStrategy"/> when registered).
    /// Pass the same <see cref="IPiiMaskingExecutionStrategy"/> sequence as used to construct <paramref name="propertyStringTransform"/> so properties without <see cref="PropertyInfo"/> still honor <see cref="PiiMaskingAttribute.Mode"/>.
    /// Pass <paramref name="bypassContextAccessor"/> and <paramref name="bypassProviders"/> so properties without <see cref="PropertyInfo"/> still honor the masking bypass.
    /// </summary>
    public static void AddPiiMaskingJsonModifier(
        this JsonSerializerOptions options,
        IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
        IPiiMaskedPropertyStringTransform propertyStringTransform,
        IEnumerable<IPiiMaskingExecutionStrategy>? executionStrategies = null,
        PiiMaskingBypassContextAccessor? bypassContextAccessor = null,
        IEnumerable<IPiiMaskingBypassProvider>? bypassProviders = null)
    {
        ArgumentNullException.ThrowIfNull(propertyStringTransform);
        var strategiesList = executionStrategies?.ToList();
        var bypassProvidersList = bypassProviders?.ToList();
        void Modifier(JsonTypeInfo typeInfo) => Modify(typeInfo, piiMaskingSettings, propertyStringTransform, strategiesList, bypassContextAccessor, bypassProvidersList);

        if (options.TypeInfoResolver is DefaultJsonTypeInfoResolver resolver)
        {
            resolver.Modifiers.Add(Modifier);
            return;
        }

        var inner = new DefaultJsonTypeInfoResolver { Modifiers = { Modifier } };
        if (options.TypeInfoResolver is null)
        {
            options.TypeInfoResolver = inner;
            return;
        }

        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, inner);
    }

    /// <summary>
    /// Registers the modifier using built-in masking only (no <see cref="IPiiMaskingPropertyContributor"/>). Prefer <see cref="AddPiiMaskingJsonModifier(JsonSerializerOptions, IOptionsMonitor{PiiMaskingSettings}, IPiiMaskedPropertyStringTransform, IEnumerable{IPiiMaskingExecutionStrategy}?, PiiMaskingBypassContextAccessor?, IEnumerable{IPiiMaskingBypassProvider}?)"/> when using DI.
    /// Pass <paramref name="bypassContextAccessor"/> and <paramref name="bypassProviders"/> to honor the masking bypass (roles, claims, policy, or custom providers).
    /// </summary>
    public static void AddPiiMaskingJsonModifier(
        this JsonSerializerOptions options,
        IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
        IEnumerable<IPiiMaskingExecutionStrategy>? executionStrategies = null,
        PiiMaskingBypassContextAccessor? bypassContextAccessor = null,
        IEnumerable<IPiiMaskingBypassProvider>? bypassProviders = null)
    {
        var strategiesList = executionStrategies?.ToList();
        var bypassProvidersList = bypassProviders?.ToList();
        void Modifier(JsonTypeInfo typeInfo) => ModifyBuiltinOnly(typeInfo, piiMaskingSettings, strategiesList, bypassContextAccessor, bypassProvidersList);

        if (options.TypeInfoResolver is DefaultJsonTypeInfoResolver resolver)
        {
            resolver.Modifiers.Add(Modifier);
            return;
        }

        var inner = new DefaultJsonTypeInfoResolver { Modifiers = { Modifier } };
        if (options.TypeInfoResolver is null)
        {
            options.TypeInfoResolver = inner;
            return;
        }

        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, inner);
    }

    private static PiiMaskingAttribute? GetPiiMaskingAttribute(JsonPropertyInfo property)
    {
        var provider = property.AttributeProvider;
        if (provider is null)
        {
            return null;
        }

        if (provider is MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttribute<PiiMaskingAttribute>(inherit: true);
        }

        foreach (var attr in provider.GetCustomAttributes(typeof(PiiMaskingAttribute), inherit: true))
        {
            if (attr is PiiMaskingAttribute piiMasking)
            {
                return piiMasking;
            }
        }

        return null;
    }

    private static void Modify(
        JsonTypeInfo typeInfo,
        IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
        IPiiMaskedPropertyStringTransform propertyStringTransform,
        IReadOnlyList<IPiiMaskingExecutionStrategy>? executionStrategies,
        PiiMaskingBypassContextAccessor? bypassContextAccessor,
        IReadOnlyList<IPiiMaskingBypassProvider>? bypassProviders)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var attribute = GetPiiMaskingAttribute(property);
            if (attribute is null)
            {
                continue;
            }

            if (property.AttributeProvider is not PropertyInfo propertyInfo)
            {
                property.CustomConverter = new PiiMaskedStringJsonConverter(piiMaskingSettings, attribute, executionStrategies, bypassContextAccessor, bypassProviders);
                continue;
            }

            property.CustomConverter = new PiiMaskedStringJsonConverter(
                piiMaskingSettings,
                propertyStringTransform,
                propertyInfo,
                attribute);
        }
    }

    private static void ModifyBuiltinOnly(
        JsonTypeInfo typeInfo,
        IOptionsMonitor<PiiMaskingSettings> piiMaskingSettings,
        IReadOnlyList<IPiiMaskingExecutionStrategy>? executionStrategies,
        PiiMaskingBypassContextAccessor? bypassContextAccessor,
        IReadOnlyList<IPiiMaskingBypassProvider>? bypassProviders)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var attribute = GetPiiMaskingAttribute(property);
            if (attribute is null)
            {
                continue;
            }

            property.CustomConverter = new PiiMaskedStringJsonConverter(piiMaskingSettings, attribute, executionStrategies, bypassContextAccessor, bypassProviders);
        }
    }
}
