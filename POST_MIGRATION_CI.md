# Post-Migration CI / Workflows — Follow-ups

*Generated 2026-06-15. Read-only assessment of `.github/workflows/*` + `.github/dependabot.yml`
against the v17 / .NET 10 migration changes on `v17-Talha`. Captures what CI work is **required**,
what is **due when the migration merges to `main` / releases**, and what is **optional cleanup**.
No workflow files were changed.*

## Snapshot of current CI

| File | Trigger | `dotnet-version` | Notes |
|---|---|---|---|
| `v17-ci.yml` | push → `v17` (+ `workflow_dispatch`) | **`10.x`** ✅ | Packs to MyGet (`beta` suffix, `version-strategy: date`). Already migration-correct. |
| `main-ci.yml` | push → `main` (paths `src/**`) | `8.x` | Correct **today** (`main` is still the pre-migration .NET 8 codebase). Must flip to `10.x` when v17 merges to `main`. |
| `tag-ci.yml` | tag `v*.*.*`, monthly cron, dispatch | `8.x` (build-pack-push) | Release workflow. Must flip to `10.x` for the first migrated release. Also publishes ~22 npm clients. |
| `auto-add-issue-to-projects.yml` | issues | — | Unrelated to migration. |
| `dependabot.yml` | — | — | NuGet on `/src` only; ignores `N3O.*`/`Karakoram.*`/`K2.*`. No npm/github-actions ecosystems. |

All three build workflows delegate to a **reusable workflow in a separate repo**:
`n3oltd/actions/.github/workflows/dotnet-build-pack-push.yml@main` (not visible in this repo).
There is **no `global.json`** anywhere, so the .NET SDK is selected purely by each workflow's
`dotnet-version` input — version flips are a one-line change per file.

---

## 🔴 Verify now — most likely CI break

**Node/npm must be available to `dotnet build`/`pack`.** The migration made the .NET build
**hard-depend on Node/npm**: the repo-root `Directory.Build.props` + `Directory.Build.targets` run
`npx turbo run build --env-mode=loose` via the `RestoreFrontendDependencies` /
`BuildFrontendWorkspace` / `BuildFrontend` targets (which replaced the old
`BuildClientApp`/`BuildClientApps`), and building `N3O.Umbraco.Cms` now requires Node/npm
(per `SESSION_HANDOFF.md`). The backoffice frontends live as `frontend/<app>` packages in the
Turborepo + npm workspace rooted at `src/` (`src/package.json`, **9 packages**). The orchestrator is
Turborepo; build agents need Node **`>=22.19.0`** (`.nvmrc` = `22.19.0` at repo root).

**Action:** confirm that the shared `n3oltd/actions` `dotnet-build-pack-push.yml` workflow sets up
Node `>=22.19.0` and runs `npm ci` in `src/` **before** `dotnet pack`. If it does not, the `v17-ci.yml`
pack job (and later `main`/`tag` packs) will fail on the turbo build target. **This fix lives in the
`n3oltd/actions` repo, not here.** This is the single highest-priority item.

---

## 🟠 Due when the migration lands on `main` / is released

These are **correct as-is today** — do not change until the merge/release happens:

- **`main-ci.yml`**: `dotnet-version: 8.x` → **`10.x`** the moment v17 merges into `main`.
- **`tag-ci.yml`** (`build-pack-push` job): `dotnet-version: 8.x` → **`10.x`** for the first migrated release.

(One-line edits each; no `global.json` to touch.)

---

## 🟡 Cleanup tied to the migration deletions (stale, not a break)

- **Orphaned npm clients in `tag-ci.yml`:** still publishes `umbraco-cropper-client` and
  `umbraco-uploader-client`, and both `clients/@n3oltd/umbraco-{cropper,uploader}-client` dirs still
  exist — but the **Cropper/Uploader plugins were deleted in session 18**. These clients are now
  orphaned. They won't fail CI (dirs exist), but the two publish jobs + dirs should be removed when
  the Cropper/Uploader deletion is finalized.
- **Deletion-candidate clients:** `umbraco-giving-cart-client` / `umbraco-giving-checkout-client`
  publish jobs relate to the checkout/accounts deletion candidates (`BACKLOG_SCOPING.md` §7) — revisit
  if those projects are dropped.

---

## ⚪ Optional improvements (pre-existing gaps, not migration-caused)

- **No PR-validation workflow.** None of the workflows run on `pull_request`, so per-project PRs into
  `v17` get no build check. The reusable workflow packs+pushes (not wanted on every PR), so a separate
  **build-only** workflow (`on: pull_request`, `run-pack: false`) would be needed to gate PRs.
- **Dependabot npm coverage.** `dependabot.yml` covers only NuGet on `/src`. The migration added a real
  npm surface (`src/package.json` Turborepo workspace, 9 `frontend/<app>` packages). Consider adding an `npm` ecosystem (and
  optionally `github-actions`) so backoffice deps get updates.
- **No tests run** (`run-tests: false` in `tag-ci.yml`; audit notes zero tests) — not a migration
  regression; no change needed unless a test project is added.
- `actions/checkout@v6` (in `tag-ci.yml`) is **current** — not stale despite an older audit note.

---

## Bottom line

- **No CI change is required on `v17-Talha` right now** — `v17-ci.yml` is already `10.x`.
- **Verify first:** Node/npm setup in the shared `n3oltd/actions` build workflow (likely the only real break).
- **Flip `8.x → 10.x`** on `main-ci.yml` + `tag-ci.yml` when the migration merges to `main` / releases.
- **Clean up** the orphaned Cropper/Uploader npm-client publish jobs when that deletion is finalized.

*Related: `BACKLOG_SCOPING.md` (backlog achievability), `MIGRATION_PR_TRACKER.md` (PR status),
`SESSION_HANDOFF.md` (session log).*
