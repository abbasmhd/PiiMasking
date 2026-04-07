using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using PiiMasking.Serialization;
using Xunit;

namespace PiiMasking.Tests;

public class PiiMaskingExecutionStrategyTests
{
    private sealed class TestOptionsMonitor : IOptionsMonitor<PiiMaskingSettings>
    {
        public required PiiMaskingSettings CurrentValue { get; set; }

        public PiiMaskingSettings Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<PiiMaskingSettings, string?> listener) => NullSubscription.Instance;

        private sealed class NullSubscription : IDisposable
        {
            public static readonly NullSubscription Instance = new();
            public void Dispose() { }
        }
    }

    /// <summary>
    /// <c>public const string Name</c> for <c>[PiiMasking(Mode = ...)]</c>; explicit interface implementation avoids clashing with the const.
    /// </summary>
    public sealed class RedactAllStrategy : IPiiMaskingExecutionStrategy
    {
        public const string Name = "RedactAll";

        string IPiiMaskingExecutionStrategy.Name => Name;

        public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) => "****";
    }

    private sealed class Dto
    {
        [PiiMasking(Mode = RedactAllStrategy.Name)]
        public string Secret { get; set; } = "";
    }

    [Fact]
    public void ExecutionStrategy_MatchingMode_AppliesBeforeBuiltin()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Secret))!;
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            executionStrategies: [new RedactAllStrategy()]);

        var result = transform.Transform(prop, "should-not-appear");
        Assert.Equal("****", result);
    }

    [Fact]
    public void ExecutionStrategy_UnsetMode_UsesBuiltin()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };

        var builtinProp = typeof(BuiltinDto).GetProperty(nameof(BuiltinDto.Name))!;
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            executionStrategies: [new RedactAllStrategy()]);

        var result = transform.Transform(builtinProp, "John Doe");
        Assert.Equal("Jo**** Do****", result);
    }

    private sealed class BuiltinDto
    {
        [PiiMasking(MaskEachWord = true)]
        public string Name { get; set; } = "";
    }

    [Fact]
    public void ExecutionStrategy_ModeWithNoRegistration_Throws()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Secret))!;
        var transform = new PiiMaskingPropertyStringTransform(monitor, executionStrategies: []);

        var ex = Assert.Throws<InvalidOperationException>(() => transform.Transform(prop, "x"));
        Assert.Contains("RedactAll", ex.Message);
    }

    [Fact]
    public void JsonSerializer_UsesExecutionStrategyWhenModeMatches()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var strategies = new RedactAllStrategy[] { new() };
        var transform = new PiiMaskingPropertyStringTransform(monitor, executionStrategies: strategies);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        jsonOptions.AddPiiMaskingJsonModifier(monitor, transform, strategies);

        var json = JsonSerializer.Serialize(new Dto { Secret = "secret" }, jsonOptions);
        Assert.Equal("""{"secret":"****"}""", json);
    }
}
