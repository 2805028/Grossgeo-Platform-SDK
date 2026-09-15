# Changelog

All notable changes to GrossGeo.SDK.Stub will be documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.17] - 2026-09-15

### Added
- **`ProductLicenseAccessor.WaitUntilReady(timeout)` / `WhenReadyAsync(timeout, ct)`.** If your
  product family runs more than one GrossGeo product in the same AutoCAD process, the static
  `GrossGeoLicense.WaitUntilReady`/`WhenReadyAsync` answer as soon as *any* initialized product is
  ready — not necessarily the one whose command is running. `ForProduct(productKey)` now returns
  an accessor with its own wait, scoped to that product alone. **The static methods are
  unchanged** — this is an addition, not a behaviour change — they now carry a doc warning
  pointing at the per-product replacement instead of leaving the ambiguity undocumented.

### Note
- Nothing on the wire, in the cache file, or in the fingerprint changes. A product built against
  2.1.16 keeps working exactly as before and gains nothing until it is rebuilt against this
  version and adopts the per-product wait where it runs more than one product.

## [2.1.16] - 2026-09-07

### Fixed
- **The offline soft landing promised for 2.1.15 was not actually in that package.** The
  published `2.1.15` was built before the code implementing it landed in the source tree (by
  about six and a half hours), and the version number was never raised in between — the tree and
  the published package carried the same number while their contents differed. This release
  corrects that: the soft landing described below is now actually in the package you download.

### Added
- **Offline soft landing.** Until now, when the paid horizon of a signed verdict expired while
  you were offline, your product was told the licence had expired and stopped. It now falls back
  to the free tier for as long as the fallback horizon in the same signed envelope is alive: the
  verdict carries the free plan tier and the free feature set instead of a refusal. The platform
  has been signing the three fallback fields since 24.08; this is the first release that reads
  them, on both targets, including the hand-written parser used on .NET Framework.

### Changed
- **The dormant multi-key validation path is now marked dormant by decision**, not by accident,
  so a reader does not mistake it for a working feature. No behaviour of yours depends on it.

### Note
- **Nothing is removed or renamed on the public surface**, and no wire format, fingerprint or
  cache file layout changes. A product built against 2.1.15 keeps working; rebuilding is what
  delivers the soft landing.

## [2.1.15] - 2026-09-06

### Fixed
- **Usage was counted against the wrong product.** `ProductLicenseAccessor.IncrementUsageAsync`
  and `GetCurrentUsageAsync` did not pass their own accessor's product key and fell back to the
  key of the last `Initialize` call. In a host that runs several GrossGeo products — AutoCAD is
  exactly that — a paid per-product limit stopped applying **silently**: the other plan has no
  limit under that code, the platform answers "allowed, unlimited" and records nothing, so
  `GetCurrentUsageAsync` kept returning zero and nothing appeared in any log. **Required for any
  product that uses per-product usage limits via `GrossGeoLicense.ForProduct`.**

### Changed
- **A failed signing-key rotation is no longer indistinguishable from losing the network.** It
  now says the platform sent a key this build of your product does not trust and that the
  product must be updated, instead of degrading into the offline cache and then, once the cache
  expires, into a plain refusal. **The list of trusted signing keys is unchanged** — products
  built on 2.1.14 trust exactly the same keys, and nothing in the field starts refusing because
  of this release.

### Added
- **`PlanTier.Unknown`, `BillingModel.Unknown` and `LicenseMode.Unknown` (all 255).** Until now,
  when no verdict was available your product silently received `Free`, `Free` and `Machine`: the
  absence of an answer looked exactly like an answer, and `Machine` is the most expensive of the
  three modes by consequence. It now receives `Unknown`. Existing members keep their numbers and
  nothing is removed — but **if you branch on any of these three enums, check that you have a
  `default` arm**: code that previously always landed somewhere will now land nowhere.

## [2.1.14] - 2026-09-03

### Changed
- **A withdrawn licence now stops on time — once you rebuild.** Until this release a refusal
  grounded in a non-operational licence state (revoked, suspended, expired, blocked) arrived
  carrying no error code at all. A refusal without a code is read by the SDK as a transient
  failure, so it fell back to the last positive verdict in the signed offline cache and went on
  serving it for as long as that cache remained valid — up to seven days. The refusal is now
  named (`LICENSE_NOT_ACTIVE`) and treated as authoritative. **The set of codes treated as an
  authoritative denial is compiled into the SDK inside your product**: updating the User Panel on
  the end user's machine does not change this, because the decision is taken inside your own
  assembly.
- **Two `Status` values change.** `Check()` called before any check completed returned
  `NotFound` with a message telling the user to activate a product they had already activated;
  it now returns `Unknown` with `ErrorCode = NOT_CHECKED`. Failure to verify the signature of the
  offline cache returned `Blocked`; it now returns `Unknown` with `SIGNATURE_INVALID`, through a
  new factory `LicenseResult.CouldNotVerify`. `IsValid` stays `false` in both, so access is not
  widened — what changes is truthfulness.
- **An `ErrorCode` that used to arrive empty now arrives with a value.** Six places substituted a
  fallback only when the code was `null`, while the panel sends an empty string — so the
  substitution never fired. Those places now yield `UNKNOWN`. **If you compare an error code
  against the empty string, that comparison will stop matching; compare against the value.**
- **A status number this SDK does not recognise now maps to `Unknown`** instead of to whichever
  name happened to carry that number.
- **The SDK-side rate limiter reports `LOCAL_RATE_LIMITED`** instead of `RATE_LIMITED`: the
  platform uses `RATE_LIMIT_EXCEEDED` for "we asked and were refused", while this one means "we
  did not ask at all", and the two were one keystroke apart.

### Added
- **Four factories that previously left `ErrorCode` null now carry one**: `NotFound` →
  `LICENSE_NOT_FOUND`, `Expired` → `LICENSE_EXPIRED`, `NoAvailableSeats` → `NO_SEATS`,
  `NoAssignment` → `LICENSE_NOT_ASSIGNED`. `Success` and `GracePeriod` deliberately never carry
  a code: both are valid states.
- **Three cache-mismatch causes are now separate**, because the person in front of the screen has
  to do something different in each: `CACHE_EXPIRED` (the stored copy aged out),
  `CACHE_CLOCK_MISMATCH` (the machine clock is wrong — fixable in ten seconds),
  `CACHE_BINDING_MISMATCH` (the stored licence belongs to another machine or another Windows
  account — not fixable by the user).
- **`Protect()` overloads that hand the refusal reason to the fallback** instead of nothing.
- **`GrossGeo.Contracts.Licensing.IpcErrorCodes`** — the shared dictionary of error codes is now
  a public type, in the `GrossGeo.Contracts` assembly that has always shipped inside this
  package. It adds a type and removes nothing.

### Note
- **Nothing in this release changes the wire format, the fingerprint, or the cache file layout**,
  so signed envelopes and existing cache files keep working, and no product needs rebuilding to
  keep functioning. Rebuilding is what delivers the new codes — and what makes a withdrawn
  licence stop on time.

## [2.1.13] - 2026-08-29

### Fixed
- **On .NET Framework the SDK still did not reach the User Panel** — 2.1.12 fixed only half of
  it. The package still depended on `System.Text.Json`, which on .NET Framework arrives with
  eight companion assemblies whose versions are reconciled by binding redirects in the
  application's configuration file. A plugin is a library loaded into someone else's process: the
  configuration in force is the host's, and it is not ours to write. In AutoCAD the result was a
  `TypeInitializationException` on `System.Text.Json.JsonSerializer`. This release removes the
  dependency instead of arguing with it: on `net48` the SDK now serialises and parses the IPC
  protocol with code of its own, and its assembly references are down to `mscorlib`, `System`,
  `System.Core`, `System.Management` and `GrossGeo.Contracts`. The wire format is unchanged to
  the character, verified against a reference captured from the running panel. On
  `net8.0-windows` `System.Text.Json` is retained. **Required for AutoCAD 2019–2024.**

## [2.1.12] - 2026-08-28

### Fixed
- **On .NET Framework the SDK did not initialise at all, and left no trace.** While hiding string
  literals, the obfuscation step emitted a decryptor referencing `System.Private.CoreLib` — the
  core library of the runtime the obfuscation tool itself runs on, which does not exist on .NET
  Framework 4.8. The first touch of the SDK therefore threw a `TypeInitializationException`
  before anything ran, including before the SDK could create its own diagnostic log: a product on
  AutoCAD 2019–2024 simply never received a verdict and nothing anywhere said why. Products
  targeting AutoCAD 2025 and later were unaffected. **Partial fix — superseded by 2.1.13.**

## [2.1.11] - 2026-08-20

### Fixed
- **The offline relaxation shipped in 2.1.9 never fired, because it asked a question that cannot
  be answered.** It tested whether the only difference between the stored and the current machine
  fingerprint was the volatile component; the fingerprint is a hash, so its components cannot be
  recovered from it — and disabling the network does not remove the MAC address at all: the SDK
  picks the fastest working non-loopback interface, so a virtual adapter (Hyper-V, VMware,
  VirtualBox, Bluetooth PAN, WSL) takes over and the MAC becomes *different* rather than absent.
  The question is now the one that can be answered: is the machine identity measured, and does it
  match exactly? Identity is four stable components — CPU, motherboard, machine GUID, volume
  serial — and all four must match, with no scoring or threshold. **Required; supersedes 2.1.10.**

### Changed
- As a consequence the relaxation now applies to **any** MAC change, not only its disappearance:
  docking, a VPN coming up, a replaced network card, a change in adapter order. That is
  deliberate — the MAC is not a property of the machine, it is a property of whichever interface
  happens to be fastest at that moment.

## [2.1.10] - 2026-08-20

*Publication date not established: taken from the day the fix landed. The version-history
record in the package places this release on the same day as 2.1.8.*

### Fixed
- **The offline licence cache was being written empty in obfuscated builds.** The cache record
  type carried no explicit JSON names and was not excluded from renaming, so obfuscation renamed
  its properties and the record round-tripped to almost nothing — measured at 230 bytes against
  1830 for the same licence written by a non-obfuscated build. Nothing reported an error: the
  file was created, encrypted, decrypted and parsed successfully; it simply held no data. Every
  offline mechanism that reads it therefore had nothing to lean on, including the relaxation
  shipped in 2.1.9. **Required for offline to work at all; supersedes 2.1.9.**

### Added
- Three independent protections for that record: explicit JSON names on every field, exclusion of
  the type from renaming, and a **round-trip self-check at write time** which reads the file back,
  compares the payload length with what was stored, and logs `CACHE ROUND-TRIP BROKEN` with the
  three numbers on any mismatch. The third holds even if the first two are defeated by a future
  change — which is the point: this defect failed silently for months.

## [2.1.9] - 2026-08-20

*Publication date not established: taken from the day the fix landed. The version-history
record in the package places this release on the same day as 2.1.8.*

### Fixed
- **Going offline no longer invalidates the offline licence.** The machine fingerprint included
  the MAC address, which is only readable while a network adapter is up; disabling the network
  turned it into `UNKNOWN_MAC`, the fingerprint changed, and an exact comparison then rejected a
  genuinely signed, valid licence. The fallback to the on-disk cache could not save it either,
  because the cache encryption key was derived from the same fingerprint. A product taken offline
  therefore lost its licence at the moment it went offline, rather than at the end of the period
  the platform had signed for. **Important for any product that must work offline.**

### Added
- **A separate stable fingerprint** (CPU, motherboard, machine GUID, volume serial — no MAC) from
  which the cache key is derived. **The wire fingerprint sent to and signed by the platform is
  unchanged**, so existing signed envelopes keep verifying.
- Rejections now name the product they concern, so a diagnostic log can be attributed.

## [2.1.8] - 2026-08-20

### Added
- **`LicenseResult.Unavailable(...)`** — "could not ask" is now a distinct outcome from
  "no licence". A silent channel, a User Panel that is not running, or the SDK's own rate
  limiter no longer reach your product as `Blocked`. `IsValid` is still `false` in both
  cases — the product must not run — but the reason differs, and you can act on it: do not
  disable features permanently, and do not prompt the user to buy a licence they already own.

### Changed
- **The cached verdict has its own per-product store, keyed by ProductKey.** Offline operation
  no longer depends on an active lease and now survives a restart. Free products previously
  lost their cached verdict on restart; they no longer do.
- **The on-disk cache record is merged instead of being replaced** by whichever writer touched
  it last. Lease renewal used to erase the ProductKey and the offline expiry, which broke
  offline degradation from the very first renewal.
- **A cached status is raised only on proof**, never by default.
- **The panel's answer budget is derived from the channel timeout** rather than being an
  independent number, so the panel cannot spend longer producing an answer than the product
  is willing to wait for one.
- **Half-connectivity (captive portal) is read as absence of network**, not as a server denial.
- Samples and docs target `2.1.8`.

## [2.1.7] - 2026-08-13

### Changed
- **Offline grace lasts the full period the platform signed** (`cacheValidUntil`) instead of
  being cut to 24 hours by the live-path replay window.
- **A failed signature or integrity check no longer deletes the cached licence** — a single
  transient failure can no longer destroy the only offline artefact you have.

### Added
- A second trusted central-key thumbprint, so replacing the signing key no longer requires
  every user to reinstall.
- The plugin version is reported to the platform on licence validation (diagnostics only —
  no version gating).

## [2.1.6] - 2026-07-21

### Added
- **Guide `docs/deep-links.md`** — how to open GrossGeo User Panel pages from a plugin
  (purchase / product / reviews / start-trial), the ProductId vs ProductKey vs UpgradeCode
  distinction, and the `RequestTrialAsync()` SDK method.

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
