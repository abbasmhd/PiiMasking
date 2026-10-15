using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PiiMasking.Bypass;
using PiiMasking.Serialization;
using Xunit;

namespace PiiMasking.Tests;

public class PiiMaskingBypassTests
{
    private sealed class TestOptionsMonitor : IOptionsMonitor<PiiMaskingSettings>
    {
        public required PiiMaskingSettings CurrentValue { get; set; }

        public PiiMaskingSettings Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<PiiMaskingSettings, string?> listener) =>
            throw new NotSupportedException();
    }

    private sealed class Dto
    {
        [PiiMasking(MaskEachWord = true)]
        public string Name { get; set; } = "";
    }

    private sealed class ThrowingProvider : IPiiMaskingBypassProvider
    {
        public bool ShouldBypass(PiiMaskingBypassContext context) => throw new InvalidOperationException("boom");
    }

    private sealed class DbFlagProvider : IPiiMaskingBypassProvider
    {
        public int CallCount { get; private set; }

        public bool ShouldBypass(PiiMaskingBypassContext context)
        {
            CallCount++;
            return context.RequestServices is TestRequestServices { BypassEnabled: true };
        }
    }

    private sealed class CountingProvider(bool result) : IPiiMaskingBypassProvider
    {
        public int CallCount { get; private set; }

        public bool ShouldBypass(PiiMaskingBypassContext context)
        {
            CallCount++;
            return result;
        }
    }

    private sealed class TestRequestServices(bool bypassEnabled) : IServiceProvider
    {
        public bool BypassEnabled { get; } = bypassEnabled;
        public object? GetService(Type serviceType) => null;
    }

    private static PiiMaskingBypassContext ContextFor(params Claim[] claims) =>
        new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };

    private static (PiiMaskingBypassContextAccessor Accessor, PiiMaskingPropertyStringTransform Transform) CreateTransformer(
        PiiMaskingSettings settings,
        params IPiiMaskingBypassProvider[] providers)
    {
        var monitor = new TestOptionsMonitor { CurrentValue = settings };
        var accessor = new PiiMaskingBypassContextAccessor();
        var transform = new PiiMaskingPropertyStringTransform(
            monitor,
            bypassContextAccessor: accessor,
            bypassProviders:
            [
                new RoleBypassProvider(monitor),
                new ClaimBypassProvider(monitor),
                ..providers,
            ]);
        return (accessor, transform);
    }

    [Fact]
    public void Role_MatchingUser_IsBypassed()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Roles = ["PiiViewer"] },
        };
        var (accessor, transform) = CreateTransformer(settings);
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "PiiViewer"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void Role_NonMatchingUser_IsMasked()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Roles = ["PiiViewer"] },
        };
        var (accessor, transform) = CreateTransformer(settings);
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "RegularUser"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void Claim_MatchingTypeAndValue_IsBypassed()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings
            {
                Claims = [new PiiMaskingClaimBypassRequirement { Type = "permission", Value = "pii:read" }],
            },
        };
        var (accessor, transform) = CreateTransformer(settings);
        accessor.Context = ContextFor(new Claim("permission", "pii:read"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void Claim_EmptyValue_MatchesAnyValueOfType()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings
            {
                Claims = [new PiiMaskingClaimBypassRequirement { Type = "pii-exempt" }],
            },
        };
        var (accessor, transform) = CreateTransformer(settings);
        accessor.Context = ContextFor(new Claim("pii-exempt", "whatever"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void NoContext_IsMasked()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Roles = ["PiiViewer"] },
        };
        var (_, transform) = CreateTransformer(settings);
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void ThrowingProvider_FailsClosed_IsMasked()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var (accessor, transform) = CreateTransformer(settings, new ThrowingProvider());
        accessor.Context = ContextFor();
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void ThrowingProvider_ContinuesWithRemainingProviders()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var (accessor, transform) = CreateTransformer(settings, new ThrowingProvider(), new DbFlagProvider());
        accessor.Context = new PiiMaskingBypassContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("Test")),
            RequestServices = new TestRequestServices(bypassEnabled: true),
        };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void CustomDbFlagProvider_BypassesWhenFlagSet()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var provider = new DbFlagProvider();
        var (accessor, transform) = CreateTransformer(settings, provider);
        accessor.Context = new PiiMaskingBypassContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("Test")),
            RequestServices = new TestRequestServices(bypassEnabled: true),
        };
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void Decision_IsMemoizedPerContext()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var provider = new CountingProvider(result: false);
        var (accessor, transform) = CreateTransformer(settings, provider);
        accessor.Context = ContextFor();
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        transform.Transform(prop, "John Doe");
        transform.Transform(prop, "John Doe");
        transform.Transform(prop, "John Doe");

        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public void JsonSerializer_BypassedUser_WritesPlainValue()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Roles = ["PiiViewer"] },
        };
        var accessor = new PiiMaskingBypassContextAccessor();
        var transform = new PiiMaskingPropertyStringTransform(
            new TestOptionsMonitor { CurrentValue = settings },
            bypassContextAccessor: accessor,
            bypassProviders: [new RoleBypassProvider(new TestOptionsMonitor { CurrentValue = settings })]);
        var options = new JsonSerializerOptions();
        options.AddPiiMaskingJsonModifier(
            new TestOptionsMonitor { CurrentValue = settings },
            transform);
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "PiiViewer"));

        var json = JsonSerializer.Serialize(new Dto { Name = "John Doe" }, options);

        Assert.Contains("\"John Doe\"", json);
    }

    [Fact]
    public void JsonSerializer_NonBypassedUser_WritesMaskedValue()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Roles = ["PiiViewer"] },
        };
        var accessor = new PiiMaskingBypassContextAccessor();
        var transform = new PiiMaskingPropertyStringTransform(
            new TestOptionsMonitor { CurrentValue = settings },
            bypassContextAccessor: accessor,
            bypassProviders: [new RoleBypassProvider(new TestOptionsMonitor { CurrentValue = settings })]);
        var options = new JsonSerializerOptions();
        options.AddPiiMaskingJsonModifier(
            new TestOptionsMonitor { CurrentValue = settings },
            transform);
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "RegularUser"));

        var json = JsonSerializer.Serialize(new Dto { Name = "John Doe" }, options);

        Assert.DoesNotContain("John", json);
    }

    [Fact]
    public void BypassNotConfigured_IsMasked()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var (accessor, transform) = CreateTransformer(settings);
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "PiiViewer"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void AddPiiMasking_RegistersBypassInfrastructure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PiiMasking:Enabled"] = "true",
                ["PiiMasking:Bypass:Roles:0"] = "PiiViewer",
                ["PiiMasking:Bypass:Claims:0:Type"] = "permission",
                ["PiiMasking:Bypass:Claims:0:Value"] = "pii:read",
            })
            .Build();
        var services = new ServiceCollection().AddPiiMasking(configuration);
        using var provider = services.BuildServiceProvider();

        var transform = provider.GetRequiredService<IPiiMaskedPropertyStringTransform>();
        var accessor = provider.GetRequiredService<PiiMaskingBypassContextAccessor>();
        accessor.Context = ContextFor(new Claim(ClaimTypes.Role, "PiiViewer"));
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }

    [Fact]
    public void DisabledMasking_ReturnsPlainValueRegardlessOfBypass()
    {
        var settings = new PiiMaskingSettings { Enabled = false };
        var (_, transform) = CreateTransformer(settings);
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));
    }
}
