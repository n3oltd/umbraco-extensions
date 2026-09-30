# media-migrate — N3O Cropper/Uploader → native Umbraco editors

Standalone console tool (SQL Server, **Umbraco 17**) that migrates existing **N3O Cropper** and **N3O Uploader**
property data to native Umbraco editors, after the N3O.Umbraco package build that removes those custom editors
has been deployed.

There are **two targets**, chosen with `--target`:

| | `inline` (default) | `mediapicker` |
|---|---|---|
| Cropper becomes | `Umbraco.ImageCropper` | `Umbraco.MediaPicker3` |
| Uploader becomes | `Umbraco.UploadField` | `Umbraco.MediaPicker3` |
| Where the file lives | **on the property**, same as the N3O editors | media library node (created by this tool) |
| Media nodes created | **none** | one per distinct file |
| `/media/...` paths | unchanged, verbatim | unchanged, verbatim (no file is moved) |
| Alt text | **lost** — no slot on either editor | kept as the media node's name |
| Source image width/height | **lost** — `ImageCropperValue` has no slot | on the media node (`umbracoWidth/Height`) |
| Files reusable/manageable in the Media section | no | yes |
| Read type in models | `ImageCropperValue` / `string` | `MediaWithCrops` |

`inline` is the closer structural match — the retired N3O editors also kept the path on the property with no
media node, so nothing is invented and the whole "media node has no published-cache row" failure class does not
arise. Its cost is the two columns of data loss above. `mediapicker` costs a media node per file but keeps alt
text and dimensions, and is what `N3O.Umbraco.Extensions`' own `IMediaUrl`/`InlineSvg` abstraction is typed
against.

It is the data-migration half of the Cropper/Uploader → native switch; the other half is the removal of the
`N3O.Umbraco.Cropper` and `N3O.Umbraco.Uploader` projects from this repository. It mirrors the design of the sibling
`N3O.Umbraco.NestedContentMigration.Cli` (`nc-migrate`): one transaction, `--dry-run`/`--apply`, raw SQL via
`Microsoft.Data.SqlClient`, and a per-item `[REVIEW]` log.

## What it does (per stored value)

Both editors stored raw files on `MediaFileManager.FileSystem` at `/media/{nodaTimeTicks}/{filename}` with **no
media node**. Whichever target is chosen, that path is reused verbatim and **no file is ever moved or copied**.

Crop rectangles are converted from **absolute pixels to relative edge insets** for both targets: `x1/y1/x2/y2`
are the fractions cropped off the left/top/right/bottom, which is what Umbraco passes straight to ImageSharp as
`cc=left,top,right,bottom`. The crop aliases and sizes come from the old data type's `cropDefinitions`.

### `--target inline`

1. **Cropper value** → `Umbraco.ImageCropper`: `{ "src", "crops":[{alias,width,height,coordinates}], "focalPoint":null }`.
2. **Uploader value** → `Umbraco.UploadField`: the **plain path string**.
3. **Flips the data type** to `Umbraco.ImageCropper` (`Umb.PropertyEditorUi.ImageCropper`) with
   `{"crops":[{alias,width,height}]}`, or to `Umbraco.UploadField` (`Umb.PropertyEditorUi.UploadField`) with the
   old `allowedExtensions` string converted to a bare lower-case `fileExtensions` array (`".png, .jpg"` →
   `["png","jpg"]`). `maxFileSizeMb`, `imagesOnly` and `altTextRequired` have no native equivalent and are dropped.

Note Umbraco's `ImageCropperConfigurationExtensions.ApplyConfiguration` **rebuilds the crop list from the data
type config on read**, matching by alias and keeping only the coordinates — so a crop alias that is not in the
data type config is discarded. That is why the same crop definitions are written to both the value and the config.

### `--target mediapicker`

1. **Registers the existing file as an Umbraco media node** — an `Image` (or `File`, by extension) whose
   `umbracoFile` reuses the existing path. Duplicate references to the same file reuse one media node.
2. **Rewrites the property value** to the single-item Media Picker shape
   `[{ "key", "mediaKey", "crops":[…], "focalPoint":null }]`, the converted crops carried as **local crops**.
3. **Flips the data type** to `Umbraco.MediaPicker3` (`Umb.PropertyEditorUi.MediaPicker`) with `multiple:false`
   and the Cropper crop definitions copied into its config.

## Usage

```
media-migrate --connection "<conn>" (--dry-run | --apply) [--editor cropper|uploader|both]
              [--target inline|mediapicker] [--media-parent <id>] [--verbose] [--log <path>]
```

- `--connection` SQL Server connection string (or set `MEDIA_MIGRATE_CONNECTION`).
- `--editor` `cropper` | `uploader` | `both` (default `both`).
- `--target` `inline` | `mediapicker` (default `inline`) — see the table above.
- `--media-parent` `--target mediapicker` only: `umbracoNode` id of the media folder for the new nodes
  (default `-1`, the Media root). Passing it with `--target inline` is **rejected** rather than ignored, since
  that target creates no media nodes at all.
- `--dry-run` run in a transaction then roll back (validates SQL + media inserts, reports what would change).
- `--apply` commit (mutually exclusive with `--dry-run`).
- `--verbose` log each data type / value / media node.
- `--log <path>` log file (default `media-migrate-<UTC>.log`).

### Standalone exe (no .NET on the target machine)

```
dotnet publish -c Release -p:StandaloneExe=true
```

Produces a single **self-contained** `bin\Release\net10.0\win-x64\publish\media-migrate.exe` (~40 MB) that
needs no .NET runtime, no SDK and no restore on the machine it runs on — copy that one file to the database
server and run it. The accompanying `.pdb` is symbols only and does not need to go with it.

The publish is not trimmed and must stay that way: `Microsoft.Data.SqlClient` resolves types by reflection, so
a trimmed build fails at `Open()` rather than at publish time. The target needs ICU (in-box on Windows 10
1903 / Server 2019 and later) because the tool cannot run under invariant globalization — see the `.csproj`.

## Per-site procedure (IMPORTANT)

1. **Back up the database AND the media store** (disk/blob). File registration + value rewrite are irreversible;
   the only rollback after `--apply` is a restore.
2. Deploy the N3O.Umbraco build that removes the custom editors.
3. Take the site **offline** (one long transaction; new media nodes + value rewrites).
4. Run with **`--dry-run --verbose`** against a **copy** of production; review the `[REVIEW]` blocks (parse
   failures, dropped alt-text, crops without coordinates).
5. Run **`--apply`**.
6. **Caches**: the published cache is now invalidated **by the tool** (see below) so it rebuilds itself on the
   next site start. Still delete the on-disk `NuCache.*.db` first, and rebuild the **Examine** indexes afterwards.
7. Regenerate ModelsBuilder models + fix any site code still referencing the removed `CroppedImage`/`FileUpload`
   types, then `uSync export` and commit. Under `--target inline` they become `ImageCropperValue` and `string`;
   under `--target mediapicker` both become `MediaWithCrops`. Watch for the members the N3O types had that no
   native type does: `.AltText` (both targets), `.Crop`/the alias indexer, and `GetUncroppedImage().Width/Height`
   (there is no source-dimension slot on `ImageCropperValue`).
8. The old SHA1-named pre-generated Cropper crop files under `/media/{ticks}/` are now dead — safe to delete.

## Multiple crops per data type

A Cropper data type could define several crops (`cropDefinitions`), and the stored value held one **positional**
rectangle per definition - index *i* of `crops` belongs to `cropDefinitions[i]`. All definitions are carried into
`ImageCropperConfiguration.crops`, and each stored rectangle is converted against the definition at its own
index, so a two-crop data type produces two aliased crops with their own coordinates:

```json
{"src":"/media/17706258337433602/112.png","crops":[
  {"alias":"x","width":230,"height":70, "coordinates":{"x1":0.0,   "y1":0.3,"x2":0.0,   "y2":0.3}},
  {"alias":"y","width":110,"height":110,"coordinates":{"x1":0.1949,"y1":0.1,"x2":0.1949,"y2":0.1}}],
 "focalPoint":null}
```

Because the mapping is positional it assumes the stored order still matches the config order; a crop definition
inserted or reordered after values were saved would shift them. Fewer rectangles than definitions leaves the
extra crops with `coordinates: null` (a centre crop, reported as "crops w/o coords"); more are ignored. All
crop definitions **must** go into the data type config, because Umbraco's `ApplyConfiguration` rebuilds the
crop list from it on read and discards any alias the config does not list.

The old value's `rotate` / `scaleX` / `scaleY` per rectangle, and its separate `cropBoxes` array (backoffice UI
state), have no `ImageCropperValue` equivalent and are not carried. On one large production site that
costs nothing: of 775
top-level Cropper values, **every one had exactly one rectangle, none had `rotate != 0`, and none had a scale
other than 1**.

## Also normalises legacy nested block shapes

Umbraco's 13→17 upgrade rewrites the block values of properties it owns, but it never traverses **into**
another editor's value. A Block List / Block Grid nested inside a **Perplex ContentBlocks** value therefore
keeps the Umbraco 13 "udi" shape forever — properties keyed by alias, a lower-case `layout`, and a `udi` per
`contentData` entry instead of a `key`. v17 still reads it, so the content renders, but it leaves an
un-upgraded value shape in the database that may not survive a future Umbraco major.

`NestedBlockShapeNormalizer` rewrites those to the v14+ key-based shape, taken verbatim from a top-level value
that Umbraco itself upgraded in the same database:

```json
{
  "contentData": [ { "contentTypeKey", "key", "values": [ {editorAlias, culture, segment, alias, value} ] } ],
  "settingsData": [ ... ],
  "expose":      [ { "contentKey", "culture": null, "segment": null } ],
  "Layout":      { "Umbraco.BlockList": [ { "contentUdi", "settingsUdi", "contentKey", "settingsKey" } ] }
}
```

Note `Layout` is **capitalised** and keeps `contentUdi` alongside the new `contentKey` — that is what Umbraco
produces, so it is reproduced exactly rather than "cleaned up".

It runs **last**, after the media passes, for two reasons: the target shape is only knowable post-upgrade, and
each value entry needs an `editorAlias`, which is read from the **live** `umbracoDataType` rows so the aliases
written are the v17 ones (including the `Umbraco.MediaPicker3` aliases this tool has just written) rather than
stale v13 names. Values already in the key-based shape are left untouched, so the pass is re-runnable.

On one large production site: **5,340 legacy block values normalised** — exactly the number of nested Nested Content properties
`nc-migrate` converts inside Perplex blocks. Verified afterwards that **zero** populated legacy `"udi"` keys
remain anywhere, nested or top-level (the 1,660 remaining `"udi": null` occurrences are Perplex's own v4
shape, which is correct).

## Notes / limitations

- **Umbraco 17 schema only** — refuses to run if `umbracoDataType.propertyEditorUiAlias` / `umbracoMediaVersion`
  are absent.
- **The published cache is invalidated automatically (fixed 2026-08-27).** This tool writes the relational rows
  (`umbracoNode`/`umbracoContent`/`umbracoContentVersion`/`umbracoMediaVersion`/`umbracoPropertyData`) but not the
  serialized published cache (`cmsContentNu`). The new media nodes therefore had no cache row, and an Umbraco 17
  site **terminated during startup** in `MediaCacheService.SeedAsync` with
  `InvalidOperationException: No data for media <id>` — before the backoffice was reachable, so the old
  "rebuild it from the backoffice" instruction was impossible to follow. The tool now deletes Umbraco's stored
  cache-serializer marker (`umbracoKeyValue` key `Umbraco.Web.PublishedCache.NuCache.Serializer`) inside the same
  transaction, which makes Umbraco's own `DatabaseCacheRebuilder.RebuildDatabaseCacheIfSerializerChanged` rebuild
  the entire cache on the next start using Umbraco's serializer. Verified end to end on a real migrated v17 site:
  the site started, zero `No data for media` errors, and `cmsContentNu` rebuilt to 3,535 rows.
  **Do not try `DELETE FROM cmsContentNu` instead** — neither Umbraco 13 nor 17 rebuilds an empty cache: v13
  throws the same `No data for media`, and v17 only starts with cache seeding disabled, after which every request
  fails with `There is no PublishedContent`.
- **Alt text** has no native media-picker slot; it is used as the new media node's name and logged as `[REVIEW]`
  so you can re-apply it manually where it matters.
- **Image metadata** (`umbracoWidth/Height/Bytes/Extension`) is set best-effort from the stored value; Umbraco
  recomputes it if the media item is re-saved.
- Files that no longer exist on disk/blob are not detected here — they surface as broken media after the rebuild.
- **Nested Cropper/Uploader values ARE converted (added 2026-08-27).** A value stored *inside* another
  editor's value — a Block List / Block Grid block or a Perplex ContentBlocks block — is not an
  `umbracoPropertyData` row, so the main pass never saw it even though it had already flipped that
  element property's data type to `Umbraco.MediaPicker3`. The page then died with
  `System.Text.Json.JsonException: The JSON value could not be converted to IEnumerable<...MediaWithCropsDto>.
  Path: $ | LineNumber: 0`. `NestedMediaMigrator` now walks every block value recursively, to any depth,
  converting nested values through the same parse → media-node → picker-value pipeline. **Two nested shapes
  exist and both are handled:**
  1. **Post-upgrade shape** — `{contentTypeKey, key, values:[{editorAlias, culture, segment, alias, value}]}`.
     Identified by the entry's stale `editorAlias`, which is also rewritten to `Umbraco.MediaPicker3`.
  2. **Legacy Umbraco 13 udi shape** — `{contentTypeKey, udi, <alias>: <value>}`, properties keyed by alias
     with **no `editorAlias` at all**. This survives inside a Perplex value because Umbraco's 13→17 upgrade
     does not traverse another editor's value. It is resolved through a `(element content-type key, property
     alias)` map captured **before** the data types are flipped — once `propertyEditorAlias` becomes
     `Umbraco.MediaPicker3` the Cropper/Uploader binding, and with it the crop definitions, is gone.

  Nested values are written back as serialized JSON strings, matching how Umbraco stores every other nested
  editor value in a block. `--editor cropper|uploader` scope is honoured, so an out-of-scope editor's nested
  values are left alone rather than broken. On one large production site: **16,385 nested values
  converted, 1,089 empty
  `editorAlias` entries corrected, 1,864 media nodes created** (only 250 before this fix).
- A failed value conversion rolls back the whole run, so nothing is left half-migrated — but every other value
  and every later pass is still **attempted** first, so one `--dry-run` reports every problem in the database
  instead of stopping at the first one. Nothing commits until the end, so continuing past a failure is free.
- **Crop rectangles with no crop definition left to name them are dropped and counted.** The retired Cropper
  stored its rectangles *positionally* — the i-th rectangle belongs to the i-th entry of the data type's
  `cropDefinitions`, with no alias on the rectangle itself. If crop definitions were removed from the data type
  after a value was saved, the surplus rectangles have no alias to be written under; they appear as
  `Crop rects DROPPED` in the summary and as a `[REVIEW]` line naming the item.
- **A value whose shape this tool cannot parse keeps its data, but still gets its `editorAlias` corrected.**
  Leaving a nested entry pointing at `N3O.Umbraco.Cropper` after its data type has been flipped would hand the
  native value editor the wrong shape; the value itself is left for you to convert by hand and reported as
  `[REVIEW]`.
- **Block property editor aliases are resolved across compositions.** Normalising a legacy block value has to
  stamp each property with its `editorAlias`, which Umbraco uses to pick the value editor. Element types used
  inside blocks compose heavily (a block item's `linkText`/`linkContent` usually comes from a shared `Link`
  element type), so resolving only the properties declared directly on the content type leaves every inherited
  property with `"editorAlias": null` and stops Umbraco rendering it. Measured on one production site before
  this was fixed: **6,926 values across 8 inherited aliases**. A property that is still unresolvable after
  compositions **and** holds a non-null value is reported as `[REVIEW]` — that means the property was renamed or
  deleted after the value was saved (1,148 such values on the same site, e.g. `dimension1` where the element
  type now declares `dimension1DataList`). Unresolvable properties holding `null` are carried across silently;
  there is nothing to do about an inert null and reporting them buried the real ones 3:1.
- **Block Grid layout metadata survives normalisation.** A Block Grid layout item carries `columnSpan`,
  `rowSpan` and an `areas` array holding a whole nested item tree. Values and layout items are cloned and then
  adjusted rather than rebuilt from a fixed key list, so members this tool does not model are preserved, and
  nested `areas[].items[]` get the same key-based fields as top-level items.
