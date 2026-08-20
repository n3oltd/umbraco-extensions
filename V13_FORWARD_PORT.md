# v13 → v17 forward-port backlog

> ✅ **DONE — merged into `v17` via [PR #905](https://github.com/n3oltd/umbraco-extensions/pull/905) (2026-07-22, merge `a132ff147`).** Every portable item below (all of "Group A") plus the follow-up v13 commit `104c48673` (#904 — day-of-week/month regular-giving filter + `CampaignValidator`) was ported and merged. The three decisions were resolved by doing them: Sentry → `SentryLoggerSettings` (errors reach Sentry **and** the Log Viewer), crowdfunding fixed fund-dimensions validator removed, offering embed codes implemented as `OfferingEmbedCodes` mirroring `CampaignEmbedCodes`. Build-verified 0 errors on `v17-Talha`. Excluded (N/A in v17): the `104c48673` `OfferingSending.cs` hunk (file refactored into `GetContentUrls`). **Runtime caveat still open:** offering embed codes need the offering doctype to carry the three embed-code property aliases (same as campaign) — a cloud/DB schema step, not code. This doc is retained as the record of what was ported.

Fixes and changes that landed on the **v13 line (`origin/main`)** *after* the v17 migration branched, and which need forward-porting into the v17 tree (`v17-Talha` / `v17`). Compiled 2026-07-22 from a per-domain audit (6 read-only agents) of every non-dependabot commit since the divergence point.

**Divergence base:** `v17` and `v17-Talha` both branched from `main` at **`8f37f1dd8`** (2026-06-08). Regenerate the candidate list with:

```bash
git fetch origin main
git log --format='%h %ci %an %s' 8f37f1dd8..origin/main --no-merges   # then drop dependabot/CI/gitignore noise
```

**Status keys:** MISSING = not in v17 tree · PARTIAL · PRESENT = already there · N/A = superseded/not applicable. Verified against the `v17-Talha` working tree by symbol/behaviour (v17 has folder/namespace renames, so path-matching is unreliable).

> ⚠️ Verify each item against the live v17 tree again before porting — v17 has diverged from v13 in several of these files, so **do not cherry-pick diffs mechanically**. Gotchas are noted per item.

---

## Already in v17 — no action
- `e3db15385` **Fix null-ref for endpoint properties in logging** — v17 `ExceptionMiddleware` (`Validation/.../Hosting/ExceptionMiddleware.cs`) already reads `endpoint?.ControllerName`/`endpoint?.ActionName`.
- `fc4501b36` **Validate dictionary translation placeholders on save** — present as `N3O.Umbraco.Extensions/Localization/Text/TextContainerContentValidator.cs` (merged via **PR #885**).

## N/A / superseded — no work
- `353792e3c`, `fadd856e2` — intermediate step / follow-on build-fix for the unified Giving campaign type; absorbed into the final state of `4e8500425`.
- `206159732` — deletes a Typesense `SearchDocumentId` helper that v17 never had (v17's indexer was redesigned); moot.
- `a45792974`, `07ff5a3c2` — currency-rounder comment churn + a self-cancelled ceil/round flip; absorbed into the net end-state (`8de0539f4`).

---

## 🔴 Critical fixes — standalone, low-risk (port first)

| # | Area | Commit(s) | What / port location | v17 status | Gotcha |
|---|---|---|---|---|---|
| 1 | Payments | `9513c627d` | **Server-authoritative charge amount.** Add `Checkout.GetPaymentAmount(PaymentObjectType)`, `IPaymentsFlow.GetPaymentAmount`, `PaymentsParameters.GetPaymentAmount`; drop client `MoneyReq Value` from Stripe/Bambora/Opayo/TotalProcessing req models + validators; charge handlers use the server-derived `Money`. | MISSING (`GetPaymentAmount` absent; `PaymentIntentReq.Value` still present) | **Security** — client currently dictates amount. Client `MoneyReq` drop wants a client regen, but the core server-side derivation is standalone. |
| 2 | Payments | `e815b2c76` | **Idempotency key hash → InvariantCulture** in `Giving.Checkout/Entities/Checkout.FormatTransactionText.cs` (lines 13–14) + `using System.Globalization;`. | MISSING (file byte-identical to pre-fix) | Trivial; fixes broken Stripe/Opayo/PayPal headers under ar/tr locales. |
| 3 | Cloud.Platforms | `f7a0e7ed0` | **Null-ref guard** in `Handlers/Compositions/GeneratePublishedCompositionsHandler.cs` — early `return None.Empty;` when `WebRoot.GetDirectory(...)` is null. | MISSING | `WebRoot.GetDirectory` returns null when dir absent → real NRE. |
| 4 | Extensions | `e1cc047ce` (+ `3b5f581e9`, `081bc36b8`) | **OpenGraph: preserve query string** — `OpenGraphBuilder` use `Url.Combine(...)` not Flurl `AppendPathSegment` (+ `using Flurl;`); rename `WithImagePath`→`WithRelativeImageUrl`; alpha-order methods. | MISSING | Crop-URL query strings currently %-encoded/lost. No external callers of `WithImagePath` in this repo (site repos out of scope). |
| 5 | Video.YouTube | `fcf556d30`, `ffb5247f0` | **nocookie host + forward arbitrary attributes** in `YouTubeVideoTagHelper`; add `shorts/` + `live/` to `GetYouTubeVideoId` regex in `Extensions/StringExtensions.cs`. | MISSING | Do **not** bring the `PROGRESS.md` that rode along in `ffb5247f0`. Port both files together (v17 is pre-restructure). |
| 6 | Newsletters.Mailchimp | `802d7ed6b` | **Omit empty FNAME/LNAME** merge fields + `member.StatusIfNew = Status.Subscribed` in `Services/MailchimpNewslettersClient.cs`. | MISSING | Clean apply; relies on `HasValue()` (already used in v17). |
| 7 | Scheduler | `80fe4016b` (#881) | **Honour 30-min JobTrigger timeout** — set `httpClient.Timeout = TimeSpan.FromMinutes(30)`, drop the dead `CancellationTokenSource`, in `Services/JobTrigger.cs`. | MISSING | ⚠️ v17's `JobTrigger` **diverged** (added `ProxyErrorRes` deserialization) — keep that block; only remove the CTS/token plumbing. |
| 8 | Giving.Checkout | `3fb9015ca` | **`SiteLanguageTag` via `GetLanguageName`** — add static `LocalizationSettings.GetLanguageName(cultureCode)` (+ `using System.Globalization;`); `CheckoutWebhookTransform.TransformTags` tags `GetLanguageName(CultureCode) ?? Site.Language`. | MISSING | Both files/namespaces unchanged in v17. |
| 9 | Extensions | `53577af03` | **`DayOfWeek` NamedLookup** + `DayOfWeekDataSource` under `Extensions/Lookups/DayOfWeek/`. | MISSING | ⚠️ New `N3O.Umbraco.Lookups.DayOfWeek` clashes with existing `DayOfWeekExtensions` on BCL `System.DayOfWeek` — fully-qualify/alias. Verify NodaTime APIs under .NET 10. |

## 🟡 Larger features — portable, higher impact

| # | Area | Commit(s) | What | v17 status | Gotcha |
|---|---|---|---|---|---|
| 10 | Extensions / Cart | `042491dab`, `195c39893`, `8de0539f4` (net; `a45792974`/`07ff5a3c2` absorbed) | **`ICurrencyRounder`** (+ `DefaultCurrencyRounder`, `NullCurrencyRounder`) under `Financial/Currency/`; `TryAddSingleton` in `ContextComposer`; threaded through `CurrencyValuesMapping` + upsell + all `Cart.*` partials/handlers; `MoneyExtensions.RoundUpToWholeNumber` → `Math.Ceiling`. | MISSING (no rounder types; `RoundUpToWholeNumber` still `Math.Round(AwayFromZero)`) | Author files in net-final form (ceil). Threads through **Giving.Cart** (a drop-decision project). Confirm no other `RoundUpToWholeNumber` callers before the ceil change. |
| 11 | Search.Typesense | `14db9d50a`, `440341576`, `be6ff0de0`, `769cbefc9`, `73903b1d1`, `4afc3cbdd` | **Per-culture indexing** — one document per published culture (`{contentKey}_{culture}` id), ambient `VariationContext` per culture, `content_key`-filtered deletes, new `RemoveContentCommand`/`Handler`/`RemoveContentFromIndex`, `CanIndex(alias)` gate, scoped `SearchApiKey`. | MISSING (indexer at pre-feature base) | Port as one consolidated **end-state** (commits partially cancel). `SearchDocument.Id` `Guid`→`string` — update any consumer reading it as Guid. Verify `IVariationContextAccessor` DI on U17. |
| 12 | Cloud | `027cb17cf` | **Published-localization format DTOs** — `PublishedDateFormat`/`PublishedNumberFormat`/`PublishedTimeFormat` wrappers; `PublishedLocalization` holds those, **drops `Terminology`**; accessor calls `.ToXxxFormat()`. | MISSING | Verify nothing in v17 reads `PublishedLocalization.Terminology` before removing it. Independent of the Giving feature. |

## 🟠 Client-regeneration cluster — regen NSwag clients FIRST
These will **not compile** against v17's current generated clients (generated from the old swagger). They hinge on regenerating the Cloud.Platforms/Payments clients against the **new Connect OpenAPI contract**, and the crop item may also need **backend#256 deployed**.

| # | Area | Commit(s) | What | Depends on |
|---|---|---|---|---|
| 13 | tooling | `dca02e628` | Point `clients/Generate-Clients.ps1` at `.../docs/openapi/<name>-v1.0.json` (was `.../docs/swagger/.../swagger.json`). | — (do first; enables the regen) |
| 14 | Cloud.Platforms | `bc921e348` | Adapt mappings to the new Connect contract: `ConnectGivingOptionsReq`, `CartItemType.NewGiving`/`NewGivingReq`, string-keyed composition assets, gift-type-keyed suggested amounts. | client regen (#13) |
| 15 | Cloud.Platforms + Giving.Allocations | `4e8500425`, `42a88984e`, `3ddca6b27` | **Unified Giving campaign type (Regular + Scheduled)** — `GivingCampaignContent`/`RegularGivingCampaignContent`; `CampaignTypes.ScheduledGiving`→`Giving`; move `RegularGivingFrequency` to Allocations (+ `Weekly` + source); mappings build `ConnectGivingOptionsReq.{Type,Regular,Scheduled}`; throw on unrecognized type. **Supersedes/combines with #14.** | client regen (`ConnectGivingOptionsReq`/`GivingType`) |
| 16 | Cloud | `d8a5e643c` | **Image crop = rect (x/y/w/h)** on the content/platforms wire (retire `PointReq` corners; fix offering height read as 0). Leave `CrowdfundingClient`/Cropper on `PointReq`. | client regen and/or hand-patch; coordinate **backend#256** |
| 17 | Cloud.Platforms | `44d12963b` | Set `dest.Giving.PaymentConfirmations = false;` in Create/Update campaign mappings (Giving branch). | #15 (needs `Giving.Type` field) |

## ⚠️ Needs a decision — not mechanical
- **Sentry** (`20eecda2b`): v17 has diverged (new `SentryWebHostBuilderExtension` + `SentryInitializer`) but still carries the old `SentryHostBuilderExtension.UseSerilog(...)` that replaces Umbraco's logger and **drops the Log Viewer sink**. v13's fix routes errors to Sentry **and** the Umbraco Log Viewer via a `SentryLoggerSettings : ILoggerSettings` (additive). **Decide:** keep v17's `UseSentry` path or adopt the `ILoggerSettings` path — and ensure Sentry isn't double-wired into logging.
- **Remove crowdfunding fixed fund-dimensions validator** (`d7648a401`, deletes `Cloud.Platforms/Validators/Offering/OfferingValidator{,.Feedback,.Fund,.Qurbani,.Sponsorship}.cs`): confirm this intentional v13 removal applies to v17. Auto-discovered `ContentValidator`s — grep for references after deleting.
- **Offering embed codes** (`ef2079c51` then `6fef5cbd9`): dropped in v17. `ef2079c51` adds `DonationButton/Form/PopupEmbedCode` accessors to `OfferingContent`; `6fef5cbd9` populates them. Re-adding needs the **offering doctype embed-code properties re-added** (won't compile otherwise), then ideally an `OfferingEmbedCodes` handler mirroring the merged `CampaignEmbedCodes` (publish/unpublish + `IContentService`), not the old `SendingContentNotification`/`TagBuilder` style. The `DonationButtonOffering`/`FormOffering`/`PopupOffering` `ElementKind`s already exist in v17.

---

*Generated 2026-07-22. Cross-referenced from `SESSION_HANDOFF.md` and `MIGRATION_PR_TRACKER.md`. Once porting begins, tick items here and reflect status on issue [n3oltd/work#729](https://github.com/n3oltd/work/issues/729).*

---

## Post-#905 forward-port (backlog compiled 2026-08-04)

Fixes/changes on `origin/main` **since #905 merged (2026-07-22)** — 9 PRs. **5 landed on `v17-Talha` + pushed; 4 deferred to focused follow-ups.** Same caveat as above: `main` is net8/v13, v17 has diverged — adapt, do not cherry-pick mechanically.

| PR | Commit | What | Status |
|----|--------|------|--------|
| #907 | `bbdf3366a` | Storage.Azure `AddFileAsync` → `GetBlobClient` + `UploadAsync(overwrite: true)` | ✅ ported + verified |
| #920 | `349b866bf` | `ContentCache` `GetOrAdd` factory overload (compute only on miss) | ✅ ported + verified |
| #908 | `4f86bebc2` | `SchedulerSettings` (worker counts + job timeout configurable) | ✅ ported + verified |
| #919 | `1cb02e9f7` | culture-aware `SpecialContentPathParser` + `FlushSpecialContentPathsComponent` | ✅ ported + verified |
| #913 | `32b509f5a` | RegularGiving in `CampaignSending` | ✅ **N/A** — v17 `IsCampaign` is composition-based (`HasComposition(CampaignContent)`), already covers RegularGiving; v13's flat-alias-list add is subsumed by the #905 unified-Giving restructure |
| #918 | `a9dad34b4` | run background jobs where queued (deploy-ordering deferral) | ⏳ **deferred** — intricate (~6 upstream review iterations); ~106-line `JobTrigger` change overlaps the #908 settings injection **and** the v17-only `ProxyErrorRes` block; verify `InheritsGenericClass` helper exists on v17 |
| #917 | `bb7554fa8` | canonical/og:url on virtual routes + sitemap BOM (18 files) | ⏳ **deferred** — large; Extensions/Search sitemap area |
| #921 | `ad530fba0` | make culture a first-class concept, one accessor (13 files) | ⏳ **deferred** — architectural; **overlaps #919**'s `SpecialContentPathParser`/`ContentCache`; likely supersedes parts of #919 |
| #922 | `b4ecfd557` | rename `SubscriptionDescriptor`→`SubscriptionId` + webhook codes (11 files) | ⏳ **deferred** — cross-cutting rename; apply **last** + consistently across the (renamed/restructured) v17 tree |

> The 4 deferred PRs must each be done carefully (build-verify per project; the full-solution build is still blocked locally by the frontend `npm ci` issue). #921 before #922; #922 last. Reflect status on #729 as they land.
