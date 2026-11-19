# Blazor Stack

**Stack:** blazor

**Concern:** frontend-ui

The Blazor stack is the technology binding for a product that has no server of its own: a .NET domain model, browser-local persistence, and a Blazor WebAssembly application compiled to static files. A team chooses it when the product runs entirely on the user's device, under the `client-only` shape declared in [Supported Scope](../../core/scope.md).

The stack is one Blazor WebAssembly application holding the whole product: the domain model, the application handlers, the browser adapters, and the pages and features. The product keeps working offline after the first load. The .NET toolchain builds, tests, and publishes the whole client.

The shared frontend pages in the [Frontend overview](../README.md) state the rules the stack builds on, and the [Technology Stacks](../../core/stacks.md) page states the rules every stack shares. This frontend sits outside the controlled UI contract, which covers React and Svelte only, so its entry point record names the boundaries that replace that contract.

## Concepts

### Where Each File Lives

Blazor names the three frontend parts as folders: `Frame/` holds the frame, `Routes/` holds one `.razor` file per address, and `Modules/` holds the use-case code at `{module}/{aggregates}/{use-case}/` in kebab case, mirroring the backend `Application/` folder the way the web mirror does.

A Razor component is a `.razor` file with its code-behind in a partial class in the `.razor.cs` file beside it. On a route entry, that partial class also carries the screen mark. For example, the Claimwick expense console keeps its frame in `Frame/`, its expense list route in `Routes/ExpenseList.razor`, and its list-expenses use-case code in `Modules/spending/expenses/list-expenses/`.

The client-only shape holds no server code, so the server-only code default in [Keep Server-Only Code Identifiable](../rendering.md#keep-server-only-code-identifiable) places nothing.

### How a Route Loads Its Data

Each route is a Razor component carrying an `@page` directive, and the Blazor router renders the one whose address matches the request. The address comes from outside the component, so the route validates each address parameter before its load reads it.

A shell is a layout component named by the `@layout` directive. It draws the shared frame once, so each route draws only its own page.

`OnInitializedAsync` is the point where a route loads its reads before its view draws, reading each value it shows from the typed store, the one shared cache on this stack. Loading there gives the view one answer for each read, with the loading state covering the wait.

### Which Files Declare an Address

A route entry is one `.razor` file with an `@page` directive. The entry carries the `<implements>` mark of the screen it renders on its partial class in its `.razor.cs` file, the way a .NET handler carries its use-case mark. The mark links the address to its screen record, so the builder and the check read the link from the class beside the route file.

Layout components and error components are not route entries. They draw no screen of their own, so they carry no screen mark: a mark on one would link an address to a screen it does not show.

### How Components Draw

Route and module code draws through the shared package's composites, which own spacing, size, and placement. A class or style binding is a `class` or `style` attribute on a markup element, and page code sets neither. A class or style attribute in route code is a second layout answering to no composite.

Two cases fall outside the composites: a drawing that no composite makes, which lives in a visual file, and raw HTML, which page code passes only as a `MarkupString` value. `MarkupString` is the one type Blazor renders without encoding, so every raw HTML sink is a type a search finds.

### How Routes Show Failure

When a route fails, it shows the failure through `Frame/RouteError.razor`, which draws the shared problem state. Every route offers the same recovery and the same problem state. The component boundary is Blazor's `ErrorBoundary`. The stack has no page-code lint, so the entry point record names the review that replaces it.

### Where Data Lives

The typed store is the one shared cache on this stack, and a component's own state is a field of its component class. `Frame/` is the frame root holding client configuration and cross-route values. Forms build on the shared package's form entry point, which wraps `EditForm`, so every form wires its fields and reports field refusals the same way.

### How Copy Is Localized

The client-only shape holds no API, so the stack generates no API client. Copy resolves through a dictionary library pinned in `packages.nuget`, under the locale-dictionary contract. The contract states one dictionary per language, with fill, plural, and fallback behavior, in every language the product ships.

### How Tests Run Without a Browser

Logic tests run as plain .NET test projects on xunit.v3, because the rules they prove need no browser. The manifest pins no component-test library and no browser-test library for this stack. Tests replace neither the Razor component model, the `NavigationManager` service, nor the typed store. For example, the expense total rule has its test beside its module, in an xunit.v3 test project.

### What Replaces the Controlled UI Contract

The controlled UI contract covers React and Svelte, so a Blazor frontend has no registry build command and no state-to-tone table. Its entry point record names Blazor and the boundaries that replace that contract, so reviewers check the named replacements instead of finding nothing to check.

### Which Versions Are Pinned

The `stack.dotnet` key in `litespec.manifest.yaml` pins the .NET version the stack builds with, and the `packages.nuget` keys pin every NuGet package version. The manifest holds the values.

## Standards

### Keep the Blazor Frame, Routes, and Modules

**ID:** `litespec/frontend/blazor/stack#keep-the-blazor-frame-routes-and-modules`

**Strength:** required

**Statement:** A Blazor frontend MUST hold the frame in `Frame/`, one `.razor` file per address in `Routes/`, and the use-case code in `Modules/` at `{module}/{aggregates}/{use-case}/` in kebab case, mirroring the backend `Application/` folder.

**Rationale:** Three fixed roots let a reader and a tool find the frame, the addresses, and the use-case code in the same place in every Blazor frontend. Under `Modules/`, the three-segment mirror translates each backend use-case path by rule, with a module-level use case dropping the aggregate segment in both trees.

**Example:** The Claimwick expense console keeps its frame in `Frame/`, its expense list route in `Routes/ExpenseList.razor`, and its list-expenses use-case code in `Modules/spending/expenses/list-expenses/`.

**Reviewed for:** Does every Blazor file sit in `Frame/`, `Routes/`, or `Modules/`? Check the project tree.

### Bind Blazor Routes to Their Address

**ID:** `litespec/frontend/blazor/stack#bind-blazor-routes-to-their-address`

**Strength:** required

**Statement:** A Blazor route entry MUST declare its address in one `@page` directive and take its query values as `[SupplyParameterFromQuery]` properties.

**Rationale:** The `@page` directive is the one place the router reads an address, and a query property the framework fills keeps the input typed.

**Example:** The Claimwick expense list declares `@page "/expenses"` and takes its page number as a `[SupplyParameterFromQuery]` property.

**Reviewed for:** Does every route entry declare exactly one `@page` directive and read query values only through `[SupplyParameterFromQuery]` properties? Check the route files.

### Validate Blazor Route Parameters Before Loading

**ID:** `litespec/frontend/blazor/stack#validate-blazor-route-parameters-before-loading`

**Strength:** required

**Statement:** A Blazor route entry MUST validate each address parameter before its load reads it.

**Rationale:** An address comes from outside the component, so a load that reads an unchecked value fails inside the page instead of at its boundary.

**Example:** The Claimwick expense list rejects a page number below one before reading the typed store.

**Reviewed for:** Does every route load validate each address parameter before reading it? Check each route's load.

### Load Blazor Route Data Before Its View Renders

**ID:** `litespec/frontend/blazor/stack#load-blazor-route-data-before-its-view-renders`

**Strength:** required

**Statement:** A Blazor route entry MUST read each value it shows from the typed store in `OnInitializedAsync` before its view renders.

**Rationale:** The typed store is the one shared cache on this stack, so a load there before the first render gives the view one answer for each read.

**Example:** The Claimwick expense list loads its claims from the typed store, showing its loading state until the load finishes.

**Reviewed for:** Does every route view wait for its `OnInitializedAsync` before drawing a read? Check each route file.

### Use a Layout for the Blazor Shell

**ID:** `litespec/frontend/blazor/stack#use-a-layout-for-the-blazor-shell`

**Strength:** required

**Statement:** A Blazor shell MUST be a layout component that inherits `LayoutComponentBase` and holds the frame every child route shares.

**Rationale:** A layout draws the shared frame once, so each route draws only its own page.

**Example:** In the Claimwick console, `Frame/MainLayout.razor` holds the navigation bar and cross-route values, and every expense route renders inside it.

**Reviewed for:** Does every route render inside the shell layout instead of repeating frame markup? Check the route files.

### Keep Class and Style Bindings Out of Blazor Page Code

**ID:** `litespec/frontend/blazor/stack#keep-class-and-style-bindings-out-of-blazor-page-code`

**Strength:** required

**Statement:** A Blazor component under `Routes/` or `Modules/` MUST NOT set a `class` or `style` attribute on any element.

**Rationale:** The shared package's composites own spacing, size, and placement, so a class or style attribute in route code is a second layout that no composite answers to.

**Example:** The expense total in `Routes/ExpenseList.razor` draws through a shared composite and sets no class attribute.

**Reviewed for:** Do any Blazor components under `Routes/` or `Modules/` set a `class` or `style` attribute? Check the `.razor` files there.

### Name Blazor Drawings with the Visual Suffix

**ID:** `litespec/frontend/blazor/stack#name-blazor-drawings-with-the-visual-suffix`

**Strength:** required

**Statement:** A Blazor file holding a drawing no composite makes MUST end with `.visual.razor`.

**Rationale:** The suffix marks the one place outside the composites, so a reader and a tool find every custom drawing in one search.

**Example:** The Claimwick receipt-scan preview lives in `ReceiptPreview.visual.razor`.

**Reviewed for:** Does every file holding a custom drawing end with `.visual.razor`? Check the project tree.

### Render Raw HTML Only Through MarkupString

**ID:** `litespec/frontend/blazor/stack#render-raw-html-only-through-markupstring`

**Strength:** required

**Statement:** A Blazor component MUST pass raw HTML to the page only as a `MarkupString` value.

**Rationale:** `MarkupString` is the one type Blazor renders without encoding, so every raw HTML sink is a type a search finds.

**Example:** The Claimwick expense note renders its sanitized HTML through `(MarkupString)`, matching its recorded sanitization decision.

**Reviewed for:** Does every raw HTML write go through a `MarkupString` value? Check for `MarkupString` and raw markup in the `.razor` files.

### Show Route Failure in the Frame's Route Error Component

**ID:** `litespec/frontend/blazor/stack#show-route-failure-in-the-frames-route-error-component`

**Strength:** required

**Statement:** A Blazor route MUST show its failure through `Frame/RouteError.razor`, which draws the shared problem state.

**Rationale:** One route error component gives every route failure the same recovery and the same problem state.

**Example:** When the Claimwick expense list fails to load, `Frame/RouteError.razor` shows the problem state with its trace identifier.

**Reviewed for:** Does every route failure render through `Frame/RouteError.razor`? Check the route files and the frame.

### Mark Each Blazor Route Entry with Its Screen

**ID:** `litespec/frontend/blazor/stack#mark-each-blazor-route-entry-with-its-screen`

**Strength:** required

**Statement:** A Blazor route entry MUST carry the `<implements>` mark of the screen it renders on its partial class in its `.razor.cs` file.

**Rationale:** The mark links the address to the screen record the way a .NET handler carries its use-case mark, so the builder and the check read a single link.

**Example:** The Claimwick `ExpenseList.razor.cs` carries `/// <implements>screen/claimwick-app.expense-list</implements>` on its partial class.

**Checked by:** `litespec model check` reports `code-placement-drift` for a screen mark outside the `Routes/` root.

### Keep Layouts and Error Components Unmarked

**ID:** `litespec/frontend/blazor/stack#keep-layouts-and-error-components-unmarked`

**Strength:** required

**Statement:** A Blazor layout component or error component MUST NOT carry a screen mark.

**Rationale:** A layout or error component draws no screen of its own, so a mark on it links an address to a screen it does not show.

**Example:** The Claimwick `Frame/MainLayout.razor.cs` carries no `<implements>` mark.

**Reviewed for:** Do any layout or error components carry a screen mark? Check `Frame/` and the route files.

### Build Blazor Forms on the Shared Form Entry Point

**ID:** `litespec/frontend/blazor/stack#build-blazor-forms-on-the-shared-form-entry-point`

**Strength:** required

**Statement:** A Blazor form MUST build on the form entry point of the shared package, which wraps `EditForm`.

**Rationale:** One entry point gives every form the same field wiring and the same display for a field refusal.

**Example:** The Claimwick expense claim form in `Modules/spending/expenses/claim-expense/` uses the shared form entry point, with no `EditForm` of its own.

**Reviewed for:** Does every Blazor form build on the shared form entry point? Check the form components under `Routes/` and `Modules/`.

### Localize Blazor Copy Through a Pinned Dictionary Library

**ID:** `litespec/frontend/blazor/stack#localize-blazor-copy-through-a-pinned-dictionary-library`

**Strength:** required

**Statement:** A Blazor frontend MUST resolve its copy through a dictionary library pinned in `packages.nuget` that meets the locale-dictionary contract.

**Rationale:** A pinned library keeps every sentence in one dictionary per language, with the fill, plural, and fallback behavior the contract states.

**Example:** The Claimwick console resolves the "Receipt required" sentence through its pinned dictionary library in every language it ships.

**Reviewed for:** Does every copy call site use the pinned dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites.

### Keep Blazor Test Mocks to the Owned Boundaries

**ID:** `litespec/frontend/blazor/stack#keep-blazor-test-mocks-to-the-owned-boundaries`

**Strength:** required

**Statement:** A Blazor test MUST NOT replace the Razor component model, the `NavigationManager` service, or the typed store.

**Rationale:** Those three are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** A Claimwick expense list test renders the real component and seeds the typed store with claims.

**Reviewed for:** Does any test replace the component model, `NavigationManager`, or the typed store? Check the test doubles.

### Run Blazor Logic Tests on xunit.v3

**ID:** `litespec/frontend/blazor/stack#run-blazor-logic-tests-on-xunitv3`

**Strength:** required

**Statement:** A Blazor frontend MUST run its logic tests as plain .NET test projects on `xunit.v3`, the version `packages.nuget` pins.

**Rationale:** Logic tests need no browser, so a plain .NET runner keeps them fast, and one pinned runner keeps every test project on the same version.

**Example:** The Claimwick expense total rule has its test beside its module, in an xunit.v3 test project.

**Reviewed for:** Does every logic test run in a plain .NET test project on the pinned `xunit.v3`? Check the test projects.

### Declare Blazor Outside the Controlled UI Contract

**ID:** `litespec/frontend/blazor/stack#declare-blazor-outside-the-controlled-ui-contract`

**Strength:** required

**Statement:** A Blazor frontend MUST declare an entry point record naming Blazor and the boundaries that replace the controlled UI contract.

**Rationale:** The controlled contract covers React and Svelte, so a Blazor frontend with no declaration looks like a frontend with nothing to check. Naming Blazor and its replacement boundaries reports it as a frontend with named replacements instead.

**Example:** The Claimwick Blazor entry point record names Blazor and the shared package boundaries standing in for the controlled contract.

**Reviewed for:** Does every Blazor frontend declare an entry point record naming Blazor and its replacement boundaries? Check the entry point records.

## Conventions

None.
