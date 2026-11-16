# Frontend Components

**Stack:** any
**Concern:** frontend-ui

Every screen in a LiteSpec frontend draws with components from one shared UI package: small primitives such as inputs and badges, and larger composites such as sections and tables built from them.

This page states what that package holds, how page code composes its parts, and where a drawing that no composite makes belongs. Page code means the components under each route or module folder. Frontend developers and coding agents apply it whenever they build or change a screen, so the product keeps one visual language and a restyle stays a change in one place.

## Concepts

### Primitives, Composites, and Page Code

A primitive is a small generic control from the pinned baseline, such as an input, a badge, or a card. A composite is a shared component fixing the layout, behavior, and accessibility for one job, such as a status badge or a data table, built from primitives. Page code is any component under a route or module folder. It composes composites inside one floorplan and declares no layout or styling of its own. The floorplan is the page component fixing the page layout ([Controlled UI Governance](ui.md)), and page code lives under the route and module folders [Frontend Structure](structure.md) defines.

The `Badge` primitive, for example, draws a label in any color, while the `StatusBadge` composite maps a business state to its tone and draws that. An expense page imports `StatusBadge`, so every color it draws is one the product agreed to.

### The Shared UI Package

The shared UI package holds every primitive and every composite in one place, behind documented entry points that applications import.

The boundary is enforced rather than described. A page reaching past the entry points, declaring its own styling, or drawing with raw elements fails lint, and each refusal names the composite to use instead, so the author gets the replacement in the same message as the refusal.

### Drawings with No Composite

Some drawings have no composite, such as a budget treemap, a receipt-scan preview, or a public marketing frame. They live in exception files whose names declare the exception, and they draw with semantic theme tokens, the named values the shared token definition holds. The file name keeps the exception visible, and the tokens keep it inside the product palette.

## Standards

### Keep Shared UI in One Package

**ID:** `litespec/frontend/components#keep-shared-ui-in-one-package`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST hold its shared primitives and its composites in one shared UI package, with no copy in an application.

**Rationale:** Two applications holding one primitive each drift at the first upstream update. One copy changes, the other still compiles, and nothing reports the split. The two applications grow into two visual languages. One package gives every application the same component, so one update reaches every screen.

**Example:** The shared UI package holds the primitives in one folder beside the composites, and the composites import them.

**Reviewed for:** Does every shared primitive and composite live in the shared package? Look in the workspace packages and the application sources.

### Compose Composites in Page Code

**ID:** `litespec/frontend/components#compose-composites-in-page-code`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component under a route or module folder MUST import only the shared package's documented entry points.

**Rationale:** A composite fixes its layout and exposes only the inputs its job needs, so every page using it draws the same shape. If page code could reach past the composite to the primitives, one page would tune spacing or order for itself, and the next page would copy that tuning instead of the composite. The product would then hold as many layouts as pages.

**Example:** An expense section in the expenses app draws its facts, its state, and its commands through shared composites, declaring no element, class, or style binding.

```text
EntitySection "title"
  StatusBadge "state"
  DetailActions "commands"
```

The section imports only the shared package entry point, so the layout it draws is the one the shared package owns for every other entity page.

**Checked by:** `litespec model check` reports `ui-import-invalid` with the file and the refused import, tagged `[litespec/frontend/components#compose-composites-in-page-code]`. Run it at the consumer root.

### Write No Class Name in Page Code

**ID:** `litespec/frontend/components#write-no-class-name-in-page-code`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component under a route or module folder MUST NOT declare a class or style binding.

**Rationale:** The floorplan and the display composites own every spacing, size, and placement choice on a page. A class or style binding in route code is a second layout answering to no composite, and the next route copies it because it looks like the local way to draw. Two sources of layout disagree on the first redesign. Keeping classes out of page code leaves the floorplan as the one place a visual change lands.

Each stack page names the class and style bindings this rule covers for its stack.

**Reviewed for:** Does any class or style binding appear in route and module sources? Look in route and module sources; each stack page names the bindings its lint refuses.

### Use Composites for Layout and Interaction in Page Code

**ID:** `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component under a route or module folder MUST NOT declare a layout or interactive element.

**Rationale:** A raw element is only markup, with no focus handling, no state behavior, and no accessibility wiring. Each page declaring one rebuilds behavior the composite already carries, usually worse. The lint refusal names the composite to use instead, so the author gets the replacement in the same message as the refusal, with no search through the shared package.

**Reviewed for:** Does any layout or interactive element appear in route and module sources? Look in route and module sources; each stack page names the refused elements and the composite each refusal names.

### Prefer a Composite over a Primitive

**ID:** `litespec/frontend/components#prefer-a-composite-over-a-primitive`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Page code MUST import a composite wherever the shared package provides one.

**Rationale:** The composite holds the product's decision for one job: its layout, behavior, and tone. The bare primitive holds none of those, so a page importing it makes each decision again for itself, and two pages answer the same question two ways. Refusing the replaced primitive in page code sends every page to the decision already made.

**Example:** Where the package provides a composite, page code uses it: `Card` becomes `Section`, `Table` becomes `DataTable`, `Badge` becomes `StatusBadge`, and `Input` and `Field` become the form fields.

**Reviewed for:** Does any page source import a primitive where the shared package provides a composite? Look in route and module imports.

### Place a Drawing No Composite Makes in an Exception File

**ID:** `litespec/frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file`

**Strength:** required

**Applies to:** artifact: component

**Statement:** An exception file MUST declare custom styling only with semantic theme tokens.

**Rationale:** Some drawings have no composite, such as a budget treemap, a receipt-scan preview, or a public marketing frame. Refusing them entirely would push the drawing into arbitrary values no token governs, which is harder to find and restyle than a declared exception. The exception file keeps the drawing visible. Its name says the page draws outside the composites, and semantic tokens keep it inside the product palette. Each stack page names the file pattern marking an exception for its stack.

**Reviewed for:** Does all custom styling sit in the files whose names mark drawings outside the composites, using only semantic theme tokens? Look in the exception files each stack page names.

### Add a Missing Composite to the Shared Package

**ID:** `litespec/frontend/components#add-a-missing-composite-to-the-shared-package`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A layout, a control, or a surface a second page needs MUST be added to the shared UI package, not written inside an application.

**Rationale:** The second page needing a layout, control, or surface proves the piece is shared, not page-specific. Written inside one application, it is reviewed against one use, styled for one context, and invisible to the floorplans that would carry it, so the third page writes it a third time or copies the first copy. Adding it to the shared package puts it where every page finds it and where its design is judged against every use.

**Reviewed for:** Does every layout, control, or surface drawn by two pages live in the shared package exports? Look in the shared package exports and the drawing pages.

### Support Keyboard, Focus, Labels, and Announcements

**ID:** `litespec/frontend/components#support-keyboard-focus-labels-and-announcements`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Interactive UI MUST support keyboard operation, visible focus, semantic elements, programmatic labels, and asynchronous status announcements.

**Rationale:** Keyboard operation, visible focus, semantic elements, programmatic labels, and status announcements come with platform controls, and all five break at once in a custom control. A mouse-only control locks out keyboard users, a missing label leaves screen-reader users guessing, and a silent update leaves anyone missing the screen change unaware of it. Starting from a platform control keeps all five properties, and the composite keeps them for every page using it.

The five properties above are the ones a component can satisfy on its own. [WCAG 2.2](https://www.w3.org/TR/WCAG22/) adds criteria that only a flow or a page can satisfy, so each route answers them where the criterion applies:

| Criterion | Level | What it means for the UI |
|:---|:---|:---|
| 1.3.5 Identify Input Purpose | AA | A field collecting information about the user names its purpose, so the browser can fill it. |
| 1.4.11 Non-text Contrast | AA | A control's boundary, state, and focus indicator reach 3:1 against what surrounds them. |
| 2.2.2 Pause, Stop, Hide | A | Content that moves or updates on its own for more than five seconds can be paused. |
| 2.4.11 Focus Not Obscured (Minimum) | AA | A focused control stays at least partly visible under a sticky header, bar, or panel. |
| 2.4.12 Focus Not Obscured (Enhanced) | AAA | A focused control stays fully visible. |
| 2.4.13 Focus Appearance | AAA | The focus indicator meets the stated minimum area and contrast. |
| 2.5.1 Pointer Gestures | A | Every swipe, pinch, or multi-point action has a visible single-pointer alternative. |
| 2.5.7 Dragging Movements | AA | Every drag action has a single-pointer alternative. |
| 2.5.8 Target Size (Minimum) | AA | A pointer target measures at least 24 by 24 pixels, or meets the stated spacing. |
| 3.2.6 Consistent Help | A | A help mechanism sits in the same relative order on every page that has one. |
| 3.3.4 Error Prevention (Legal, Financial, Data) | AA | A command that moves money or changes recorded data can be reviewed, confirmed, or reversed. |
| 3.3.7 Redundant Entry | A | Information already given in the same process is not asked for again. |
| 3.3.8 Accessible Authentication (Minimum) | AA | Sign-in offers a path with no cognitive function test. |
| 3.3.9 Accessible Authentication (Enhanced) | AAA | That path asks for no object recognition or personal content test either. |
| 4.1.3 Status Messages | AA | A status that appears without a focus move reaches assistive technology through a live region. |

The level column marks which target each criterion belongs to: AA for every project, AAA for a project that targets AAA.

**Reviewed for:** Does every interactive UI support keyboard operation, visible focus, semantic elements, programmatic labels, and status announcements? Look in the route suite output.

### Show a Focus Indicator That Stands Out

**ID:** `litespec/frontend/components#show-a-focus-indicator-that-stands-out`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A control MUST show, on keyboard focus, an indicator that reaches 3:1 contrast against the colors next to it, drawn in place of any platform indicator it removes.

**Rationale:** WCAG 2.4.7 Focus Visible and 1.4.11 Non-text Contrast require a visible indicator. A keyboard user finds their place on the page by the indicator alone, so a faint ring on a gray row hides it. The indicator shows on keyboard focus rather than on every click, so pointer users see no ring they did not ask for, and keyboard users always see one. The Approve button on an expense row, for example, shows a ring staying visible on the selected row's tinted background.

**Reviewed for:** Does every control show a focus indicator at 3:1 against its surroundings on keyboard focus? Look in the route suite output and the shared package's focus styles.

### Render Data States

**ID:** `litespec/frontend/components#render-data-states`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A data component MUST render loading, empty, error, forbidden, and ready.

**Rationale:** A component rendering only its ready state leaves the user with a blank region while data loads, after it fails, or when there is nothing to show. Each missing state is a screen with no design and no test covering it. Naming every state up front gives each one a designed drawing.

A route adds `not-found` under `litespec/frontend/rendering#define-every-route-state`, because a missing target resolves at the route rather than inside a component. Each state name is a term in the project glossary.

**Reviewed for:** Does every data component render loading, empty, error, forbidden, and ready, with state names from the glossary? Look in the shared package components and the glossary.

### Render Control States

**ID:** `litespec/frontend/components#render-control-states`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A control MUST render disabled and pending so that a second submission stays impossible while one is in flight and recovery stays usable.

**Rationale:** A control with no disabled state invites a second submission while the first is still in flight, and a control with no pending state shows no sign anything is happening. Blocking the duplicate submission and keeping recovery usable gives the user one safe path: wait for the result, or recover. State names are glossary terms, as under `litespec/frontend/components#render-data-states`.

**Reviewed for:** Does every control render disabled and pending, blocking a second submission while one is in flight and keeping recovery usable? Look in the shared package components.

### Keep Component Inputs Narrow

**ID:** `litespec/frontend/components#keep-component-inputs-narrow`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A composite MUST receive the values and callbacks it needs, not a client, a store, or an aggregate.

**Rationale:** The shared package serves applications it was not written against. A composite taking a client, a store, or an aggregate takes one application's data model as its input, so it fits no application with a different model, and a change to that model breaks the shared package. Values and callbacks keep the composite free of any application's shape: the page translates its model into plain inputs, and the composite renders what it is given.

**Reviewed for:** Does every composite receive values and callbacks instead of a client, a store, or an aggregate? Look in the shared package component signatures.

### Use Declared Visual Variants

**ID:** `litespec/frontend/components#use-declared-visual-variants`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A repeated component variant MUST use theme tokens through the shared package's variant helper rather than repeated literal values.

**Rationale:** When each composite writes its own pixel values, colors, or conditional branches for one variant, the copies start equal and drift with every later edit, because nothing points the author at the other copies. The variant helper holds each variant once, in theme tokens, so a change to the variant reaches every composite using it, and every value stays inside the theme.

**Reviewed for:** Does every repeated variant use the shared variant helper with theme tokens? Look in the shared package components.

### Sanitize Untrusted Rich Content

**ID:** `litespec/frontend/components#sanitize-untrusted-rich-content`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST NOT render untrusted rich content without its recorded sanitization decision.

**Rationale:** Rich content written by one user renders in another user's browser, so unsanitized markup is a script injection path. Whoever writes the content runs code as every user. The recorded decision names the sanitizer, the allowed elements and attributes, the link policy, and the test cases, so the boundary stays explicit and reviewable instead of a different ad-hoc escape on every page.

**Reviewed for:** Does every untrusted rich content render carry a recorded decision naming the sanitizer, the allowed elements and attributes, the link policy, and the test cases? Look in the decision records and the rendering components.

### Use a Composite for a Content Image

**ID:** `litespec/frontend/components#use-a-composite-for-a-content-image`

**Strength:** required

**Applies to:** artifact: component

**Statement:** An informative or decorative image MUST render through the shared image component, which owns its shape, its sizing, and its fallback.

**Rationale:** An informative image needs meaningful alternative text, and a decorative image needs empty alternative text so assistive tools skip it. A source and label written directly in a page bypass both rules. The author then tracks which kind each image is and writes the matching text, on every page. The shared image component owns the shape, the sizing, the fallback, and the text policy, so each image gets the handling its kind needs. The raw-element rule refuses unwrapped images in page code under `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`.

**Reviewed for:** Does every content image render through the shared image component with the text policy for its kind? Look in page sources for unwrapped images.

## Conventions

### Name Components for Their Role

**ID:** `litespec/frontend/components#name-components-for-their-role`

**Strength:** default

**Applies to:** artifact: component

**Default:** Name a component for the role it plays, such as `ExpenseLinesSection` or `ReplayDeliveryDialog`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A name such as `ExpenseComponent` or `GenericModal` describes the file type, so it fits any component and tells a searcher nothing. A name stating the role, such as `ExpenseLinesSection` or `ReplayDeliveryDialog`, tells the next author what the component does before they open it, and keeps two components for two jobs from sharing one vague name.

**Reviewed for:** Does every component name state the role it plays? Look in the shared package and module sources.

### Make Touch Targets Fit a Finger

**ID:** `litespec/frontend/components#make-touch-targets-fit-a-finger`

**Strength:** default

**Applies to:** artifact: component

**Default:** Size every pointer target at least 44 by 44 pixels where the primary pointer is coarse, and keep the 24 by 24 pixel minimum everywhere else.

**Replacement:** A consumer can replace this default by recording, in its design contract, the target size its touch layouts use.

**Rationale:** The 24 pixel minimum of WCAG 2.5.8 stops a mouse user from missing a control, but a finger covers more than that. A staff member approving expenses on a phone taps Reject when they meant Approve. A larger target on coarse pointers keeps dense desktop tables while giving touch layouts room. The mobile stacks already size targets for a finger.

**Reviewed for:** Does every pointer target on a touch layout measure at least 44 by 44 pixels? Look in the route suite output at the phone breakpoint.

### Expose Card and Section Titles as Headings

**ID:** `litespec/frontend/components#expose-card-and-section-titles-as-headings`

**Strength:** default

**Applies to:** artifact: component

**Default:** Render a visible card, panel, or section title as a heading at its correct level.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Some primitive sources render a title prop as a plain text element. That removes the title from the page outline, so screen-reader users lose it as a navigation point and the route suite's heading check misses it. Rendering the title as a heading at its correct level keeps the outline and the test in agreement.

**Reviewed for:** Does every visible card, panel, and section title render as a heading at its correct level? Look in the route suite output, which resolves each as a heading.

### Keep Domain Values Typed Until Display

**ID:** `litespec/frontend/components#keep-domain-values-typed-until-display`

**Strength:** default

**Applies to:** artifact: component

**Default:** Keep branded and generated identifier types until the presentation boundary converts them.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Dates, money, and status values carry meaning a display string loses: the instant, the amount, the code. Converting early spreads formatting choices across every component touching the value, and each one picks its own locale and zone. Keeping the typed value until the presentation boundary converts it in one place, with explicit locale and zone behavior.

**Reviewed for:** Does every display string come from the presentation boundary with an explicit locale and zone? Look in the presentation boundary and the components feeding it.

### Keep Error Boundaries Scoped

**ID:** `litespec/frontend/components#keep-error-boundaries-scoped`

**Strength:** default

**Applies to:** artifact: component, route

**Default:** Use the route error surface for route failure, and a component boundary only where recovery keeps the page usable.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** An error boundary catching a failure with no recovery of its own hides the error and leaves the surrounding page broken but silent. The route error surface owns route failure, with a designed recovery. A component boundary earns its place only where the rest of the page stays usable without the failed part. Each stack page names the error surface for its stack.

**Reviewed for:** Does every route failure render on the route error surface, and does every component boundary leave its surroundings usable? Look in the route error surfaces and the boundary components.

## Reference Example

This informative example demonstrates `litespec/frontend/components#compose-composites-in-page-code` and `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`.

An expense section renders its facts, its commands, and its states by composing display composites and shared form fields. It declares no element and no styling, so the layout it draws is the one every other entity page draws.
