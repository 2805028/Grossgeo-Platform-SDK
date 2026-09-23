# Changelog

All notable changes to GrossGeo.SDK.Stub will be documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.2] - 2026-09-23

### Changed
- **Docs only, no code changes.** README and the developer guide said a `net8.0-windows` build
  "loads and runs" in AutoCAD 2027 / Civil 3D 2027 and left declaring that support up to the
  reader after their own testing (`LGC-1333`). That measured whether the build loads, not
  whether it is binary-compatible. Per Autodesk, a `net8.0-windows` build is NOT binary-compatible
  with AutoCAD 2027 itself, even where it does load — full recompile required. This is specific to
  AutoCAD 2027, not to running on a .NET 10 host in general: `net8.0-windows` builds load and run
  normally on AutoCAD 2026.1.2 and 2025 U1.4, which also host .NET 10. AutoCAD 2027 requires a
  build targeting `net10.0-windows` (already supported since `2.2.0`); `net8.0-windows` is not
  supported for it.

## [2.2.1] - 2026-09-23

### Fixed
- **`SessionExpired` reported a fixed `HEARTBEAT_FAILED` code/text after repeated heartbeat
  failures, regardless of the real reason the gateway refused the heartbeat (`LGC-1316`).**
  `SendSessionHeartbeatForProductAsync` read the response's `ErrorCode`/`ErrorMessage` only for
  its own diagnostic log and discarded them before raising `SessionExpired` — a revoked licence,
  a normally-ended session, an internal panel error and a lost connection all surfaced to your
  product as the same literal string. The event now carries the value the gateway actually
  returned, falling back to the previous literals only when the response supplied none.
  `SessionExpiredEventArgs` itself did not change (same two string properties); a product that
  branches on this event's `ErrorCode` will start seeing values it has not seen from it before —
  most notably `LICENSE_NOT_ACTIVE` for a revoked/blocked licence.

## [2.2.0] - 2026-09-19

### Fixed
- **A rejected/expired license could still grant a feature that was on the plan before the
  rejection — including a product-wide feature meant to work even WITHOUT a licence
  (`LGC-1197`).** The two places that build the result you get back from any check — a live
  signed response and the offline cache — copied `Features`/`FeatureLimits` off the signed
  payload unconditionally, without checking whether that payload's own verdict was actually
  valid. `ProductLicenseAccessor.HasFeature` — the form this README's own examples use — had no
  separate check either, so it could answer `true` for a feature you shouldn't have. The SDK's
  older static `GrossGeoLicense.HasFeature` already had a narrower guard for exactly this
  (`LGC-519`); this closes it at the source instead, for both call forms, and adds the accessor's
  own guard on top as a second, independent layer, so it matches the static form: **a rejected
  verdict now grants nothing, full stop — no exception for product-wide "always on" features**
  (owner decision; an earlier draft of this release tried to carve those out and reverted, because
  the SDK has no way to tell a product-wide feature apart from a plan one in the signed list it
  receives — only the platform does, before it merges them). **The platform clears product-wide
  features on a rejected verdict too, as of the same release this SDK version ships with** — an
  interim server build kept them on purpose while the owner's decision was still open (so that no
  one was narrowed by accident before a decision existed), and that interim form is being replaced
  to match. This SDK-side gate does not depend on or wait for the platform's timing either way: it
  answers `false` on its own, before any server-provided list is even consulted. The fix is at the two
  result-builders, so it reaches every reader of `LicenseResult.Features`/`.FeatureLimits` on a
  rejected verdict — not only `HasFeature`/`RequireFeature`/`RequireFeatureOrThrow` (via
  `GrossGeoLicense.ForProduct(...)` or the static `GrossGeoLicense`), but also the static
  `GrossGeoLicense.Features`/`.FeatureLimits` properties and anything that displayed them. **If
  your product uses a product-wide feature that is meant to work without a licence, read this
  before you rebuild:** with this release there is no remaining way to keep it working through
  `HasFeature`/`RequireFeature` alone — both already answer `false`/skip whenever the verdict
  itself is rejected (no licence at all, an expired one, wrong machine, etc.), regardless of how
  you call them. If you need that feature to keep working without a licence, stop gating it on a
  licence check at all.
- **`CheckLimit`/`RequireLimit`/`RequireLimitOrThrow` (both the static form and the accessor) could
  still run on a rejected license, because they never checked `IsValid` at all — only whether a
  limit number was present (`LGC-1197`, follow-up found by the same review).** Before this fix, a
  rejected-but-signed verdict with a real limit number in the envelope (e.g. a plan-limit of 50)
  and a usage under that number (e.g. 3) made `CheckLimit` answer "not exceeded" and
  `RequireLimit` *run the action* — the same as a valid, paid verdict would, with no verdict check
  anywhere in the path. `CheckLimit` only ever refused when the actual usage exceeded the raw
  number, never because the verdict itself was a rejection. Gating `Features`/`FeatureLimits`
  above made this worse: with `FeatureLimits` now `null` on a rejected verdict, "limit not
  configured" and "verdict rejected" became indistinguishable, and the same permissive answer
  would have followed for every rejected verdict, not just the ones with a usage under the number.
  Both forms now check
  the verdict directly: a verdict that was received and rejected is treated as exceeding every
  limit, regardless of what number the envelope carries. **Deliberately unchanged:** the case where
  no verdict has ever been received at all (`Initialize`/`CheckAsync` not yet completed) still
  answers "not exceeded" — that is a separate, still-open question tracked in
  `ARefusedLicenceIsNotAPermissionTests.cs`, not decided by this release.
- **`AcquireSessionAsync`/`AcquireSessionForProductAsync` answered the same `NOT_CONCURRENT_MODE`
  for two different situations (`LGC-1208`, found live during the §13 acceptance run this release
  closes).** One is genuine: the verdict is known and the plan really isn't Concurrent. The other
  is "we don't have a verdict at all yet, or its licence mode came back `Unknown`" — typically the
  panel wasn't reachable when this was called — and `NOT_CONCURRENT_MODE`'s message ("Лицензия не
  требует concurrent сессии" — "this licence doesn't need a concurrent session") is simply false in
  that case: no plan was ever read. The second situation now returns a new code,
  `LICENSE_MODE_UNKNOWN`, instead. If your product distinguishes `AcquireSessionAsync` failures by
  `ErrorCode`, add a case for it — treat it like any other "could not verify yet" outcome (retry
  once the licence check completes), not like `NOT_CONCURRENT_MODE`.
- **`Initialize` never called (not just not yet finished) used to answer `Status = Blocked` —
  "you have no rights" — instead of "we couldn't ask" (`LGC-210`).** Three internal pre-checks
  (before the SDK even has an IPC client to ask anything) returned a blocked verdict with
  `ErrorCode = NOT_INITIALIZED`; a product following the documented `Protect(onBlocked: ...)`
  pattern showed "license blocked" to a user whose licence was perfectly fine — the SDK simply
  hadn't been asked to check yet. Now returns `Status = NetworkError` with the same
  `NOT_INITIALIZED` code (there is no separate `Unavailable` status — this is the same value a
  real network failure gets, because the underlying claim is the same: "could not verify"),
  matching how a gateway refusal that isn't an authoritative denial has worked since 25.08.
  `Protect(onBlocked: ...)` still fires exactly as before — the callback decision is driven by
  `IsValid`, not `Status` — but the `Status` value your handler receives inside it changes from
  `Blocked` to `NetworkError` for this specific case.
- **A product blocked by moderation (`PRODUCT_BLOCKED`) is now treated as a final answer, not a
  connectivity blip (`LGC-1150`/`LGC-1153`).** An SDK built against 2.1.17 or earlier reads a
  moderation block the same way it reads a lost connection: a transient failure, so it keeps
  serving the last positive verdict from the signed offline cache for as long as that cache stays
  valid — the platform sets that ceiling per licence mode and billing model (see the correction
  under 2.1.14 below), from none at all for a Concurrent-mode licence up to 30 days for most
  others. `PRODUCT_BLOCKED` is now in that same authoritative-denial set, alongside
  `LICENSE_EXPIRED`/`LICENSE_NOT_FOUND`: no cache fallback, `Status = Blocked` with
  `ErrorCode = PRODUCT_BLOCKED` right away, once your product is moderated off the platform.
  **The set of authoritative codes is compiled into the SDK inside your product, the same as
  every entry already in it** — the platform side of this pairing does not change what an
  already-shipped build of your product does; only a rebuild against 2.2.0 closes the gap.
  Nothing about this is new server behaviour to plan around — the platform already stops signing
  fresh verdicts for a blocked product; this release only stops the SDK from stretching the last
  one it already had.

### Added
- **Feature key material (`ProductLicenseAccessor.TryGetFeatureKey` / `GetFeatureKeyAvailability` /
  `GetFeatureKeyAsync`, and the new `FeatureKeyAvailability` enum in `GrossGeo.Contracts`).** For
  features whose value lives in the product's own data (lookup tables, templates, coefficients),
  the platform can now hand your product an actual secret — random bytes tied to a
  `(featureCode, kid)` pair — instead of just a yes/no verdict. You encrypt the valuable data with
  it at build time and decrypt on the user's machine; a patched `HasFeature` no longer unlocks
  anything by itself, because the verdict and the material are separate. `kid` is a version tag you
  choose yourself (e.g. `2026-09`), so rotating the secret does not break already-shipped releases
  still pointing at the old one. The material lives only in this product's own
  `ProductLicenseAccessor` — there is no static `GrossGeoLicense` member for it, matching how every
  other per-product answer works after 2.1.0.
- **Material is available offline for about 24 hours past a stale signature, not just until the
  next live check**, closing a gap where the live validation path would have discarded material
  from an otherwise-valid signed response for being a few seconds older than its replay window.
- **`ReleaseSessionWithResultAsync` (`GrossGeoLicense` and `ProductLicenseAccessor`).** The
  existing `ReleaseSessionAsync` sends the release request and returns without telling you whether
  the gateway actually confirmed it — a failed release there looked identical to a successful one.
  The new method returns a `SessionReleaseResult` (`IsSuccess`, `ErrorCode`, `ErrorMessage`) built
  from the gateway's real response. `ReleaseSessionAsync` itself is unchanged and still does not
  report the outcome — switch to the new method if your product needs to know.
- **`UpdateCheckResult.Completeness` (`UpdateCheckCompleteness` enum, new in `GrossGeo.Contracts`)**,
  returned by `CheckForUpdatesAsync`. Distinguishes "we asked every installed product and none has
  an update" (`Complete`) from "the sweep behind this answer did not reach every product"
  (`Partial`/`AllFailed`) and "nothing installed to check" (`NothingInstalled`) — an empty update
  list used to mean all four of those silently. Defaults to `Unknown` on any path that did not get
  a real answer (gateway refusal, exception) — this SDK does not manufacture a completeness claim
  it cannot back. Requires the User Panel to actually report the field (server/panel side landed
  first, this release reads it for the first time). `UpdateCheckResult.NoUpdate(UpdateCheckCompleteness)`
  and `.Available(string, DateTime?, string?, bool, UpdateCheckCompleteness)` are new overloads,
  not new parameters on the existing ones — the old `NoUpdate()` and `Available(...)` signatures are
  byte-for-byte unchanged, so a precompiled caller keeps working without a rebuild.
- **Two opt-in environment variables, read fresh on every use rather than cached (`LGC-1201`),
  meant for test/CI hosts that link this SDK's source directly — not something a normal product
  install needs to set.** `GROSSGEO_SDK_NO_PANEL_LAUNCH=1` stops the SDK from spawning a real
  `GrossGeo.UserPanel.exe` when no panel answers (it still answers "unavailable" exactly as
  before — this only removes the side effect of launching one). `GROSSGEO_DATA_DIR` redirects
  where the SDK reads/writes its own cache, signing-key cache and diagnostic log, reusing the same
  variable name and `?? fallback` shape the User Panel already uses for the same purpose. Both
  default to today's behaviour when unset; nothing changes for a product that does not set them.

### Note
- **Requires the server (feature-key storage, Developer API) and the User Panel (material cache,
  IPC field) to ship first — this SDK change alone does not turn the feature on.** The material
  itself is served by the User Panel, not cached by the SDK on disk: without a running panel (or at
  least one recent live check), `GetFeatureKeyAvailability` reports `PanelUnavailable`, even while
  the cached licence verdict itself is still `IsValid = true` from the SDK's own offline cache.
  Read that as "panel not reachable", not "not entitled" — they call for different UI. See
  `docs/external/grossgeo-sdk-developer-guide.md` §8 and the package `README.md` for the full
  contract and an example.

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
  **Correction, added in 2.2.0's notes:** "up to seven days" above was a simplification and was
  never true for every combination. The platform sets the offline cache ceiling server-side, per
  licence mode and billing model
  (`LicenseService.ResolveOfflineCacheLimit`): a Concurrent-mode licence has no offline grace at
  all (session occupancy cannot be verified offline); a user-bound subscription is capped at 7
  days; everything else — Machine mode, Perpetual, fixed-term contracts — up to 30 days, or the
  licence's own expiry if that comes sooner. Nothing about the mechanism itself changed since
  2.1.14; only this description of it does.
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
