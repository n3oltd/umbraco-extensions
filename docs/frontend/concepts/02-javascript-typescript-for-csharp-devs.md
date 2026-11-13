# 02 — JavaScript and TypeScript for C# Developers

This document is a practical crash course in JavaScript (JS) and TypeScript (TS) aimed squarely at a C# developer. It covers exactly what you need to read and understand the app code in this repo. There are ten years of JS history to ignore; this document skips the legacy parts and focuses on modern (ES2022+) JS as used here.

---

## JavaScript vs TypeScript

**JavaScript** is a dynamically-typed scripting language that runs natively in every browser and in Node.js. It has no compiler step — the source file *is* the file the runtime executes.

**TypeScript** is a superset of JavaScript: every valid JS file is also a valid TS file. TypeScript adds a **static type system** on top and a **compile step** (`tsc`) that erases the types and emits plain JavaScript. The types exist only at edit-time and compile-time; by the time the code runs in the browser, they are completely gone.

```
your-file.ts  →  tsc (type-checker)  →  your-file.js  →  browser
```

This is fundamentally different from C#, where the type information is preserved in the IL and is inspectable at runtime via reflection. A TypeScript `as` cast (see below) has zero runtime effect — it is purely a compile-time instruction.

All source files in this repo are `.ts` or `.tsx` (TSX = TypeScript with JSX — see [08 — React](./08-react.md)).

---

## `let`, `const`, and `var`

JS has three variable declarations. Ignore `var` — it is legacy. Use only:

| Keyword | C# equivalent | Mutability |
|---------|--------------|------------|
| `const` | `readonly` local variable | The binding cannot be reassigned (but an object's contents can still change) |
| `let` | Ordinary local variable | Can be reassigned |

```typescript
const name = 'N3O';       // cannot do: name = 'other';
let count = 0;
count = count + 1;        // fine
```

Prefer `const` everywhere. Use `let` only when reassignment is needed.

---

## Types, interfaces, and `type` aliases

TypeScript types look familiar to a C# developer, with a few differences.

**Primitive types** use lowercase names: `string`, `number` (one type for all numbers — no `int`/`double`/`float`), `boolean`, `void`, `null`, `undefined`.

**Interfaces** declare the shape of an object:

```typescript
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-children.repository.ts
export interface DynamicListViewItem {
    unique: string;
    name: string;
    state: string;
    icon: string;
    createDate: string;
    editPath: string;
}
```

This is equivalent to a C# record or POCO. No `class` keyword needed — an interface is a pure type description. You can `implements` an interface, but you never need to: any object with matching properties satisfies the interface automatically (see structural typing below).

**`type` aliases** name a type expression. They can do most of what `interface` can, plus union types:

```typescript
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts
export type AuthFetch = (input: string, init?: RequestInit) => Promise<Response>;
```

This names a function signature as a type alias. The `?` after `init` means the parameter is optional.

**Union types** let a value be one of several types:

```typescript
type State = 'Published' | 'Draft' | 'Unknown';
```

C# has no direct equivalent in classic code, though `OneOf<T1,T2>` libraries approximate it. In TypeScript, union types are first-class.

---

## Structural typing vs nominal typing

C# uses **nominal typing**: two types are compatible only if one explicitly inherits from or implements the other. TypeScript uses **structural typing**: two types are compatible if their shapes match.

```typescript
interface Named { name: string; }

function greet(x: Named) { return `Hello, ${x.name}`; }

// This works even though there is no `implements Named`:
const obj = { name: 'Alice', age: 30 };
greet(obj); // ✓ — obj has a `name: string` property, so it satisfies Named
```

This matters when reading code: you will see objects being passed to functions without explicit type annotations. TypeScript infers compatibility from shape.

---

## `null`, `undefined`, and the nullish operators

C# has one "nothing" value: `null`. JavaScript has two:

| Value | Meaning | C# analogue |
|-------|---------|-------------|
| `null` | Explicitly set to "no value" | `null` |
| `undefined` | Never assigned / property does not exist | (no direct equivalent — closest is an uninitialised variable) |

Both are falsy. In practice, treat them similarly. TypeScript's strict mode (enabled via `"strict": true` in `tsconfig.json`, which this repo uses) enforces null-safety in the same way C# nullable reference types do.

**Optional chaining `?.`** — same meaning as C# `?.`:

```typescript
const name = item.variants?.[0]?.name ?? '(unnamed)';
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-children.repository.ts
```

If `item.variants` is `null`/`undefined`, or if `[0]` is `undefined`, the expression short-circuits to `undefined` rather than throwing.

**Nullish coalescing `??`** — same as C# `??`:

```typescript
const total = data?.total ?? items.length;
```

If the left side is `null` or `undefined`, use the right side instead. Unlike the JS `||` operator, `??` only triggers on `null`/`undefined`, not on `0` or `''`.

---

## Functions and arrow functions

JavaScript has two main syntaxes for functions.

**Named function declaration** (similar to a static C# method):

```typescript
function toListViewItem(item: UmbDocumentTreeItemModel): DynamicListViewItem {
    return { unique: item.unique, /* … */ };
}
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-children.repository.ts
```

**Arrow function** (a lambda — similar to a C# lambda `x => x.Name`):

```typescript
const items = (data?.items ?? []).map(toListViewItem);
// Or inline:
const names = items.map(item => item.name);
```

Arrow functions are compact and are the idiomatic choice for callbacks and short expressions.

**The `this` problem.** In C# `this` always refers to the current class instance. In JavaScript `this` depends on *how* the function is called, which causes bugs in callbacks. Arrow functions capture `this` from their surrounding scope (like a C# lambda captures `this`). Class method syntax and arrow functions eliminate most `this` issues; this repo uses TypeScript classes throughout, so you rarely need to think about this.

---

## Private class fields with `#`

TypeScript supports both the TypeScript-specific `private` keyword and the JavaScript-native **private class fields** using `#` prefix. This repo uses `#` for true encapsulation:

```typescript
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-children.repository.ts
export class DynamicChildrenRepository {
    readonly #treeRepository: UmbDocumentTreeRepository;

    constructor(host: UmbControllerHost) {
        this.#treeRepository = new UmbDocumentTreeRepository(host);
    }
}
```

`#treeRepository` is genuinely inaccessible outside the class at runtime — the JavaScript engine enforces it. The TypeScript `private` keyword is erased at compile time and is not enforced at runtime. When you see `#`, think `private readonly` in C#.

---

## `Promise`, `async`, and `await`

JavaScript's async model is based on **Promises** — analogous to C#'s `Task<T>`.

| C# | TypeScript |
|----|-----------|
| `Task` | `Promise<void>` |
| `Task<string>` | `Promise<string>` |
| `async Task<string> GetAsync()` | `async function getAsync(): Promise<string>` |
| `await someTask` | `await somePromise` |

```typescript
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-children.repository.ts
async getChildren(unique: string): Promise<DynamicListViewChildren> {
    const { data } = await this.#treeRepository.requestTreeItemsOf({
        parent: { unique, entityType: 'document' },
        paging: { skip: 0, take: PAGE_SIZE },
    });
    const items = (data?.items ?? []).map(toListViewItem);
    return { items, total: data?.total ?? items.length };
}
```

`async` functions always return a `Promise`. The `await` keyword unwraps the resolved value — exactly as in C#.

**Floating promises.** Unlike C#, where unawaited Tasks produce a compiler warning, JavaScript silently ignores an unawaited Promise. You will see `void this.#load(unique)` in the codebase — the `void` operator is an explicit signal that the Promise is intentionally not awaited (analogous to `_ = DoSomethingAsync()` in C# to suppress CA2012). Where you see `void`, the developer deliberately chose not to chain further work on the result.

---

## Modules: `import` and `export`

JavaScript's module system (covered in depth in [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md)) uses `import` and `export` in place of C#'s `using` and `public`.

A brief primer:

```typescript
// export a named symbol (like `public` in C#):
export interface DynamicListViewItem { … }
export class DynamicChildrenRepository { … }

// export a default (one per file — like a main type):
export default N3oWelcomeDashboardElement;

// import named symbols from another file:
import { DynamicChildrenRepository } from './dynamic-children.repository';
// import a named symbol from a package:
import { createRoot } from 'react-dom/client';
// import a type only (erased at compile time, never emitted to JS):
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
```

`import type` is a TypeScript feature — it is stripped entirely from the emitted JS. Use it whenever you only need the type (not the value) of something.

---

## Destructuring and spread

**Object destructuring** unpacks properties from an object into local variables:

```typescript
// instead of: const data = result.data; const total = result.total;
const { data } = await this.#treeRepository.requestTreeItemsOf(…);
```

**Array destructuring** does the same for arrays:

```typescript
const [first, second] = someArray;
```

**Spread (`...`)** copies properties or array items:

```typescript
// merge objects (like C# with-expressions on records):
return fetch(input, { ...init, credentials: config.credentials, headers });
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts
```

The expression `{ ...init, credentials: …, headers: … }` creates a new object with all properties of `init`, then overrides `credentials` and `headers`. This is a very common pattern in the codebase.

---

## Template literals

Use backticks instead of `+` string concatenation:

```typescript
console.error('[WorkspaceVisibilityCondition] Unexpected response shape from', endpoint, '— expected { visible: boolean }, got', data);
// But also:
headers.set('Authorization', `Bearer ${token}`);
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts
```

`\`Bearer ${token}\`` — backtick strings with `${}` interpolation — are the JS equivalent of C# `$"Bearer {token}"`.

---

## `unknown`, `any`, and `as` casts

**`any`** disables type-checking for a value. Avoid it. It is the equivalent of using `dynamic` in C#.

**`unknown`** is a safe "I don't know the type yet" — you must narrow it (with a type guard or a cast) before using it. Prefer `unknown` over `any`.

**`as` casts** tell the compiler "trust me, this is of type X." They have **no runtime effect** — unlike C# `(string)obj` or `obj as string`, which perform actual conversions or return `null` if the cast fails. A TypeScript `as` cast is purely a compile-time instruction: if you are wrong, you get a runtime error at the point you *use* the value, not at the cast.

```typescript
const data = await response.json() as { visible?: boolean };
// From: src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/workspace-visibility-condition.ts
```

`response.json()` returns `unknown` (it parsed JSON, whose shape is unknown at compile time). The `as` cast tells the compiler "treat this as `{ visible?: boolean }`." The compiler believes you; the browser does not verify it. The code that follows (`typeof data.visible !== 'boolean'`) is the actual runtime check.

---

## The DOM in one paragraph

The **DOM (Document Object Model)** is the browser's live tree of HTML elements. JavaScript can read and manipulate it via global objects: `document` is the root of the tree; `document.createElement('div')` creates an element; `.appendChild(child)` inserts it. Elements fire **events** (`click`, `input`, `connectedCallback`) and you attach handlers with `.addEventListener('click', handler)`. In the backoffice, you rarely manipulate the DOM directly — Lit and React do it for you — but you will see low-level DOM calls in the web-component shell code:

```typescript
// From: src/Plugins/WelcomeDashboard/…/Apps/src/welcome-dashboard.ts
const shadow = this.attachShadow({ mode: 'open' });
this.#mount = document.createElement('div');
shadow.appendChild(this.#mount);
```

`attachShadow` creates an isolated **Shadow DOM** (a subtree with its own CSS scope — see [06 — Web Components](./06-web-components-and-shadow-dom.md)). `document.createElement` creates a new `<div>`. `appendChild` inserts it into the shadow tree. This is the entire DOM API you need to understand to read the shell code.

---

## `.d.ts` declaration files

A `.d.ts` file is a **type declaration file** — it contains type information but no executable code. It is the TypeScript equivalent of a C# reference assembly (a `.dll` with no implementation, only public signatures). The TypeScript compiler uses `.d.ts` files to know the types exported by a library without needing the library's source.

When you install an npm package that is written in plain JS (no TypeScript source), there is often a corresponding `@types/<package>` npm package that provides the `.d.ts` declarations — exactly like Roslyn analysers that add metadata to pre-compiled assemblies.

This repo also has hand-authored `.d.ts` files for cases where auto-generated types are absent or wrong:

```typescript
// src/Plugins/WelcomeDashboard/…/Apps/src/uui-react.d.ts  (and others)
// Teaches TypeScript that uui-* web-component tag names are valid in JSX.
```

```typescript
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-env.d.ts
declare module '*.css?inline' {
    const css: string;
    export default css;
}
```

This `.d.ts` tells TypeScript that importing a CSS file with the `?inline` query suffix (a Vite feature) yields a `string`. Without it, the compiler would reject `import styles from './foo.css?inline'`.

---

## Decorators

TypeScript supports **decorators** — annotations applied to classes, methods, or properties. They look like C# attributes:

```typescript
@customElement('n3o-welcome-dashboard')  // ≈ [CustomElement("n3o-welcome-dashboard")]
export class N3oWelcomeDashboardElement extends HTMLElement { … }

@state()   // ≈ [ReactiveProperty]
private _items: DynamicListViewItem[] = [];
```

Decorators are a compile-time feature. `@customElement(name)` calls `customElements.define(name, TheClass)` for you. `@state()` from Lit marks a property so that Lit re-renders the component when it changes. They are explained further in [07 — Lit](./07-lit.md).

---

## Summary — the JS/TS features you will encounter most

| Feature | Example in this repo | C# analogue |
|---------|---------------------|-------------|
| `const`/`let` | throughout | `readonly`/`var` |
| Arrow function | `.map(item => item.name)` | `x => x.Name` lambda |
| `async`/`await` | `async getChildren()` | `async Task<T>` |
| `Promise<T>` | return type of async methods | `Task<T>` |
| `?.` optional chain | `item.variants?.[0]?.name` | C# `?.` |
| `??` nullish coalesce | `data?.total ?? 0` | C# `??` |
| Destructuring | `const { data } = await …` | C# deconstruct (value tuples) |
| Spread `...` | `{ ...init, headers }` | `with` on records |
| Template literal | `` `Bearer ${token}` `` | `$"Bearer {token}"` |
| `interface` | `DynamicListViewItem` | C# interface / record |
| `type` alias | `AuthFetch = (…) => Promise<…>` | `delegate` |
| Union type | `'Published' \| 'Draft'` | (no direct C# equiv.) |
| `as` cast | `response.json() as { visible?: boolean }` | Compile-time only — no runtime check |
| `#privateField` | `#treeRepository` | `private readonly` |
| Decorator `@` | `@customElement(…)`, `@state()` | `[Attribute]` |
| `.d.ts` file | `vite-env.d.ts`, `uui-react.d.ts` | Reference assembly / XML doc |
| `import type` | `import type { … }` | Erased at compile time |

Continue to [03 — Node, npm, and the Workspace](./03-node-npm-and-the-workspace.md).
