# `@n3o/backoffice-core` — The Shared Backoffice Foundation

## What it is and why it exists

Every N3O backoffice plugin needs to talk to HTTP API controllers that are protected with `[Authorize]`.
In Umbraco 17 the backoffice authenticates with a **bearer token** (OAuth 2.0 / OpenID Connect), not a browser
cookie. A plain `fetch()` call therefore gets a `401 Unauthorized` because the token is never included.

`@n3o/backoffice-core` solves that once, centrally:

1. **`createAuthFetch`** — a factory that returns a `fetch` wrapper pre-loaded with the current bearer token and
   the correct `credentials` setting.
2. **`UmbAuthFetchMixin`** — a TypeScript mixin that wires any Umbraco element to the auth context so it always
   has a ready-to-use `authFetch`.
3. **`WorkspaceVisibilityCondition`** — a reusable backoffice *condition* that calls a manifest-configured HTTP
   endpoint and shows or hides an extension (workspace view, content app, etc.) based on the typed
   `{ visible: boolean }` response.

The package is the **Tier A** shared runtime: it is compiled to a single file (`auth-fetch.js`) and exposed via
an **import-map entry** so every other plugin can write:

```ts
import { createAuthFetch, UmbAuthFetchMixin } from '@n3o/backoffice-core';
```

and the browser resolves that bare specifier to the one already-loaded copy.

> **Concept reminders:** [ES modules and import maps](../concepts/04-es-modules-and-import-maps.md) explains bare
> specifiers and import maps. [Umbraco backoffice extensions](../concepts/09-umbraco-backoffice-extensions.md)
> explains what a *condition* extension is and how extensions are registered.

---

## Files at a glance

| Path (relative to `src/`) | What it is |
|---|---|
| `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/package.json` | npm package descriptor; declares the `@n3o/backoffice-core` name and the dev-time `exports` map |
| `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/tsconfig.json` | TypeScript config — extends the shared `@n3o/build/tsconfig` base |
| `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/vite.config.ts` | Vite build config — uses the shared `n3oPluginConfig` helper; maps two source files to two output files |
| `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts` | Source: `createAuthFetch` factory + `UmbAuthFetchMixin` mixin |
| `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/workspace-visibility-condition.ts` | Source: `WorkspaceVisibilityCondition` backoffice condition class |
| `N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/umbraco-package.json` | Umbraco extension manifest: registers the condition + the import-map entry |
| `N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js` | **Compiled output** of `auth-fetch.ts` (committed; served at runtime) |
| `N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/workspace-visibility-condition.js` | **Compiled output** of `workspace-visibility-condition.ts` |

---

## Config files

### `package.json`

```json
{
    "name": "@n3o/backoffice-core",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "exports": {
        ".": "./src/auth-fetch.ts"
    },
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "devDependencies": {
        "@n3o/build": "*"
    }
}
```

Key points:

- **`"name": "@n3o/backoffice-core"`** — this is the bare specifier other packages write in their `import`
  statements. The npm workspace (root `src/package.json`) resolves it to this local package via a symlink in
  `node_modules/@n3o/backoffice-core`. Think of it like a NuGet package name that is resolved from a local
  project reference.
- **`"type": "module"`** — all `.js` files in this package are treated as ES modules (i.e. `import`/`export`,
  not `require`). See [ES modules](../concepts/04-es-modules-and-import-maps.md).
- **`"exports": { ".": "./src/auth-fetch.ts" }`** — the `exports` map is Node's way of saying "when something
  imports `@n3o/backoffice-core` (the root `.` export), give it `./src/auth-fetch.ts`." This is the
  **dev-time** resolution used by the TypeScript compiler and Vite during local development. At runtime the
  browser never sees this map; it uses the import map in `umbraco-package.json` instead.
- **`"private": true`** — the package cannot be published to any registry. All N3O packages in this workspace
  are private.
- **`"devDependencies": { "@n3o/build": "*" }`** — `@n3o/build` is the shared build-config package at
  `N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/`. The `*` version range means "whatever version is in the
  workspace" (always the local copy).

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "include": ["src"]
}
```

There is almost nothing here because all real configuration lives in the shared base. `"extends"` is analogous
to inheriting from a base class. `"include": ["src"]` tells the compiler to only check files under the `src/`
folder.

The shared base (`N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json`) sets:

- `"target": "ES2022"` — compile to modern JavaScript (no IE polyfills).
- `"module": "ESNext"` / `"moduleResolution": "bundler"` — use `import`/`export` syntax; resolve imports the
  way Vite does (supports bare specifiers, package `exports` maps, etc.).
- `"strict": true` — full TypeScript strict mode (think `nullable reference types` on in C#).
- `"noEmit": true` — `tsc` is run only as a **type-checker**; it never writes `.js` files. Vite does the
  actual compilation.
- `"types": ["@umbraco-cms/backoffice/extension-types"]` — automatically pulls in Umbraco's global ambient
  type declarations (extension element registries, etc.) without needing an explicit `import`.

> See [Vite and the build](../concepts/05-vite-and-the-build.md) for why `noEmit` is normal, and
> [TypeScript for C# devs](../concepts/02-javascript-typescript-for-csharp-devs.md) for `strict` mode.

### `vite.config.ts`

```ts
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.BackofficeCore/auth-fetch': 'src/auth-fetch.ts',
        'N3O.Umbraco.BackofficeCore/workspace-visibility-condition': 'src/workspace-visibility-condition.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
});
```

`n3oPluginConfig` is a thin wrapper around Vite's `defineConfig` defined in
`N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`. Its full expansion:

```js
export function n3oPluginConfig(options) {
    const { entries, outDir, react = false, additionalExternals = [], sourcemap = true } = options;

    const external = [/^@umbraco/];   // <-- key: all @umbraco-cms/* packages are EXCLUDED from the bundle
    // ...
    return defineConfig({
        build: {
            lib: { entry: entries, formats: ['es'] },
            outDir,
            emptyOutDir: false,
            sourcemap,
            rollupOptions: {
                external,
                output: { entryFileNames: '[name].js' },
            },
        },
    });
}
```

Important points:

- **`external: [/^@umbraco/]`** — any import whose specifier starts with `@umbraco` is treated as external.
  Vite/Rollup leaves those `import` statements in the output file unchanged; the browser resolves them at
  runtime from Umbraco's own import map. This is essential: bundling Umbraco code into N3O's files would create
  duplicate class instances and break context lookups. The analogy in .NET is marking an assembly reference as
  `CopyLocal = false` because the host process already loads it.
- **`formats: ['es']`** — output is ESM only (no CommonJS, no UMD). Required for backoffice plugins.
- **`emptyOutDir: false`** — Vite does not delete the output directory before building. This is important
  because multiple plugins all write into the same `wwwroot/App_Plugins/` folder tree.
- **`entryFileNames: '[name].js'`** — the entry key (e.g. `N3O.Umbraco.BackofficeCore/auth-fetch`) becomes the
  output filename, so the output is `wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js`.
- **Two entries** — the package produces two independent output files. `auth-fetch.js` is the shared library;
  `workspace-visibility-condition.js` is the condition extension. They are separate because other plugins only
  need to import `auth-fetch.js`; the condition is loaded only by Umbraco when it sees the manifest.

> See [Vite and the build](../concepts/05-vite-and-the-build.md) for a full explanation of library mode and
> entry points.

---

## `src/auth-fetch.ts` — line by line

```ts
import { UmbElementMixin, type UmbElement } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT, type UmbOpenApiConfiguration } from '@umbraco-cms/backoffice/auth';
```

Two imports from Umbraco's packages. These are **external** at runtime (not bundled in). Umbraco's own import
map makes `@umbraco-cms/backoffice/auth` available to the browser.

- `UmbElementMixin` — the base mixin that gives a web component `consumeContext`, `observe`, and other Umbraco
  hooks. Think of it as a base class or interface that all Umbraco elements implement. It is re-exported at the
  bottom of this file so consumers get it from one place.
- `UmbOpenApiConfiguration` — a TypeScript `interface` (think: a C# interface / `record`) that represents the
  current auth configuration. Its most important member is `token(): Promise<string | undefined>`, an async
  method that returns the current bearer token string, and `credentials: RequestCredentials`.
- `UMB_AUTH_CONTEXT` — a *context token* (a typed key used with Umbraco's dependency injection system for
  web components). See [Umbraco backoffice extensions](../concepts/09-umbraco-backoffice-extensions.md) for a
  full explanation of context tokens.

```ts
export type AuthFetch = (input: string, init?: RequestInit) => Promise<Response>;
```

A named type alias (like a C# `delegate`) describing the shape of a `fetch`-compatible function: takes a URL
string and optional `RequestInit` options, returns a `Promise<Response>`. Exporting it lets other files
type-check their `authFetch` fields without repeating the signature.

```ts
type Constructor<T = object> = new (...args: any[]) => T;
```

A utility type used by the mixin. It says "a `Constructor<T>` is anything that can be called with `new` and
produces a `T`". This is the standard TypeScript idiom for writing mixins; you will see it again immediately
below.

### `createAuthFetch`

```ts
export function createAuthFetch(config: UmbOpenApiConfiguration): AuthFetch {
    return async (input, init = {}) => {
        const token = await config.token();
        const headers = new Headers(init.headers);

        if (token) {
            headers.set('Authorization', `Bearer ${token}`);
        }

        return fetch(input, { ...init, credentials: config.credentials, headers });
    };
}
```

This is a **factory function**: given a `UmbOpenApiConfiguration`, it returns a new `AuthFetch` function. The
returned function is a closure — it captures `config` from the outer scope. The C# analogue is a factory method
that captures a dependency and returns a `Func<string, Task<HttpResponseMessage>>` delegate.

Line by line:

- `return async (input, init = {}) => { ... }` — returns an `async` arrow function (a lambda, in C# terms).
  The `init = {}` default means you can call `authFetch('/api/foo')` without a second argument.
- `const token = await config.token()` — calls the async method on the auth config to get the current access
  token. This is called on every request so the token is always fresh (tokens expire; the auth context handles
  refresh internally).
- `const headers = new Headers(init.headers)` — creates a mutable `Headers` object, pre-seeded with any
  headers the caller already supplied. `Headers` is a browser built-in (part of the Fetch API).
- `if (token) { headers.set('Authorization', \`Bearer ${token}\`) }` — conditionally adds the `Authorization`
  header. The guard means the function degrades gracefully if called before the user is authenticated.
- `return fetch(input, { ...init, credentials: config.credentials, headers })` — calls the real browser
  `fetch`. `...init` spreads the caller's options; `credentials` and `headers` are overridden last so they
  always win. `config.credentials` is typically `'include'` so cookies are also sent (some Umbraco internal
  endpoints still use cookies alongside the bearer token).

**Why this exists:** A plain `fetch('/umbraco/backoffice/api/foo')` gets a `401` because the backoffice's
`[Authorize]` attribute checks for a bearer token in the `Authorization` header. The token is held by
`UMB_AUTH_CONTEXT` inside the Umbraco SPA, not in a browser cookie. This wrapper bridges that gap.

### `UmbAuthFetchMixin`

```ts
export const UmbAuthFetchMixin = <T extends Constructor<UmbElement>>(superClass: T) =>
    class extends superClass {
        authFetch: AuthFetch | null = null;

        authFetchChanged?(authFetch: AuthFetch | null): void;

        constructor(...args: any[]) {
            super(...args);

            this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
                this.authFetch = authContext ? createAuthFetch(authContext.getOpenApiConfiguration()) : null;
                this.authFetchChanged?.(this.authFetch);
            });
        }
    };
```

**What is a TypeScript mixin?**

A mixin is a function that takes a class as input and returns a new class that extends it, adding behaviour.
The signature is always the same pattern:

```ts
const MyMixin = <T extends Constructor<Base>>(superClass: T) =>
    class extends superClass { /* added members */ };
```

In C# there is no direct equivalent because C# classes have single inheritance and no first-class way to inject
members into a class hierarchy. The closest analogy is a **partial class** or a **decorator attribute** that
augments a class at compile time — but mixins are more powerful because multiple mixins can be composed in a
chain: `class Foo extends MixinB(MixinA(UmbElementMixin(LitElement))) { }`.

Applied to `UmbAuthFetchMixin`:

- `<T extends Constructor<UmbElement>>` — the type parameter `T` must be a constructor that produces something
  that implements `UmbElement`. This is a constraint (like `where T : UmbElement` in C#) that ensures `this`
  inside the mixin body has all of `UmbElement`'s methods (including `consumeContext`).
- The returned anonymous class has two new members:
  - **`authFetch: AuthFetch | null = null`** — a public field (initialized to `null`). Type `AuthFetch | null`
    is a union type: the value is either an `AuthFetch` function or `null`. In C# terms: `AuthFetch? authFetch`.
  - **`authFetchChanged?(authFetch: AuthFetch | null): void`** — an *optional method* (the `?` makes it
    optional, not abstract). A subclass may define `authFetchChanged` to receive a callback whenever the auth
    fetch function is rebuilt. If the subclass does not define it, the `?.` call at the bottom silently skips
    it. The C# analogue is a virtual method with an empty default body.
- In the `constructor`:
  - `super(...args)` — forwards all constructor arguments to the superclass. Always required in a JS
    constructor that calls `super`.
  - `this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => { ... })` — registers a subscription with
    Umbraco's context system. Whenever the auth context is provided (or changes), the callback fires with the
    new value. `consumeContext` is provided by `UmbElementMixin`. Think of it as constructor-injection of a
    scoped service that re-fires if the service instance changes.
  - Inside the callback: if `authContext` is truthy, call `createAuthFetch` with the current config and store
    the result in `this.authFetch`. If it is falsy (user logged out), set `this.authFetch = null`.
  - `this.authFetchChanged?.(this.authFetch)` — the optional-chaining call (`?.`) means: if the subclass
    defined `authFetchChanged`, call it; otherwise do nothing. This is the notification hook.

**Usage pattern in a subclass:**

```ts
class MyElement extends UmbAuthFetchMixin(UmbElementMixin(LitElement)) {
    override authFetchChanged(authFetch: AuthFetch | null) {
        if (authFetch) {
            void this.#loadData();
        }
    }

    async #loadData() {
        const res = await this.authFetch!('/umbraco/backoffice/api/my-data');
        // ...
    }
}
```

```ts
export { UmbElementMixin };
```

Re-exports `UmbElementMixin` from this module. This means consumers can write:

```ts
import { UmbAuthFetchMixin, UmbElementMixin } from '@n3o/backoffice-core';
```

instead of also importing from `@umbraco-cms/backoffice/element-api`. It is a convenience re-export that
reduces the number of import lines in consumer files.

---

## `src/workspace-visibility-condition.ts` — line by line

```ts
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbConditionConfigBase, UmbConditionControllerArguments } from '@umbraco-cms/backoffice/extension-api';
import { createAuthFetch, type AuthFetch } from './auth-fetch.js';
```

The last import uses a **relative path with a `.js` extension**. Even though the source file is `.ts`, the
import specifier must be `.js` — this is a TypeScript + ESM rule: TypeScript resolves `.js` imports to `.ts`
sources, and the compiled output keeps the `.js` extension so the browser's module loader can find the file.

### Response type and condition config

```ts
export interface WorkspaceVisibilityRes {
    visible: boolean;
}

export type WorkspaceVisibilityConditionConfig = UmbConditionConfigBase & {
    endpoint?: string;
};
```

- `WorkspaceVisibilityRes` — the shape the HTTP endpoint must return. `interface` in TypeScript is like a C#
  `interface` or a `record` with no methods: just a shape contract.
- `WorkspaceVisibilityConditionConfig` — extends `UmbConditionConfigBase` (Umbraco's base type for all
  condition configs) with one extra optional field: `endpoint`. The `&` is an *intersection type* (like
  `where T : Base, IExtra` in generics, but for object shapes). When the manifest author registers this
  condition, they can supply `"endpoint": "/umbraco/backoffice/api/my-check"` in the condition's config block.

### Class declaration

```ts
export class WorkspaceVisibilityCondition extends UmbConditionBase<WorkspaceVisibilityConditionConfig> {
    #args: UmbConditionControllerArguments<WorkspaceVisibilityConditionConfig>;
    #authFetch: AuthFetch | null = null;
    #unique: string | null = null;
```

- Extends `UmbConditionBase<WorkspaceVisibilityConditionConfig>` — Umbraco's base class for all conditions.
  The generic parameter tells the base class what shape the config object is. `UmbConditionBase` provides the
  `this.permitted` property (a boolean; when set to `true` the extension becomes visible) and calls
  `args.onChange(permitted)` to notify the extension registry.
- `#args`, `#authFetch`, `#unique` — TypeScript private class fields (the `#` prefix is native JavaScript
  private, not TypeScript's `private` keyword). Unlike TypeScript `private`, `#` fields are truly inaccessible
  at runtime, even via `as any` casting. In C# terms: `private readonly` fields.

### Constructor

```ts
constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<WorkspaceVisibilityConditionConfig>) {
    super(host, args);
    this.#args = args;

    this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
        this.#authFetch = authContext ? createAuthFetch(authContext.getOpenApiConfiguration()) : null;
        void this.#evaluate();
    });

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
        if (!context) { return; }

        this.observe(context.unique, (unique) => {
            this.#unique = unique ?? null;
            void this.#evaluate();
        });
    });
}
```

- `host: UmbControllerHost` — the element that "owns" this controller (conditions are implemented as
  controllers in Umbraco 17). The base class needs it for lifecycle management.
- `args: UmbConditionControllerArguments<...>` — contains `args.config` (the condition config from the
  manifest, including `endpoint`) and `args.onChange` (a callback the condition must invoke when its permitted
  state changes).
- **`this.consumeContext(UMB_AUTH_CONTEXT, ...)`** — subscribes to the auth context exactly as in the mixin.
  When it arrives, build a fresh `authFetch` and immediately re-evaluate visibility.
- **`this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, ...)`** — subscribes to the document workspace
  context. This context is provided by the workspace the user is currently editing; it holds the current
  document's unique ID (a GUID string). `UMB_DOCUMENT_WORKSPACE_CONTEXT` is the context token (typed key) for
  that service.
  - `if (!context) { return; }` — guards against the context being torn down (e.g. on navigation away).
  - **`this.observe(context.unique, ...)`** — `observe` is another method from `UmbConditionBase` / the
    Umbraco controller system. It subscribes to an **observable** (think: an `IObservable<T>` / RxJS subject)
    on the context. Whenever the document's `unique` property emits a new value (e.g. when the user navigates
    to a different document), the callback fires.
  - Inside: store the new unique key and re-evaluate.
- **`void this.#evaluate()`** — calls the private async method and explicitly discards the returned Promise
  with `void`. This is needed because the callback signatures here are synchronous; the `void` suppresses the
  TypeScript warning about an unhandled promise. The C# analogue is `_ = EvaluateAsync();`.

### `#evaluate()` and `#isPermitted()`

```ts
async #evaluate(): Promise<void> {
    const endpoint = this.#args.config?.endpoint;

    if (!endpoint || !this.#unique || !this.#authFetch) {
        return;
    }

    this.permitted = await this.#isPermitted(endpoint, this.#unique);
    this.#args.onChange(this.permitted);
}
```

- `this.#args.config?.endpoint` — optional chaining (`?.`). If `config` is `null` or `undefined`, returns
  `undefined` instead of throwing. The C# equivalent is `this.args.Config?.Endpoint`.
- The `if` guard: if any of the three prerequisites is missing (no endpoint configured, no document open, user
  not authenticated), bail out silently. The extension stays in whatever state it was in last (initially
  `false`, i.e. hidden).
- `this.permitted = await ...` — sets the `permitted` property inherited from `UmbConditionBase`. Setting it
  updates the base class's internal state.
- `this.#args.onChange(this.permitted)` — **must** be called after updating `permitted` to notify the
  extension registry. Without this call, the UI will never react.

```ts
async #isPermitted(endpoint: string, unique: string): Promise<boolean> {
    try {
        const response = await this.#authFetch!(`${endpoint}/${unique}`, {
            headers: { Accept: 'application/json' },
        });

        if (!response.ok) {
            return false;
        }

        const data = await response.json() as { visible?: boolean };

        if (typeof data.visible !== 'boolean') {
            console.error(
                '[WorkspaceVisibilityCondition] Unexpected response shape from',
                endpoint,
                '— expected { visible: boolean }, got',
                data,
            );
            return false;
        }

        return data.visible;
    } catch {
        return false;
    }
}
```

- `this.#authFetch!` — the non-null assertion (`!`) tells TypeScript "I know this is not null here." It is
  safe because `#evaluate` already guards against `!this.#authFetch`. In C# this is `this.authFetch!`.
- `` `${endpoint}/${unique}` `` — template literal (C# interpolated string `$"{endpoint}/{unique}"`). The
  resulting URL is e.g. `/umbraco/backoffice/api/my-check/11111111-2222-3333-4444-555555555555`.
- `if (!response.ok)` — `response.ok` is `true` for 2xx status codes. Any non-2xx (including `401`, `403`,
  `404`) returns `false`, hiding the extension.
- `await response.json() as { visible?: boolean }` — parses the response body as JSON and casts it to a shape
  with an optional `visible` boolean. The `as` cast is **not** a runtime check; it only affects the TypeScript
  type-checker.
- **Runtime guard:** `if (typeof data.visible !== 'boolean')` — because the `as` cast is compile-time only,
  this explicit runtime check confirms the actual value is a boolean. If the API returns something unexpected
  (wrong shape, missing field), it logs an error with the full context and returns `false`. This prevents a
  silent "always visible" bug if someone points the condition at the wrong endpoint.
- `catch { return false; }` — any network error or JSON parse failure hides the extension silently. This is a
  deliberate defensive choice: a condition that throws would break the whole extension row.

---

## `umbraco-package.json` — the manifest

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.BackofficeCore",
    "name": "N3O Backoffice Core",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "condition",
            "alias": "N3O.Condition.WorkspaceVisibility",
            "name": "N3O Workspace Visibility Condition",
            "api": "/App_Plugins/N3O.Umbraco.BackofficeCore/workspace-visibility-condition.js"
        }
    ],
    "importmap": {
        "imports": {
            "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
        }
    }
}
```

Umbraco scans `wwwroot/App_Plugins/*/umbraco-package.json` on startup and processes each manifest. This file
does two independent things:

### 1. Registers the condition extension

```json
{
    "type": "condition",
    "alias": "N3O.Condition.WorkspaceVisibility",
    "name": "N3O Workspace Visibility Condition",
    "api": "/App_Plugins/N3O.Umbraco.BackofficeCore/workspace-visibility-condition.js"
}
```

- **`"type": "condition"`** — tells the extension registry this is a condition, not a dashboard or editor.
- **`"alias": "N3O.Condition.WorkspaceVisibility"`** — the unique string identifier that other manifests
  reference when they want to use this condition. For example, a workspace view manifest would include:
  ```json
  { "conditions": [{ "alias": "N3O.Condition.WorkspaceVisibility", "endpoint": "/umbraco/backoffice/api/my-check" }] }
  ```
- **`"api"`** — the URL path to the JS file that exports the condition class. Umbraco loads this file lazily
  (only when a manifest that uses this condition is first rendered) and looks for a `default` export that
  extends `UmbConditionBase`. Note that `workspace-visibility-condition.ts` exports `WorkspaceVisibilityCondition`
  as both a named export and the default export (`export default WorkspaceVisibilityCondition`), satisfying
  this requirement.

### 2. Injects the import-map entry

```json
"importmap": {
    "imports": {
        "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
    }
}
```

This is the runtime counterpart of the `exports` map in `package.json`. Umbraco merges all import-map entries
from all manifests into the browser's native import map (a `<script type="importmap">` tag in the HTML). After
this:

- Any JS module running in the backoffice can write `import { createAuthFetch } from '@n3o/backoffice-core'`
  and the browser resolves it to `/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js`.
- Crucially, there is only **one** copy of `auth-fetch.js` loaded regardless of how many plugins import it.
  If each plugin bundled its own copy, the `UmbAuthFetchMixin` class identity would differ between modules and
  context lookups could break.

> See [ES modules and import maps](../concepts/04-es-modules-and-import-maps.md) for a detailed explanation
> of how bare specifiers and browser import maps work.

---

## Concepts demonstrated

| Concept | Where to learn more |
|---|---|
| ES modules, bare specifiers, import maps | [04-es-modules-and-import-maps](../concepts/04-es-modules-and-import-maps.md) |
| Vite library mode, externals, `n3oPluginConfig` | [05-vite-and-the-build](../concepts/05-vite-and-the-build.md) |
| Web components, custom elements | [06-web-components-and-shadow-dom](../concepts/06-web-components-and-shadow-dom.md) |
| Umbraco context API (`consumeContext`, `observe`), extensions, conditions | [09-umbraco-backoffice-extensions](../concepts/09-umbraco-backoffice-extensions.md) |
| N3O bridge pattern (Tier A shared runtime, import-map-based sharing) | [10-the-n3o-bridge-pattern](../concepts/10-the-n3o-bridge-pattern.md) |
| TypeScript basics, union types, generics, interfaces | [02-javascript-typescript-for-csharp-devs](../concepts/02-javascript-typescript-for-csharp-devs.md) |
| npm workspaces, `@n3o/build` package | [03-node-npm-and-the-workspace](../concepts/03-node-npm-and-the-workspace.md) |

---

## Gotchas

### The silent 401 problem

If you call `fetch('/umbraco/backoffice/api/something')` directly from a backoffice element — without going
through `createAuthFetch` / `authFetch` — the request will succeed in the browser's network tab but the server
will return `401 Unauthorized`. The browser does not throw; you just get back a `Response` with
`response.ok === false` and `response.status === 401`.

This is easy to miss because:
1. The browser console shows no error (unlike CORS failures).
2. The Umbraco backoffice may even render as if the call succeeded, if the code ignores the status.

**Rule:** Every `fetch` call that targets an `[Authorize]`-decorated controller must use `this.authFetch` (if
in a mixin-augmented element) or a `createAuthFetch`-built function. Never use bare `fetch` for authenticated
endpoints.

### The condition silently hides on any error

`WorkspaceVisibilityCondition` returns `false` (hides the extension) for every failure: non-2xx status, network
error, JSON parse failure, wrong response shape. There is no error bubble or toast.

Consequences:
- If your endpoint URL is wrong (typo, missing route), the extension just disappears. The only signal is the
  `console.error` for a wrong response shape; other failures are silent.
- If the user has insufficient permissions for the visibility endpoint itself (the endpoint returns `403`), the
  extension hides rather than showing an error. This may be the desired behaviour, but confirm it is
  intentional for each use-case.

**Debugging approach:** Open the browser DevTools Network tab, filter by XHR/Fetch, and look for the request
to `{endpoint}/{key}`. Inspect the status and response body directly.

### TypeScript `as` casts are not runtime checks

```ts
const data = await response.json() as { visible?: boolean };
```

The `as` cast tells TypeScript "treat this as this shape" but does not add any runtime validation. If the
server returns `{ result: true }` instead of `{ visible: true }`, TypeScript will not catch it at compile time
and the code will not throw at runtime. That is why the explicit `typeof data.visible !== 'boolean'` guard
exists immediately after: it is the actual runtime safety net.

### Relative imports must use `.js` extension

The import inside `workspace-visibility-condition.ts`:

```ts
import { createAuthFetch, type AuthFetch } from './auth-fetch.js';
```

uses `.js` not `.ts`. TypeScript + ESM requires this: the compiler resolves `.js` imports to `.ts` sources
during type-checking, and the compiled output keeps `.js` so the browser's ESM loader can find the real file.
Writing `.ts` here would break at runtime.

### `void` on async calls inside synchronous callbacks

```ts
this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
    // ...
    void this.#evaluate();   // <-- void is required
});
```

`consumeContext` expects a synchronous callback. `#evaluate` is `async` and returns a `Promise`. If you write
`this.#evaluate()` without `void`, TypeScript (under `strict` mode) emits a warning about a floating promise.
The `void` operator explicitly discards the promise and silences the warning. It is not the same as
`await this.#evaluate()` — the function continues without waiting. This is intentional because the callback
must return `void`, not `Promise<void>`.
