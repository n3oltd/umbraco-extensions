# Nested Content → Block List migration CLI

A **standalone** command-line tool that converts Umbraco **Nested Content** property data and data types
to **Block List**, run **offline against the database** — independent of the website and of any Umbraco/N3O
package. It exists so you can:

- Migrate the data with the site **taken offline** (run it against the DB, not on app startup), and
- **Test against an existing Umbraco 13 site without upgrading it** (produce v13-compatible Block List).

It replaces the old on-startup `PackageMigrationPlan` (`N3ONestedContentMigrationPlan` +
`NestedContentToBlockListMigration` in the `N3O.Umbraco` repo's `N3O.Umbraco.Extensions`, now commented out).

Although it lives in this repository, it takes **no dependency** on any project in it — it talks to SQL Server
directly and references only `Microsoft.Data.SqlClient` and `Newtonsoft.Json`, so it runs against a database
whose site has not been upgraded yet. It is marked `IsPackable=false`, so it is built by CI but never packaged.

## What it does

Inside a single transaction:

1. Finds **every** data type using `Umbraco.NestedContent`, whether or not a content property points at it
   (see [Converts EVERY Nested Content data type](#converts-every-nested-content-data-type-and-renames-them)).
2. Resolves each Nested Content element-type alias → content-type key (GUID).
3. Converts those data types **in place** to `Umbraco.BlockList` (config rebuilt from the element types, with
   **inline editing mode enabled** — `useInlineEditingAsDefault: true`), carrying `minItems`/`maxItems` across
   to `validationLimit` and renaming `Nested X (min, max)` to `X Block List (min, max)`.
4. Rewrites every stored property value (`umbracoPropertyData.textValue`) from the Nested Content JSON
   array to the Block List JSON shape.

A **dry run** executes all of the above and then **rolls back**, so it validates the SQL against the real
schema and reports the exact counts without changing anything. Every value is attempted even after one fails,
so one dry run gives you the complete list of what needs attention.

> The tool converts the Nested Content data type **in place** — you do **not** need a pre-existing Block
> List data type. The element types referenced by the Nested Content config must exist (they normally do).

## Perplex ContentBlocks v3 → v4 (`--include-perplex`)

Opt-in second pass (off by default) for sites that use **Perplex ContentBlocks**. Perplex v4 changed block
storage from NestedContent to the Block Editor shape and ships **no content migration**, so existing v13
Perplex content does not render on v17 until it is converted. With `--include-perplex`, the same run also
rewrites every `Perplex.ContentBlocks` value from the v3 shape (each block's `content` is a NestedContent
array) to the v4 shape (`content: {contentTypeKey, key, values:[{editorAlias, alias, value}]}`, `version`
3→4), in the same transaction.

Unlike the Nested Content → Block List pass (which writes the v13 udi shape and lets Umbraco's own 13→17
upgrade finish it), this writes the **final v4 shape directly** — Umbraco's upgrade does not touch Perplex's
custom value. Element/content-type keys are stable across the upgrade, so v4 keys written on the v13 database
still resolve on v17.

**Intended flow:** take the site offline → `nc-migrate --include-perplex --apply` against the v13 DB → deploy
the Umbraco 13→17 + Perplex-4 upgrade. (It runs on the v13 database, in the same offline window as the NC pass;
it never leaves a v4-shape value on a still-running v3 site.)

**Nested Nested Content is converted recursively (fixed 2026-08-26).** An element property whose own value
is Nested Content is converted to Block List too, to any depth. This matters because step 1 flips *every*
in-use NC data type to Block List — including the ones assigned to element-type properties — so leaving an
inner value as an NC array would leave the data type and its stored value disagreeing, and the inner content
would silently fail to render after the 13→17 upgrade (where `Umbraco.NestedContent` no longer exists).
The inner value's representation is preserved: Umbraco stores a nested complex-editor value as a *serialized
JSON string* inside the parent's `contentData` entry (verified against real v13 data — `"links":"[{...}]"`),
so a string in yields a string out. Nested blocks, dropped aliases, generated keys and collisions are folded
into the run totals, and the summary reports `Nested NC properties : <n> converted recursively, <n> left
verbatim`. Anything that genuinely cannot be converted is still copied verbatim and flagged `[REVIEW]`.

Handled automatically: the `PropType` block meta field is dropped (it is not a content property); orphaned
property values (a property since removed from the element type) are dropped and flagged; blocks whose element
type can't be resolved are dropped and flagged. **Not carried across:** Perplex v3 `variants` (culture/segment
variant content) — flagged for manual review if present.

> ⚠️ **Unverified — test on a local test site first.** The v4 shape is matched from an observed live value, not
> a Perplex spec. Restore a copy, `--dry-run` then `--apply --include-perplex`, run the 13→17 + Perplex-4
> upgrade, and confirm Perplex renders the converted blocks in the backoffice **and** front-end before running
> against real/client data.

## Converts EVERY Nested Content data type, and renames them

`Umbraco.NestedContent` does not exist from Umbraco 14 on, so **any** data type left on it renders as
*"This property editor could not be found"* in the v17 backoffice - even one with no stored values behind it.
Every Nested Content data type is therefore converted, whether or not a content-type property points at it.

Skipping the unassigned ones would be wrong here, even though Perplex ContentBlocks v3 block definitions point at
an NC data type and require that editor. That does not survive the upgrade: **Perplex v4 stores no data type
reference at all** (no `Perplex.ContentBlocks` config in a migrated database mentions one). On one large
production site, skipping them left **78 of 130** data types broken in the backoffice: 76 Perplex v3 per-block
data types (named after their block) plus two genuine site ones that simply had no property bound at the time.
All 78 were fully orphaned - no property binding, no config reference, no stored value, no relation - so the
Perplex ones are dead weight in v17. They are reported rather than deleted, leaving that as a deliberate
decision.

### Config carried across, and what is dropped

`minItems`/`maxItems` are the same constraint as Block List's `validationLimit`, so they carry straight across
into `{"min":N,"max":N}`. Either key can be absent in Nested Content, meaning "no limit", which becomes `null`
(`BlockListConfiguration.NumberRange` is `int? Min` / `int? Max`). On one large production site 125 of
130 data types carried a
limit; the other 5 had none in v13.

Beware: several v13 names disagree with their own config - `Nested Accordian Item (1, 6)` really has
`minItems: 3`, `Nested Agenda Item (1, n)` really has `maxItems: 5`. The **config** is carried across, since
that is the constraint Umbraco enforces; the name is just a label and its suffix is preserved as-is. So a
renamed data type can end up with a suffix that does not match its limits - that drift is pre-existing v13 data,
not something this tool introduces.

Dropped, because Umbraco 17 has nowhere to put them - `BlockListConfiguration` exposes only `blocks`,
`validationLimit` and `useSingleBlockMode`, and its `BlockConfiguration` only the two element-type keys:

| Nested Content | note |
|---|---|
| `nameTemplate` (per content type) | the item label template; v17 `BlockConfiguration` has no server-side label |
| `confirmDeletes`, `showIcons`, `expandsOnLoad`, `hideLabel` | no Block List equivalent |

### Renaming

The name is part of the editor's identity to a content editor, and "Nested X" stops being true the moment it is
a Block List, so a data type following the N3O convention is renamed:

```
Nested Price Handle (0, 5)          ->  Price Handle Block List (0, 5)
Nested Feedback Custom Field (0, n) ->  Feedback Custom Field Block List (0, n)
Nested  Challenges Testimonial Item (2, 10)  ->  Challenges Testimonial Item Block List (2, 10)
Nested Speaker Details Item         ->  Speaker Details Item Block List
```

The min/max suffix is carried across untouched, a double space after `Nested` is tolerated, and a name that does
not start with `Nested` is left alone - which is what keeps the Perplex per-block names (`Accordian Block`) as
they are. A data type's name is `umbracoNode.text`.

## Invalidates the published cache

`cmsContentNu` is a serialized snapshot of every content item and this tool does not write it, so without
invalidating it the site keeps serving **pre-migration Nested Content** and throws
`Cannot deserialize the current JSON array ... into type 'BlockValue'` on every affected page. The tool now
deletes Umbraco's cache-serializer marker (`umbracoKeyValue` key
`Umbraco.Web.PublishedCache.NuCache.Serializer`) in the same transaction, which makes Umbraco rebuild the whole
published cache itself on the next start.

In the normal 13→17 flow the Umbraco upgrade rebuilds the cache anyway, so this only matters for a site that
stays on v13 after the migration, which would otherwise serve stale content until the cache was rebuilt
by hand.
**Do not delete `cmsContentNu` instead**: neither Umbraco 13 nor 17 rebuilds an empty cache (v13 throws
`No data for media <id>` on load, v17 fails to start at all).

## Requirements

- **To build:** .NET 10 SDK (the project targets `net10.0`).
- **To run:** nothing, if you use the standalone exe below — it carries its own runtime. Running via
  `dotnet run` instead needs the .NET 10 SDK, and the framework-dependent build needs the .NET 10 runtime.
- **SQL Server** (full or LocalDB). SQLite is not supported.
- A database backup taken before running with `--apply`.

### Standalone exe (no .NET on the target machine)

```
dotnet publish -c Release -p:StandaloneExe=true
```

Produces a single **self-contained** `bin\Release\net10.0\win-x64\publish\nc-migrate.exe` (~40 MB) that needs
no .NET runtime, no SDK and no restore on the machine it runs on — copy that one file to the database server
and run it. The accompanying `.pdb` is symbols only and does not need to go with it.

The publish is not trimmed and must stay that way: `Microsoft.Data.SqlClient` resolves types by reflection, so
a trimmed build fails at `Open()` rather than at publish time. The target needs ICU (in-box on Windows 10
1903 / Server 2019 and later) because the tool cannot run under invariant globalization — see the `.csproj`.

## Usage

From this project's folder:

```
dotnet run -- \
  --connection "<sql server connection string>" \
  (--dry-run | --apply) \
  [--verbose] [--log <path>]
```

(Or from anywhere: `dotnet run --project src/Migrations/N3O.Umbraco.NestedContentMigration.Cli -- ...`.
Or build once with `dotnet build -c Release` and run `bin\Release\net10.0\nc-migrate.exe ...`.)

| Option | Meaning |
|---|---|
| `--connection` | SQL Server connection string to the Umbraco 13 database (**required**, or set the `NC_MIGRATE_CONNECTION` environment variable instead so credentials stay out of shell history / process listings). |
| `--dry-run` | Run everything in a transaction, then roll back; report what **would** change. |
| `--apply` | Commit the changes. Mutually exclusive with `--dry-run`. |
| `--include-perplex` | Also convert `Perplex.ContentBlocks` values from the v3 (NestedContent) shape to the v4 (Block Editor) shape, in the same transaction. **Off by default.** See [Perplex ContentBlocks](#perplex-contentblocks-v3--v4---include-perplex) below. |
| `--verbose` | Log each data type / property value processed. |
| `--log` | Write the full log to this file (default: `nc-migrate-<UTC timestamp>.log` in the current directory). Every item that wasn't cleanly migrated is written as a clearly-separated `[REVIEW]` block — node id + name, property alias, and the reason — so you can find and manually check each one. |
| `--help`, `-h` | Show help. |

You must pass exactly **one** of `--dry-run` or `--apply`.

### Output shape — Umbraco 13, then let Umbraco upgrade it

The tool always writes the **Umbraco 13 (udi-based)** Block List shape:

| | Layout | contentData | data-type column |
|---|---|---|---|
| **written** | `contentUdi: umb://element/<guidN>` | `udi` + flat props | (no `propertyEditorUiAlias`) |

It runs against the **Umbraco 13 database** while still on 13 — the v13 backoffice then reads/renders the
Block List. It **detects the schema** (presence of `umbracoDataType.propertyEditorUiAlias`) and **refuses to
run on a v14+ database**, where Block List is already native and Nested Content no longer exists.

When you later upgrade Umbraco 13 → 17, Umbraco's own built-in migrations convert the udi-based Block List
values to the key-based v17 shape — so the tool deliberately does **not** produce the v17 shape itself.

> ✅ **udi→key is verified.** A real **direct 13 → 17** upgrade (vanilla Umbraco 13.14.0 → 17.4.2) was run
> against a tool-migrated database: Umbraco's own migrations recognised the tool's output and converted it to
> the v17 key-based Block List with **no data loss**. **Front-end rendering** is still unverified — always
> `--dry-run` then `--apply` against a **restored copy** and confirm the backoffice editor and front-end
> before touching production.

## Examples

Dry run against a local LocalDB v13 site (verbose):

```
dotnet run -- \
  --connection "Server=(localdb)\MSSQLLocalDB;Database=UmbracoDb;Trusted_Connection=True;TrustServerCertificate=True" \
  --dry-run --verbose
```

Apply for real (after a backup):

```
dotnet run -- \
  --connection "Server=.;Database=UmbracoDb;User Id=sa;Password=...;TrustServerCertificate=True" \
  --apply
```

Exit code is `0` on success, `1` on validation failure or error.

## Safe workflow

1. **Back up the database.** The only rollback is a restore — there is no inverse migration.
2. Take the site **offline** (or run against a restored copy). The whole run is one long transaction over
   `umbracoPropertyData`; running it against a live database can block the site.
3. `--dry-run` and read the summary — confirm the data-type count, the converted/skipped block counts, the
   generated-key count, and that there are no unexpected "unmatched element type", "nested Nested Content",
   or "unparseable config" warnings. **Open the log file and review every `[REVIEW]` block** — each one names
   a content node + property that wasn't cleanly migrated and why (left unchanged, failed, nested Nested
   Content copied verbatim, dropped blocks, reserved-name clashes, generated keys), so you can manually check
   those items. (Dry-run validates the SQL and the counts only — it does **not** prove the converted JSON
   renders, and it holds the same locks as `--apply`.)
4. `--apply`.
5. **Rebuild caches — mandatory.** Raw SQL doesn't refresh the things that actually render content:
   - Clear NuCache: delete the on-disk `NuCache.Content.db` / `NuCache.Partial.db` (under `umbraco/Data/TEMP`)
     so it rebuilds from the database.
   - Rebuild the Examine indexes.
   - Republish the affected content (or restart the site with the cache cleared).
6. Bring the site up and spot-check that the converted properties render in **both** the backoffice editor
   and on the front-end.

## Notes / limitations

- Only `umbracoPropertyData.textValue` is rewritten (where Nested Content stores its JSON). Both draft and
  published versions are converted. The published cache (`cmsContentNu` / NuCache) and Examine indexes are
  **not** touched — see step 5, you must rebuild them after `--apply`.
- **Re-runnable / idempotent:** a value that isn't a Nested Content array (e.g. already migrated) is left
  untouched, never overwritten. After a successful `--apply` the data types are already Block List, so a
  second run finds nothing to do.
- **All-or-nothing:** if any property value fails to convert, the entire transaction is rolled back — a data
  type is never committed as Block List while some of its values are still raw Nested Content. Every remaining
  value is still *attempted* first, so the log lists all of them rather than stopping at the first.
- **Every Nested Content data type is converted**, including ones with no `cmsPropertyType` reference — see
  [Converts EVERY Nested Content data type](#converts-every-nested-content-data-type-and-renames-them).
- **Inline editing mode** is enabled on every converted Block List data type
  (`useInlineEditingAsDefault: true`).
- **Nested Content inside Nested Content is NOT converted** — the inner value is copied verbatim and the
  count is reported as a warning. Convert those manually.
- Blocks whose element-type alias can't be resolved to an **element** content type are **skipped** and
  reported (not silently dropped) — fix the missing element type and re-run if you see those warnings.
- **Verified:** the written v13 value + config shape survives a real direct **13 → 17** upgrade (Umbraco's
  own migrations convert udi→key with no data loss). **Still unverified:** front-end rendering — validate on a
  restored copy before production.
