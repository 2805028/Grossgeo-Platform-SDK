# Changelog

All notable changes to GrossGeo.SDK.Stub will be documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Samples + docs bumped to 2.1.6.** All 8 sample `.csproj` and every version reference in
  `README.md` / `samples/README.md` now target `2.1.6`.
- **New guide `docs/deep-links.md`** — how to open GrossGeo User Panel pages from a plugin
  (purchase / product / reviews / start-trial), the ProductId vs ProductKey vs UpgradeCode
  distinction, and the `RequestTrialAsync()` SDK method.

## [2.1.6] - 2026-07-21

### Added
- **`GrossGeoLicense.RequestTrialAsync()`** — opens the User Panel at your product and asks it to
  show the trial-activation confirmation, using the `ProductKey` you already have (no ProductId
  needed). Never activates a license silently. Companion `grossgeo://start-trial/{productId|productKey}`
  URI. See `docs/deep-links.md`.

### Security
- The published package is now **Authenticode-signed** (all DLLs) and the `.nupkg` is
  **author-signed** on NuGet.org (GROSSGEOTECH LLC).

## [2.1.5] - 2026-07-20

### Security
- **Live IPC response signatures are now verified.** The SDK cryptographically validates the
  RSA-SHA256 signature of live responses from the User Panel against a pinned central key (with
  anti-replay binding), not just the offline cache — closing a pipe-squatting vector where a
  local process could impersonate the panel and return a fake "valid" license.
- ⚠️ **Requires GrossGeo User Panel `1.0.2606.4007` or newer.** Older panels sign responses with a
  key the pinned SDK does not trust, so the license will not validate. End users only need to
  accept the panel auto-update.

### Changed
- `sdkVersion` is now reported over IPC (previously empty).

## [2.1.4] - 2026-06-29

### Changed
- **Samples build via NuGet.** All 8 sample `.csproj` reference the SDK as a `PackageReference`
  (`GrossGeo.Contracts` types ship embedded in the package — no separate Contracts reference).
- **Sample READMEs aligned with manifests + licensing model v3:** removed the obsolete `IsPublic`
  feature flag and `Period`/separate Monthly-Yearly plan rows; merged monthly/yearly pricing and
  `maintenanceYearlyPrice` as a plan attribute; feature-limit tables corrected to the four valid
  limit keys (`maxPerCall`, `maxPerDay`, `maxPerMonth`, `maxTotal`).

### Fixed
- **Invalid sample ProductKeys.** `TestProduct.Installer` (`GG-INST-TEST-0007`) and
  `TestProduct.PluginDll` (`GG-PDLL-TEST-0008`) used a legacy key shape rejected by the SDK
  format validator (`GG-XXXX-XXXX-XXXX-XXXX`), so `GrossGeoLicense.Initialize` failed with
  `Invalid ProductKey format`. Replaced with valid-format demo placeholders.

### Removed
- Test account credentials (emails + passwords) and internal back-end testing steps from
  `samples/README.md`.

### Note
- **Republished as a consistent, obfuscated build.** The package ships `GrossGeo.SDK.Stub` 2.1.4
  with the embedded `GrossGeo.Contracts` also at 2.1.4 (same strong-name token) — resolves a
  distribution mismatch where an obfuscated SDK.Stub 2.1.x was paired with a stale
  `GrossGeo.Contracts.dll` 1.0.0. No API changes vs 2.1.3 — reference 2.1.4 and let NuGet restore
  both DLLs from the single package (do not hand-copy Contracts).

## [2.1.3] - 2025-06-17

### Fixed
- **Concurrent sessions in multi-product processes.** Static `AcquireSession` /
  `ReleaseSession` / `SendSessionHeartbeat` were gated by the last `Initialize`; with several
  products in one AutoCAD process, acquiring a Concurrent session failed with
  `NOT_CONCURRENT_MODE`. Session methods are now available per-product on
  `ProductLicenseAccessor` (`ForProduct(key).AcquireSessionAsync(...)`); the static methods stay
  compatible by auto-resolving the single Concurrent product.

## [2.1.1] - 2025-06-16

### Fixed
- **CRIT-01 — obfuscation broke IPC deserialization.** The obfuscation step had
  `UseUnicodeNames=true`, which renamed types to invisible Unicode glyphs; `System.Text.Json`
  then failed with `Could not resolve type ' '` during IPC deserialization, so licensed
  products were seen as Free/Invalid. Disabled `UseUnicodeNames` and added explicit
  `[JsonPropertyName]` to all cached-license DTO fields (offline cache in the obfuscated build).
  This release replaces the broken obfuscated 2.1.0 on NuGet.

### Changed
- **Manifests:** all 8 sample products migrated from the single `product-manifest.json` to the two-manifest format — `plans-manifest.json` (plans + features carrying `planCodes`/`limits`) and `release-manifest.json` (one release per file), matching the platform `import-manifest/v2` schemas. `productKey`, `planFeatures`, `featureLimits`, `expectations` and `fixtureFile` are no longer part of the manifests.
- **README:** manifest section rewritten for the two-manifest format; Feature Guards / Feature Limits references corrected (`planFeatures` → feature `planCodes`, `featureLimits` → feature `limits`).

### Removed
- Stale `product-manifest.json` from all 8 samples (superseded by `plans-manifest.json` + `release-manifest.json`).
- Fixture release archives (`samples/**/fixtures/*.bundle.zip`, `*.installer.zip`) and `samples/fixtures-checksums.sha256` — sample release bundles are no longer committed to the repo (added to `.gitignore`).

## [2.1.0] - 2025-06-15

### Added
- `ProductLicenseAccessor` class for multi-plugin scenarios (multiple plugins in one AutoCAD process)
- `GrossGeoLicense.ForProduct(productKey)` returns `ProductLicenseAccessor` bound to a specific product
- `GrossGeoLicense.Shutdown(productKey)` shuts down a specific product instead of global shutdown
- Multi-plugin documentation section in README
- Migration table from static `GrossGeoLicense` to `ProductLicenseAccessor`

### Changed
- All 8 samples updated to use `ProductLicenseAccessor` pattern
- Quick Start guide updated with `ForProduct()` example

## [2.0.0] - 2025-06-01

### Added
- Usage tracking: `IncrementUsageAsync`, `GetCurrentUsageAsync` for MaxPerDay/MaxPerMonth limits
- `HasFeatureAsync` — async feature check with license refresh
- `RequireLimit` — limit guard with fallback callback
- `UsageResult` model with `CurrentUsage`, `Limit`, `Remaining`
- New limit types: `MaxPerDay`, `MaxPerMonth`
- Plan fields: `description`, `highlights[]`, `badge`, `isRecommended`, `maintenanceYearlyPrice`
- FAQ section in README
- "Key Concepts" section in README for developer onboarding
- TestProduct.Installer sample (DistributionType.Installer)
- TestProduct.PluginDll sample (DistributionType.PluginDll)

### Changed
- **BREAKING:** Licensing model v3 — one plan = one service level
  - `monthlyPrice` + `yearlyPrice` on a single plan (period chosen at purchase, not plan creation)
  - Maintenance modeled as `maintenanceYearlyPrice` attribute on Perpetual plans (not a separate plan)
- **BREAKING:** Removed `PlanTier.Maintenance` (10) — use `maintenanceYearlyPrice` on Perpetual plan
- **BREAKING:** Removed `BillingModel.Contract` (3) — Enterprise reserved
- **BREAKING:** Removed `SubscriptionPeriod.Quarterly` — not used
- All 8 samples: manifests updated to v3 format
- README completely rewritten with detailed developer documentation

### Removed
- `billingPeriod` field from plan manifests (period is now on order/subscription)
- `isPublic` field from feature manifests (unprotected code is public by design)
- `requiresPlanCode` field from plan manifests (Maintenance is an attribute, not a plan)
- `LoadPublicFeaturesAsync` — no longer needed (use `HasFeature` for default features)
- Separate Maintenance plans in samples (replaced by `maintenanceYearlyPrice`)
- Separate Monthly/Yearly plans in samples (merged into single plan with both prices)

## [1.0.0] - 2025-03-02

### Added
- Initial public release
- License validation via IPC (Named Pipe)
- Plan tiers: Free, Pro, ProPlus, Maintenance, Enterprise
- Billing models: Free, Subscription, Perpetual, Contract
- License modes: User, Machine, Concurrent
- Feature guards with fallback
- Feature limits
- Concurrent sessions with heartbeat
- Graceful degradation (DPAPI cache, 7-day grace period)
- Plugin update checks
- Multi-target: net48 (AutoCAD 2019-2024), net8.0-windows (AutoCAD 2025+)
