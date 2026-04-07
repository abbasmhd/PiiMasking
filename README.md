# PiiMasking

PiiMasking is a **personally identifiable information (PII) masking** library for .NET applications that serialize models with **System.Text.Json**. It applies **declarative, configuration-driven** transforms to selected `string` properties when **writing** JSON—partially obscuring values such as display names or email local parts—while **deserialization** continues to populate models with **unmodified** strings from incoming payloads.

The core package integrates via **`JsonTypeInfo` modifiers**, **`Microsoft.Extensions.Options`**, and optional **dependency injection** helpers for use in custom pipelines. **PiiMasking.AspNetCore** adds **ASP.NET Core MVC** integration by registering the same behavior on **`JsonOptions`**, so HTTP APIs can enable masking without hand-wiring converters per type.

## Purpose

APIs and logs often need to return or record **human-readable** text that still contains personally identifiable information (names, email local parts, free-text display values). Shipping that data unchanged increases privacy risk, compliance surface, and accidental exposure in downstream systems.

This library exists to **mask those string values at serialization time** in a **consistent, configurable way**: you mark properties with `[PiiMasking]`, turn masking on or off (and tune suffixes or literal-preserving separators) via configuration, and **incoming JSON is still deserialized as plain text** so your domain model and persistence stay unchanged. Masking applies when **writing** JSON (or when you call the same transform from your own pipelines).

It is not a full data-classification or DLP product; it is a **focused building block**—predictable string transforms, optional ASP.NET Core integration, and **extension points** (`IPiiMaskingPropertyContributor`, custom `IPiiMaskedPropertyStringTransform`) when built-in rules are not enough for your app.

## Packages

| Package | Description |
|--------|-------------|
| `PiiMasking` | Core: `PiiMaskingSettings`, built-in masking strategies, `[PiiMasking]`, JSON converter + modifier, DI helpers |
| `PiiMasking.AspNetCore` | `AddPiiMaskingMvcJson()` to register the modifier on MVC `JsonOptions` |

## Quick start (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddPiiMasking(builder.Configuration);
builder.Services.AddPiiMaskingMvcJson();
```

```json
{
  "PiiMasking": {
    "Enabled": true,
    "MaskSuffix": "****",
    "LiteralWordMaskSeparators": [ " on behalf of " ]
  }
}
```

Mark string properties:

```csharp
[PiiMasking(MaskEachWord = true)]
public string? DisplayName { get; set; }
```

## Extending masking (`IPiiMaskingPropertyContributor`)

When `PiiMasking:Enabled` is true, the default `PiiMaskingPropertyStringTransform` runs all registered **`IPiiMaskingPropertyContributor`** instances **in registration order**. The first non-null result is written; if every contributor returns null, built-in `[PiiMasking]` rules run (email, each word, literals, segment).

Register one or more contributors after `AddPiiMasking`:

```csharp
builder.Services.AddSingleton<IPiiMaskingPropertyContributor, DisplayNamePlainIdContributor>();
```

Example contributor (e.g. restore “plain user id in parentheses” in your app):

```csharp
public sealed class DisplayNamePlainIdContributor : IPiiMaskingPropertyContributor
{
    public string? TryMask(PropertyInfo property, string value, PiiMaskingAttribute marker, PiiMaskingSettings settings)
    {
        if (property.Name != nameof(UserDto.DisplayName))
            return null;

        // Your rules: use marker / settings.MaskSuffix / your own masking helpers, etc.
        return MyMasking.MaskDisplayNameWithPlainUserId(value, settings.MaskSuffix);
    }
}
```

**MVC JSON** (`AddPiiMaskingMvcJson`) resolves `IPiiMaskedPropertyStringTransform` from DI, so contributors apply automatically.

**Manual `JsonSerializerOptions`**: use the modifier overload that takes the transform:

```csharp
options.AddPiiMaskingJsonModifier(piiMaskingSettingsMonitor, propertyStringTransform);
```

The single-argument `AddPiiMaskingJsonModifier(options, monitor)` path uses **built-in rules only** (no contributors).

**Replacing the whole pipeline**: register your own `IPiiMaskedPropertyStringTransform` **before** `AddPiiMasking` so `TryAddSingleton` keeps yours (the default is not registered). If you register after `AddPiiMasking`, avoid duplicate `IPiiMaskedPropertyStringTransform` registrations for the same service provider.

## Samples (input → output)

Examples use the default mask suffix `****` (`PiiMaskingSettings.DefaultMaskSuffix`; override with `PiiMasking:MaskSuffix`).

### Built-in masking — segment and email

| Input | Output | Rule |
|--------|--------|------|
| `samson` | `Sa****` | Segment |
| `Jo` | `Jo****` | Segment |
| `a` | `a****` | Segment |
| `samson.user@mail.example.com` | `Sa****@mail.example.com` | Email (local part only) |
| `joe@app.mail.contoso.com:443` | `Jo****@app.mail.contoso.com:443` | Email |
| `ab@c.d` | `ab****@c.d` | Email |

If the value already contains the configured suffix (e.g. `Sa****`), it is returned trimmed and unchanged so you do not double-mask.

### Built-in masking — each word

| Input | Output | Rule |
|--------|--------|------|
| `Abe David Smith` | `Ab**** Da**** Sm****` | Each word |
| `John  Doe` (extra spaces) | `Jo**** Do****` | Each word |
| `Abe David (jdoe01)` | `Ab**** Da**** (j****` | Each word (parenthetical is one token) |

### Built-in masking — literals (e.g. `" on behalf of "`)

With `LiteralWordMaskSeparators` including ` on behalf of ` (as in the config sample above):

| Input | Output | Rule |
|--------|--------|------|
| `John Doe on behalf of Jane Smith` | `Jo**** Do**** on behalf of Ja**** Sm****` | Literals + each word |
| `John Doe (jodo01) on behalf of Jane Smith (jasm02)` | `Jo**** Do**** (j**** on behalf of Ja**** Sm**** (j****` | Literals + each word |

The literal text is matched case-insensitively but copied from the source (e.g. `ON BEHALF OF` stays as-is in the output).

With `[PiiMasking(MaskEachWordRespectingLiterals = true, LeaveRemainderUnmaskedAfterLiterals = true)]` and a separator like ` on `, text after the **last** matched literal is left plain (see `MaskEachWordRespectingLiterals(..., leaveRemainderUnmasked: true)`).

### JSON serialization (masking enabled)

With `[PiiMasking]` on properties, `PiiMasking:Enabled` true, and the library wired into your JSON pipeline (e.g. `AddPiiMasking` + `AddPiiMaskingMvcJson()`), a type like:

```csharp
public sealed class UserDto
{
    [PiiMasking(MaskEachWord = true)]
    public string? DisplayName { get; set; }

    [PiiMasking(AsEmail = true)]
    public string? Email { get; set; }
}
```

might serialize from plain in-memory values to JSON similar to:

| Field (in memory) | JSON value (output) |
|-------------------|---------------------|
| `DisplayName` = `"Abe David (jdoe01)"` | `"Ab**** Da**** (j****"` |
| `Email` = `"samson@contoso.com"` | `"Sa****@contoso.com"` |

Deserialization still receives the plain strings from incoming JSON; masking applies when **writing** JSON.

## Custom JSON pipelines (e.g. custom converters)

`AddPiiMasking` registers `IPiiMaskedPropertyStringTransform` (`PiiMaskingPropertyStringTransform` by default). Call `Transform(property, value)` when writing strings so masking matches `[PiiMasking]` rules. Replace the singleton registration if you need a composite implementation.

## Build & pack

```bash
dotnet pack -c Release -o ./artifacts
```

## GitHub Actions (`.github/workflows/dotnet.yml`)

The **.NET** workflow restores, builds, runs tests, and packs the solution. It installs .NET **8.0.x** and **10.0.x** so multi-targeted projects build on the runner.

| Trigger | What runs |
|--------|-----------|
| **Push** or **pull request** to `main` or `master` | Restore → build → test → pack (uses `Version` / `PackageVersion` from each `.csproj`). Nothing is published to NuGet. |
| **Release** → **Published** | Same validation, then pack with **`Version` and `PackageVersion` set from the release tag** (a leading `v` is stripped, e.g. `v1.2.3` → `1.2.3`). Pushes all `*.nupkg` and `*.snupkg` from `./artifacts` to **nuget.org** (`--skip-duplicate`). Uploaded workflow artifacts include the packages for that run. |
| **workflow_dispatch** (Actions → run workflow manually) | Same as push/PR: build, test, pack only; no NuGet publish. |

### Publish to NuGet from CI

1. In the GitHub repo, open **Settings** → **Secrets and variables** → **Actions** and add a secret named **`NUGET_API_KEY`** with a [NuGet API key](https://www.nuget.org/account/apikeys) that has permission to **push** the package IDs `PiiMasking` and `PiiMasking.AspNetCore`.
2. Create a [GitHub Release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository) and **publish** it. Use a tag that matches the version you want on NuGet (e.g. `1.0.1` or `v1.0.1`). The workflow maps that tag to `PackageVersion` for both packable projects.
3. After the workflow succeeds, confirm the versions on [nuget.org](https://www.nuget.org/).

If **`NUGET_API_KEY`** is missing when a release is published, the publish step fails; fix the secret and re-run the failed job or create a new release as needed.

## Tests

```bash
dotnet test
```

## License

MIT (adjust as required by your organisation).
