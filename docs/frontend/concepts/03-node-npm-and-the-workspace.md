# 03 — Node.js, npm, and the npm Workspace

> **Prerequisites:** read [01-the-big-picture](01-the-big-picture.md) and [02-javascript-typescript-for-csharp-devs](02-javascript-typescript-for-csharp-devs.md) first.
>
> **What this doc explains:** what Node.js and npm are, how they relate to the .NET build, the structure and meaning of every important `package.json` field, and how the single `src/` npm workspace ties all the plugin projects together.

---

## 1. Node.js — a JavaScript runtime you will never deploy

Node.js is a program that runs JavaScript outside of a browser.
In a browser, JavaScript is executed by the browser's own engine (Chrome uses V8).
Node.js packages that same engine as a standalone executable, so you can run `.js` files from the command line.

**In this repo, Node is used only as a build-time toolchain — exactly like Roslyn is used to compile C# but is never shipped to users.**
At runtime the Umbraco backoffice is a web page; it runs in the visitor's browser, not in Node.
Node's only job here is to let `npm` and `Vite` (the bundler) run during `dotnet build`.

C# analogy: Node is to JavaScript what the .NET SDK is to C# — the thing that knows how to compile/process the source, never the thing that runs in production.

---

## 2. npm — the NuGet of JavaScript

npm (Node Package Manager) is the dominant package manager for JavaScript.
It reads a manifest file (`package.json`), downloads packages from the npm public registry (npmjs.com), and stores them locally so build tools can import them.

| Concept | .NET equivalent |
|---------|-----------------|
| npm registry | nuget.org |
| `package.json` | `.csproj` / `.sln` |
| `package-lock.json` | `packages.lock.json` |
| `node_modules/` folder | `~/.nuget/packages` + `obj/` |
| `npm ci` | `dotnet restore` |
| `npm run build` | `dotnet build` / `msbuild /t:Build` |

---

## 3. `package.json` — the manifest

Every JavaScript project has a `package.json` at its root.
It declares the project's identity, its scripts, and its dependencies — comparable to the combination of a `.csproj` file and a `packages.config`.

### 3.1 The root workspace manifest

File: `src/package.json`

```json
{
  "name": "n3o-umbraco-clientapps",
  "private": true,
  "workspaces": [
    "**/Apps/**",
    "N3O.Umbraco.Cms/Build/*",
    "!**/bin/**",
    "!**/obj/**"
  ],
  "devDependencies": {
    "@umbraco-cms/backoffice": "17.3.5",
    "typescript": "~5.7.0",
    "vite": "^6.0.0",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "@types/react": "^19.0.0",
    "@types/react-dom": "^19.0.0"
  },
  "overrides": {
    "@umbraco-cms/backoffice": "17.3.5",
    ...
  }
}
```

Key fields:

| Field | Meaning |
|-------|---------|
| `name` | The project's package name. Used as an identifier if this were published to npm. Here it's an internal name only. |
| `private: true` | Prevents accidentally publishing this package to the npm registry. Every package in this repo sets this — none are meant to be published publicly. Comparable to marking a .csproj as non-packable. |
| `workspaces` | Declares this as the root of an npm workspace (mono-repo). See section 5 below. |
| `devDependencies` | Packages needed only at build time — see section 3.2. |
| `overrides` | Forces every package in the entire tree to use exactly these versions, preventing version mismatches. Comparable to `<PackageReference>` with `VersionOverride` in a `Directory.Packages.props`. |

### 3.2 A leaf plugin manifest

File: `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/package.json`

```json
{
    "name": "n3o-umbraco-dynamiclistviews",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "devDependencies": {
        "@n3o/build": "*"
    }
}
```

File: `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/package.json`

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

Field-by-field:

| Field | Meaning |
|-------|---------|
| `name` | The package's identifier within the workspace. Names beginning with `@scope/` (e.g. `@n3o/build`, `@n3o/backoffice-core`) follow the npm scoped-package convention; `@n3o` is the scope, the rest is the name. |
| `version` | Semantic version. Not published; used internally by npm to track the package. |
| `private: true` | Never publish to npm. |
| `type: "module"` | Every `.js` file in this package is an ES Module (see [04-es-modules-and-import-maps](04-es-modules-and-import-maps.md)). Without this, Node defaults to the older CommonJS format. This flag is required for modern JS tooling. |
| `exports` | Declares which files are the public API of this package — analogous to `public` in C#. `"."` means "the main entry when someone writes `import ... from '@n3o/backoffice-core'`". Only `BackofficeCore` and `@n3o/build` need this; plain plugin apps (like DynamicListViews) are built by Vite directly and have no importable API. |
| `scripts` | Named shell commands run via `npm run <name>`. Think of these as MSBuild targets. |
| `devDependencies` | See section 3.3 below. |

### 3.3 `dependencies` vs `devDependencies`

In a production npm package (e.g. a library published to npmjs.com), `dependencies` are packages needed at runtime by consumers, and `devDependencies` are tools needed only while you develop the package.

**In this repo everything is a `devDependency` — and that is correct.** Here is why:

- None of these packages are published to npm.
- All packages (Vite, TypeScript, React, `@umbraco-cms/backoffice`) are only needed to **compile** the TypeScript source into plain JavaScript. The build output — a `.js` file in `wwwroot/App_Plugins/` — does not need npm at all.
- React itself is not bundled into the output `.js` file (it is kept external — see [04-es-modules-and-import-maps](04-es-modules-and-import-maps.md) and [05-vite-and-the-build](05-vite-and-the-build.md)). The browser gets React from a separate served file at runtime via the import map.

C# analogy: every npm package here is a build tool (`devDependency`) — none ship in the deployment artefact, just as Roslyn, MSBuild tasks, or code generators don't ship in a published NuGet package.

The one exception is `@n3o/react-runtime` (`src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/package.json`), which lists `react` and `react-dom` as `dependencies` (not `devDependencies`) because its sole job is to **bundle** React into a served `.js` file. It needs React as an input to its build, not just as a type reference. That distinction doesn't affect you as a consumer.

---

## 4. `package-lock.json` and `node_modules`

### 4.1 `package-lock.json`

When you run `npm install` or `npm ci`, npm resolves the full dependency tree — every package and every package's package — and writes `src/package-lock.json`. This lock file records the exact version of every resolved package so builds are reproducible.

C# analogy: `packages.lock.json` in a .NET project. Commit it to source control; never edit it by hand.

### 4.2 `node_modules/`

npm downloads the resolved packages into `src/node_modules/`. This is the local package cache — equivalent to `~/.nuget/packages` plus `obj/` for a .NET project.

`node_modules/` is listed in the repo's `.gitignore` (line 276: `node_modules/`) and is never committed. It is recreated on demand by `npm ci`.

**Hoisting:** in an npm workspace (see section 5), packages from all sub-projects are installed into a single `node_modules/` at the workspace root (`src/node_modules/`), rather than separate `node_modules/` inside each plugin folder. This is called *hoisting*. It means:

- There is one copy of `vite`, one copy of `typescript`, one copy of `@umbraco-cms/backoffice` — shared by all plugins.
- You will never see a `node_modules/` inside an `Apps/` folder.

### 4.3 `npm ci` vs `npm install`

| Command | Behaviour |
|---------|-----------|
| `npm install` | Resolves dependencies, may update `package-lock.json`, writes `node_modules`. Used when adding or changing packages. |
| `npm ci` | Installs **exactly** what is in `package-lock.json` — no resolution, no changes to the lock file. Fails if `node_modules` is inconsistent with the lock file. Fast and reproducible. |

In this repo the .NET build always calls `npm ci` (never `npm install`) to guarantee reproducibility. Think of `npm ci` as the JavaScript equivalent of `dotnet restore --locked-mode`.

---

## 5. npm workspaces — one mono-repo for all plugins

### 5.1 The problem without workspaces

Without workspaces, each plugin would be a completely independent npm project: its own `node_modules`, its own copy of Vite, its own copy of `@umbraco-cms/backoffice`. That wastes disk space, makes version management fragile, and means running `npm ci` in 16 different folders.

### 5.2 How npm workspaces work

The root `src/package.json` declares a `workspaces` array:

```json
"workspaces": [
    "**/Apps/**",
    "N3O.Umbraco.Cms/Build/*",
    "!**/bin/**",
    "!**/obj/**"
]
```

This tells npm: "every folder matched by these globs that contains a `package.json` is a member of this workspace".

The globs expand to every `Apps/<plugin>/package.json` in every project under `src/`, plus the two `Build/*` foundation packages (`@n3o/build` and `@n3o/react-runtime`). The `!` prefix excludes `bin/` and `obj/` folders.

With workspaces active:

1. A single `npm ci` at `src/` installs **all** packages for **all** workspace members into one shared `src/node_modules/`.
2. Each workspace member can declare dependencies on other workspace members by name. npm creates a **symlink** in `node_modules/` pointing to the local package folder — no network fetch needed.

### 5.3 Symlinked workspace packages in practice

The shared build config is at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/` and its `package.json` declares `"name": "@n3o/build"`.

Every plugin's `package.json` has:

```json
"devDependencies": {
    "@n3o/build": "*"
}
```

The `"*"` version means "any version — just find it in the workspace". npm creates:

```
src/node_modules/@n3o/build  →  symlink  →  src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/
```

When a plugin's `vite.config.ts` writes:

```typescript
import { n3oPluginConfig } from '@n3o/build';
```

Node follows that symlink and loads the local source directly — no publishing to npm, no version numbers. It is exactly equivalent to a C# `<ProjectReference>` in a `.csproj`.

| npm workspace concept | C# / .NET equivalent |
|-----------------------|----------------------|
| Root `src/package.json` with `workspaces` | A `.sln` file listing projects |
| Member `package.json` | A `.csproj` |
| Symlinked workspace dependency (`@n3o/build: "*"`) | `<ProjectReference>` |
| Single hoisted `src/node_modules/` | Shared `~/.nuget/packages` cache |
| `package-lock.json` at workspace root | Solution-level `packages.lock.json` |

---

## 6. How `dotnet build` restores and builds the workspace

You never need to run `npm ci` manually. The MSBuild integration handles it automatically.

File: `Directory.Build.targets` at the repo root.

### 6.1 Restore — runs once

```xml
<Target Name="RestoreClientWorkspace"
        Condition="'$(MSBuildProjectName)' == 'N3O.Umbraco.Extensions'"
        BeforeTargets="BeforeBuild"
        Inputs="$(MSBuildThisFileDirectory)src\package-lock.json"
        Outputs="$(MSBuildThisFileDirectory)src\node_modules\.n3o-restore-stamp">
    <Exec Command="npm ci" WorkingDirectory="$(MSBuildThisFileDirectory)src" />
    <Touch Files="$(MSBuildThisFileDirectory)src\node_modules\.n3o-restore-stamp" AlwaysCreate="true" />
</Target>
```

This target runs `npm ci` in `src/` exactly once per full solution build, triggered when `N3O.Umbraco.Extensions` is built (because every client-app host transitively references it). The `Inputs`/`Outputs` stamp file makes it incremental — `npm ci` only re-runs when `package-lock.json` changes. This is the JavaScript equivalent of `dotnet restore`.

### 6.2 Build — runs per plugin project

```xml
<Target Name="BuildClientApp"
        Condition="Exists('$(MSBuildProjectDirectory)\Apps') And ..."
        BeforeTargets="BeforeBuild"
        DependsOnTargets="ResolveProjectReferences">
    <ItemGroup>
        <_N3OClientAppPackage Include="$(MSBuildProjectDirectory)\Apps\**\package.json"
                              Exclude="$(MSBuildProjectDirectory)\Apps\**\node_modules\**" />
    </ItemGroup>
    <Exec Command="npm run build"
          WorkingDirectory="%(_N3OClientAppPackage.RootDir)%(_N3OClientAppPackage.Directory)" />
</Target>
```

For every `*.StaticAssets` project that has an `Apps/` folder, MSBuild finds every `package.json` within it and runs `npm run build` in that directory. The `build` script in each `package.json` (`"build": "tsc --noEmit && vite build"`) first type-checks with TypeScript then bundles with Vite. The output lands in `wwwroot/App_Plugins/<plugin>/`.

### 6.3 Fail-fast guard

If you attempt to build a plugin project in isolation (without building `N3O.Umbraco.Extensions` first), the `EnsureClientWorkspaceRestored` target detects that `src/node_modules` is missing and emits a clear error message, rather than a cryptic Vite "cannot find module" failure.

---

## 7. Summary map

```
src/                                ← npm workspace root
├── package.json                    ← workspace manifest; devDependencies shared by all
├── package-lock.json               ← exact resolved versions; committed; never edit by hand
├── node_modules/                   ← git-ignored; recreated by `npm ci`
│   ├── @umbraco-cms/backoffice/    ← hoisted; shared by all plugins
│   ├── vite/                       ← hoisted
│   ├── typescript/                 ← hoisted
│   └── @n3o/
│       ├── build/                  ← symlink → N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/
│       └── react-runtime/          ← symlink → N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/
├── N3O.Umbraco.Cms/
│   ├── Build/
│   │   ├── N3O.Umbraco.BuildConfig/    ← @n3o/build  (shared Vite + TS config)
│   │   └── N3O.Umbraco.ReactRuntime/   ← @n3o/react-runtime (bundles React)
│   └── Apps/
│       ├── N3O.Umbraco.BackofficeCore/ ← @n3o/backoffice-core (auth-fetch)
│       └── N3O.Umbraco.DynamicListViews/
├── Data/N3O.Umbraco.Data.StaticAssets/Apps/
│   ├── N3O.Umbraco.Data.Export/
│   └── ...
└── ... (every other *.StaticAssets project's Apps/)
```

Next: [04-es-modules-and-import-maps](04-es-modules-and-import-maps.md) — how the browser resolves bare `import` specifiers like `@umbraco-cms/backoffice/external/lit` via an import map, and how this repo extends that import map to expose shared React and `@n3o/backoffice-core`.
