# Changelog

All notable changes to GrossGeo.SDK.Stub will be documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
