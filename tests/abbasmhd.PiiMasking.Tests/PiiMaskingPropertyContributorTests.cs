using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using abbasmhd.PiiMasking.Serialization;
using Xunit;

namespace abbasmhd.PiiMasking.Tests;

public class PiiMaskingPropertyContributorTests
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

    private sealed class FixedContributor(string? whenPropertyName, string? result) : IPiiMaskingPropertyContributor
    {
        public string? TryMask(PropertyInfo property, string value, PiiMaskingAttribute marker, PiiMaskingSettings settings) =>
            property.Name == whenPropertyName ? result : null;
    }

    private sealed class Dto
    {
        [PiiMasking(MaskEachWord = true)]
        public string Name { get; set; } = "";
    }

    [Fact]
    public void Contributor_NonNullResult_ShortCircuitsBuiltin()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            [new FixedContributor(nameof(Dto.Name), "CUSTOM")]);

        var result = transform.Transform(prop, "John Doe");
        Assert.Equal("CUSTOM", result);
    }

    [Fact]
    public void Contributor_Null_DelegatesToBuiltin()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            [new FixedContributor(nameof(Dto.Name), null)]);

        var result = transform.Transform(prop, "John Doe");
        Assert.Equal("Jo**** Do****", result);
    }

    [Fact]
    public void Contributors_RunInOrder_FirstNonNullWins()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            [
                new FixedContributor(nameof(Dto.Name), null),
                new FixedContributor(nameof(Dto.Name), "SECOND"),
            ]);

        var result = transform.Transform(prop, "John Doe");
        Assert.Equal("SECOND", result);
    }

    [Fact]
    public void JsonSerializer_UsesTransformWithContributors()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            [new FixedContributor(nameof(Dto.Name), "JSON-CUSTOM")]);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        jsonOptions.AddPiiMaskingJsonModifier(monitor, transform);

        var json = JsonSerializer.Serialize(new Dto { Name = "John Doe" }, jsonOptions);
        Assert.Equal("""{"name":"JSON-CUSTOM"}""", json);
    }
}
