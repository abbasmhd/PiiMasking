# Changelog

All notable changes to this project will be documented in this file.

## [0.0.1] - 2026-04-09

Initial public release of `PiiMasking`.

### Added

- Core masking library published as `PiiMasking.Core`
- ASP.NET Core integration package published as `PiiMasking.AspNetCore`
- Attribute-driven masking for `System.Text.Json` serialization via `[PiiMasking]`
- Built-in masking modes for segment masking, email masking, per-word masking, and literal-aware masking
- ASP.NET Core MVC registration with `AddPiiMaskingMvcJson()`
- Extensibility through `IPiiMaskingPropertyContributor`
- Named masking modes through `IPiiMaskingExecutionStrategy` and `PiiMaskingAttribute.Mode`
- Shared text formatting helper via `PiiMaskingTextFormatter`
- CI workflow to build, test, pack, and publish NuGet packages

### Changed

- Refined the project README for clearer installation, usage, extensibility, and publishing guidance
- Extracted shared masking operations to reduce duplication across built-in strategies
- Improved execution strategy error messages to list available registered strategy names
- Added validation for masking settings and de-duplication for literal separators

### Packaging

- Set the initial package version to `0.0.1`
- Added NuGet repository metadata and tags for improved package discoverability
