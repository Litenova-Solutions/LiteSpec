# Frontend Components

## Intent

Every screen in a LiteSpec frontend draws with components from one shared UI package: small primitives such as inputs and badges, and larger composites such as sections and tables built from them. This page states what that package holds, how page code, the components under each route or module folder, composes its parts, and where a drawing that no composite makes belongs. Frontend developers and coding agents apply it whenever they build or change a screen, so the product keeps one visual language and a restyle stays a change in one place.

## Concepts

### Primitives, Composites, and Page Code

A primitive is a small generic control from the pinned baseline, such as an input, a badge, or a card. A composite is a shared component that fixes the layout, behavior, and accessibility for one job, such as a status badge or a data table, built from primitives. Page code is any component under a route or module folder: it composes composites inside one floorplan and declares no layout or styling of its own. Page code lives under the route and module folders that [Frontend Structure](structure.md) defines, and the floorplan is the page component that fixes the page layout ([Controlled UI Governance](ui.md)).

For example, the `Badge` primitive draws a label in any color, while the `StatusBadge` composite maps a business state to its tone and draws that. An expense page imports `StatusBadge`, so every color it draws is one the product agreed to.

### The Shared UI Package

The shared UI package holds every primitive and every composite in one place, behind documented entry points that applications import. The boundary is enforced rather than described: a page that reaches past the entry points, declares its own styling, or draws with raw elements fails lint, and each refusal names the composite to use instead. The author therefore gets the replacement in the same message as the refusal and does not have to search the package.

### Drawings with No Composite

Some drawings have no composite: a budget treemap, a receipt-scan preview, or a public marketing frame. They live in exception files whose names declare the exception, and they draw with semantic theme tokens, the named values the shared token definition holds. The file name keeps the exception visible, and the tokens keep it inside the product palette.

## Standards

### Keep Shared UI in One Package

**ID:** `litespec/frontend/components#keep-shared-ui-in-one-package`

**Statement:** A workspace MUST hold its shared primitives and its composites in one shared UI package, with no copy in an application.

**Rationale:** When two applications each hold a copy of one primitive, the first upstream update changes one copy and misses the other. Both copies still compile, so nothing reports the split, and the two applications drift into two visual languages. One package gives every application the same component, so one update reaches every screen.

**Example:** The primitives sit in one folder beside the composites, and the composites import them.

**Reviewed for:** Which shared primitives or composites live outside the shared package? Look in the workspace packages and the application sources.

### Compose Composites in Page Code

**ID:** `litespec/frontend/components#compose-composites-in-page-code`

**Statement:** A component under a route or module folder MUST import only the shared package's documented entry points.

**Rationale:** A composite fixes its layout and exposes only the inputs its job needs, so every page that uses it draws the same shape. If page code could reach past the composite to the primitives, one page would tune spacing or order for itself, and the next page would copy that tuning instead of the composite. The product would then hold as many layouts as pages.

**Example:** An expense section in the expenses app draws its facts, its state, and its commands through shared composites, and declares no element, class, or style binding.

```text
EntitySection "title"
  StatusBadge "state"
  DetailActions "commands"
```

The section imports only the shared package entry point, so the layout it draws is the one the shared package owns for every other entity page.

**Checked by:** `litespec ui` reports `[litespec/frontend/components#compose-composites-in-page-code]` with the file and the refused import. Run it at the consumer root.

### Write No Class Name in Page Code

**ID:** `litespec/frontend/components#write-no-class-name-in-page-code`

**Statement:** A component under a route or module folder MUST NOT declare a class or style binding.

**Rationale:** The floorplan and the display composites own every spacing, size, and placement choice on a page. A class or style binding in route code is a second layout that answers to no composite, and the next route copies it because it looks like the local way to draw. Two sources of layout then disagree on the first redesign, so keeping classes out of page code leaves the floorplan as the one place a visual change lands.

Each stack page names the class and style bindings this rule covers for its stack.

**Reviewed for:** Which class or style bindings appear in route and module sources? Look in route and module sources; each stack page names the bindings its lint refuses.

### Use Composites for Layout and Interaction in Page Code

**ID:** `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`

**Statement:** A component under a route or module folder MUST NOT declare a layout or interactive element.

**Rationale:** A raw element is only markup: it carries no focus handling, no state behavior, and no accessibility wiring, so each page that declares one rebuilds behavior the composite already carries, usually worse. The lint refusal names the composite to use instead, so the author gets the replacement in the same message as the refusal and does not have to search the shared package.

**Reviewed for:** Which layout or interactive elements appear in route and module sources? Look in route and module sources; each stack page names the refused elements and the composite each refusal names.

### Prefer a Composite over a Primitive

**ID:** `litespec/frontend/components#prefer-a-composite-over-a-primitive`

**Statement:** Page code MUST import a composite wherever the shared package provides one.

**Rationale:** The composite holds the product's decision for one job: which layout, which behavior, which tone. The bare primitive holds none of those, so a page that imports it makes each decision again for itself, and two pages answer the same question two ways. Refusing the replaced primitive in page code sends every page to the decision already made.

**Example:** `Card` is `Section`, `Table` is `DataTable`, `Badge` is `StatusBadge`, and `Input` and `Field` are the form fields.

**Reviewed for:** Which page sources import a primitive where the shared package provides a composite? Look in route and module imports.

### Place a Drawing No Composite Makes in an Exception File

**ID:** `litespec/frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file`

**Statement:** An exception file MAY declare custom styling only with semantic theme tokens.

**Rationale:** Some drawings have no composite: a budget treemap, a receipt-scan preview, or a public marketing frame. Refusing them entirely would push the drawing into arbitrary values that no token governs, which is harder to find and restyle than a declared exception. The exception file keeps the drawing visible: its name says this page draws outside the composites, and semantic tokens keep it inside the product palette. Each stack page names the file pattern that marks an exception for its stack.

**Reviewed for:** Which custom styling sits outside the files whose names mark drawings outside the composites, or uses values other than semantic theme tokens? Look in the exception files each stack page names.

### Add a Missing Composite to the Shared Package

**ID:** `litespec/frontend/components#add-a-missing-composite-to-the-shared-package`

**Statement:** A layout, a control, or a surface a second page needs MUST be added to the shared UI package, not written inside an application.

**Rationale:** The second page that needs a layout, control, or surface proves the piece is shared, not page-specific. Written inside one application, it is reviewed against one use, styled for one context, and invisible to the floorplans that would carry it, so the third page writes it a third time or copies the first copy. Adding it to the shared package puts it where every page finds it and where its design is judged against every use.

**Reviewed for:** Which layout, control, or surface drawn by two pages is missing from the shared package exports? Look in the shared package exports and the drawing pages.

### Support Keyboard, Focus, Labels, and Announcements

**ID:** `litespec/frontend/components#support-keyboard-focus-labels-and-announcements`

**Statement:** Interactive UI MUST support keyboard operation, visible focus, semantic elements, programmatic labels, and asynchronous status announcements.

**Rationale:** Keyboard operation, visible focus, semantic elements, programmatic labels, and status announcements come free with platform controls and all break at once in a custom control. A mouse-only control locks out keyboard readers, a missing label leaves screen-reader users guessing, and a silent update leaves anyone who does not see the screen change unaware of it. Starting from a platform control keeps all five properties, and the composite keeps them for every page that uses it.

The five properties above are the ones a component can satisfy on its own. [WCAG 2.2](https://www.w3.org/TR/WCAG22/) adds criteria that only a flow or a page can satisfy, so each route answers them where the criterion applies:

| Criterion | Level | What it means for the UI |
|:---|:---|:---|
| 2.4.11 Focus Not Obscured (Minimum) | AA | A focused control stays at least partly visible under a sticky header, bar, or panel. |
| 2.4.12 Focus Not Obscured (Enhanced) | AAA | A focused control stays fully visible. |
| 2.4.13 Focus Appearance | AAA | The focus indicator meets the stated minimum area and contrast. |
| 2.5.7 Dragging Movements | AA | Every drag action has a single-pointer alternative. |
| 2.5.8 Target Size (Minimum) | AA | A pointer target measures at least 24 by 24 pixels, or meets the stated spacing. |
| 3.2.6 Consistent Help | A | A help mechanism sits in the same relative order on every page that has one. |
| 3.3.7 Redundant Entry | A | Information already given in the same process is not asked for again. |
| 3.3.8 Accessible Authentication (Minimum) | AA | Sign-in offers a path with no cognitive function test. |
| 3.3.9 Accessible Authentication (Enhanced) | AAA | That path asks for no object recognition or personal content test either. |

The level column marks which target each criterion belongs to: AA for every project, AAA for a project that targets AAA.

**Reviewed for:** Which interactive UI lacks keyboard operation, visible focus, semantic elements, programmatic labels, or status announcements? Look in the route suite output.

### Render Data States

**ID:** `litespec/frontend/components#render-data-states`

**Statement:** A data component MUST render loading, empty, error, forbidden, and ready.

**Rationale:** A component that renders only its ready state leaves the reader with a blank region while data loads, after it fails, or when there is nothing to show. Each missing state is a screen with no design and no test visiting it. Naming every arrival state up front gives each one a designed drawing.

A route adds `not-found` under `litespec/frontend/rendering#represent-route-states`, because a missing target resolves at the route rather than inside a component. Each state name is a term in the project glossary.

**Reviewed for:** Which data components miss a loading, empty, error, forbidden, or ready state, or name a state outside the glossary? Look in the shared package components and the glossary.

### Render Control States

**ID:** `litespec/frontend/components#render-control-states`

**Statement:** A control MUST render disabled and pending so that a second submission stays impossible while one is in flight and recovery stays usable.

**Rationale:** A control with no disabled state invites a second submission while the first is still in flight, and a control with no pending state shows no sign that anything is happening. Blocking the duplicate submission and keeping recovery usable give the reader one safe path: wait, or recover. State names are glossary terms, as under `litespec/frontend/components#render-data-states`.

**Reviewed for:** Which controls miss a disabled or pending state, allow a second submission while one is in flight, or leave recovery unusable? Look in the shared package components.

### Keep Component Inputs Narrow

**ID:** `litespec/frontend/components#keep-component-inputs-narrow`

**Statement:** A composite MUST receive the values and callbacks it needs, not a client, a store, or an aggregate.

**Rationale:** The shared package serves applications it was not written against. A composite that takes a client, a store, or an aggregate takes one application's data model as its input, so it fits no application with a different model, and a change to that model breaks the shared package. Values and callbacks keep the composite free of any application's shape: the page translates its model into plain inputs, and the composite renders what it is given.

**Reviewed for:** Which composite inputs name a client, a store, or an aggregate instead of values and callbacks? Look in the shared package component signatures.

### Use Declared Visual Variants

**ID:** `litespec/frontend/components#use-declared-visual-variants`

**Statement:** A repeated component variant MUST use theme tokens through the shared package's variant helper rather than repeated literal values.

**Rationale:** When each composite writes its own pixel values, colors, or conditional branches for one variant, the copies start equal and drift with every later edit, because nothing points the author at the other copies. The variant helper holds each variant once, in theme tokens, so a change to the variant reaches every composite that uses it and every value stays inside the theme.

**Reviewed for:** Which repeated variants bypass the shared variant helper or carry literal values in composites? Look in the shared package components.

### Sanitize Untrusted Rich Content

**ID:** `litespec/frontend/components#sanitize-untrusted-rich-content`

**Statement:** A component MUST NOT render untrusted rich content without its recorded sanitization decision.

**Rationale:** Rich content written by one user renders in another user's browser, so unsanitized markup is a script injection path: whoever writes the content runs code as every reader. The recorded decision names the sanitizer, the allowed elements and attributes, the link policy, and the test cases, so the boundary is explicit and reviewable instead of a different ad-hoc escape on every page.

**Reviewed for:** Which untrusted rich content renders without a recorded decision naming the sanitizer, the allowed elements and attributes, the link policy, and the test cases? Look in the decision records and the rendering components.

### Use a Composite for a Content Image

**ID:** `litespec/frontend/components#use-a-composite-for-a-content-image`

**Statement:** An informative or decorative image MUST render through the shared image component, which owns its shape, its sizing, and its fallback.

**Rationale:** An informative image needs meaningful alternative text, and a decorative image needs empty alternative text so assistive tools skip it. A source and label written directly in a page bypass both rules: the author tracks which kind each image is and writes the matching text, on every page. The shared image component owns the shape, the sizing, the fallback, and the text policy, so each image gets the handling its kind needs. The raw-element rule refuses unwrapped images in page code under `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`.

**Reviewed for:** Which content images render outside the shared image component or against the text policy for their kind? Look in page sources for unwrapped images.

## Conventions

### Name Components for Their Role

**ID:** `litespec/frontend/components#name-components-for-their-role`

**Default:** Name a component for the role it plays, such as `ExpenseLinesSection` or `ReplayDeliveryDialog`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A name such as `ExpenseComponent` or `GenericModal` describes the file type, so it fits any component and tells a searcher nothing. A name that states the role, such as `ExpenseLinesSection` or `ReplayDeliveryDialog`, tells the next author what the component does before they open it, and keeps two components for two jobs from sharing one vague name.

**Reviewed for:** Which component names hide the role they play? Look in the shared package and module sources.

### Expose Card and Section Titles as Headings

**ID:** `litespec/frontend/components#expose-card-and-section-titles-as-headings`

**Default:** Render a visible card, panel, or section title as a heading at its correct level.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Some primitive sources render a title prop as a plain text element. That removes the title from the page outline, so screen-reader users lose it as a navigation point and the route suite's heading check misses it. Rendering the title as a heading at its correct level keeps the outline and the test in agreement.

**Reviewed for:** Which visible card, panel, or section titles render as non-heading elements? Look in the route suite output, which resolves each as a heading.

### Keep Domain Values Typed Until Display

**ID:** `litespec/frontend/components#keep-domain-values-typed-until-display`

**Default:** Keep branded and generated identifier types until the presentation boundary converts them.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Dates, money, and status values carry meaning a display string loses: the instant, the amount, the code. Converting early spreads formatting choices across every component that touches the value, and each one picks its own locale and zone. Keeping the typed value until the presentation boundary converts it in one place, with explicit locale and zone behavior.

**Reviewed for:** Which display strings are produced before the presentation boundary, or without an explicit locale and zone? Look in the presentation boundary and the components feeding it.

### Keep Error Boundaries Scoped

**ID:** `litespec/frontend/components#keep-error-boundaries-scoped`

**Default:** Use the route error surface for route failure, and a component boundary only where recovery keeps the page usable.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** An error boundary that catches a failure with no recovery of its own hides the error and leaves the surrounding page broken but silent. The route error surface owns route failure, with a designed recovery; a component boundary earns its place only where the rest of the page stays usable without the failed part. Each stack page names the error surface for its stack.

**Reviewed for:** Which route failures render outside the route error surface, and which component boundaries leave their surroundings unusable? Look in the route error surfaces and the boundary components.

## Reference Example

This informative example demonstrates `litespec/frontend/components#compose-composites-in-page-code` and `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`.

An expense section renders its facts, its commands, and its states by composing display composites and shared form fields. It declares no element and no styling, so the layout it draws is the one every other entity page draws.
