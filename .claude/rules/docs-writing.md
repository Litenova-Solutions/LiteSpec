---
paths:
  - "docs/**/*.md"
---

# Docs Writing Rules

This rule applies to every page under `docs/`. It carries the LiteSpec Style Card: plainspoken technical prose from one developer to a capable colleague.

The full card with the golden rewrite lives in `WRITING.md`. The long rewriting workflow lives in the on-demand skill at `templates/skill/rewrite-docs/SKILL.md`.

## Voice

Lead with the answer, the task, or the fact, then give the reason and one concrete Claimwick example.

Write for the reader who arrives cold from a link or a search result: name who the page concerns and what is at stake before any pointer.

Name the actor and the act: active voice, present tense, ordinary technical verbs.

Use literal domain words from the glossary, define a new term where it first appears, and avoid figures of speech.

State what the reader can check: the observable behavior, the exact path, command, or value, and the limit.

## Paragraphs and Structure

Keep one idea in each paragraph, two to four sentences, each paragraph on one line in Markdown.

Use a list only for truly parallel items, a table for comparisons or repeated attributes, and prose for connected reasoning.

Keep normative strength intact: never soften a requirement into advice, never harden advice into a requirement, and never drop a condition when you shorten a sentence.

Show one Claimwick example per rule, with names and figures from the cast or from a cited record.

## Replace Stilted Phrasing

| Avoid | Use instead |
|---|---|
| `gains a home` | `lives in`, `is stored in` |
| `refuses other folders` | `only the listed folders are allowed` |
| `names no ` | `lists no`, `states no` |
| `the homes of records` | `where records live`, `record locations` |
| `holds the values ... speaks` | `states the values in plain words` |
| `seamlessly`, `instantly`, `effortlessly` | The measured time, count, or visible result |
| `unlock the power of` | What the reader builds with it |

## Unsafe Rewrites

| Original | Unsafe rewrite | Why it breaks meaning |
|---|---|---|
| A node **may contain** layer folders. | A node **contains** layer folders. | Permission becomes a requirement. |
| Endpoints **without a handler** live in the host. | Endpoints **outside a use case** live in the host. | The placement condition changes. |
| Types used by **two or more** modules. | Types used by **every** module. | The scope widens. |
| Domain code does not depend on outer layers. | Infrastructure changes never affect behavior. | A dependency rule becomes an outcome guarantee. |

## Load the Full Workflow When Editing

For more than a one-line fix, load `templates/skill/rewrite-docs/SKILL.md` before drafting.

That skill holds Copyedit versus Rewrite versus Technical revision, the fact inventory, contradiction checks, and the semantic verification pass.
