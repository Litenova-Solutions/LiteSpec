---
schemaVersion: 2
package: "@claimwick/ui"
profile: application-balanced
tokens: tokens/claimwick.tokens.json
pageFrames:
  list-page: ListPage
  record-page: EntityPage
  form-page: FormPage
---

# Claimwick shared UI package design contract

Read this before composing anything. The list below is the whole of it. A screen that needs a pattern outside it needs a decision first.

## Baseline

shadcn-svelte 1.7.0, style nova on bits-ui, baseColor neutral, font geist. The manifest documents these values as defaults that pass every token limit.

## Tokens

Values live in tokens/claimwick.tokens.json, in the W3C Design Tokens format, version 2025.10. The file carries the light and dark schemes, and the component stylesheet is generated from it. Change a value there. A color written into a component is a color nobody can theme and nobody can find.

## Page Frames

The page frames this package provides, each mapped to the floorplan that implements it. A route composes exactly one of them, or names the decision behind a freestyle layout.

| Page frame | Component | Use it when the user |
|:---|:---|:---|
| `list-page` | `ListPage` | Finds one record among many. The page shows the result count. |
| `record-page` | `EntityPage` | Acts on one record: its facts, its state, and the actions that state permits. |
| `form-page` | `FormPage` | Supplies the input of one command, with one primary action. |

## Vocabulary

The composites applications compose from. Name the ones a page reaches for, one sentence each.

- A `button` is the only clickable element. A `div` carrying an `onClick` is refused.
- A `badge` is a state in one word, and its tone is one of five.
- A `card` is a grouped fact set, never a screen.

## Motion

| Name | Duration | Where |
|:---|:---|:---|
| instant | 0ms | Any state change a button causes. |
| quick | 120ms | Row hover, badge change, focus ring. |
| slow | 240ms | Drawer open, mobile navigation. |

When the reader asks for reduced motion, `quick` and `slow` become a fade of at most 120ms, and nothing slides, scales or loops.

A number is never animated. The number is the answer.

## Browser Support

Baseline widely available. Every platform feature this package and its routes use has worked in every major browser for at least 30 months, or the file that uses it carries a fallback beside it.
