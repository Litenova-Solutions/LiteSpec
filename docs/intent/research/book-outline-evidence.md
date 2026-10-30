---
{
  "type": "research",
  "id": "research/book-outline-evidence",
  "title": "Book Outline Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "Where does the book outline place each record today, and where does that placement invent a parent LiteSpec never named?",
  "conclusion": "The outline places Behavior correctly but invents Intent groupings from file paths and build order, splits the glossary by layer, and deletes the Implementation and Proof layers it builds, so the book teaches a second hierarchy beside the model's own.",
  "sources": [
    {
      "path": "docs/core/model.md",
      "read": "2026-10-30"
    },
    {
      "path": "docs/tools/book.md",
      "read": "2026-10-30"
    }
  ]
}
---

# Book Outline Evidence

## Answer

This record distils the book outline investigation of 2026-10-30 into the claims this release relies on. It was read against the approved mockup builder at `.agents/mockups/book-v6/build-data.js`, read on 2026-10-30; that folder is untracked working material and is not a `sources` entry. Sample counts below come from the mockup's `data.json` (1,381 of 21,548 records, with 1,988 more as outside stubs).

LiteSpec gives exactly three ways to earn a parent in the tree, and the book may use no fourth: identifier containment, a required single link, or no parent as a top-level list under the record's own layer. Two corollaries matter. A parent must be read off the child's own identifier or fields, never by inverting someone else's link without saying so, because links live on the record that points. And book labels must be a record type, a field name, a status, or a glossary term, so any outline node that is none of those is an invented grouping by definition. Confidence is high: every verdict below is measured against the governing provision, the identifier table, the Links table, and the containment paragraph.

Intent is mostly misplaced. Product, goal, problem, constraint, research, journey, and actor are correctly flat: nothing in their identifiers contains a parent and none carries a required parent link. Feature has two candidate parents and LiteSpec picks neither; the builder nests a feature under the first problem that claims it and silently drops any second parent through its placed-once guard, leaving the 71 refused features (all with no `solves`) as unlabelled roots beside live features. Requirement is the clearest wrong place: the builder invents a `requirement-page/<file path>` node from the source file, but a requirement page is a page style, not a record type, so the grouping breaks the outline provision and the label provision at once, and hides the 719 requirements naming neither anchor. In the sample, 32 topics and 32 pages coincide, so grouping by topic and by file look identical; at any other size only the topic is in the identifier. Decision sits in the wrong layer: all 74 decisions land in one Intent list, but 71 are technical decisions that belong under Implementation, and a reader meets implementation choices inside a layer that must never contain mechanism. Term is split across Intent and Behavior by its `layer` field, but that field says which clock the word follows, not where the entry lives; the glossary is one list the split gives two homes. Journey is correctly flat, because its `goal` is a sentence, not an identifier; nesting journeys under goals needs a model change first, not a builder choice.

Behavior is mostly right. Aggregates sit under modules by identifier prefix, scenarios group under their result through the scenario's own `result` field, screens nest under the entry point their field and identifier both name, and `host` is correctly ignored for placement. Four placements are wrong or fragile. Rules file under the first governed aggregate instead of their identifier home, so a cross-aggregate rule is forced under an aggregate it merely governs; `governs` is trace, not parentage. Events collect onto aggregates through use-case `raises` lists instead of the aggregate their identifier names, so an unraised or cross-raised event is misplaced. Results and scenarios attach through shared files, and scenarios on a reaction page have no outlined parent at all, though the specification allows them there. Reactions, jobs, notifications, and settings claim a module only when the source path contains a module segment; anything else becomes a Behavior root through the stray sweep, which appends every unplaced record regardless of type, promoting records whose module is merely outside the sample to roots beside modules. Only sample-listed modules receive children at all, which is sampling rather than outline logic, but a reader cannot tell the difference. Use cases need a stated precedence the specification never gives: aggregate where the model assigns one, module otherwise, never a file path. Roles, permissions, and entry points are correctly flat; nesting permissions under roles would duplicate every shared permission, and only 4 of 23 actors record any `holds`, so any outline built on holds would orphan most actors.

Governance has the right shape and the wrong label: known gaps and deviations list separately with the right entries, but the builder emits the list as a third layer beside Intent and Behavior, where the provision wants governance beside all four layers, attached by subject links that never nest. Implementation and Proof have no place at all, the largest single defect: the builder constructs both outlines, then deletes every code, route, test, and finding record before writing, while the totals still count entries no reader can open. The deleted groupings rest on invented nodes (`architecture-layer`, `folder`, `test-project`, `finding-code`, and others), none of them a permitted label. LiteSpec already defines both parents in the marks: code sits under the record it implements, tests under the lowest record they prove, findings under their subject, with file locations covered by source links on each entry.

Five gaps are LiteSpec's, not the builder's. Feature needs a primary-parent rule. Requirement needs a stated tree home: the topic in its own identifier, with links as trace. Journey needs a goal identifier field. Rule needs its identifier home resolved as a typed reference. Governance needs the attach-by-link rule stated once. A sixth, smaller gap: a platform-wide setting still carries a module-shaped identifier, so LiteSpec should name what module value it carries or admit a platform group. The investigation proposes eleven outline rules: four layer roots with governance beside them, each record placed exactly once by identifier then required link then type list, Intent ordered with requirements by topic and decisions split by home, features nested by a stated precedence with extra links as trace, Behavior ordered module then access then published set then operations with identifier-homed rules and events and an explicit outside-this-book note, one undivided glossary, Implementation and Proof under the records their marks name, governance linked never nested, and any unplaceable record reported as a build finding with no stray sweep.
