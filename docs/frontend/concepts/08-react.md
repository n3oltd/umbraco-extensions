# 08 — React Fundamentals

This document explains React for a .NET/C# developer who has never written frontend JavaScript. It focuses on the concepts this repo actually uses, grounded in real source files.

---

## The core idea

React's central claim is:

> **UI = f(state)**  
> Your UI is a pure function of your data. You describe what the screen should look like for any given data. React figures out the minimum DOM changes needed to get there.

**C# analogy:** imagine a Razor view that re-renders automatically whenever its model changes, but React tracks exactly which HTML nodes changed and only patches those — it does not throw away and recreate the whole page.

You never write `document.getElementById('status').innerText = 'Done'`. Instead you describe: "when `processing` is false and `errorMessage` is null, render a button labelled 'Export'." React handles the DOM.

---

## Components

A **component** is a function that returns what the UI should look like.

```tsx
// src/Plugins/WelcomeDashboard/.../Apps/src/welcome-dashboard-app.tsx  lines 8-25
export function WelcomeDashboardApp() {
    return (
        <uui-box headline="Help & Support">
            <p>
                Please visit the N3O Support Centre to view the latest help articles,
                documentation and to contact our support team with any queries.
            </p>
            <p>
                <a href="https://support.n3o.ltd" target="_blank" rel="noopener">
                    Visit Support Centre &rarr;
                </a>
            </p>
            <style>{styles}</style>
        </uui-box>
    );
}
```

**C# analogy:** a component is like a method that returns a view model describing its own UI. The framework (React) calls this method whenever it needs to know what the component should look like, and updates the real DOM to match.

Key points:
- Component names start with a capital letter (`WelcomeDashboardApp`, not `welcomeDashboardApp`). React uses this to distinguish components from plain HTML elements.
- The function returns JSX (explained next).
- This component has no props and no state — it is entirely static. It is the simplest possible component.

---

## JSX

The HTML-like syntax inside the function body is **JSX**:

```tsx
return (
    <uui-box headline="Help & Support">
        <p>Please visit...</p>
    </uui-box>
);
```

JSX is **not** HTML and is **not** a string. It is syntax sugar that TypeScript compiles to function calls. Specifically, with the "automatic" JSX transform (which this repo uses, configured in `vite-config.js` line 15):

```
<uui-box headline="Help & Support">...</uui-box>
    ↓ compiles to ↓
jsx(uui-box, { headline: "Help & Support" }, ...children)
```

That `jsx` function comes from `react/jsx-runtime`, which is kept **external** in the Vite build and resolved at runtime via the import map in `N3O.Umbraco.ReactRuntime/umbraco-package.json`:

```json
// src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json  lines 7-10
"react/jsx-runtime": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-jsx-runtime.js"
```

This means there is one copy of the JSX runtime across all plugins — not one per plugin. See [04 — ES modules & import maps](./04-es-modules-and-import-maps.md) and [05 — Vite & the build](./05-vite-and-the-build.md) for the full import-map story.

**JSX rules (differences from HTML):**

| HTML | JSX |
|---|---|
| `class="foo"` | `className="foo"` (`class` is a reserved JS keyword) |
| `for="id"` | `htmlFor="id"` |
| Self-closing: `<br>` | Must close: `<br />` |
| Inline style: `style="color: red"` | `style={{ color: 'red' }}` (a JS object, not a string) |
| Comments: `<!-- ... -->` | `{/* ... */}` |

---

## Props

**Props** are the read-only inputs to a component — the data passed in from the parent.

```tsx
// src/Data/.../Apps/N3O.Umbraco.Data.Export/src/data-export-app.tsx  lines 12-16
interface DataExportAppProps {
    contentKey: string | null;
    authFetch: AuthFetch | null;
}

export function DataExportApp({ contentKey, authFetch }: DataExportAppProps) {
```

**C# analogy:** props are like constructor parameters or an immutable DTO passed into a method. The component receives them; it cannot change them. If the parent passes a new value, React calls the component function again with the new props.

**This repo's convention:** define a named `interface` for props (e.g. `DataExportAppProps`), place it immediately above the component function, and destructure the props in the function signature (`{ contentKey, authFetch }`). The repo does not use `React.FC<Props>` — components are plain function declarations.

The shell (Lit web component) passes props when it calls `createElement`:

```typescript
// src/Data/.../Apps/N3O.Umbraco.Data.Export/src/data-export.ts  lines 68-73
this.#root?.render(
    createElement(DataExportApp, {
        contentKey: this.#contentKey,
        authFetch: this.authFetch,
    }),
);
```

`createElement(Component, props)` is the non-JSX equivalent of `<DataExportApp contentKey={...} authFetch={...} />`. The shell uses `createElement` because it is plain TypeScript with no JSX extension.

---

## State: `useState`

Components are re-called by React whenever their data changes. To store data that changes over time, use the `useState` hook:

```tsx
// data-export-app.tsx  lines 22-26
const [contentType, setContentType] = useState<ContentType | null>(null);
const [format, setFormat] = useState<string>('excel');
const [includeUnpublished, setIncludeUnpublished] = useState<boolean>(false);
const [metadatas, setMetadatas] = useState<ContentMetadata[]>([]);
const [exportableProperties, setExportableProperties] = useState<ExportableProperty[]>([]);
```

`useState<T>(initialValue)` returns a tuple: the current value, and a setter function. The TypeScript destructuring `const [value, setValue]` names them.

**Why not just assign to a variable?** Because local variables in a function are gone when the function returns. React needs to store state somewhere that survives between renders. `useState` stores the value in React's internal memory, keyed to the position of this hook call in this component instance.

**Why not mutate the current value?** Mutation does not tell React anything changed, so no re-render happens. You must call the setter:

```tsx
// Correct — React sees the new value and re-renders:
setFormat('csv');

// Wrong — React never knows this changed:
format = 'csv';  // This would also be a compile error since format is const
```

**Functional updates** — when the new state depends on the old state, pass a function to avoid stale closures:

```tsx
// data-export-app.tsx  lines 57-58
const selectAllMetadatas = (): void => setMetadatas((prev) => prev.map((m) => ({ ...m, selected: true })));
const clearSelectedMetadatas = (): void => setMetadatas((prev) => prev.map((m) => ({ ...m, selected: false })));
```

`setMetadatas(prev => ...)` receives the guaranteed-current value as `prev`. Using `(prev => ...)` instead of `(metadatas => ...)` avoids the risk of capturing a stale `metadatas` from an earlier render's closure.

---

## `useEffect` — synchronising with the outside world

`useEffect` runs a side-effect after React has rendered. Use it for things that are inherently external: fetching data, subscribing to an event, starting a timer.

```typescript
// use-export.ts  lines 17-54
useEffect(() => {
    if (!contentKey || !authFetch) {
        return;
    }

    let active = true;

    const load = async (): Promise<void> => {
        const [typesRes, metaRes] = await Promise.all([
            authFetch(`/umbraco/backoffice/api/ContentTypes/${contentKey}/relations?type=descendant`, ...),
            authFetch('/umbraco/backoffice/api/Exports/lookups/contentMetadata', ...),
        ]);

        const types = (await typesRes.json()) as ContentType[];
        const metadata = (await metaRes.json()) as ContentMetadata[];

        // ...sort and mutate metadata...

        if (active) {
            setContentTypes(types);
            setMetadatas(metadata);
        }
    };

    void load();

    return () => {
        active = false;     // cleanup: mark this effect as stale
    };
}, [contentKey, authFetch]);  // dependency array
```

**The three parts of `useEffect`:**

1. **The effect function** — runs after render. Can be `async` indirectly (by calling an `async` inner function) but must not itself be declared `async`.
2. **The cleanup function** — the optional return value. React calls it before running the effect again (if deps changed) and when the component unmounts. Use it for cancellation, unsubscription, clearing timers.
3. **The dependency array** — the second argument. React only re-runs the effect when one of these values changes (by reference equality). An empty array `[]` means "run once on mount." Omitting it entirely means "run after every render" (almost never what you want).

**`useEffect` is an escape hatch.** It is for synchronising React state with something outside React. Do not use it to derive data from other state (compute it inline instead), and do not use it to handle events (use event handlers instead).

**The `active` flag pattern** (seen above) handles the race where a second fetch starts before the first completes. The first effect's cleanup sets `active = false`; the first fetch's `if (active)` check then discards its stale results. This is a common pattern — you will also see it with `AbortController`.

---

## The Rules of Hooks

Hooks (`useState`, `useEffect`, `useRef`, etc.) have two non-negotiable rules:

1. **Only call hooks at the top level** — not inside `if`, `for`, or nested functions. React tracks hooks by call order; conditional calls break the order between renders.
2. **Only call hooks inside React function components** (or other custom hooks).

**C# analogy:** think of hooks as a slot array on the component instance. React fills slot 0, slot 1, slot 2 on each call. If you put `useState` inside an `if` block, slot 0 might be skipped on one render, corrupting every slot after it.

---

## `useRef` — a mutable box that survives renders

`useRef` gives you a mutable container whose `.current` property you can read and write without triggering a re-render.

```typescript
// use-export.ts  lines 82-83
const generationRef = useRef<number>(0);
const pollTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
```

Two uses:

1. **Storing mutable values that should not trigger a re-render** — here, a generation counter and a timer ID. Changing them does not cause a re-render; they just need to persist across renders.
2. **Holding DOM element references** — `<input ref={inputRef} />` makes `inputRef.current` point to the real DOM input. (Not used in this repo's shown files, but the standard pattern.)

**C# analogy:** think of `useRef` as an instance field on the component — a `ref` to a box. Reading/writing `.current` does not notify anyone.

---

## `createRoot(container).render(<App/>)` — mounting a React tree

React lives inside a DOM node that you choose. The shell (Lit web component) is responsible for creating that container and calling `createRoot`:

```typescript
// src/Plugins/WelcomeDashboard/.../Apps/src/welcome-dashboard.ts  lines 17-37
export class N3oWelcomeDashboardElement extends HTMLElement {
    #root?: Root;
    #mount: HTMLDivElement;

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);
    }

    connectedCallback(): void {
        this.#root ??= createRoot(this.#mount);
        this.#root.render(createElement(WelcomeDashboardApp));
    }

    disconnectedCallback(): void {
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

- `this.attachShadow({ mode: 'open' })` creates a **shadow DOM** — an isolated DOM subtree. Styles inside cannot bleed out; styles outside cannot bleed in.
- `createRoot(this.#mount)` creates a React root attached to the `#mount` div inside the shadow DOM.
- `this.#root.render(...)` renders the React component tree into that root. This is the bridge: the web component (Lit/native) owns the outer shell; React owns everything inside `#mount`.
- `this.#root?.unmount()` tears down the React tree and cleans up. Call this in `disconnectedCallback` to avoid memory leaks.

The data-export shell is more involved because it needs to pass props that come from Umbraco context:

```typescript
// data-export.ts  lines 67-74
#render(): void {
    this.#root?.render(
        createElement(DataExportApp, {
            contentKey: this.#contentKey,
            authFetch: this.authFetch,
        }),
    );
}
```

Every time the document key or auth fetch changes, `#render()` is called again. React compares the new props to the previous ones and only updates the parts of the DOM that actually changed.

This is the **bridge pattern** used throughout this repo: a Lit (or plain `HTMLElement`) shell handles Umbraco context and lifecycle; a React component tree handles everything the user sees.

---

## Conditional rendering

React components return JSX. Use JavaScript's own conditional operators:

```tsx
// data-export-app.tsx  lines 122-131
{!processing && contentType && !hasSelection ? (
    <p className="hint">Select at least one metadata field or property to export.</p>
) : null}

{errorMessage ? (
    <div className="errorBox">
        <uui-icon name="icon-alert"></uui-icon>
        <span>{errorMessage}</span>
    </div>
) : null}
```

`null` (and `undefined`, `false`) render nothing — the JSX equivalent of Lit's `nothing`.

---

## Lists and `key`

When rendering a list, each item must have a `key` prop — a string or number that uniquely identifies the item within that list:

```tsx
// export-options.tsx  lines 39-43
{contentTypes.map((item) => (
    <option key={item.alias} value={item.alias}>
        {item.name}
    </option>
))}
```

React uses `key` to match items between renders. Without it, React cannot tell which item was added, removed, or reordered — it would re-create all DOM nodes on every change. With `key`, React can move or patch only the affected nodes.

**C# analogy:** `key` is like the primary key on a database row — it tells React "this item in the new list is the same object as that item in the old list."

---

## Controlled inputs

In HTML, a form input has its own internal state (whatever the user typed). React typically takes over that state, making the component the "single source of truth." This is called a **controlled input**:

```tsx
// export-options.tsx  lines 33-36
<select
    className="nativeSelect"
    value={contentType?.alias ?? ''}
    onChange={onContentTypeChange}
    disabled={processing || contentTypes.length === 0}>
```

- `value={...}` sets what the select displays. React always renders what you pass — the select has no independent state.
- `onChange={...}` fires when the user changes the selection. Your handler updates state, which causes a re-render, which updates `value`. The cycle: user action → handler → `setState` → re-render → DOM update.

If you put `value` on an input without an `onChange`, React will log a warning because the user can type but nothing updates the value — the field is stuck.

---

## Custom hooks

A **custom hook** is a regular function whose name starts with `use` and which calls other hooks. It is how you extract and share stateful logic between components — equivalent to a service class or extension method in C#.

```typescript
// use-export.ts  lines 10-57
export function useExportServerData(
    contentKey: string | null,
    authFetch: AuthFetch | null,
): ExportServerData {
    const [contentTypes, setContentTypes] = useState<ContentType[]>([]);
    const [metadatas, setMetadatas] = useState<ContentMetadata[]>([]);

    useEffect(() => {
        // ... fetch content types and metadatas ...
    }, [contentKey, authFetch]);

    return { contentTypes, metadatas };
}
```

The caller (`DataExportApp`) just calls this and gets back state:

```tsx
// data-export-app.tsx  lines 19-20
const { contentTypes, metadatas: initialMetadatas } = useExportServerData(contentKey, authFetch);
const { processing, progress, errorMessage, doExport } = useExportRun(authFetch);
```

The component has no idea how the data is fetched — it just consumes the hook's return value. This is the standard React way to separate concerns: hooks own data logic, components own rendering.

---

## `DataExportApp` — annotated walkthrough

File: `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Export/src/data-export-app.tsx`

```
Props come in:
    contentKey — current document's GUID (from the Lit shell via context)
    authFetch — authenticated fetch function (from UmbAuthFetchMixin in the shell)
         ↓
Server data loaded by hooks (useExportServerData — fetches content types + metadata on mount)
         ↓
Local UI state: contentType, format, includeUnpublished, metadatas, exportableProperties
         ↓
useEffect syncs initialMetadatas (from hook) into local metadatas state so user can toggle selections
         ↓
Derived values: selectedMetadataCount, selectedPropertyCount, hasSelection, canExport
         ↓
render: <ExportOptions>, two <SelectableFieldList>, progress/error UI, Export button
```

The `useEffect` that syncs `initialMetadatas`:

```tsx
// data-export-app.tsx  lines 29-31
useEffect(() => {
    setMetadatas(initialMetadatas);
}, [initialMetadatas]);
```

When `useExportServerData` finishes fetching and sets its internal `metadatas` state, it returns a new `initialMetadatas` array. This triggers the `useEffect` here (because `initialMetadatas` changed), which copies it into local `metadatas` state so the user can toggle individual items without mutating the hook's data.

---

## `useExportRun` — `useRef` for polling cancellation

File: `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Export/src/use-export.ts`

```typescript
// use-export.ts  lines 82-90
const generationRef = useRef<number>(0);
const pollTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

// Cancel any pending poll tick when the component unmounts.
useEffect(() => {
    return () => {
        clearTimeout(pollTimerRef.current);
        generationRef.current += 1;
    };
}, []);
```

The `useEffect` with `[]` runs once on mount. Its cleanup function runs on unmount. It clears any pending `setTimeout` and increments the generation counter to invalidate any in-flight polls. This prevents "setState after unmount" — a common bug where an async operation completes after the component is gone and tries to call `setProcessing(false)` on a dead component.

When a new export starts, `doExport` immediately clears the previous timer and increments the generation, so the old poll loop is abandoned:

```typescript
// use-export.ts  lines 151-152
clearTimeout(pollTimerRef.current);
generationRef.current += 1;
```

---

## Summary

| Concept | What it does | C# analogy |
|---|---|---|
| Component | A function returning JSX; describes UI for given data | A Razor partial or view-model renderer |
| JSX | Syntactic sugar compiled to `jsx(...)` calls | Razor syntax compiled to C# render code |
| Props | Read-only inputs passed by the parent | Constructor parameters / immutable DTO |
| `useState` | Stores mutable local state; triggers re-render on change | `INotifyPropertyChanged` backing field |
| `useEffect` | Side-effect after render; cleanup on unmount/deps-change | `OnInitializedAsync` + `IDisposable.Dispose` |
| `useRef` | A mutable box that survives renders without triggering one | An instance field |
| `createRoot(...).render(...)` | Mounts a React tree into a DOM node | `WebApplication.Run()` — starts the runtime |
| `key` | Identifies list items for efficient reconciliation | Primary key / identity |
| Custom hook | A function starting with `use` that encapsulates stateful logic | A service class or extension method |
| Cleanup function | Returned from `useEffect`; runs on unmount or before re-run | `IDisposable.Dispose()` |

---

*Previous: [07 — Lit and Umbraco's backoffice element model](./07-lit.md)*
