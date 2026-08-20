# N3O.Umbraco — project instructions

This repo is an **in-progress migration of N3O.Umbraco to Umbraco 17.3.5 / .NET 10**. The work spans many sessions and is tracked in a set of migration docs at the repo root. **Do not work from memory about migration state** — the docs below are the source of truth.

## Read these at the start of every session (repo root)

Read the orientation docs **first**, before planning or editing, so you know what's done, what's in progress, and what's blocked:

1. **`SESSION_HANDOFF.md`** — start here. Per-session log; orients the next session (current state, run command, what was just done, what's open).
2. **`MIGRATION_AUDIT_2026-06-10.md`** — current source of truth for *remaining* work (supersedes the older blocker/findings docs where they disagree).
3. **`MIGRATION_PR_TRACKER.md`** — per-project PR status + the **Backlog** of remaining tasks. The migration ships as one PR per project (base = `v17`), cut from the integration branch `v17-Talha`.

## Reference docs (read when relevant to the task)

- **`MIGRATION_PLAN.md`** / **`MIGRATION_BLOCKERS.md`** — overall plan + blocker list (predate the audit; defer to the audit where they conflict).
- **`REVIEW_FINDINGS.md`** — branch-review findings tracker.
- **`TECH_DEBT_AND_MODERNIZATION.md`** — confirmed bugs, security gaps, modernization items.
- **`REACT_MIGRATION_GUIDE.md`** — Lit→React backoffice plugin recipe (shared React runtime + import map).
- **`TYPESCRIPT_MIGRATION_GUIDE.md`** — plain-JS→TypeScript+Vite ClientApp build recipe.
- **`PACKAGING_RCL_RESEARCH.md`** — backoffice asset packaging (RCL / static web assets) research + rollout.
- **`NOT_REQUIRED_TO_RUN.md`** — optional / removable / dead items.
- **`BELLISSIMA_MIGRATION_GUIDE.md`** / **`BELLISSIMA_MIGRATION_LOG.md`** — AngularJS→Bellissima migration guide + log.

## Before removing anything, check the sites monorepo for usage

These projects are **shared packages consumed by client sites in a separate repo** — the sites monorepo at `D:\Development\n3oltd\sites` (e.g. `site-mh`, `site-afh`). A public type/method/extension with **zero callers in this repo may still be used by a site.** Before deleting or removing any public API during the migration, **grep the sites monorepo** (`git grep <symbol> D:\Development\n3oltd\sites`, or ripgrep across `sites/src`) for usages. If a site uses it, **port it to a v17-compatible version** (rewritten against the v17 API) rather than deleting it — deletion is a breaking change for that site's own v17 upgrade. "Zero callers" is only true when it holds across **both** repos.

> When you make a material change to migration state, update `SESSION_HANDOFF.md` (and `MIGRATION_PR_TRACKER.md` if it changes task/PR status) so future sessions stay oriented. Commits are handled by Talha — do not commit or push unless explicitly asked.
