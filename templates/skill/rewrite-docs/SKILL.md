---
name: rewrite-docs
description: Rewrite documentation from a fact inventory in plainspoken voice without changing technical meaning. Load only when editing docs, guides, or explanation files.
---

# Rewrite Docs Skill

This skill rewrites documentation ground up from facts: it extracts what must not change, drafts fresh prose from that inventory, and verifies the result against it.

Load it only when editing human documentation. For a one-line fix, apply the Style Card in `WRITING.md` directly and skip this skill.

## Set the Scope of the Edit

Decide which kind of work the task asks for before you change a word.

- **Copyedit:** Fix grammar, punctuation, word choice, and sentence flow. Keep the organization.
- **Rewrite:** Reorganize sentences, paragraphs, and sections for clarity. Keep technical meaning and required page structure.
- **Technical revision:** Change requirements, architecture, capabilities, examples, or definitions. Do this only when the task asks for it.

When asked to make a page clearer, treat it as a rewrite, not a technical revision.

An authoritative source, such as the standard that defines a term, tells you whether a technical claim is correct. It does not by itself add that fix to an editorial task.

## List What Must Not Change

Before drafting, write the fact inventory for the page: requirements and their strength, permissions, conditions, exceptions, scope, dependency direction, placement rules, defined terms, commands, paths, identifiers, field names, finding codes, and the meaning of each example.

Keep required structure: section headings, metadata, provision IDs, strength lines, statements, rationales, examples, and links with their anchors.

Small wording changes can change a rule:

| Original | Unsafe rewrite | Why it breaks meaning |
|---|---|---|
| A node **may contain** layer folders. | A node **contains** layer folders. | Permission becomes a requirement. |
| Endpoints **without a handler** live in the host. | Endpoints **outside a use case** live in the host. | The placement condition changes. |
| Types used by **two or more** modules. | Types used by **every** module. | The scope widens. |
| Domain code does not depend on outer layers. | Infrastructure changes never affect behavior. | A dependency rule becomes an outcome guarantee. |

## Draft from the Inventory

Discard the old sentence skeletons and write fresh prose from the inventory.

Group the facts by reader task first: what the reader does, the prerequisites, the steps, the expected result, and the failure cases.

Use prose for connected reasoning, lists for parallel items, and tables for comparisons or repeated attributes.

Replace awkward phrasing with ordinary technical words:

| Awkward phrasing | Clearer alternative |
|---|---|
| Code that names several modules | Code that references several modules |
| A closed set refuses other entries | Only the listed entries are allowed |
| What another module may name | What another module may reference |
| This page holds the bindings | This page defines how the rules apply |

Pick the replacement that fits the sentence: `references`, `uses`, and `depends on` are not interchangeable.

## Check for Contradictions

Compare prose, normative statements, tables, examples, and check descriptions against each other.

Look for disagreements about allowed folders or values, required versus optional elements, ownership and placement, dependency direction, exceptions, and what tools actually check.

Example conflict: a paragraph states subfolders are unrestricted inside layer folders while a table states that use-case `Application/` and `WebApi/` folders hold files only.

Do not pick a side silently. Find the authoritative source; when the task permits technical revision, fix the page to match it, otherwise report the conflict in a short note beside the draft.

## Preserve Meaning When Cutting

Remove repetition, not requirements.

Before deleting a sentence, ask whether it carries a definition, a condition, an exception, a responsibility, a scope boundary, or an explanation the rule needs to apply.

A definition stays even when it interrupts the opening: move it to the concept section where it belongs instead of dropping it.

Keep each condition when you split a long paragraph into one part per task.

## Match the Rewrite to the Page Type

For a standard or reference page, state the subject and scope directly, keep definitions and constraints close to the rules they explain, and keep normative wording and provision fields exact.

For a guide, organize around the task, put prerequisites before instructions, and state expected results and relevant failure cases.

For an overview page, explain the problem and the approach, describe effects through supported mechanisms, and keep scope and limits visible.

## Review the Meaning After Rewriting

Compare the finished draft with the source inventory and reject any draft that alters technical meaning.

- Did any permission become a requirement, or any advice become a rule?
- Did any condition grow broader or narrower, or any exception disappear?
- Did any definition change, or any required section or field go missing?
- Did the draft imply a new capability or a new guarantee about tools, tests, or agents?
- Does each example still match the rule beside it, with names and figures from the cast or a cited record?

## Editing Sequence

1. Name the page type and the reader task.
2. List the required structure and the facts that must not change.
3. Flag ambiguity and contradictions.
4. Reorganize the facts around the reader task, not the source order.
5. Rewrite awkward phrasing in plain words.
6. Remove filler, repetition, and framing that adds no fact.
7. Compare the draft with the inventory for semantic changes.
8. Check examples, commands, identifiers, and links.
9. Read for natural flow without chatty or choppy sentences.
10. Return the draft with unresolved issues listed separately.

Success criterion: the reader learns the same technical content with less effort, with no new claims added as clarification.
