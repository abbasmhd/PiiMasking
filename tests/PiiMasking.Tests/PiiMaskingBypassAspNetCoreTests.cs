using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PiiMasking;
using PiiMasking.AspNetCore;
using PiiMasking.Bypass;
using Xunit;

namespace PiiMasking.Tests;

public class PiiMaskingBypassAspNetCoreTests
{
    private sealed class Dto
    {
        [PiiMasking(MaskEachWord = true)]
        public string Name { get; set; } = "";
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<PiiMaskingSettings>
    {
        public required PiiMaskingSettings CurrentValue { get; set; }

        public PiiMaskingSettings Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<PiiMaskingSettings, string?> listener) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAuthorizationService(Func<string, AuthorizationResult> policyEvaluator) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
            Task.FromResult(policyEvaluator(policyName));

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
            throw new NotSupportedException();
    }

    private static async Task<PiiMaskingBypassContext?> InvokeMiddlewareAsync(
        PiiMaskingSettings settings,
        IAuthorizationService? authorizationService,
        PiiMaskingBypassContextAccessor accessor)
    {
        PiiMaskingBypassContext? captured = null;
        var middleware = new PiiMaskingBypassMiddleware(
            innerContext =>
            {
                captured = accessor.Context;
                return Task.CompletedTask;
            },
            new TestOptionsMonitor { CurrentValue = settings },
            accessor);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity("Test"));
        await middleware.InvokeAsync(httpContext, authorizationService);
        return captured;
    }

    [Fact]
    public async Task Policy_Succeeds_BypassesMasking()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Policy = "CanViewRawPii" },
        };
        var accessor = new PiiMaskingBypassContextAccessor();
        var service = new FakeAuthorizationService(_ => AuthorizationResult.Success());

        var captured = await InvokeMiddlewareAsync(settings, service, accessor);

        Assert.True(captured?.PolicyEvaluationSucceeded);
        Assert.Null(accessor.Context);
    }

    [Fact]
    public async Task Policy_Fails_KeepsMasking()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Policy = "CanViewRawPii" },
        };
        var accessor = new PiiMaskingBypassContextAccessor();
        var service = new FakeAuthorizationService(_ => AuthorizationResult.Failed());

        var captured = await InvokeMiddlewareAsync(settings, service, accessor);

        Assert.False(captured?.PolicyEvaluationSucceeded);
    }

    [Fact]
    public async Task Policy_EvaluationThrows_KeepsMasking()
    {
        var settings = new PiiMaskingSettings
        {
            Enabled = true,
            Bypass = new PiiMaskingBypassSettings { Policy = "CanViewRawPii" },
        };
        var accessor = new PiiMaskingBypassContextAccessor();
        var service = new FakeAuthorizationService(_ => throw new InvalidOperationException("policy exploded"));

        var captured = await InvokeMiddlewareAsync(settings, service, accessor);

        Assert.False(captured?.PolicyEvaluationSucceeded);
    }

    [Fact]
    public async Task NoPolicyConfigured_PolicyNotEvaluated()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var accessor = new PiiMaskingBypassContextAccessor();

        var captured = await InvokeMiddlewareAsync(settings, authorizationService: null, accessor);

        Assert.NotNull(captured);
        Assert.Null(captured.PolicyEvaluationSucceeded);
        Assert.NotNull(captured.User);
    }

    [Fact]
    public async Task Context_IsClearedAfterPipeline()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var accessor = new PiiMaskingBypassContextAccessor();

        var captured = await InvokeMiddlewareAsync(settings, null, accessor);

        Assert.NotNull(captured);
        Assert.Null(accessor.Context);
    }

    [Fact]
    public async Task PolicyBypassProvider_PolicySucceeded_Bypasses()
    {
        var settings = new PiiMaskingSettings { Enabled = true };
        var accessor = new PiiMaskingBypassContextAccessor();
        var transform = new PiiMaskingPropertyStringTransform(
            new TestOptionsMonitor { CurrentValue = settings },
            bypassContextAccessor: accessor,
            bypassProviders: [new PolicyBypassProvider()]);
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        accessor.Context = new PiiMaskingBypassContext { PolicyEvaluationSucceeded = true };
        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));

        accessor.Context = new PiiMaskingBypassContext { PolicyEvaluationSucceeded = false };
        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));

        await Task.CompletedTask;
    }

    [Fact]
    public void AddPiiMaskingBypass_RegistersPolicyProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PiiMasking:Enabled"] = "true",
                ["PiiMasking:Bypass:Policy"] = "CanViewRawPii",
            })
            .Build();
        var services = new ServiceCollection()
            .AddPiiMasking(configuration)
            .AddPiiMaskingBypass();
        using var provider = services.BuildServiceProvider();

        var providers = provider.GetRequiredService<IEnumerable<IPiiMaskingBypassProvider>>().ToList();
        var accessor = provider.GetRequiredService<PiiMaskingBypassContextAccessor>();
        var transform = provider.GetRequiredService<IPiiMaskedPropertyStringTransform>();
        var prop = typeof(Dto).GetProperty(nameof(Dto.Name))!;

        Assert.Contains(providers, p => p is PolicyBypassProvider);

        accessor.Context = new PiiMaskingBypassContext { PolicyEvaluationSucceeded = true };
        Assert.Equal("John Doe", transform.Transform(prop, "John Doe"));

        accessor.Context = new PiiMaskingBypassContext { PolicyEvaluationSucceeded = false, User = null };
        Assert.NotEqual("John Doe", transform.Transform(prop, "John Doe"));
    }
}
