# Cropper / Uploader → Native Media Picker — Migration Runbook

*Created 2026-06-15 (session 18). Companion to the editor removal landed on `v17-Talha`.*

## Context

The custom N3O property editors **`N3O.Umbraco.Cropper`** and **`N3O.Umbraco.Uploader`** were AngularJS-era
editors that, on Umbraco 17, depended on a bundled cropperjs/Formstone + global **jQuery** (which the Bellissima
backoffice no longer provides), leaving uploads broken at runtime. The migration decision (Talha, 2026-06-15) was:

> **Switch to Umbraco's native pickers, hard-replace the read-side types with native types, adopt native behavior
> (on-the-fly ImageSharp crop URLs instead of pre-generated crop files; alt-text carried onto the media node), and
> run the existing-data migration as a separate offline CLI** — exactly the pattern already chosen for the
> NestedContent → BlockList migration (`MIGRATION_BLOCKERS.md` BLOCKER-06; the on-startup plan was removed in
> commit `635b54329` and extracted to a sibling CLI app).

The **framework-side editor removal** (delete the 6 plugin projects, refactor in-repo consumers to native
`MediaWithCrops`, drop the Cropper entry from the `N3O.Umbraco.Data` import/export pipeline, switch DemoSite's
Uploader data types to native `Umbraco.MediaPicker3`) is done in-repo and build-verified.

**This document covers the remaining half: the per-site DATA migration**, which cannot run in this library repo
because it requires the Umbraco runtime (`IMediaService`) to create media nodes. It is run once per consuming site.

## Why a code-only/SQL-only migration is not enough

Both editors stored **raw files directly on `MediaFileManager.FileSystem`** (disk `~/media/` or Azure Blob behind
`Umbraco.StorageProviders.AzureBlob`) at path `/media/{nodaTimeTicks}/{filename}` with **no Umbraco media node**.
The stored property value is N3O JSON, not a media reference:

- **Uploader** (`ValueTypes.Json`, alias `N3O.Umbraco.Uploader`):
  ```json
  { "altText": "...", "extension": ".png", "filename": "logo.png", "sizeMb": 0.12, "urlPath": "/media/638.../logo.png" }
  ```
- **Cropper** (`ValueTypes.Json`, alias `N3O.Umbraco.Cropper`):
  ```json
  { "src": "/media/638.../photo.jpg", "mediaId": "638...", "filename": "photo.jpg", "width": 1920, "height": 1080,
    "altText": "...", "crops": [ { "x": 100, "y": 50, "width": 1600, "height": 900 } ] }
  ```
  The `crops` array is **positional** — index *i* maps to the *i*-th `CropDefinition` (label/alias/width/height)
  on the data type's configuration, and the coordinates are **absolute pixels**.

Native `Umbraco.MediaPicker3` references **registered media-library nodes by GUID**:
```json
[ { "key": "<item-guid>", "mediaKey": "<media-node-guid>", "mediaTypeAlias": "Image",
    "focalPoint": null, "crops": [ { "alias": "thumbnail", "width": 200, "height": 200,
    "coordinates": { "x1": 0.1, "y1": 0.1, "x2": 0.9, "y2": 0.9 } } ] } ]
```

So every existing file must first be **registered as a media node**, then each property value **rewritten** to the
native shape. The files themselves do not move — they already live in the same `MediaFileManager` backing store
Umbraco uses for its media library.

## The migration (per site) — `media-migrate` CLI ✅ BUILT

A standalone console app **`N3O.Umbraco.MediaEditorMigration.Cli`** (assembly `media-migrate`) now exists as a
sibling of this repo at **`D:\AI Migration Test\Cropper Uploader Migration\`** (built 2026-06-15, session 18; build
0 errors). It mirrors the `N3O.Umbraco.NestedContentMigration.Cli` (`nc-migrate`) pattern: pure raw SQL via
`Microsoft.Data.SqlClient`, a single transaction, `--dry-run`/`--apply`, schema detection, and a per-item
`[REVIEW]` log. Flags: `--connection <cs>`, `--editor cropper|uploader|both` (default both), `--media-parent <id>`,
`--dry-run|--apply`, `--verbose`, `--log <path>`. It creates media nodes by writing the relational rows directly
(rather than booting Umbraco), so — exactly like `nc-migrate` — **caches must be rebuilt after `--apply`**. See that
project's `README.md` for the full procedure; the design below documents what it does.

### Phase 1 — discover affected data types & properties (raw SQL, v17 schema)
```sql
-- data types using the custom editors
SELECT nodeId AS Id, config FROM umbracoDataType
WHERE propertyEditorAlias IN ('N3O.Umbraco.Cropper', 'N3O.Umbraco.Uploader');

-- property types bound to those data types
SELECT id, dataTypeId, Alias FROM cmsPropertyType WHERE dataTypeId IN (@dataTypeIds);

-- stored values to migrate
SELECT id, propertyTypeId, textValue FROM umbracoPropertyData
WHERE propertyTypeId IN (@propertyTypeIds) AND textValue IS NOT NULL AND textValue != '';
```
> v17 schema reminders (same as the NC→BlockList port): `umbracoDataType` PK is **`nodeId`** (not `id`) and has a
> **`propertyEditorUiAlias`** column; there is no `umbracoContentType` table — content types are `cmsContentType`
> joined to `umbracoNode` for the GUID key.

### Phase 2 — register each distinct file as a media node (`IMediaService`)
For each distinct `mediaId/filename` (or `urlPath`):
1. Resolve the physical file via the existing bridge `MediaFileManagerExtensions.GetSourceFile(mediaId)`
   (`Plugins/N3O.Umbraco.Plugins/Extensions/MediaFileManagerExtensions.cs`) — it returns the most-recent file
   under the `mediaId` folder.
2. `IMediaService.CreateMedia(name, parentId, mediaTypeAlias)` — `"Image"` for images, `"File"` otherwise.
3. Set the `umbracoFile` property to an `ImageCropperValue` (images) / file path (files) pointing at the **existing**
   path (no copy needed — the file is already in `MediaFileManager.FileSystem`). Set width/height (images) and
   `umbracoBytes`/`umbracoExtension` as Umbraco expects.
4. Carry **`altText`** onto the media node (e.g. `umbracoFile` alt or a description property) — native picker JSON
   has no alt-text slot.
5. `IMediaService.Save(media)` → capture the new **media node GUID**.
   Maintain a `urlPath → mediaKey` map so duplicate references to the same file reuse one node.

### Phase 3 — rewrite each property value & flip the data type
Inside a transaction (rolled back under `--dry-run`):
- **Uploader →** `[{ "key": <new-guid>, "mediaKey": <mediaKey>, "mediaTypeAlias": "Image"|"File" }]`
- **Cropper →** one media item with `crops[]` built from the positional `crops` + the data type's `CropDefinition`
  aliases, converting **absolute px → relative fractions**:
  `x1 = x / width`, `y1 = y / height`, `x2 = (x + cropWidth) / width`, `y2 = (y + cropHeight) / height`
  (clamp to [0,1]). Circle crops collapse to their bounding rectangle. Focal point: default `{0.5,0.5}` (or derive).
- Flip the data type:
  ```sql
  UPDATE umbracoDataType
  SET propertyEditorAlias = 'Umbraco.MediaPicker3',
      propertyEditorUiAlias = 'Umb.PropertyEditorUi.MediaPicker',
      config = @nativeConfigJson
  WHERE nodeId = @dataTypeNodeId;
  ```
  `@nativeConfigJson` = `{"filter":"","multiple":false,"startNodeId":null,"ignoreUserStartNodes":false,`
  `"enableLocalFocalPoint":false,"crops":[<cropDefs for Cropper>],"validationLimit":{"min":0,"max":1}}`.
- `UPDATE umbracoPropertyData SET textValue = @nativeJson WHERE id = @id;`

### Phase 4 — post-migration
- Regenerate **ModelsBuilder** models on the site (`SourceCodeManual`) and re-commit: `File`/`Logo`-style properties
  become `MediaWithCrops` (single) / `IEnumerable<MediaWithCrops>` (multiple).
- Update any site code still casting to the removed `FileUpload` / `CroppedImage` types, and rewrite any
  `OpenGraphImageCropperHandler` subclasses to read crop URLs from `MediaWithCrops` (native on-the-fly crop URLs).
- Run `uSync export` and commit (data types/content types now reference the native editor).
- The old SHA1-named pre-generated Cropper crop files under `/media/{ticks}/` are now dead — safe to delete.

## Per-site checklist
- [ ] **Back up the database AND the media store** (disk/blob) — file registration + value rewrite are irreversible.
- [ ] Run the CLI with `--dry-run` against a **copy** of the production DB; review the report (counts, any
      unmatched/missing files, circle-crop collapses).
- [ ] Run for real in a maintenance window.
- [ ] Regenerate ModelsBuilder models + fix site C# consumers; rebuild.
- [ ] Verify rendered output before/after in staging (images, crops, focal points, downloadable files).
- [ ] `uSync export` → commit.
- [ ] Delete orphaned pre-generated crop files.

## In-repo consequence already accepted
The `N3O.Umbraco.Data` import/export pipeline no longer has a `Cropper` `PropertyType` (the `CropperValueReq/Res`
structured import/export is gone). Content using the native media editors is not handled by the structured Data
import/export beyond the generic converters; adding native-media support to that pipeline is a separate feature if
needed.
