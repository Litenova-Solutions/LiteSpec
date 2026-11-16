# Controlled UI Governance

**Stack:** any
**Concern:** frontend-ui

Every screen in a product draws from one shared UI package: the ready-made controls, the interaction behavior, the page layouts, and the named visual values.

This page states the rules for governing that package. Frontend teams and agents apply it whenever they add or change a screen, a component, or a named value.

## Concepts

### Decide Each Design Choice Once

A person records each design choice once, where the choice is made: the token set, each composite, each pattern option, and each floorplan. The route suite and the stack lint enforce each recorded choice on every route, so a route inside the contract needs no review of its own.

### Three Layers of a Screen

A screen is one page a person sees through an entry point. A route is the code that renders a screen. Every screen answers three questions, and each question has one owner, so no two places decide the same thing.

| Layer | It decides | Owner | Proof |
|:---|:---|:---|:---|
| Product | Which pattern option a screen draws, what it says, and which command is primary | The pattern pages and the product's pattern catalog | The catalog entry and the floorplan check |
| Behavior | How each control answers the keyboard, focus, pointer, screen reader, motion setting, color scheme, and browser | The behavior Conventions on the pattern pages and on [Frontend Components](components.md), drawn from WCAG 2.2 | The route suite and the component tests |
| Parts | The components and their look | The pinned baseline in the shared UI package | The source lock and the component tests |

The parts layer does not weaken the behavior layer. When a baseline component falls short of a behavior Standard, the shared package configures the component, wraps it in a composite, or records a fork, and no route works around it. A dialog that closes when the user presses outside it, for example, is configured once in the shared form dialog to keep unsaved input, so `litespec/frontend/patterns/dialogs-and-overlays#keep-unsaved-input-through-a-dismissal` holds on every route.

### The Floorplan Set

A floorplan is a page component that fixes one page layout and exposes named slots for the domain to fill. The shared package holds the closed set, and a route composes exactly one of them. A closed set lets an author choose a known shape by rule rather than creating a new layout. Each floorplan implements the chosen option of one page-frame pattern from the pattern catalog ([UI patterns](patterns.md)).

The reference set holds one floorplan for each user task. Your workspace declares the floorplans its routes need, and each declaration carries the rule for choosing it.

| Floorplan | Choose it when the user | Slots it carries |
|:---|:---|:---|
| `WorkPage` | Processes items one after another | The queue, the current item, and its actions |
| `ListPage` | Finds one record among many | Filters, the result count, and the rows |
| `EntityPage` | Acts on one record | Facts, state, the actions that state permits, and related records |
| `FormPage` | Supplies the input of one command | The fields, one primary action, and the refusals |
| `OutcomePage` | Receives one answer after an action | The answer and the next step |

The domain fills the slots, so a floorplan defines no state, action, or refusal.

| Slot | Domain source |
|:---|:---|
| Status | The aggregate states named in the specification |
| Actions | The use cases the current state permits |
| Refusals | The refusal results of the use case the route calls |

For example, a Claimwick expense page composes `EntityPage`. It fills the facts slot with the expense fields, the state slot with the expense state, and the actions slot with the use cases the expense state permits.

A floorplan exposes its name on its frame. A reviewer and the route suite read that name to learn which floorplan a route composes.

### The Screen Blueprint

A screen record is the Behavior record that describes one screen. Its blueprint is the part of the record that says how the screen is built and where it leads. The `pageFrame` field names the page-frame pattern the screen follows and the option the product catalog chose. The `leadsTo` field names the screens a person reaches from this screen in one step.

The explanation file beside the screen record holds one `wireframe` block, written in the wireframe language that [UI patterns](patterns.md) defines. Each action in that block names a use case the screen offers, each state names a glossary term for the record shown, and each refusal names a result of an offered use case.

For example, the Claimwick expense list names its page frame and lists the expense record and the new expense form in `leadsTo`. Its wireframe shows the list rows, the filter actions, and the states an expense can carry.

The book renders each blueprint for readers. It draws the wireframe on the screen page, builds the navigation map of each entry point from `leadsTo`, and shows each flow as a strip of its step screens in step order.

### The Tone Set

A tone is the meaning of a state, such as success or a call for a decision. A page picks a tone instead of picking a color directly. The set is closed, so every page chooses from the same meanings.

| Tone | The state it means |
|:---|:---|
| `neutral` | Nothing is wrong and nothing is asked for |
| `positive` | The thing asked for happened |
| `caution` | A person has to decide |
| `destructive` | The thing asked for is refused |
| `info` | The page is explaining rather than reporting |

Each application keeps one table that maps its state codes to tones. Because the mapping lives in one place, the same state shows the same color on every screen.

For example, Claimwick maps its paid state to `positive` once. Every screen that shows a paid expense draws the same fill, whether it is a list row or a record page.

### The Token Roles and Limits

A token is a named visual value, such as a color or a border shade, that screens draw instead of writing raw values. A token role is the job that value does, such as page background or input boundary. LiteSpec names each role once. Your project supplies the values in one token file, written in the W3C Design Tokens format, version 2025.10. That file is JSON: each token carries `$value` and `$type`, and aliases use braces.

Each role is judged by a limit instead of an exact color. The table states the limit each role meets, measured in WCAG 2 relative luminance. A color scheme is a top-level group named `light` or `dark`, and your project declares the schemes it ships. The manifest baseline values are defaults that pass every limit.

| Role | Limit |
|:---|:---|
| `page`, `card` | The card reads as a separate surface on the page |
| `card-edge` | Differs from `page` and `card`, at least 1.2:1 against the page |
| `separator` | Contrast against `card` no greater than the card edge's |
| `control-boundary` | Reaches 3:1 on `card` |
| `text`, `muted-text` | Reach 4.5:1 on `page` and `card` |
| `focus-ring` | Reaches 3:1 on `card` and `page` |
| `primary`, `primary-text` | `primary-text` reaches 4.5:1 on `primary` |
| `tone.<name>` | Each tone's `text` reaches 4.5:1 on its `fill` |
| `chart.1` to `chart.3` | Each reaches 3:1 on `card`, and every pair stays apart under protanopia and deuteranopia simulation |
| `magnitude` | Steps rise monotonically in lightness |

Each tone from the Tone Set carries `text`, `fill`, and `edge`. Chart colors stay apart with a CIE76 delta E of at least 10 after Machado 2009 simulation at severity 1.0.

For example, Claimwick ships `light` and `dark` schemes. Its card edge differs from the page by 1.2:1 in both, so cards read as separate surfaces while separators stay quieter. The values differ by scheme, but both meet the same limit.

### The Interface Quality Requirements

A quality requirement is a product-level requirement with a measure and a target, such as a contrast ratio the text reaches. The consumer preset ships six of them with `litespec init`, so every project starts with UI quality covered. Each carries `kind: quality` with its measure and target, and cites the constraint or goal your project names.

You tighten a requirement by editing its record in your project. The route suite and the token check prove the six.

| Requirement | It holds |
|:---|:---|
| `requirement/ui-text-contrast` | Body text reaches 4.5:1 and large text 3:1 in each declared scheme |
| `requirement/ui-control-boundary` | Each field, select, checkbox, radio, and switch boundary reaches 3:1 against its surface |
| `requirement/ui-focus-indicator` | Each focus indicator reaches 3:1 against the surface beside it |
| `requirement/ui-chart-series` | Each chart color reaches 3:1 against its surface, and every pair stays apart under color-vision simulation |
| `requirement/ui-reflow` | No clipped content at 320 CSS pixels |
| `requirement/ui-target-size` | Every pointer target meets 24 by 24 CSS pixels or its spacing |

For example, Claimwick starts with `requirement/ui-text-contrast`. Its body text reaches 4.5:1 in both schemes, and the suite fails any screen that falls short.

### The Command Surface

A command is a use case that changes something. Its surface is where a person runs it: a button, a dialog, or a page. The surface follows the size of the input, because a larger surface than the command needs wastes the user's attention.

| The command takes | Surface |
|:---|:---|
| Nothing, reversible | A button |
| Nothing, irreversible | A confirmation dialog |
| One to four inputs | A form dialog |
| More than four inputs | A form page |

For example, Claimwick runs a reversible approval from a button, asks for confirmation before an irreversible cancel, opens a form dialog for an expense with three fields, and moves to a form page when the input grows past four fields.

### The Route Suite

The route suite is the automated check for a frontend: one specification that visits every route and asserts the same checks on each. A visual snapshot can supplement an assertion, but it does not replace one. Snapshots vary across platforms, fonts, and hardware, so the assertion is the gate.

| Check | What the suite asserts |
|:---|:---|
| Floorplan | The route composes exactly one floorplan |
| Reference | The route names its reference or is a recorded freestyle route |
| Accessibility | The WCAG 2.2 AA check reports zero violations |
| Heading | The route carries exactly one top-level heading |
| Landmark | The route carries one main content region |
| Reflow | At 320 pixels the route shows no clipped content; a data table scrolls inside its region, which takes keyboard focus and carries a name |
| Text spacing | The route still shows no clipped content under the WCAG text-spacing override |
| Target size | Every pointer target meets the minimum size or spacing |
| Keyboard | Every interactive element is reachable and shows a focus indicator at 3:1 contrast against its surroundings |
| Route change | Following a link moves focus to the new route's top-level heading |
| Color scheme | The accessibility check passes in each color scheme the project declares |
| Motion | Under the reduced-motion setting, no spatial, scaling, or looping animation runs |
| Announcements | Asynchronous status changes announce through the platform's polite or assertive live region, both present when the route loads |
| Formats | Money and moment text follows the forms the entry point names, in the reader's language |

The suite finds its routes in the application route definition instead of reading a list. A new route is therefore checked with no new specification. A dynamic segment resolves to a record the deployment holds, and falls back to an absent record when the deployment holds none.

For example, when Claimwick adds an expense route, the suite visits it on the next run, checks its floorplan, heading, landmarks, and contrast, and reports any gap.

### Proving Results

A screen's computed `calls` name the use cases it reaches, and each use case declares its results. The one result obligation for a screen is `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`: every written refusal of a use case the screen offers carries its `sentence` on the result record, the sentence a person reads.

`refusal-sentence-missing` checks the sentence from the records alone. A browser test asserting the code does not stand in for the sentence, and a derived refusal, one the model derives instead of anyone writing it, carries none of its own. Success results are proved through their scenarios, as [Frontend Testing](testing.md) describes. For example, the expense form offers the use case that raises `spending.receipt-missing`, and that refusal carries its sentence on its result.

## Standards

### Record One Shared UI Package

**ID:** `litespec/frontend/ui#record-one-shared-ui-package`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST record one shared UI package in `.litespec/project.yaml`.

**Rationale:** Two general-purpose visual systems in one workspace make every component choice ambiguous: each screen picks its source, and the product ships two looks. Recording one shared UI package in `.litespec/project.yaml` settles the source for every controlled frontend. A frontend outside the contract declares another platform and says what replaces the contract.

**Checked by:** `litespec model check` reports `ui-package-invalid` for controlled frontends with no `ui.package` in `.litespec/project.yaml`, and `ui-dependency-invalid` tagged `[litespec/frontend/ui#record-one-shared-ui-package]` for a second general-purpose visual dependency. Run it at the consumer root.

### Pin the Baseline in the Shared Package

**ID:** `litespec/frontend/ui#pin-the-baseline-in-the-shared-package`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST install the pinned baseline from `litespec.manifest.yaml` into its shared UI package once.

**Rationale:** The design baseline installed once in the shared UI package gives every application the same primitives at the same pinned version. Installing per application makes a copy per application, and each copy drifts on its own update schedule with nothing reporting the split. An additional design-baseline source is permitted only under a waiver recorded against this provision.

**Reviewed for:** Does the pinned baseline install once, and only in the shared UI package? Look in the shared package manifest, the shared lock file, and each application manifest.

### Publish a Design Contract

**ID:** `litespec/frontend/ui#publish-a-design-contract`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST publish one design contract with its shared UI package, carrying the required sections: Baseline, Tokens, Page Frames, Vocabulary, Motion, Browser Support.

**Rationale:** An author that has read no contract picks their own tokens, motion, browser features, and components for each page. The picks differ on the next page, and the user sees the difference as clutter. One design contract published with the shared UI package keeps every answer in one file. Baseline names the pinned design baseline and its version, Tokens names the path of the token values file, Page Frames maps each page-frame pattern the product uses to the component that implements it, Vocabulary names the composites a page reaches for, Motion states the durations and what each becomes under the reduced-motion setting, and Browser Support states the target every platform feature is judged against. The contract names no screen: each screen names its own page frame on its record. Proposed by decision 0089.

**Example:** Claimwick publishes its contract with `@claimwick/ui`. The metadata block names the package, the token file, and the component behind each page frame it draws:

```yaml
schemaVersion: 2
package: "@claimwick/ui"
profile: application-balanced
tokens: tokens/claimwick.tokens.json
pageFrames:
  list-page: ListPage
  record-page: EntityPage
  form-page: FormPage
```

**Checked by:** `litespec model check` reports `ui-design-contract-invalid` tagged `[litespec/frontend/ui#publish-a-design-contract]` for a missing contract, a missing metadata block, a package mismatch, or a missing required section. Run it at the consumer root.

### Map Each Page Frame to Its Floorplan

**ID:** `litespec/frontend/ui#map-each-page-frame-to-its-floorplan`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The `Page Frames` section of the design contract MUST map each page-frame pattern the product uses to the floorplan component that implements it, each with a when-to-use rule.

**Rationale:** Without a when-to-use rule, authors choose floorplans by resemblance. Two authors pick different floorplans for one kind of page, and users meet the same kind in two layouts. A map from each page-frame pattern to its component, with the rule that selects it, lets authors choose by rule. A floorplan outside the map is a decision for the shared package, not a choice a route makes on its own. Proposed by decision 0089.

**Example:** Claimwick's Page Frames section states each page frame, its component, and the question that selects it:

```markdown
## Page Frames

| Page frame | Component | Use it when the user |
|:---|:---|:---|
| `list-page` | `ListPage` | Finds one record among many. The page shows the result count. |
| `record-page` | `EntityPage` | Acts on one record: its facts, its state, and the actions that state permits. |
| `form-page` | `FormPage` | Supplies the input of one command, with one primary action. |
```

**Reviewed for:** Does every page frame the product uses name its component and its when-to-use rule in the Page Frames section? Look in the design contract.

### Compose Each Route from One Floorplan

**ID:** `litespec/frontend/ui#compose-each-route-from-one-floorplan`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST render exactly one floorplan from the declared set, unless it is a recorded freestyle route.

**Rationale:** The floorplan fixes the layout, the heading position, and the slots, so a reviewer reads which floorplan a route composes and knows what the page looks like before opening it. The slots take domain values only: the aggregate state, the use cases that state permits, and the refusal results of the called use case. Exactly one floorplan per route keeps each page to one known shape, unless it is a recorded freestyle route.

**Example:** The Claimwick expense record composes `EntityPage` and fills its slots from the expense: its facts, its state, the actions that state permits, and its related records.

**Reviewed for:** Does every route render exactly one floorplan, or record a freestyle decision? Look in the route sources and in the suite output that reads the floorplan name from each frame.

### Keep Layout Inside Floorplans

**ID:** `litespec/frontend/ui#keep-layout-inside-floorplans`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Route code MUST NOT declare layout outside its floorplan and display composites.

**Rationale:** A layout declaration in a route file is a second layout beside the floorplan: the route draws its own spacing while claiming the floorplan's shape. The next route copies the declaration because it looks like the accepted local practice, and the floorplan stops describing what routes render. Floorplan and display composites own layout, so one change there updates every route composing them.

**Reviewed for:** Does any route file declare layout outside its floorplan and display composites? Look in the route sources.

### Justify Each Freestyle Route with a Decision Record

**ID:** `litespec/frontend/ui#justify-each-freestyle-route-with-a-decision-record`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route that composes no declared floorplan MUST name the decision record that justifies its layout.

**Rationale:** A route that composes no declared floorplan is either the first consumer of a floorplan that does not exist yet or a genuine one-off. Naming the decision record that justifies its layout makes either case reviewable and keeps the declared set closed. A second route of the same shape is the signal to add the floorplan instead of recording another freestyle route.

**Reviewed for:** Does every freestyle route name the decision record that justifies its layout? Look in the freestyle routes and the decision records.

### Name the Reference Behind Each Page Type

**ID:** `litespec/frontend/ui#name-the-reference-behind-each-page-type`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST name its page type's reference, with a freestyle route naming its decision record instead.

**Rationale:** An author with no named reference builds a page from primitives, and the next author builds another page for the same type. Naming the page type's reference keeps every page of one type following the one drawing it derives from, so the type stays one design. A freestyle route names its decision record instead, since it derives from no reference.

**Reviewed for:** Does every route name its page type reference, or its decision record for a freestyle route? Look in the route sources.

### Link Each Screen to Its Next Screens

**ID:** `litespec/frontend/ui#link-each-screen-to-its-next-screens`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST name every screen a person reaches from it in one step in `leadsTo`.

**Rationale:** The screens and their links are the navigation, and they exist in records before any route is built. The book draws each entry point as a map of places and connections, and each flow as a strip of its step screens. A screen that no other screen leads to leaves its readers with no way in unless it starts its entry point. A screen that leads nowhere and offers no use case ends their task with nothing done. Proposed by decision 0089.

**Example:** The Claimwick expense list names the expense record and the new expense form in `leadsTo`. A reader on the list reaches the record to act on one expense, or the form to start a new one, each in one step.

**Checked by:** `litespec model check` reports `screen-unreachable` for a screen no other screen leads to that is not its entry point's start, and `screen-dead-end` for a screen that leads nowhere and offers no use case. Run it at the consumer root.

### Name Money and Moment Formats on Each Entry Point

**ID:** `litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST name the money and moment forms its screens render in `formats`, rendered with the locale data (CLDR) of the reader's language and the currency and country the record carries, with no format string, separator, or symbol written in code.

**Rationale:** One record serves readers in several languages, and each language writes its separators its own way. The entry point names one money option and one moment option from the money and moment patterns, and each screen renders them with the locale data (CLDR) of the reader's language and the currency and country the record carries. A form fixed in code shows one reader the wrong separators and hides the choice from every check. Proposed by decision 0089.

**Example:** The same Claimwick expense of EUR 1,212.35 reads `EUR 1.212,35` for a Dutch reader and `EUR 1,212.35` for an English reader.

**Checked by:** `litespec model check` reports `sentence-language-missing` for a written refusal result with no sentence for a language its use case's entry points declare. Run it at the consumer root.

### Select a Tone from the Closed Set

**ID:** `litespec/frontend/ui#select-a-tone-from-the-closed-set`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A state indicator MUST take one tone from the closed set, each application declaring its state-to-tone mapping once as an exhaustive table of glossary terms.

**Rationale:** A page that picks its own color picks a meaning nobody agreed to: the same red celebrates on one screen and warns on another. One state-to-tone mapping per application, declared once as an exhaustive table of glossary terms, makes the same state read the same way on every screen. A new state fails to compile until its tone and its word exist, so no state ships uncolored or mislabeled.

**Reviewed for:** Does every application map each state to a tone in one exhaustive table of glossary terms? Look in the application state mapping.

### Keep Tokens in One Definition

**ID:** `litespec/frontend/ui#keep-tokens-in-one-definition`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST use the shared token definition and declare no value, with no literal color in any shipped definition.

**Rationale:** One token definition is what makes two applications look like one product. An application uses the shared definition and declares no value of its own, so no second definition introduces a value the first does not define. A definition the shared package ships declares no literal color, so every value traces to the one token file the design contract names.

**Reviewed for:** Does any application declare its own token value, or does any shipped definition carry a literal color? Look in the application and shared token definitions.

### Meet the Limits of Every Token Role

**ID:** `litespec/frontend/ui#meet-the-limits-of-every-token-role`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every token value in each declared color scheme MUST meet the limit the Concepts table states for its role.

**Rationale:** Readable text and findable controls are measured limits, not pinned values: a palette passes when every role meets its limit, whatever values it holds. Judging values instead of limits refuses a readable palette for differing from the baseline while accepting a baseline value on a new surface. The manifest baseline stays as defaults chosen because they pass, and a project sets its own values with no waiver while they meet the limits. Proposed by decision 0089.

**Example:** Claimwick's card edge differs from the page by 1.2:1 in both schemes, so cards read as separate surfaces while separators stay quieter.

**Checked by:** `litespec model check` reports `design-token-limit-failed` for a token role that misses its limit in a declared scheme. Run it at the consumer root.

### Keep Token Values in One Token File

**ID:** `litespec/frontend/ui#keep-token-values-in-one-token-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep every token value in the one token file the design contract's Tokens section names, written in the W3C Design Tokens format, version 2025.10.

**Rationale:** When values scatter across stylesheets, each limit depends on a hand-run script that runs once and is forgotten. Design tools and token converters have no file they read, and the check has no file to judge. One token file holds every value where tools read it: components read the stylesheet generated from it, and the check reads it to judge every limit. Proposed by decision 0089.

**Example:** Claimwick keeps every value in `tokens/claimwick.tokens.json` and builds its component stylesheet from that file:

```json
{
  "card": {
    "$value": "#ffffff",
    "$type": "color"
  }
}
```

**Reviewed for:** Does every token value live in the named token file and follow the W3C Design Tokens format? Look in the token file and the stylesheets.

### Give Every Input a Visible Boundary

**ID:** `litespec/frontend/ui#give-every-input-a-visible-boundary`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The boundary of a text field, select, checkbox, radio, or switch MUST reach 3:1 contrast against the surface it sits on.

**Rationale:** WCAG 1.4.11 covers non-text contrast. A reader with low vision finds a field by its edge, and a field drawn at the faint separator step disappears into the page. Its own boundary token keeps separators quiet and controls findable.

**Example:** The amount field on the Claimwick expense form keeps a visible edge on the card behind it in both color schemes, so it stays findable where a separator would fade.

**Reviewed for:** Does every input boundary reach 3:1 against its surface in each declared scheme? Look in the token definitions and the rendered form fields.

### Select a Command Surface by Size

**ID:** `litespec/frontend/ui#select-a-command-surface-by-size`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A command MUST use the surface the Concepts table assigns to its input.

**Rationale:** The surface fits the input: a button for a reversible action, a confirmation dialog for an irreversible one, a form dialog for one to four inputs, and a form page beyond that. A form dialog with more inputs than the table allows scrolls, loses its title, and hides the refusal beside the field that caused it. An oversized surface for a small command wastes the user's attention.

**Reviewed for:** Does every command use the surface the Concepts table assigns to its input? Look in the command surfaces.

### Show the Trace ID in Error Detail

**ID:** `litespec/frontend/ui#show-the-trace-id-in-error-detail`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Every visible error MUST show its trace identifier in copyable form in the error detail, from the problem body or the `X-Trace-Id` header.

**Rationale:** Support reads the trace identifier to find the failed request, and the user copies it from the detail without retyping. Shown in copyable form from the problem body or the `X-Trace-Id` header, it connects the user's report to the server's logs. The headline carries what happened in words; the identifier belongs in the detail.

**Reviewed for:** Does every visible error show its trace identifier in copyable form in the detail? Look in the error surfaces.

### Pass Every Route Suite Check

**ID:** `litespec/frontend/ui#pass-every-route-suite-check`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST pass each route suite check.

**Rationale:** The route suite visits every route and asserts each check the Concepts section names: the floorplan, the reference, accessibility, the heading, the landmark, reflow, text spacing, target size, keyboard use, the focus move on a route change, each color scheme, reduced motion, status announcements, and the formats. The suite discovers its routes from the application route definition, so a new route is checked with no new specification. Every route passing every check keeps each address inside the contract by default.

**Reviewed for:** Does every discovered route pass each route suite check? Look in the suite output and the CI job logs.

### Restrict Styling to Semantic Tokens

**ID:** `litespec/frontend/ui#restrict-styling-to-semantic-tokens`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A style MUST use a semantic token rather than a raw value.

**Rationale:** A raw value in a style traces to no token. The theme leaves it unchanged through every restyle, and the next author copies the value instead of the token. The shared package definition is the authoritative style surface, holding the tokens, the fonts, and the documented resets. A style built from semantic tokens stays inside the product's visual language by construction.

**Reviewed for:** Does any style use a raw value instead of a semantic token? Look in the stylesheets and the shared package definition.

### Keep Each Behavior Package Justified and on Baseline

**ID:** `litespec/frontend/ui#keep-each-behavior-package-justified-and-on-baseline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A behavior package MUST name the decision record justifying it for the capability it supplies, and render through baseline components and tokens.

**Rationale:** A behavior package adds a capability the design baseline lacks, such as a chart or a rich editor, and naming the decision record that justifies it for that capability keeps the decision tied to the need. The decision follows the capability rather than the visual catalog, so no package duplicates a component the design baseline already provides. Rendering through baseline components and tokens keeps the new capability inside the product's look.

**Reviewed for:** Does every behavior package name its decision record and render through baseline components and tokens? Look in the package declarations and the decision records.

### Name the Evidence for Each UI Change

**ID:** `litespec/frontend/ui#name-the-evidence-for-each-ui-change`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A primitive, composite, token, source, floorplan, or route change MUST name the evidence in the matrix in this section.

**Rationale:** Each change class carries different risk: a token change reaches every route in every density profile, while a primitive change reaches one component. The matrix maps each class to the narrowest evidence that represents its risk, so each change runs the proof its reach requires and no more.

| Change class | Narrowest evidence |
|:---|:---|
| Primitive or variant | The component test for that primitive |
| Floorplan or display composite | The route suite on one route that composes it |
| Token or global style | The route suite on one route in each profile |
| Route | The route suite on that route, and every test proving a result it calls |
| Pattern decision | The catalog entry changes with its implementation |
| Fork | The fork record and the component test |
| Behavior package | The accessibility check for the capability it supplies |

**Reviewed for:** Does every change name the evidence the matrix assigns to its class? Look in the change descriptions and the named evidence.

### Declare Each Frontend Outside the Contract

**ID:** `litespec/frontend/ui#declare-each-frontend-outside-the-contract`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A frontend outside the controlled platforms MUST declare an entry point record naming that platform and the boundaries replacing this contract.

**Rationale:** A device client with no shared component and no router sits outside the floorplan set and the component rules: the contract's checks do not apply to it. Declaring an entry point record that names its platform and the boundaries replacing this contract keeps the decision visible, instead of letting the frontend appear as one with nothing to check.

**Reviewed for:** Does every frontend outside the controlled platforms declare an entry point record with its platform and replacement boundaries? Look in the entry point records.

## Conventions

### Select a Density Profile for Each Frontend

**ID:** `litespec/frontend/ui#select-a-density-profile-for-each-frontend`

**Strength:** default

**Applies to:** artifact: any

**Default:** Select one density profile from `public-light`, `application-balanced`, or `admin-dense` for each controlled frontend.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Density is a property of the audience: `public-light` suits a surface a user meets once, `application-balanced` suits a surface a signed-in staff member works in, and `admin-dense` suits a surface an operator reads all day. All three profiles share one design baseline, so choosing per frontend changes density without changing the visual language.

**Checked by:** `litespec model check` reports `ui-frontend-invalid` for a controlled frontend with no `ui` block declaring its density profile or a profile outside `public-light`, `application-balanced`, `admin-dense`. Run it at the consumer root.

### Keep a Blocked Command Visible with Its Reason

**ID:** `litespec/frontend/ui#keep-a-blocked-command-visible-with-its-reason`

**Strength:** default

**Applies to:** artifact: component, route

**Default:** Leave a command blocked only by the current state or role visible and disabled, with the reason beside it.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A user looking for an action needs to learn it exists and why it is unavailable, such as a missing permission or an unmet precondition. A control that vanishes instead looks like a product that does not offer the action at all. Staying visible and disabled with the reason beside it shows both the existence and the block.

**Reviewed for:** Does every command blocked only by state or role stay visible and disabled with its reason beside it? Look in the command surfaces.
