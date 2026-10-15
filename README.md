# PiiMasking

`PiiMasking` is a .NET library for masking personally identifiable information in JSON output. It plugs into `System.Text.Json`, supports `Microsoft.Extensions.Options`, and optionally integrates with ASP.NET Core MVC so you can annotate selected `string` properties and mask them consistently during serialization.

The library is designed for outbound data shaping. Your in-memory models stay unchanged, and incoming JSON is still deserialized as plain text.

## Why use it

- Apply masking declaratively with `[PiiMasking]`
- Keep masking behavior configuration-driven
- Support ASP.NET Core MVC without per-model converter wiring
- Extend the pipeline with contributors, named strategies, or a custom transform
- Bypass masking for selected users by role, claim, authorization policy, or a custom per-user flag
- Preserve normal deserialization behavior for incoming payloads

## Packages

| Package | Purpose |
| --- | --- |
| [`PiiMasking.Core`](https://www.nuget.org/packages/PiiMasking.Core) | Core library: settings, built-in masking rules, `[PiiMasking]`, JSON modifier/converter, and DI registration |
| [`PiiMasking.AspNetCore`](https://www.nuget.org/packages/PiiMasking.AspNetCore) | ASP.NET Core MVC integration via `AddPiiMaskingMvcJson()` |

Supported target frameworks:

- `net8.0`
- `net10.0`

## Installation

Install the core package:

```bash
dotnet add package PiiMasking.Core
```

If you want ASP.NET Core MVC integration, add the companion package as well:

```bash
dotnet add package PiiMasking.AspNetCore
```

## Quick start

### ASP.NET Core

Register the services:

```csharp
builder.Services.AddPiiMasking(builder.Configuration);
builder.Services.AddPiiMaskingMvcJson();
```

Configure the library:

```json
{
  "PiiMasking": {
    "Enabled": true,
    "MaskSuffix": "****",
    "LiteralWordMaskSeparators": [" on behalf of "]
  }
}
```

Annotate the properties you want masked:

```csharp
public sealed class UserDto
{
    [PiiMasking(MaskEachWord = true)]
    public string? DisplayName { get; set; }

    [PiiMasking(AsEmail = true)]
    public string? Email { get; set; }
}
```

With masking enabled, values such as these:

```text
DisplayName = "Abe David"
Email = "samson@contoso.com"
```

serialize to JSON like this:

```json
{
  "displayName": "Ab**** Da****",
  "email": "Sa****@contoso.com"
}
```

## How it works

1. Add `[PiiMasking]` to a `string` property.
2. Configure `PiiMaskingSettings` through `IConfiguration`.
3. When `PiiMasking:Enabled` is `true`, outbound JSON is masked during serialization.
4. Deserialization still reads incoming JSON as plain text.

By default, `[PiiMasking]` applies segment-style masking. You can opt into other behaviors such as email masking, per-word masking, or named strategies.

## Configuration

| Setting | Description | Default |
| --- | --- | --- |
| `Enabled` | Turns masking on or off | `true` when omitted |
| `MaskSuffix` | Suffix appended to masked values | `****` |
| `LiteralWordMaskSeparators` | Separators used by literal-aware masking | Empty |
| `Bypass` | Sources (roles, claims, policy) that skip masking | Not set (always mask) |

## Built-in masking modes

| Attribute usage | Example input | Example output |
| --- | --- | --- |
| `[PiiMasking]` | `samson` | `Sa****` |
| `[PiiMasking(AsEmail = true)]` | `samson.user@mail.example.com` | `Sa****@mail.example.com` |
| `[PiiMasking(MaskEachWord = true)]` | `Abe David Smith` | `Ab**** Da**** Sm****` |
| `[PiiMasking(MaskEachWordRespectingLiterals = true)]` | `John Doe on behalf of Jane Smith` | `Jo**** Do**** on behalf of Ja**** Sm****` |

Notes:

- Email masking only masks the local part before `@`.
- Literal-aware masking uses `PiiMasking:LiteralWordMaskSeparators` and preserves the matched literal text from the source.
- If a value already includes the configured mask suffix, the built-in logic avoids double-masking it.
- `LeaveRemainderUnmaskedAfterLiterals` can be combined with `MaskEachWordRespectingLiterals` when you want text after the last literal separator to remain unchanged.

## Bypassing masking for selected users

Masking is skipped entirely for the current user when any bypass source matches. All sources are opt-in, and the decision is made once per request. A failing bypass source never leaks PII: masking stays applied (fail-closed).

Configure built-in sources under `PiiMasking:Bypass`:

```json
{
  "PiiMasking": {
    "Enabled": true,
    "Bypass": {
      "Roles": ["PiiViewer"],
      "Claims": [{ "Type": "permission", "Value": "pii:read" }],
      "Policy": "CanViewRawPii"
    }
  }
}
```

- `Roles`: users in any listed role (via `ClaimsPrincipal.IsInRole`) see plain values.
- `Claims`: users holding any listed claim (empty `Value` matches any value of the type) see plain values.
- `Policy`: an ASP.NET Core authorization policy; users it passes see plain values.

For the policy source, enable the bypass middleware in the ASP.NET Core package:

```csharp
builder.Services.AddPiiMasking(builder.Configuration);
builder.Services.AddPiiMaskingMvcJson();
builder.Services.AddPiiMaskingBypass();
```

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UsePiiMaskingBypass(); // after UseAuthentication/UseAuthorization
```

For any other source — such as a per-user flag in the database — implement `IPiiMaskingBypassProvider` and resolve the decision from the request-scoped services exposed on the bypass context:

```csharp
public sealed class DbFlagBypassProvider : IPiiMaskingBypassProvider
{
    public bool ShouldBypass(PiiMaskingBypassContext context)
    {
        if (context.RequestServices?.GetService<UserMaskingSettings>() is not { } userSettings)
        {
            return false;
        }

        return userSettings.MaskingDisabledForCurrentUser;
    }
}
```

```csharp
builder.Services.AddSingleton<IPiiMaskingBypassProvider, DbFlagBypassProvider>();
```

Keep database-backed providers cheap: the bypass decision is evaluated at most once per request and then cached, so a scoped lookup in `ShouldBypass` (e.g. preload user settings in a service) runs once, not per serialized property.

## Extensibility

### `IPiiMaskingPropertyContributor`

Use contributors when masking needs to depend on application-specific rules, such as a particular property name or DTO shape.

The default transform evaluates registered contributors in registration order. The first non-null result wins. If every contributor returns `null`, the built-in rules run.

```csharp
builder.Services.AddSingleton<IPiiMaskingPropertyContributor, DisplayNamePlainIdContributor>();
```

```csharp
public sealed class DisplayNamePlainIdContributor : IPiiMaskingPropertyContributor
{
    public string? TryMask(PropertyInfo property, string value, PiiMaskingAttribute marker, PiiMaskingSettings settings)
    {
        if (property.Name != nameof(UserDto.DisplayName))
        {
            return null;
        }

        return MyMasking.MaskDisplayNameWithPlainUserId(value, settings.MaskSuffix);
    }
}
```

### `IPiiMaskingExecutionStrategy`

Use a named strategy when you want to select a masking rule declaratively through the attribute instead of encoding everything in booleans.

Execution order in the default pipeline is:

1. `IPiiMaskingPropertyContributor`
2. `IPiiMaskingExecutionStrategy` selected by `Mode`
3. Built-in `[PiiMasking]` rules

If `Mode` is set and no registered strategy has a matching `Name`, serialization fails fast with an `InvalidOperationException`.

```csharp
public sealed class RedactPhoneStrategy : IPiiMaskingExecutionStrategy
{
    public const string Name = "Phone";

    string IPiiMaskingExecutionStrategy.Name => Name;

    public string? Mask(string value, PiiMaskingAttribute marker, PiiMaskingSettings settings)
    {
        return "****";
    }
}
```

Register and use it like this:

```csharp
builder.Services.AddPiiMasking(builder.Configuration);
builder.Services.AddSingleton<IPiiMaskingExecutionStrategy, RedactPhoneStrategy>();
builder.Services.AddPiiMaskingMvcJson();
```

```csharp
[PiiMasking(Mode = RedactPhoneStrategy.Name)]
public string? Mobile { get; set; }
```

### `IPiiMaskedPropertyStringTransform`

`AddPiiMasking` registers `IPiiMaskedPropertyStringTransform` and uses `PiiMaskingPropertyStringTransform` by default. If you have a custom serializer or converter pipeline, call `Transform(property, value)` so your output stays aligned with `[PiiMasking]`.

If you want to replace the default behavior, register your own `IPiiMaskedPropertyStringTransform` before calling `AddPiiMasking`.

### `PiiMaskingTextFormatter`

`PiiMaskingTextFormatter.Apply(...)` is a lightweight helper for applying the built-in rules directly. It does not resolve contributors or named execution strategies.

## Manual `JsonSerializerOptions`

For non-MVC scenarios, register the JSON modifier directly.

Preferred overload when using DI:

```csharp
var strategies = serviceProvider.GetServices<IPiiMaskingExecutionStrategy>().ToList();
var transform = serviceProvider.GetRequiredService<IPiiMaskedPropertyStringTransform>();

options.AddPiiMaskingJsonModifier(piiMaskingSettingsMonitor, transform, strategies);
```

There is also a built-in-only overload:

```csharp
options.AddPiiMaskingJsonModifier(piiMaskingSettingsMonitor);
```

Use the overload that accepts `IPiiMaskedPropertyStringTransform` when you want contributors to participate.

## Development

Run the test suite:

```bash
dotnet test
```

Create NuGet packages locally:

```bash
dotnet pack -c Release -o ./artifacts
```

## CI and publishing

The `.github/workflows/dotnet.yml` workflow:

- Restores, builds, tests, and packs on pushes and pull requests
- Builds and packs on manual runs
- Publishes packages to NuGet on release publication

To publish from CI:

1. Add a `NUGET_API_KEY` repository secret with permission to push `PiiMasking.Core` and `PiiMasking.AspNetCore`.
2. Publish a GitHub release with the target version tag, such as `1.0.1` or `v1.0.1`.
3. Confirm the published packages on [nuget.org](https://www.nuget.org/).

The release workflow maps the release tag to `Version` and `PackageVersion`, stripping a leading `v` when present.

## License

Licensed under the MIT License.
