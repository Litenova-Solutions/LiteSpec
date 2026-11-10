# Blazor Stack

**Stack:** blazor

**Concern:** frontend-ui

## Intent

The Blazor stack is the technology binding for a product that has no server of its own: a .NET domain model, browser-local persistence, and a Blazor WebAssembly application compiled to static files. A team chooses it when the product runs entirely on the user's device, under the `client-only` shape declared in [Supported Scope](../../core/scope.md). This frontend sits outside the controlled UI contract, which covers React and Svelte only, so its entry point record names the boundaries that replace that contract.

## Concepts

### What the Stack Holds

The stack is one Blazor WebAssembly application holding the whole product on the user's device: the domain model, the application handlers, the browser adapters, and the pages and features. It publishes to static files, so the product starts with no server and keeps working offline after the first load. The .NET toolchain builds, tests, and publishes the whole client. The shared frontend pages in the [Frontend overview](../README.md) state the rules the stack builds on, and the [Technology Stacks](../../core/stacks.md) page states the rules every stack shares.

### Version Pins

The `stack.dotnet` key in `litespec.manifest.yaml` pins the .NET version the stack builds with, and the `packages.nuget` keys pin every NuGet package version. These keys name the pins; the manifest holds the values.

### Structure Bindings

Blazor names the three frontend parts as folders: `Frame/` for the frame, `Routes/` for the addresses, and `Modules/` for the use-case code. A Razor component is a `.razor` file, and its code-behind is a partial class in a `.razor.cs` file beside it.

### Rendering Bindings

A Blazor route is a Razor component with an `@page` directive, and the Blazor router renders the one whose address matches the request. A shell is a layout component that the `@layout` directive names. `OnInitializedAsync` is the point where a route loads its reads before its view draws. The client-only shape holds no server code, so the server-code default has nothing to place.

### Component Bindings

A class or style binding spells the `class` or `style` attribute on a markup element. Raw HTML is a `MarkupString` value. The component boundary is Blazor's `ErrorBoundary`, and the route failure component is `Frame/RouteError.razor`. The stack has no page-code lint, so the entry point record names the review that replaces it.

### Route Entry Bindings

A route entry is one `.razor` file with an `@page` directive. Layout components and error components are not route entries, and they carry no screen mark.

### Data Bindings

The frame root, which holds client configuration and cross-route values, is `Frame/`. A component's own state is a field of its component class. The client-only shape holds no API, so the stack generates no API client.

### Testing Bindings

Logic tests run as plain .NET test projects on xunit.v3. The manifest pins no component-test library and no browser-test library for this stack.

### UI Bindings

This frontend sits outside the controlled UI contract, so it has no registry build command and no state-to-tone table. Its entry point record names the checks that replace them.

## Standards

### Keep the Blazor Frame, Routes, and Modules

**ID:** `litespec/frontend/blazor/stack#keep-the-blazor-frame-routes-and-modules`

**Strength:** required

**Statement:** A Blazor frontend MUST hold the frame in `Frame/`, one `.razor` file per address in `Routes/`, and the use-case code in `Modules/`.

**Rationale:** Three fixed roots let a reader and a tool find the frame, the addresses, and the use-case code in the same place in every Blazor frontend.

**Example:** The Claimwick expense console keeps its frame in `Frame/`, the expense list route in `Routes/ExpenseList.razor`, and the expense use-case code in `Modules/expenses/`.

**Reviewed for:** Which Blazor files sit outside `Frame/`, `Routes/`, and `Modules/`? Look in the project tree.

### Bind Blazor Routes to Their Address

**ID:** `litespec/frontend/blazor/stack#bind-blazor-routes-to-their-address`

**Strength:** required

**Statement:** A Blazor route entry MUST declare its address in one `@page` directive and take its query values as `[SupplyParameterFromQuery]` properties.

**Rationale:** The `@page` directive is the one place the router reads an address, and a query property the framework fills keeps the input typed.

**Example:** The expense list route declares `@page "/expenses"` and takes its page number as a `[SupplyParameterFromQuery]` property.

**Reviewed for:** Which route entries declare more than one `@page` directive, or read query values from the URL by hand? Look in the route files.

### Validate Blazor Route Parameters Before Loading

**ID:** `litespec/frontend/blazor/stack#validate-blazor-route-parameters-before-loading`

**Strength:** required

**Statement:** A Blazor route entry MUST validate each address parameter before its load reads it.

**Rationale:** An address comes from outside the component, so a load that reads an unchecked value fails inside the page instead of at its boundary.

**Example:** The expense list route rejects a page number below one before it reads the typed store.

**Reviewed for:** Which route loads read an address parameter before validating it? Look in each route's load.

### Load Blazor Route Data Before Its View Renders

**ID:** `litespec/frontend/blazor/stack#load-blazor-route-data-before-its-view-renders`

**Strength:** required

**Statement:** A Blazor route entry MUST read each value it shows from the typed store in `OnInitializedAsync` before its view renders.

**Rationale:** The typed store is the one shared cache on this stack, so a load there before the first render gives the view one answer for each read.

**Example:** The expense list route loads its claims from the typed store and shows its loading state until the load finishes.

**Reviewed for:** Which route views draw a read before its `OnInitializedAsync` finishes? Look in each route file.

### Use a Layout for the Blazor Shell

**ID:** `litespec/frontend/blazor/stack#use-a-layout-for-the-blazor-shell`

**Strength:** required

**Statement:** A Blazor shell MUST be a layout component that inherits `LayoutComponentBase` and holds the frame every child route shares.

**Rationale:** A layout draws the shared frame once, so each route draws only its own page.

**Example:** `Frame/MainLayout.razor` holds the navigation bar and the cross-route values every expense route shows.

**Reviewed for:** Which routes repeat frame markup instead of rendering inside the shell layout? Look in the route files.

### Keep Class and Style Bindings Out of Blazor Page Code

**ID:** `litespec/frontend/blazor/stack#keep-class-and-style-bindings-out-of-blazor-page-code`

**Strength:** required

**Statement:** A Blazor component under `Routes/` or `Modules/` MUST NOT set a `class` or `style` attribute on any element.

**Rationale:** The shared package's composites own spacing, size, and placement, so a class or style attribute in route code is a second layout that no composite answers to.

**Example:** The expense total in `Routes/ExpenseList.razor` draws through a shared composite and sets no class attribute.

**Reviewed for:** Which Blazor components under `Routes/` or `Modules/` set a `class` or `style` attribute? Look in the `.razor` files there.

### Name Blazor Drawings with the Visual Suffix

**ID:** `litespec/frontend/blazor/stack#name-blazor-drawings-with-the-visual-suffix`

**Strength:** required

**Statement:** A Blazor file holding a drawing no composite makes MUST end with `.visual.razor`.

**Rationale:** The suffix marks the one place outside the composites, so a reader and a tool find every custom drawing in one search.

**Example:** A receipt-scan preview is `ReceiptPreview.visual.razor`.

**Reviewed for:** Which custom drawings sit in `.razor` files whose names lack the `.visual.razor` suffix? Look in the project tree.

### Render Raw HTML Only Through MarkupString

**ID:** `litespec/frontend/blazor/stack#render-raw-html-only-through-markupstring`

**Strength:** required

**Statement:** A Blazor component MUST pass raw HTML to the page only as a `MarkupString` value.

**Rationale:** `MarkupString` is the one type Blazor renders without encoding, so every raw HTML sink is a type a search finds.

**Example:** The expense note renders its sanitized HTML through `(MarkupString)` after its recorded sanitization decision.

**Reviewed for:** Which Blazor components write raw HTML through another type? Look for `MarkupString` and raw markup in the `.razor` files.

### Show Route Failure in the Frame's Route Error Component

**ID:** `litespec/frontend/blazor/stack#show-route-failure-in-the-frames-route-error-component`

**Strength:** required

**Statement:** A Blazor route MUST show its failure through `Frame/RouteError.razor`, which draws the shared problem state.

**Rationale:** One route error component gives every route failure the same recovery and the same problem state.

**Example:** An expense list that fails to load shows the problem state from `Frame/RouteError.razor` with its trace identifier.

**Reviewed for:** Which route failures render outside `Frame/RouteError.razor`? Look in the route files and the frame.

### Mark Each Blazor Route Entry with Its Screen

**ID:** `litespec/frontend/blazor/stack#mark-each-blazor-route-entry-with-its-screen`

**Strength:** required

**Statement:** A Blazor route entry MUST carry the `<implements>` mark of the screen it renders on its partial class in its `.razor.cs` file.

**Rationale:** The mark links the address to the screen record the way a .NET handler carries its use-case mark, so the builder and the check read one link.

**Example:** `ExpenseList.razor.cs` carries `/// <implements>screen/claimwick-app.expense-list</implements>` on its partial class.

**Reviewed for:** Which route entries lack a screen mark on their partial class? Look in each `.razor.cs` file under `Routes/`.

### Keep Layouts and Error Components Unmarked

**ID:** `litespec/frontend/blazor/stack#keep-layouts-and-error-components-unmarked`

**Strength:** required

**Statement:** A Blazor layout component or error component MUST NOT carry a screen mark.

**Rationale:** A layout or error component draws no screen of its own, so a mark on it links an address to a screen it does not show.

**Example:** `Frame/MainLayout.razor.cs` carries no `<implements>` mark.

**Reviewed for:** Which layout or error components carry a screen mark? Look in `Frame/` and the route files.

### Build Blazor Forms on the Shared Form Entry Point

**ID:** `litespec/frontend/blazor/stack#build-blazor-forms-on-the-shared-form-entry-point`

**Strength:** required

**Statement:** A Blazor form MUST build on the form entry point of the shared package, which wraps `EditForm`.

**Rationale:** One entry point gives every form the same field wiring and the same display for a refusal on its field.

**Example:** The expense claim form in `Modules/expenses/` uses the shared form entry point and no `EditForm` of its own.

**Reviewed for:** Which Blazor forms bypass the shared form entry point? Look in the form components under `Routes/` and `Modules/`.

### Localize Blazor Copy Through a Pinned Dictionary Library

**ID:** `litespec/frontend/blazor/stack#localize-blazor-copy-through-a-pinned-dictionary-library`

**Strength:** required

**Statement:** A Blazor frontend MUST resolve its copy through a dictionary library pinned in `packages.nuget` that meets the locale-dictionary contract.

**Rationale:** A pinned library keeps every sentence in one dictionary per language, with the fill, plural, and fallback behavior the contract states.

**Example:** The Claimwick console resolves the "Receipt required" sentence through its pinned dictionary library in every language it ships.

**Reviewed for:** Which Blazor copy bypasses the pinned dictionary library, or misses fill, plural, typed keys, or a declared fallback? Look in the copy call sites.

### Keep Blazor Test Mocks to the Owned Boundaries

**ID:** `litespec/frontend/blazor/stack#keep-blazor-test-mocks-to-the-owned-boundaries`

**Strength:** required

**Statement:** A Blazor test MUST NOT replace the Razor component model, the `NavigationManager` service, or the typed store.

**Rationale:** Those three are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** An expense list test renders the real component and seeds the typed store with claims.

**Reviewed for:** Which tests replace the component model, `NavigationManager`, or the typed store? Look in the test doubles.

### Run Blazor Logic Tests on xunit.v3

**ID:** `litespec/frontend/blazor/stack#run-blazor-logic-tests-on-xunitv3`

**Strength:** required

**Statement:** A Blazor frontend MUST run its logic tests as plain .NET test projects on `xunit.v3`, the version `packages.nuget` pins.

**Rationale:** Logic tests need no browser, so a plain .NET runner keeps them fast, and one pinned runner keeps every test project on the same version.

**Example:** The expense total rule has its test beside its module, in an xunit.v3 test project.

**Reviewed for:** Which logic tests run on another runner, or outside a plain .NET test project? Look in the test projects.

### Declare Blazor Outside the Controlled UI Contract

**ID:** `litespec/frontend/blazor/stack#declare-blazor-outside-the-controlled-ui-contract`

**Strength:** required

**Statement:** A Blazor frontend MUST declare an entry point record naming Blazor and the boundaries that replace the controlled UI contract.

**Rationale:** The controlled contract covers React and Svelte, so a Blazor frontend declares what replaces its checks instead of reporting as one with nothing to check.

**Example:** The Claimwick Blazor entry point record names Blazor and the shared package boundaries that replace the controlled contract.

**Reviewed for:** Which Blazor frontends declare no entry point record naming Blazor and its replacement boundaries? Look in the entry point records.

## Conventions

None.
