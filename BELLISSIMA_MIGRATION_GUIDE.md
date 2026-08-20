# Bellissima Migration Guide (AngularJS → Lit) — shared reference for all plugin agents

You are migrating one N3O Umbraco backoffice plugin from the **AngularJS** backoffice (Umbraco 13)
to the **Bellissima / Lit web component** backoffice (Umbraco 17). Backend C# API endpoints are
UNCHANGED — you are only rewriting the frontend and its registration.

## MUST read first (the canonical reference — a fully migrated plugin)
- `D:\AI Migration Test\Umbraco17Test\Umbraco17Test-1\wwwroot\App_Plugins\N3O.Umbraco.DynamicListViews\umbraco-package.json`
- `D:\AI Migration Test\Umbraco17Test\Umbraco17Test-1\wwwroot\App_Plugins\N3O.Umbraco.DynamicListViews\dynamic-list-view.js`
- `D:\AI Migration Test\Umbraco17Test\Umbraco17Test-1\wwwroot\App_Plugins\N3O.Umbraco.DynamicListViews\dynamic-list-view-condition.js`

Mirror this style exactly: vanilla JS (NOT TypeScript), ES module imports from `@umbraco-cms/backoffice/*`,
`LitElement` + `UmbElementMixin`, `static properties`/`static styles`, private `#methods`, `customElements.define`,
`export default`. No build step — plain `.js` shipped as-is.

## Core rules
1. **No TypeScript, no bundler.** Plain ES-module `.js` files using the Umbraco backoffice external imports
   (`@umbraco-cms/backoffice/external/lit`, etc.). These are import-mapped at runtime by Umbraco 17.
2. **One top-level component per file.** Keep file naming close to the original plugin name.
3. **Do NOT over-engineer.** Port the existing behaviour faithfully — same API calls, same fields, same UX.
   Do not add features, options, or abstractions that the AngularJS original did not have.
4. **Keep third-party libraries.** Files like `handsontable.full.min.js`, `cropperjs/`, `editorjs-*.js`,
   `formstone/` stay in place; import them as ES modules (`import '...'`) or load them dynamically. Do not
   rewrite or remove them.
5. **CSS:** prefer inlining the plugin's own CSS into the Lit component's `static styles = css\`...\``. If a
   third-party CSS file is large, you may load it. Use `var(--uui-...)` tokens where the reference does.
6. **Delete the AngularJS artefacts** you replace: `package.manifest`, `*.Controller.js`, AngularJS `*.html`
   views (the ones using `ng-controller`/`ng-*`/`{{ }}`). KEEP third-party libs and any `lang/` you convert.
7. Create exactly one `umbraco-package.json` per plugin folder, listing the extension(s).

## Backend endpoints
Read the AngularJS controller to find the exact `fetch`/`$http` URLs and request/response shapes. Reuse them
verbatim. If a `contentResource.getById(editorState.current.id)` or `editorState.current` pattern is used,
replace it by consuming the workspace context (see below) to get the current document key.

## Extension type cookbook

### A) Property editor UI  (`propertyEditorUi`)
For custom property editors (model.value + model.config in AngularJS).
```json
{
  "$schema": "../../umbraco-package-schema.json",
  "name": "N3O <Name>",
  "version": "1.0.0",
  "extensions": [
    {
      "type": "propertyEditorUi",
      "alias": "N3O.PropertyEditorUi.<Name>",
      "name": "N3O <Name>",
      "element": "/App_Plugins/<folder>/<file>.js",
      "meta": {
        "label": "N3O <Name>",
        "icon": "icon-document",
        "group": "common",
        "propertyEditorSchemaAlias": "Umbraco.Plain.Json"   // or Umbraco.Plain.String — match the stored data shape; omit if UI-only
      }
    }
  ]
}
```
Lit element implements the property-editor contract:
```js
import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UmbPropertyValueChangeEvent } from '@umbraco-cms/backoffice/property-editor';
// config (the prevalue/config) arrives as UmbPropertyEditorConfigCollection on the `config` property.

class N3o<Name>Element extends UmbElementMixin(LitElement) {
    static properties = {
        value: { type: ... },          // the editor value (was $scope.model.value)
        _state: { state: true },
    };
    #value;
    get value() { return this.#value; }
    set value(v) { this.#value = v; /* requestUpdate */ }

    // config: set by Umbraco; read prevalues with config.getValueByAlias('x')
    @property({ attribute: false }) config;   // if using decorators is awkward in plain JS, use a setter

    #onChange() {
        this.dispatchEvent(new UmbPropertyValueChangeEvent());   // tells Umbraco the value changed
    }
    render() { return html`...`; }
}
customElements.define('n3o-<name>', N3o<Name>Element);
export default N3o<Name>Element;
```
NOTE: in plain JS (no decorators) declare reactive props via `static properties = {...}` and read config via a
plain setter `set config(c){ this._config = c; }`. After mutating `value`, dispatch `new UmbPropertyValueChangeEvent()`.

### B) Workspace view  (was "Content App": `IContentAppFactory` + controller)
Use `workspaceView` (see the DynamicListViews reference). Get the current document key from the workspace context:
```js
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
// in constructor:
this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
    if (!context) return;
    this.observe(context.unique, (unique) => { if (unique) this.#init(unique); }, '_obsUnique');
});
```
umbraco-package.json:
```json
{
  "type": "workspaceView",
  "alias": "N3O.WorkspaceView.<Name>",
  "name": "N3O <Name>",
  "element": "/App_Plugins/<folder>/<file>.js",
  "meta": { "label": "<Tab Label>", "pathname": "<slug>", "icon": "icon-..." },
  "conditions": [ { "alias": "Umb.Condition.WorkspaceAlias", "match": "Umb.Workspace.Document" } ]
}
```
If the content app should only show for certain nodes, also add a custom `condition` extension (see the
reference `dynamic-list-view-condition.js` using `UmbConditionBase` + a backend check).

### C) Dashboard  (`dashboard`)
```json
{
  "type": "dashboard",
  "alias": "N3O.Dashboard.<Name>",
  "name": "N3O <Name>",
  "element": "/App_Plugins/<folder>/<file>.js",
  "meta": { "label": "<Label>", "pathname": "<slug>" },
  "conditions": [ { "alias": "Umb.Condition.SectionAlias", "match": "Umb.Section.Content" } ]
}
```
For an iframe-wrapper dashboard (Scheduler/Hangfire), the Lit element just renders an `<iframe src="...">`
filling the host. For an (almost) empty welcome dashboard, render the static HTML in the Lit template.

### D) Block editor custom view  (`blockEditorCustomView`) — Blocks.Preview
```json
{
  "type": "blockEditorCustomView",
  "alias": "N3O.BlockCustomView.Preview",
  "name": "N3O Block Preview",
  "element": "/App_Plugins/<folder>/<file>.js"
}
```
The element receives `content`, `settings`, `config` properties from the block editor. Implement
`UmbBlockEditorCustomViewElement` shape. Port the preview fetch + render-html logic from the AngularJS
controller (it called a backend preview endpoint and bound the returned HTML).

### E) Entry-point script / bundle (Blazor.BackOffice)
If the existing JS is plain (non-AngularJS) loader code, register it as a backoffice entry point:
```json
{ "type": "bundle", "alias": "N3O.Bundle.<Name>", "name": "N3O <Name>", "js": "/App_Plugins/<folder>/<file>.js" }
```
Keep the JS; just replace `package.manifest` with `umbraco-package.json`. Verify the JS uses no AngularJS.

## Localization (`lang/*.xml`)
If a plugin had `lang/en.xml`, you can convert it to a `localization` extension, OR (simpler, no behaviour
change) hard-code the few English strings inline. Do not block on this — note it in your report.

## What to verify before finishing
- The `umbraco-package.json` is valid JSON and `element`/`js` paths point to files that exist in the folder.
- No remaining `angular`, `$scope`, `ng-controller`, `assetsService`, `editorState` references in shipped files.
- Imports only reference real `@umbraco-cms/backoffice/*` subpaths used by the reference (lit, element-api,
  property-editor, document, data-type, components, extension-registry). If you need another, prefer the ones
  the reference uses; flag anything uncertain in your report rather than inventing an import path.
- The old `package.manifest` and AngularJS controller/view files are deleted.

## Report back (structured)
Return: plugin name, extension type(s) chosen, files created, files deleted, third-party libs kept,
backend endpoints used, anything uncertain/blocked, and any import paths you are not 100% sure exist.
