---
schemaVersion: 2
package: "@__SCOPE__/ui"
profile: application-balanced
tokens: src/styles/tokens.css
pageFrames:
  home-page: HomePage
  list-page: ListPage
  work-page: WorkPage
  record-page: RecordPage
  settings-page: SettingsPage
  form-page: FormPage
---

# __PROJECT__ shared UI package design contract

Read this before composing anything. The list below is the whole of it. A screen that needs a pattern outside it needs a decision first.

## Baseline

shadcn-svelte 1.7.0, style nova on bits-ui, baseColor neutral, font geist. The manifest documents these values as defaults that pass every token limit.

## Tokens

Values live in src/styles/tokens.css, generated from the shadcn-svelte registry preset, carrying the light and dark schemes. Change a value there and nowhere else: a color written into a component is a color nobody can theme and nobody can find.

## Page Frames

The page frames this package provides, each mapped to the floorplan that implements it. A route composes exactly one of them, or names the decision behind a freestyle layout.

| Page frame | Component | Use it when the user |
|:---|:---|:---|
| `home-page` | `HomePage` | Sees waiting work first and the period summary second. |
| `list-page` | `ListPage` | Finds one record among many and opens it. |
| `work-page` | `WorkPage` | Clears waiting items or makes one focused decision. |
| `record-page` | `RecordPage` | Acts on one record: its facts, its state, and the actions that state permits. |
| `settings-page` | `SettingsPage` | Configures one domain area without vertical clutter. |
| `form-page` | `FormPage` | Supplies the input of one command, with one primary action. |

## Vocabulary

The composites applications compose from. Name the ones a page reaches for, one sentence each.

- A `button` runs an action on the page, and a link moves the reader to another place.
- A `data-table` lists rows of one record kind under one caption.
- A `text-link` moves the reader to another address with the shared focus state.
- A `property-list` aligns the facts of one record in key and value columns.
- A `metric-grid` holds the period summary in balanced desktop columns.
- A `field-grid` lines the fields of one form group on 12 aligned columns.
- An `action-bar` orders the primary, secondary, and danger actions of one page in matching sizes.
- A `subsection` divides one card that truly holds two labeled groups.

## Motion

| Name | Duration | Where |
|:---|:---|:---|
| instant | 0ms | The default for a state change a button causes. |
| quick | 120ms | Row hover, badge change, focus ring. |
| slow | 240ms | Drawer open, mobile navigation. |

These timings are defaults. A product may set its own, within the reduced-motion limit below.

When the reader asks for reduced motion, `quick` and `slow` become a fade of at most 120ms, and nothing slides, scales or loops.

A number is never animated. The number is the answer.

## Browser Support

Baseline widely available. Every platform feature this package and its routes use has worked in every major browser for at least 30 months, or the file that uses it carries a fallback beside it.
