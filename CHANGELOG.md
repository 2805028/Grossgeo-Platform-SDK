# Changelog

All notable changes to GrossGeo.SDK.Stub will be documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.5] - 2026-09-29

**Required.** Fixes the verification-key cache that the SDK wrote on AutoCAD 2019–2024 and then
could not use, closes a licence served from the offline cache with the machine clock more
than 2 hours behind the latest time the SDK had seen for that product on that runtime, removes the
start-up delay that SDK 2.2.4 introduced on AutoCAD 2025 and later (the first licence check waited
about 15 seconds and answered `MACHINE_NOT_IDENTIFIED`), and keeps the offline licence cache in the
folder of the product that owns it. No existing public signature changes; one public constant is added
(`IpcErrorCodes.ClockUntrusted`). On AutoCAD 2025 and later the platform loads its SDK copy from the
User Panel bundle normally first, before the products, and your product normally runs that copy, so a fix reaches
participants with the platform release that carries the same SDK version.

### Changed
- **Submitting a release whose .NET 8+ main assembly cannot be read is refused (`400 SDK_STUB_VERSION_UNREADABLE`).**
  On AutoCAD 2025 and later the platform loads its own copy of the SDK first, and the server reads the
  `GrossGeo.SDK.Stub` version each release needs from the assemblies of its .NET 8+ folders (assemblies
  whose obfuscator duplicates metadata streams or puts pointer tables into the compressed table stream —
  tricks AutoCAD still loads — are read the way the runtime reads them). The submission is refused
  only when no reference to `GrossGeo.SDK.Stub` could be read and, in a .NET 8+ folder that contains
  `GrossGeo.SDK.Stub.dll`, the main assembly of that folder (the `dllPath` of each declared runtime; if the release declares no
  runtimes, its `netloadDllPath`; if that is empty too, the one the server finds from `PackageContents.xml`
  or the folder layout) cannot be parsed at all — the file is damaged or is not a valid PE image, or its
  metadata is hidden by an obfuscator. The message names those files and the release keeps its status.
  Check the assembly, rebuild it without metadata obfuscation (leave
  `GrossGeo.SDK.Stub.dll` out of it), upload the corrected archive under the same file name (or delete the old
  release file first) and submit again. The refusal starts working when the first platform release that ships
  an SDK newer than 2.2.4 is published; before that the submission goes through. It does not apply to a
  folder without `GrossGeo.SDK.Stub.dll`, to unreadable secondary assemblies, to .NET Framework folders or
  folders of undetermined runtime, to installers (`.msi`), or to files skipped because of size or count.
  Details: the publishing API guide, step 5.

### Fixed
- **The SDK now checks the machine clock before serving its own offline cache and before
  accepting a verdict the User Panel marks as served from its stale cache (`LGC-178`).** If the
  clock is more than 2 hours behind the latest time the SDK has seen for the product on that
  runtime (.NET Framework and .NET 8 keep separate records), or has stepped back more than
  2 hours in total, a licence check returns `CACHE_CLOCK_MISMATCH` instead of a licence from the
  cache. The refusal is not final and the offline cache is kept. Setting the clock right does not
  lift it by itself; the next server answer the SDK accepts does (an answer is not accepted while
  the clock is more than 5 minutes behind the server's). The message names both actions: check the
  date and time, or connect to the internet. The code exists since 2.1.14 but now arrives in more
  cases — if you branch on `LicenseResult.ErrorCode`, make sure it is handled. With a User Panel
  release that judges the clock itself, `IncrementUsage` and session results can also return
  `CACHE_CLOCK_MISMATCH`.
- **An older signed server refusal no longer erases a newer offline cache of the running build
  target (`LGC-1150`).** A refusal that revokes the right (`LICENSE_NOT_FOUND`, `LICENSE_EXPIRED`,
  `LICENSE_NOT_ACTIVE`, `PRODUCT_BLOCKED`, `USER_NOT_ASSIGNED`) and carries a signed envelope is
  judged by the SDK itself when it has a verification key: it erases the offline cache (of both
  build targets) only if the signature verifies, the envelope is a refusal for this product with
  one of these codes, and it was signed no earlier than the running target's cached answer —
  whether or not the panel marked it as stale; if the running target has no readable cached
  answer, it erases without comparing. An envelope that is incomplete or does not verify never
  erases. A refusal without an envelope, or with one while the SDK has no keys at all, follows the
  2.2.4 rule: it erases unless the panel marked it as served from its stale cache. The logic is
  the same on .NET Framework and .NET 8.
- **`DaysRemaining` no longer freezes offline at the moment of signing (`LGC-1402`).** The SDK
  computes it at every check from the signed expiry, rounding up like the server (previously a
  cache three days offline still reported 30 when 27 remained). `DaysRemaining` is the number of
  days until the licence term ends, not an entitlement flag: with `IsValid = false` it can be
  positive (for example, the seat is not assigned or the machine is not bound). Whether the
  licence is in effect is decided only by `IsValid`.
- **.NET Framework: the public-key cache is now written and read in one canonical PEM form
  (`LGC-1405`).** The SDK wrote its verification-key cache in a form it did not accept on load, so
  after a live answer on AutoCAD 2019–2024 the next start on any runtime found no usable key in
  the cache, and a start that fell back to the offline licence cache before any successful User
  Panel answer could report that the panel had not passed the verification key yet. SDK 2.2.5 also
  accepts a key cache written by earlier versions.
- **.NET Framework, Concurrent seats with several products in one AutoCAD: a heartbeat that a hung
  or slow panel accepted but did not confirm now holds the shared heartbeat timer for at most 10
  seconds — as on .NET 8 (`LGC-1360`).** Previously it could hold it for up to 2 ×
  `IpcTimeoutSeconds` per such session. Known limitation: with five or more simultaneous Concurrent
  products in one AutoCAD, when the panel is slow enough that each heartbeat exchange takes its
  full 10-second budget without failing, a seat can still be lost without an event.
- **AutoCAD 2025 and later: the first licence check in a process no longer waits out the 15-second
  fingerprint window (`LGC-1424`, introduced in 2.2.4).** In 2.2.4 the first check of every start that
  reached a licence request waited the whole 15 seconds and returned the temporary
  `MACHINE_NOT_IDENTIFIED`; the licence became valid about 20 seconds later, on the automatic re-ask.
  The cause was a stall inside the fingerprint collection: the SDK's own assemblies
  (`System.Management`, `System.Security.Cryptography.ProtectedData`, `GrossGeo.Contracts`) were first
  loaded on a pool thread, and on AutoCAD 2025 a thread other than the main one cannot load an assembly
  from outside the process's own list while the main thread is still inside a plug-in's `Initialize`
  waiting for that very work. The SDK now loads these assemblies on the calling thread when it is an STA thread, such as
  AutoCAD's main thread, and always looks up the cache-invalidation type there, before the background
  collection starts. Measured in an AutoCAD 2025
  console on the maintainer's machine: 2.2.4 answered in about 16 seconds with `MACHINE_NOT_IDENTIFIED`
  on 5 of 5 cold starts, this version in 1.1–1.5 seconds with none; on AutoCAD 2022 (.NET Framework) the
  first check took under half a second on 5 of 5 cold starts, with 2.2.4 and with this version's code alike (the change does not touch that runtime). The message of the
  temporary `MACHINE_NOT_IDENTIFIED` refusal is softer now (the licence check is still running and will
  repeat by itself); the code now reaches only a product built against 2.2.4 or newer, an older build
  gets `NETWORK_ERROR` (in 2.2.4 the code was not limited by the build version). The same trap awaits your own code — see the developer guide,
  section 6 (Documentation below).
- **The offline licence cache lives in the folder of the product that owns it (`LGC-1423`).** Until
  now the SDK created one cache per process from the `CacheDirectory` of the FIRST product initialised
  and silently ignored the `CacheDirectory` of every other product in the same process. On AutoCAD 2025
  and later, where all products run the same SDK copy, a product's cache could end up in another
  product's folder, and in a session where a different product initialised first it was looked for
  somewhere else — an offline start did not find its signed cache. Now each product writes to its own
  `CacheDirectory`. The first time a product's cache is read in a process (and again after another
  product's folder becomes known), the SDK also reads the folders the process knows (other products' and
  the default one); if one holds a newer signed answer than the product's own folder, or the own folder
  has none, it moves that answer to the product's own folder and removes the other copies once the moved
  one reads back. A refusal that revokes the right erases the product's
  cache in every folder the process knows and stops the SDK from reading copies elsewhere for that
  product for the rest of the process. Limits: a copy written by 2.2.1–2.2.3 (a shared `<hash>.cache`
  file) in another product's folder is not moved; a copy in a folder the process did not know at the
  time of the erase survives until a session that does know it.
- **`OpenProductPageAsync()` opens the product card (`LGC-1425`).** The `product` page no longer starts
  the trial dialog, and the `purchase` and `reviews` sections with a product key work. This needs a
  User Panel newer than 1.0.2643; on an older panel the trial dialog still appears and the sections are
  ignored, with no error returned to your product. If such a panel is running but not answering, the
  trial dialog does not appear either: the older panel reads `product/<key>` only with a product GUID.
- **Machine fingerprint: the SDK now computes it the way the User Panel does on computers whose firmware
  reports a blank or padded board serial number.** Where the firmware returns a motherboard serial number
  made of spaces only, a manufacturer placeholder ("To be filled by O.E.M.") with spaces around it, or an
  answer made of spaces ahead of the real serial number, SDK 2.2.4 and earlier took the first non-empty
  answer as is (rejecting only the exact string "To be filled by O.E.M."), so the SDK and the User Panel
  got different fingerprints on such computers. What changes there: licences in Machine mode that were
  refused with `MACHINE_NOT_BOUND` although the computer is registered and bound in the User Panel start
  to pass, and the offline
  licence cache of every product that gets the new reading becomes invalid once, on those computers only;
  it is restored at the first contact with the User Panel, and until then the product cannot start offline
  on such a computer. Nothing changes on other computers. Under AutoCAD 2019–2024 only a product rebuilt on 2.2.5
  gets the new reading; under AutoCAD 2025 and later, once the platform ships SDK 2.2.5.
- **`InitializeSync` waits at most 5 seconds for the first licence verdict** (all targets), regardless of
  `IpcTimeoutSeconds`. If no verdict arrives in time — for example, the User Panel does not answer — it
  returns a transient "still checking" result (`ErrorCode = NOT_CHECKED`, `Status = Unknown`). The check then
  continues in the background; when its outcome differs from what was returned, `LicenseRefreshed` is raised (if the background check
  fails with an error, the event is not raised).
  Subscribe to `LicenseRefreshed` before calling `InitializeSync`. A product built against an SDK older than
  2.1.14 receives the familiar transient `NETWORK_ERROR` instead of `NOT_CHECKED`.
- **`Initialize` no longer holds the calling thread on .NET Framework (AutoCAD 2019–2024).** Before, the whole
  exchange with a User Panel that accepted the connection but did not answer ran on the caller's thread.
  AutoCAD's start-up stood for about 40–55 seconds, even for a product that did not wait for the returned
  task. Now only a short preparation runs on the calling thread.
- **The update check at `Initialize` (`CheckForUpdatesOnInit`) runs in the background after the licence
  verdict**, instead of before `Initialize` returns.
- **On .NET Framework a dropped connection to the User Panel is now retried**, as intended; before, the retry
  never ran.
- **Note for your logger:** on .NET Framework the SDK now calls your `ILicenseLogger` from a background
  thread, including during `Initialize`. Do not block the main thread on SDK calls (`.Wait()`, `.Result`)
  while your logger marshals synchronously to the main thread (`Invoke`): that deadlock has no timeout. Wait
  with `await`, or marshal log calls asynchronously (`BeginInvoke`).
- On AutoCAD 2019–2024 these changes reach a product once it is rebuilt against 2.2.5; on AutoCAD 2025 and
  later — once the platform ships SDK 2.2.5.

### Documentation
- **Renewal window semantics (`LGC-1394`).** For a subscription that is due to renew (active, not
  cancelled at period end), in about the last hour of the paid period the server moves the licence
  expiry to 24 hours after the period end — the renewal window. `ExpiresAt` and `DaysRemaining`
  then count to the window end, not to the paid-until date. The expiry moves again when the
  renewal is decided: forward when the renewal is paid or a payment grace starts, back to the
  period end if the subscription is cancelled at period end. Decide the right by `IsValid`; do not
  compare `ExpiresAt` with the current time yourself or show it as the paid-until date.
- **The developer guide lists every error code.** Section 17 now has a table of every `ErrorCode` the SDK
  can return: what it means, whether the SDK reads it as final, temporary or "sign-in needed", what
  happens to the offline cache (erased, withheld, served), which result type carries it and from which
  SDK version. Rule for studios: branch on `Status` and `ErrorCode`, never on the text of `Message`;
  read a code your SDK version does not know as a temporary failure — the SDK does the same.
- **Do not wait for background work in a plug-in's `Initialize` (developer guide, section 6).** On
  AutoCAD 2025 and later a thread other than the main one cannot load an assembly from outside the
  process's own list while the main thread runs your `Initialize`; waiting there for a background task
  that touches such an assembly stalls until its own limit. The same section now says that your
  `ILicenseLogger` is called from a background thread, including during the synchronous initialisation,
  and covers satellite resource assemblies.
- **Compare the plan tier only together with `IsValid`, and do not rely on `IsInGracePeriod`.** With no verdict `PlanTier`, `BillingModel` and `LicenseMode` are `Unknown` (255), so an order comparison such as `PlanTier >= PlanTier.Pro` is true; the README and the migration guide examples now check `IsValid` and `Unknown` first, and the remarks of the three enums say so. Review your own tier comparisons. `IsInGracePeriod` is not set by SDK 2.2.x (always `false`): an answer served from the offline cache is `IsOfflineMode = true`, and the remaining offline days are in `Message`; the README no longer promises the flag.

## [2.2.4] - 2026-09-25

**Recommended** if your product uses Concurrent seats, may run on a machine whose WMI subsystem is
slow to start (shortly after boot, inside a VM), or runs beside other GrossGeo products in one
AutoCAD. Every public signature is unchanged; the new members are additions. Items marked
"rebuild" reach a product only once it is rebuilt against this version.

### Added
- **`SessionResult.IsRetrying` and the `GrossGeoLicense.SessionAcquired` event (`LGC-1353`,
  `LGC-1354`).** A Concurrent seat request left unfulfilled after a temporary refusal (panel busy,
  connection lost) used to stay that way until your product called Acquire again — typically not
  until the next restart. `AcquireSessionAsync` / `AcquireSessionForProductAsync` now retry a
  non-authoritative refusal in the background (5/15/30/60 s, then every 5 minutes) until the seat
  is acquired, the SDK shuts down or the request is released. `IsRetrying` is true while a retry
  runs; `SessionAcquired` fires once when it succeeds. A refusal by substance
  (`SESSION_LIMIT_REACHED`, `LICENSE_NOT_ACTIVE` and similar) still returns immediately.
- **`ErrorCode = MACHINE_NOT_IDENTIFIED` (`LGC-1356`).** A transient result for a machine whose
  WMI has not answered inside the 15-second window (below). If you branch on
  `LicenseResult.ErrorCode`, add this case; it is new, not a renaming.
- **`FEATURE_LIMITS_UNAVAILABLE` and `FEATURE_LIMIT_NOT_CONFIGURED` (`LGC-1376`).** The gateway
  code `FEATURE_NOT_AVAILABLE` covered three situations: not in your plan, no limit data cached
  right now, and a limit the studio never configured. The last two have their own codes,
  recognised from this version; an SDK built against an earlier version keeps receiving
  `FEATURE_NOT_AVAILABLE` for both. No path in the SDK sends the requests these codes answer
  today (`GetFeatureLimit` reads the local cache), so this is groundwork you will not observe yet.
- **A diagnostic line for a co-loaded older `GrossGeo.Contracts` (`LGC-1358`).** At `Initialize`
  the SDK compares the loaded copy of `GrossGeo.Contracts` with the revision it was built for and
  logs one line, without an exception or a behaviour change, when an older copy from another
  GrossGeo product in the same AutoCAD process won the strong-name collision. Useful when
  diagnosing a `TypeLoadException`; see the developer guide.

### Changed
- **The SDK tells what a product was built against from the product's own assembly reference
  (`LGC-1385`).** On AutoCAD 2025 and later every product in the process runs the newest loaded
  `GrossGeo.SDK.Stub`, so a visible-semantics change shipped in one release reached products that
  had never been rebuilt against it (the 2.2.3 `ProtectOrThrow` reason was the first case). The
  SDK now reads the referenced version from each caller of `Initialize` / `InitializeSync` (and
  their overloads): a product not yet rebuilt against 2.2.3 keeps the pre-2.2.3 `ProtectOrThrow`
  reason; rebuilding switches it to the newer, more truthful one. No code change is required.
- **The offline cache is kept per runtime (`LGC-1380`, rebuild).** A product running in
  AutoCAD 2022 (.NET Framework) and in AutoCAD 2025 (.NET 8) on the same machine overwrote each
  other's cache and read the other's as missing, so a user offline for more than a day was refused
  while the signed offline window was still open. net48 and net8 now have their own file; the
  previous shared file is picked up once when it is in the runtime's format and left untouched.
  Resetting the licence (refresh, invalidate, clear) removes the cache for all runtimes.
- **An authoritative refusal about the product right erases the SDK's offline cache
  (`LGC-1150`, rebuild).** `LICENSE_NOT_FOUND`, `LICENSE_EXPIRED`, `LICENSE_NOT_ACTIVE`,
  `PRODUCT_BLOCKED` and `USER_NOT_ASSIGNED`, delivered live by the User Panel in response to a
  licence check, now erase the cache for that product. Before, a revoked or blocked licence kept
  working from it until the signed offline window ended (up to 7 days per-user, up to 30 for
  per-machine and perpetual; never past the signed expiry). The refusal's own `LicenseResult` is
  unchanged. Later checks no longer fall back to the erased cache: without the panel you get
  `NetworkError` with `USER_PANEL_NOT_RUNNING` or `NETWORK_ERROR`, or what the panel itself holds
  (its stored answer flagged `IsFromStaleCache`, or `NetworkError` with its transient code). The
  cache is kept on temporary refusals, on refusals that are not about the right
  (`FEATURE_NOT_AVAILABLE`, `SESSION_EXPIRED`, `INVALID_PRODUCT_KEY`, `FINGERPRINT_REQUIRED` and,
  for now, `MACHINE_NOT_BOUND` / `MACHINE_LIMIT_EXCEEDED`), on usage-counter and Concurrent seat
  refusals, and on any answer the panel serves from its own records (`IsFromStaleCache = true`).
  User Panel 1.0.2633.8001 to 1.0.2638.19001 delivers machine and fingerprint refusals as
  `LICENSE_NOT_ACTIVE`, so those do erase it. User Panel 1.0.2632.3001 and older sends every
  refusal inside a licence answer without a code; the SDK treats them as temporary and keeps the
  cache, so with those panels only `LICENSE_NOT_FOUND`, `LICENSE_EXPIRED` and, when the panel has no
  record of its own, `PRODUCT_BLOCKED` erase it.

### Fixed
- **A machine whose WMI had not warmed up could be answered with placeholder hardware IDs
  (`LGC-1356`, rebuild).** On a cold boot or in some VMs the licence check used `UNKNOWN_CPU`,
  `UNKNOWN_MB` or `UNKNOWN_VOL`, which could quietly change the machine fingerprint, and the
  offline-cache key derived from it, between runs on the same machine. WMI reads are now bounded
  by a 15-second window started at `Initialize`; a machine that answers inside it gets its real
  fingerprint, and one that does not gets the transient `MACHINE_NOT_IDENTIFIED` above, asked again
  with the same rhythm as a temporary refusal.
- **A running User Panel whose gateway slots were all busy was treated as not running
  (`LGC-1381`, rebuild).** The SDK started a second copy, which popped the panel window up over the
  user's work. It now checks the panel's single-instance mutex: a running panel that does not
  answer (busy, still starting, shutting down) is not launched again, and the check returns the new
  transient `ErrorCode = USER_PANEL_BUSY`, picked up by the same retry as a temporary refusal.
  `OpenProductPageAsync` and `RequestTrialAsync`, which are explicit user actions, still bring the
  running panel's window up and now also deliver the navigation to the product page.

## [2.2.3] - 2026-09-24

**REQUIRED.** Closes an AutoCAD crash and an AutoCAD 2019–2024 hang caused by the SDK, and a
Concurrent seat being lost without any event.

### Fixed
- **An exception in your `SessionExpired` or `LicenseRefreshed` handler crashed `acad.exe`
  (`LGC-1350`).** The heartbeat timer ran as `async void`, and the event was raised inside the
  `try` whose `catch` raised it a second time without protection. Every subscriber is now called
  in isolation, the error goes to the SDK log with the subscriber's name, and `SessionExpired` is
  raised once per session.
- **.NET Framework (AutoCAD 2019–2024): an exchange with a User Panel that accepted the
  connection but never answered blocked the calling thread forever (`LGC-1352`)** — at load, in
  a command or on exit. The exchange is now bounded by `IpcTimeoutSeconds` and ends with
  `CONNECTION_FAILED`.
- **Concurrent mode: a licence re-check dropped the session token (`LGC-1351`).** A purchase or
  trial of any product, an account switch, a panel restart or `ClearLocalCache` replaced the
  licence result with one that had no token; heartbeat stopped silently and the server released
  the seat. Sessions now live in their own per-product store that re-checks do not touch. A
  session that is active but has no token raises `SessionExpired` with `SESSION_TOKEN_LOST`.
- **Concurrent mode with several products in one AutoCAD: only the last-acquired session got a
  heartbeat (`LGC-1360`).** One timer now serves every active session on its own schedule; a
  failure or expiry affects only its own product.
- **A temporary refusal stuck until restart (`LGC-1353`).** A refusal that is not authoritative
  (panel busy, timeout, sign-in still in progress) is now re-asked in the background
  (5/15/30/60 s, then every 5 minutes); `LicenseRefreshed` fires when the result changes.
- **`ProtectOrThrow` without a verdict reported "licence invalid" (`LGC-1355`).** It now reports
  "not checked yet" (`Status = Unknown`, `NOT_CHECKED`). `RequireFeatureOrThrow` tells apart
  "could not check, retry", "licence invalid" and "not in your plan".
  **Update:** on AutoCAD 2025 and later a product runs on the SDK shipped with the platform. The
  `RequireFeatureOrThrow` change is a fix and applies to every product without a rebuild; the
  `ProtectOrThrow` change alters the status your code receives, so it applies only to products
  rebuilt against the version that introduced it — a product built against an earlier version
  keeps the previous behaviour.

### Added
- `SessionExpiredEventArgs.ProductKey` (nullable) and a three-argument constructor — which
  product's seat was released (`LGC-1360`).

### Changed
- `Shutdown()` and `Shutdown(productKey)` release the Concurrent seat themselves and wait for the
  panel at most 2 seconds (`LGC-1351`). Remove `ReleaseSessionAsync().Wait()` from `Terminate`:
  on the main thread it held AutoCAD's exit for up to 20 seconds, and on .NET Framework without
  limit.
- Documentation: `SessionExpired` and `LicenseRefreshed` arrive on background threads; the README
  and samples now call the AutoCAD API from them only through the main thread (a `Dispatcher` or
  `Control` captured synchronously in `Initialize`).

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
  the developer guide (`grossgeo-sdk-developer-guide.md`) §8 and the package `README.md` for the full
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
  **Update:** since the platform release that preloads the SDK when AutoCAD starts, on AutoCAD 2025
  and later a product runs on the SDK shipped with the platform. This item is a fix — a withdrawn
  licence reported as still valid — so on AutoCAD 2025 and later it now takes effect WITHOUT a
  rebuild; on AutoCAD 2019–2024 each product keeps its own copy of the SDK, and there a rebuild is
  still what delivers it. Changes to the meaning of what your code receives, as opposed to fixes,
  still apply only to products rebuilt against the version that introduced them.
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
- **Guide `grossgeo-sdk-deeplinks-guide.md`** — how to open GrossGeo User Panel pages from a plugin
  (purchase / product / reviews / start-trial), the ProductId vs ProductKey vs UpgradeCode
  distinction, and the `RequestTrialAsync()` SDK method.

### Added
- **`GrossGeoLicense.RequestTrialAsync()`** — opens the User Panel at your product and asks it to
  show the trial-activation confirmation, using the `ProductKey` you already have (no ProductId
  needed). Never activates a license silently. Companion `grossgeo://start-trial/{productId|productKey}`
  URI. See `grossgeo-sdk-deeplinks-guide.md`.

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
