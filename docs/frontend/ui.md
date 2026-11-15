# Controlled UI Governance

**Stack:** any
**Concern:** frontend-ui

## Intent

The shared UI package is a constrained construction language: the ready-made controls, the interaction behavior, the page layouts, and the named visual values every screen draws with. This page states how the workspace governs that language: the visual authority, the design contract, the token roles and limits, the screen blueprint, the floorplan set, and the route suite that proves every route. Frontend teams and agents apply it whenever they add or change a screen, a component, or a named value, so every page of one kind looks and behaves as one page.

## Concepts

### Decide Each Design Choice Once

A person records each design decision once, where the decision is made: the token set, each composite, each pattern option, and each floorplan. The route suite and the stack lint enforce each recorded decision on every route, so a route inside the contract needs no review of its own.

### Three Layers of a Screen

A screen answers three questions, and each has one owner.

| Layer | It decides | Owner | Proof |
|:---|:---|:---|:---|
| Product | Which pattern option a screen draws, what it says, and which command is primary | The pattern pages and the product's pattern catalog | The catalog entry and the floorplan check |
| Behavior | How each control answers the keyboard, focus, pointer, screen reader, motion setting, color scheme, and browser | The behavior Standards on the pattern pages and on [Frontend Components](components.md), drawn from WCAG 2.2 | The route suite and the component tests |
| Parts | The components and their look | The pinned baseline in the shared UI package | The source lock and the component tests |

The parts never lower the behavior. When a baseline component falls short of a behavior Standard, the shared package configures it, wraps it in a composite, or records a fork, and no route works around it. A dialog that closes on a press outside it, for example, is configured once in the shared form dialog so that `litespec/frontend/patterns/dialogs-and-overlays#keep-unsaved-input-through-a-dismissal` holds on every route.

### The Floorplan Set

A floorplan is a page component that fixes one page layout and exposes named slots for the domain to fill. The shared package holds the closed set, and a route composes exactly one of them. A closed set lets an author choose a known shape by rule rather than invent a layout. Each floorplan implements the chosen option of one page-frame pattern from the pattern catalog ([UI patterns](patterns.md)).

The reference set holds one floorplan per user task. A workspace declares the ones its routes need, and each carries the rule for choosing it.

| Floorplan | Choose it when the user | Slots it carries |
|:---|:---|:---|
| `WorkPage` | Processes items one after another | The queue, the current item, and its actions |
| `ListPage` | Finds one record among many | Filters, the result count, and the rows |
| `EntityPage` | Acts on one record | Facts, state, the actions that state permits, and related records |
| `FormPage` | Supplies the input of one command | The fields, one primary action, and the refusals |
| `OutcomePage` | Receives one answer after an action | The answer and the next step |

The domain fills the slots, so a floorplan invents no state, action, or refusal.

| Slot | Domain source |
|:---|:---|
| Status | The aggregate states named in the specification |
| Actions | The use cases the current state permits |
| Refusals | The refusal results of the use case the route calls |

A floorplan exposes its name on its frame. A reviewer and the route suite read that name to learn which floorplan a route composes.

### The Screen Blueprint

A screen record carries its blueprint: `pageFrame` names the page-frame pattern it follows and the option the product catalog chose, and `leadsTo` names the screens a person reaches from it in one step. Its explanation file holds one `wireframe` block, drawn in the wireframe language the [UI patterns](patterns.md) page states. Each action in the block names a use case the screen offers, each state names a glossary term of the record shown, and each refusal names a result of an offered use case. The book draws the wireframe on the screen page, the navigation map of each entry point from `leadsTo`, and each flow as a strip of the screens of its steps in step order.

### The Tone Set

A tone names what a state means. The set is closed: a page picks one of them rather than a color.

| Tone | The state it means |
|:---|:---|
| `neutral` | Nothing is wrong and nothing is asked for |
| `positive` | The thing asked for happened |
| `caution` | A person has to decide |
| `destructive` | The thing asked for is refused |
| `info` | The page is explaining rather than reporting |

The mapping from a state code to a tone lives in one table in each application. A user then sees the same color for the same state on every screen.

### The Token Roles and Limits

LiteSpec names each token role once, and the project supplies the values in one token file in the W3C Design Tokens format, version 2025.10. The file is JSON: each token carries `$value` and `$type`, and aliases use braces. The table states the limit each role meets, measured in WCAG 2 relative luminance. A scheme is a top-level group named `light` or `dark`, and the project declares the schemes it ships. The manifest baseline values are defaults chosen because they pass every limit.

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

Each tone of the Tone Set carries `text`, `fill`, and `edge`. Chart separation is CIE76 delta E of at least 10 after Machado 2009 simulation at severity 1.0.

### The Interface Quality Requirements

The consumer preset ships six product-level quality requirements with `litespec init`, so every project starts with them. Each carries `kind: quality` with its measure and target, and cites the constraint or goal the project names. A project tightens one by editing its own record. The route suite and the token check prove them.

| Requirement | It holds |
|:---|:---|
| `requirement/ui-text-contrast` | Body text reaches 4.5:1 and large text 3:1 in each declared scheme |
| `requirement/ui-control-boundary` | Each field, select, checkbox, radio, and switch boundary reaches 3:1 against its surface |
| `requirement/ui-focus-indicator` | Each focus indicator reaches 3:1 against the surface beside it |
| `requirement/ui-chart-series` | Each chart color reaches 3:1 against its surface, and every pair stays apart under color-vision simulation |
| `requirement/ui-reflow` | No clipped content at 320 CSS pixels |
| `requirement/ui-target-size` | Every pointer target meets 24 by 24 CSS pixels or its spacing |

### The Command Surface

A command's surface follows the size of its input. A larger surface than the command needs costs the user attention it has no use for.

| The command takes | Surface |
|:---|:---|
| Nothing, reversible | A button |
| Nothing, irreversible | A confirmation dialog |
| One to four inputs | A form dialog |
| More than four inputs | A form page |

### The Route Suite

The route suite is one specification per frontend that visits every route. It asserts the checks below, and a visual snapshot supplements an assertion without replacing one. A snapshot varies across platforms, fonts, and hardware, so an assertion is the gate.

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

The suite discovers its routes from the application route definition rather than from a list. A new route is therefore checked with no new specification. A dynamic segment resolves to a record the deployment holds, and falls back to an absent record when it holds none.

### Result Proof

A screen's computed `calls` name the use cases it reaches, and each use case declares its results. The one result obligation for a screen is `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`: every refusal a screen can reach through its computed `calls` carries its sentence on its result or has a browser test asserting its code. Success results are proved through their scenarios as [Frontend Testing](testing.md) states.

Result proof for routes follows `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`. That Behavior provision is the one result obligation for a screen: every written refusal of a use case the screen offers carries its `sentence` on the result record, the sentence a person reads, and `refusal-sentence-missing` checks it from the records alone. A browser test asserting the code does not stand in for the sentence, and a derived refusal carries none of its own. Success results stay proved through their scenarios. For example, the expense form offers the use case that raises `spending.receipt-missing`, and that refusal carries its sentence on its result.

## Standards

### Select One Visual Authority

**ID:** `litespec/frontend/ui#select-one-visual-authority`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST record one shared UI package in `.litespec/project.yaml`.

**Rationale:** Two general-purpose visual systems in one workspace make every component choice ambiguous: each screen picks its source, and the product ships two looks. Recording one shared UI package in `.litespec/project.yaml` settles the source for every controlled frontend. A frontend outside the contract declares another platform and says what replaces the contract.

**Checked by:** `litespec model check` reports `ui-package-invalid` for controlled frontends with no `ui.package` in `.litespec/project.yaml`, and `ui-dependency-invalid` tagged `[litespec/frontend/ui#select-one-visual-authority]` for a second general-purpose visual dependency. Run it at the consumer root.

### Pin the Baseline in the Shared Package

**ID:** `litespec/frontend/ui#pin-the-baseline-in-the-shared-package`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST install the pinned baseline from `litespec.manifest.yaml` once, into its shared UI package.

**Rationale:** The design baseline installed once in the shared UI package gives every application the same primitives at the same pinned version. Installing per application makes a copy per application, and each copy drifts on its own update schedule with nothing reporting the split. An additional design-baseline source is permitted only under a departure recorded against this provision.

**Reviewed for:** Which pinned manifest baselines install outside the shared UI package, or install more than once? Look in the shared package manifest and lock, and the application manifests.

### Publish a Design Contract

**ID:** `litespec/frontend/ui#publish-a-design-contract`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST publish one design contract with its shared UI package, carrying the required sections: Baseline, Tokens, Page Frames, Vocabulary, Motion, Browser Support.

**Rationale:** An author that has read no contract picks their own tokens, motion, browser features, and components for each page. The picks differ on the next page, and the user sees the difference as clutter: same product, different looks. One design contract published with the shared UI package keeps every answer in one file. Baseline names the pinned design baseline and its version, Tokens names the path of the token values file, Page Frames maps each page-frame pattern the product uses to the component that implements it, Vocabulary names the composites a page reaches for, Motion states the durations and what each becomes under the reduced-motion setting, and Browser Support states the target every platform feature is judged against. The contract names no screen: each screen names its own page frame on its record. Proposed by decision 0089.

**Example:** The metadata block names the package, the token file, and the component behind each page frame.

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

### Declare a Closed Floorplan Set

**ID:** `litespec/frontend/ui#declare-a-closed-floorplan-set`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The `Page Frames` section of the design contract MUST map each page-frame pattern the product uses to the floorplan component that implements it, each with a when-to-use rule.

**Rationale:** A floorplan with no when-to-use rule leaves the choice to resemblance: two authors pick different floorplans for one kind of page, and the kind ships in two layouts. Mapping each page-frame pattern to its floorplan component with its rule in the design contract's Page Frames section makes the choice mechanical. A floorplan outside the map is a design decision for the package, not a route-level choice. Proposed by decision 0089.

**Example:** The section states each page frame, its component, and the question that selects it.

```markdown
## Page Frames

| Page frame | Component | Use it when the user |
|:---|:---|:---|
| `list-page` | `ListPage` | Finds one record among many. The page shows the result count. |
| `record-page` | `EntityPage` | Acts on one record: its facts, its state, and the actions that state permits. |
| `form-page` | `FormPage` | Supplies the input of one command, with one primary action. |
```

**Reviewed for:** Which page frames miss the design contract Page Frames section, their component, or their when-to-use rule? Look in the design contract.

### Compose Each Route from One Floorplan

**ID:** `litespec/frontend/ui#compose-each-route-from-one-floorplan`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST render exactly one floorplan from the declared set, unless it is a recorded freestyle route.

**Rationale:** The floorplan fixes the layout, the heading position, and the slots, so a reviewer reads which floorplan a route composes and knows what the page looks like before opening it. The slots take domain values only: the aggregate state, the use cases that state permits, and the refusal results of the called use case. Exactly one floorplan per route keeps each page to one known shape, unless it is a recorded freestyle route.

**Example:** An expense route composes the entity floorplan and fills each slot from the expense.

**Reviewed for:** Which routes render zero or several floorplans without recording a freestyle decision? Look in the route sources and the suite output reading the floorplan name from each frame.

### Keep Layout Inside Floorplans

**ID:** `litespec/frontend/ui#keep-layout-inside-floorplans`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Route code MUST NOT declare layout outside its floorplan and display composites.

**Rationale:** A layout declaration in a route file is a second layout beside the floorplan: the route draws its own spacing while claiming the floorplan's shape. The next route copies the declaration because it looks like the local way to draw, and the floorplan stops describing what routes render. Floorplan and display composites own layout, so one change there reaches every route composing them.

**Reviewed for:** Which route files declare layout outside floorplan and display composites? Look in the route sources.

### Record a Freestyle Route

**ID:** `litespec/frontend/ui#record-a-freestyle-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route that composes no declared floorplan MUST name the decision record that justifies its layout.

**Rationale:** A route that composes no declared floorplan is either the first consumer of a floorplan that does not exist yet or a genuine one-off. Naming the decision record that justifies its layout makes either case reviewable and keeps the declared set closed. A second route of the same shape is the signal to add the floorplan instead of recording another freestyle route.

**Reviewed for:** Which freestyle routes name no decision record justifying their layout? Look in the freestyle routes and the decision records.

### Derive Each Page Type from a Named Reference

**ID:** `litespec/frontend/ui#derive-each-page-type-from-a-named-reference`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST name its page type's reference, with a freestyle route naming its decision record instead.

**Rationale:** An author with no named reference invents a page from primitives, and the next author invents another page for the same type. Naming the page type's reference keeps every page of one type on the one drawing it derives from, so the type stays one design. A freestyle route names its decision record instead, since it derives from no reference.

**Reviewed for:** Which routes name neither their page type reference nor, for a freestyle route, their decision record? Look in the route sources.

### Lead Each Screen to the Screens a Person Reaches Next

**ID:** `litespec/frontend/ui#lead-each-screen-to-the-screens-a-person-reaches-next`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST name in `leadsTo` every screen a person reaches from it in one step.

**Rationale:** The screens and their links are the navigation, drawn from the records before any route exists: the book draws each entry point's screens as a map of places and connections, and each flow as a strip of its screens in step order. A screen no other screen leads to strands its readers unless it starts its entry point, and a screen that leads nowhere and offers no use case ends their task with nothing done. Proposed by decision 0089.

**Example:** The Claimwick expense list names the expense record and the new expense form in `leadsTo`, so a reader reaches either in one step.

**Checked by:** `litespec model check` reports `screen-unreachable` for a screen no other screen leads to that is not its entry point's start, and `screen-dead-end` for a screen that leads nowhere and offers no use case. Run it at the consumer root.

### Format Money and Moments from the Reader's Language and the Business's Country

**ID:** `litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST name the money and moment forms its screens render in `formats`, rendered with the locale data (CLDR) of the reader's language and the currency and country the record carries, with no format string, separator, or symbol written in code.

**Rationale:** One record serves readers in several languages, and each language writes its separators its own way. The entry point names one money option and one moment option from the money and moment patterns, and each screen renders them with the locale data (CLDR) of the reader's language and the currency and country the record carries. A form fixed in code shows one reader the wrong separators and hides the choice from every check. Proposed by decision 0089.

**Example:** The Claimwick expense of EUR 1,212.35 reads `EUR 1.212,35` for a Dutch reader and `EUR 1,212.35` for an English reader, from the same record.

**Checked by:** `litespec model check` reports `sentence-language-missing` for a written refusal result with no sentence for a language its use case's entry points declare. Run it at the consumer root.

### Select a Tone from the Closed Set

**ID:** `litespec/frontend/ui#select-a-tone-from-the-closed-set`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A state indicator MUST take one tone from the closed set, each application declaring its state-to-tone mapping once as an exhaustive table of glossary terms.

**Rationale:** A page that picks its own color picks a meaning nobody agreed to: the same red celebrates on one screen and warns on another. One state-to-tone mapping per application, declared once as an exhaustive table of glossary terms, makes the same state read the same way on every screen. A new state fails to compile until its tone and its word exist, so no state ships uncolored or mislabeled.

**Reviewed for:** Which applications lack a state-to-tone mapping, or map a state outside the exhaustive table of glossary terms? Look in the application state mapping.

### Keep Tokens in One Definition

**ID:** `litespec/frontend/ui#keep-tokens-in-one-definition`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST use the shared token definition and declare no value, with no literal color in any shipped definition.

**Rationale:** One token definition is what makes two applications look like one product. An application uses the shared definition and declares no value of its own, so no second definition introduces a value the first has no opinion about. A definition the shared package ships declares no literal color, so every value traces to the one token file the design contract names.

**Reviewed for:** Which applications declare their own token value, and which shipped definitions carry a literal color? Look in the application and shared token definitions.

### Meet the Limits of Every Token Role

**ID:** `litespec/frontend/ui#meet-the-limits-of-every-token-role`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every token value in each declared color scheme MUST meet the limit the Concepts table states for its role.

**Rationale:** Readable text and findable controls are measured limits, not pinned values: a palette passes when every role meets its limit, whatever values it holds. Judging values instead of limits refuses a readable palette for differing from the baseline while waving through a baseline value on a new surface. The manifest baseline stays as defaults chosen because they pass, and a project sets its own values with no departure while they meet the limits. Proposed by decision 0089.

**Example:** The Claimwick card edge differs from the page by 1.2:1 in both schemes, so cards read as separate surfaces while separators stay quieter.

**Checked by:** `litespec model check` reports `token-limit-failed` for a token role that misses its limit in a declared scheme. Run it at the consumer root.

### Keep Token Values in One Token File

**ID:** `litespec/frontend/ui#keep-token-values-in-one-token-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep every token value in the one token file the design contract's Tokens section names, written in the W3C Design Tokens format, version 2025.10.

**Rationale:** One token file gives every value one home that design tools and token converters read: the components read the stylesheet generated from that file, and the check reads the file to judge every limit. Values scattered across stylesheets leave each limit to a hand-run script that runs once and is forgotten. Proposed by decision 0089.

**Example:** Claimwick keeps its values in `tokens/claimwick.tokens.json`, and generates its component stylesheet from that file.

```json
{
  "card": {
    "$value": "#ffffff",
    "$type": "color"
  }
}
```

**Reviewed for:** Which token values live outside the named token file, or break the W3C Design Tokens format? Look in the token file and the stylesheets.

### Give Every Input a Visible Boundary

**ID:** `litespec/frontend/ui#give-every-input-a-visible-boundary`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The boundary of a text field, select, checkbox, radio, or switch MUST reach 3:1 contrast against the surface it sits on.

**Rationale:** WCAG 1.4.11 Non-text Contrast: a reader with low vision finds a field by its edge, and a field drawn at the faint separator step disappears into the page. Giving inputs their own boundary token keeps separators quiet and controls findable. For example, the amount field on the Claimwick expense form keeps a visible edge on the card behind it in both color schemes.

**Reviewed for:** Which input boundary token falls below 3:1 against its surface in a declared color scheme? Look in the token definitions and the rendered form fields.

### Select a Command Surface by Size

**ID:** `litespec/frontend/ui#select-a-command-surface-by-size`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A command MUST use the surface the Concepts table assigns to its input.

**Rationale:** The surface fits the input: a button for a reversible action, a confirmation dialog for an irreversible one, a form dialog for one to four inputs, and a form page beyond that. A form dialog with more inputs than the table allows scrolls, loses its title, and hides the refusal beside the field that caused it. An oversized surface for a small command costs the user attention the command has no use for.

**Reviewed for:** Which commands use a surface other than the one the Concepts table assigns to their input? Look in the command surfaces.

### Show the Trace ID in Error Detail

**ID:** `litespec/frontend/ui#show-the-trace-id-in-error-detail`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Every visible error MUST show its trace identifier in copyable form in the error detail, from the problem body or the `X-Trace-Id` header.

**Rationale:** Support reads the trace identifier to find the failed request, and the user copies it from the detail without retyping. Shown in copyable form from the problem body or the `X-Trace-Id` header, it connects the user's report to the server's logs. The headline carries what happened in words; the identifier belongs in the detail.

**Reviewed for:** Which visible errors hide the trace identifier from copyable detail? Look in the error surfaces.

### Check Every Route

**ID:** `litespec/frontend/ui#check-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST pass each route suite check.

**Rationale:** The route suite visits every route and asserts each check the Concepts section names: the floorplan, the reference, accessibility, the heading, the landmark, reflow, text spacing, target size, keyboard use, the focus move on a route change, each color scheme, reduced motion, and status announcements. The suite discovers its routes from the application route definition, so a new route is checked with no new specification. Every route passing every check keeps each address inside the contract by default.

**Reviewed for:** Which discovered routes fail a route suite check? Look in the suite output and the CI job logs.


### Restrict Styling to Semantic Tokens

**ID:** `litespec/frontend/ui#restrict-styling-to-semantic-tokens`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A style MUST use a semantic token rather than a raw value.

**Rationale:** A raw value in a style answers to no token: the theme leaves it unchanged through every restyle, and the next author copies the value instead of the token. The shared package definition is the authoritative style surface, holding the tokens, the fonts, and the documented resets. A style built from semantic tokens stays inside the product's visual language by construction.

**Reviewed for:** Which styles use raw values instead of semantic tokens? Look in the stylesheets and the shared package definition.

### Govern Behavior Packages

**ID:** `litespec/frontend/ui#govern-behavior-packages`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A behavior package MUST name the decision record justifying it for the capability it supplies, and render through baseline components and tokens.

**Rationale:** A behavior package adds a capability the design baseline lacks, such as a chart or a rich editor, and naming the decision record that justifies it for that capability keeps the decision tied to the need. The decision follows the capability rather than the visual catalog, so no package duplicates a component the design baseline already provides. Rendering through baseline components and tokens keeps the new capability inside the product's look.

**Reviewed for:** Which behavior packages name no decision record justifying them, or render outside baseline components and tokens? Look in the package declarations and the decision records.

### Prove UI Behavior and Appearance

**ID:** `litespec/frontend/ui#prove-ui-behavior-and-appearance`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A primitive, composite, token, source, floorplan, or route change MUST name the evidence in the matrix in this section.

**Rationale:** Each change class carries different risk: a token change reaches every route in every profile, while a primitive change reaches one component. The matrix maps each class to the narrowest evidence that represents its risk, so a change runs the evidence its reach requires and no more.

| Change class | Narrowest evidence |
|:---|:---|
| Primitive or variant | The component test for that primitive |
| Floorplan or display composite | The route suite on one route that composes it |
| Token or global style | The route suite on one route in each profile |
| Route | The route suite on that route, and every test proving a result it calls |
| Pattern decision | The catalog entry changes with its implementation |
| Fork | The fork record and the component test |
| Behavior package | The accessibility check for the capability it supplies |

**Reviewed for:** Which changes name no evidence the matrix assigns to their class? Look in the change descriptions and the named evidence.

### Record a Frontend Outside the Controlled Contract

**ID:** `litespec/frontend/ui#record-a-frontend-outside-the-controlled-contract`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A frontend outside the controlled platforms MUST declare an entry point record naming that platform and the boundaries replacing this contract.

**Rationale:** A device client with no shared component and no router sits outside the floorplan set and the component rules: the contract's checks do not apply to it. Declaring an entry point record that names its platform and the boundaries replacing this contract keeps the decision visible, instead of letting the frontend report as one with nothing to check.

**Reviewed for:** Which frontends outside the controlled platforms declare no entry point record naming their platform and replacement boundaries? Look in the entry point records.

## Conventions

### Use the Product Profiles

**ID:** `litespec/frontend/ui#use-the-product-profiles`

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

**Rationale:** A command blocked only by the current state or role stays visible and disabled, with the reason beside it. The user looking for the action learns it exists and why it is unavailable, such as a missing permission or an unmet precondition. A control that vanishes instead looks like a product that does not offer the action at all.

**Reviewed for:** Which commands blocked only by state or role vanish instead of staying visible and disabled with the reason beside them? Look in the command surfaces.
