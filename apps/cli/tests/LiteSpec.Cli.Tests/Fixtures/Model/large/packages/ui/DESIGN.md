---
schemaVersion: 2
package: "@fixture/ui"
profile: application-balanced
tokens: tokens/fixture.tokens.json
pageFrames:
  list-page: ListPage
  record-page: EntityPage
  form-page: FormPage
---

# fixture shared UI package design contract

Read this before composing anything. The list below is the whole of it. A screen that needs a pattern outside it needs a decision first.

## Baseline

State the pinned design baseline and its version: the system, style, base, colors, and font the package builds on.

## Tokens

Name the path of the token values file, in the W3C Design Tokens format, version 2025.10, with the schemes it carries. Values live there, and the component stylesheet is generated from it.

## Page Frames

The page frames this package provides, each mapped to the floorplan that implements it. A route composes exactly one of them, or names the decision behind a freestyle layout.

| Page frame | Component | Use it when the user |
|:---|:---|:---|
| `list-page` | `ListPage` | Finds one record among many. The page shows the result count. |
| `record-page` | `EntityPage` | Acts on one record: its facts, its state, and the actions that state permits. |
| `form-page` | `FormPage` | Supplies the input of one command, with one primary action. |

## Vocabulary

The composites applications compose from. Name the ones a page reaches for, one sentence each.

- A `button` runs an action on the page, and a link moves the reader to another place. A clickable element that is neither carries its role, its keyboard behavior and its focus state.
- A `badge` is a state in one word, and its tone is one of five.
- A `card` is a grouped fact set, never a screen.

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
