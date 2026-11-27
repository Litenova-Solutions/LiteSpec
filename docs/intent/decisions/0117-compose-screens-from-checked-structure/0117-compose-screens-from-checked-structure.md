# Compose Screens from Checked Structure

LiteSpec states screen composition as required standards that apply to route files, with the screen record naming its frame and primary action, the wireframe carrying the structure, and the checks holding each one. An agent building a screen meets the rules where it builds, and the faults fail before they ship.

## Context

Agents that follow LiteSpec still build screens with the same faults. Pages do too many jobs. Cards sit inside cards and borders inside borders. Forms lie on the page background. Neighbouring surfaces share one fill, such as a table header the colour of the page. Button groups mix sizes and placement. One element is drawn two ways on two pages. Long forms stay undivided while short fields stretch full width. A screen offering many use cases becomes one long page with a card per use case. Raw identifiers, codes and message keys reach the screen.

A gap analysis traced four causes. Pattern provisions print as bare identifiers on route files, so the agent never reads them. The screen record never asks for the page frame or the primary action, and the wireframe is judged only for presence and name resolution. The token roles cannot describe stacked layers. Nothing reads the rendered page.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Leave screen structure to patterns and review | No new provisions, and each product keeps its own layout judgment. | The faults repeat on every product, because guidance the agent never meets cannot shape the screen it builds. |
| State composition as required standards with checked structure | Every route file carries the rules as statements, the record names its frame and action, the wireframe carries the structure, and the checks hold each one. | Fifteen new provisions to keep, and products with evidence for other limits must set them. |
| Check structure only in the consumer route suite | No new provisions and no new commands; each product asserts what it needs. | The rules exist where no other product can reuse them, and nothing reports a missing assertion. |

## Decision

The page `docs/frontend/ui.md` holds fifteen required composition standards applying to artifact route, so `rules` prints them as statements on route files. The screen record requires `pageFrame` with a route and gains optional `primaryAction` naming an offered use case. The project file gains optional `ui.limits` with `sections`, `formFields` and `offers`, defaulting to three sections, six fields and four offers.

The wireframe language bans card and region nesting, requires every field under a section part, requires tabs or navigation past the section limit, requires grouped regions past the field limit, and allows one primary action per whole view. The screen blueprint check reports each breach.

## Consequences

The pattern pages draw every toolbar inside its section, one primary action per view, and no card inside a region, so the taught shapes agree with the rules. The layer roles are decided in decision 0118, and the rendered-page audit in decision 0119. This decision supersedes `decision/0028-one-default-and-two-pattern-options`: composition answers move from pattern options to required standards, because pattern provisions print as bare identifiers the agent never reads.

## Revisit Conditions

Reopen this when screens still ship the listed faults after the checks run, or when projects routinely waive the composition rules instead of following them.
