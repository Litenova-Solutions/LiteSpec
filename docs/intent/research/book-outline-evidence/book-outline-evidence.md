# Book Outline Evidence

The book is the generated reference that LiteSpec builds from its records. The book outline decides where each record appears in the book and under which parent. This research record checks where the current outline builder places each record, and where that placement invents a parent that LiteSpec never defined. It matters because readers learn a product's structure from the book, so an invented grouping teaches a second hierarchy beside the real one.

## Findings

LiteSpec allows 3 ways for a record to earn a parent, and the book may use no fourth. The current outline places Behavior records mostly right. It invents Intent groupings from file paths and build order, splits the glossary by layer, and drops the Implementation and Proof records that the builder constructs.

The 3 ways are identifier containment, a required single link, and no parent, which places the record as a top-level list under its own layer. The record states 2 corollaries. A parent must be read from the child's own identifier or fields, never by inverting another record's link without saying so. Book labels must be a record type, a field name, a status or a glossary term, so any other outline node is an invented grouping. The record gives no confidence for these rules.

Product, goal, problem, constraint, research, journey and actor are correctly flat. Nothing in their identifiers holds a parent, and none carries a required parent link. The record gives high confidence for this verdict. It measures each verdict against the governing provision, the identifier table, the Links table and the containment paragraph.

Feature has 2 candidate parents, and LiteSpec picks neither. The builder nests a feature under the first problem that claims it. Its placed-once guard silently drops any second parent, which leaves 71 features with no `solves` as unlabelled roots beside the live features. The record gives high confidence for this verdict.

The builder invents a `requirement-page/<file path>` node from the source file. A requirement page is a page style, not a record type, so the grouping breaks both the outline provision and the label provision. The grouping also hides the 719 requirements that name neither a feature nor a `basedOn` entry. In the sample, 32 requirement pages and 32 files coincide, so grouping by page and grouping by file look the same. At any other size, only the feature entry decides. The record gives high confidence for this verdict.

All 74 decisions land in one Intent list. Of them, 71 are technical decisions that belong under Implementation. A reader therefore meets implementation choices inside a layer that must never contain mechanism. The record gives high confidence for this verdict.

Term is split across Intent and Behavior by its `layer` field. That field says which clock the word follows, not where the entry lives. The glossary is one list, and the split gives it 2 homes. Journey is correctly flat, because its `goal` is a sentence, not an identifier. Nesting journeys under goals needs a model change first, not a builder choice. The record gives high confidence for these verdicts.

Behavior is mostly right. Aggregates sit under modules by identifier prefix. Scenarios group under their result through the scenario's own `result` field. Screens nest under the entry point that both their field and their identifier name. The builder correctly ignores `host` for placement. The record gives high confidence for this verdict.

The outline has 4 placements that are wrong or fragile. Rules file under the first aggregate they govern, not under their identifier home, so a cross-aggregate rule lands under an aggregate it only governs. The record notes that `governs` is trace, not parentage. Events collect onto aggregates through use-case `raises` lists, not through the aggregate their identifier names. An event that no use case raises is therefore misplaced. The record gives high confidence for these verdicts.

Results and scenarios attach through shared files. A scenario on a reaction page has no outlined parent, although the specification allows it there. Reactions, jobs, notifications and settings claim a module only when their source path has a module segment. Any other record becomes a Behavior root through the stray sweep. The sweep appends every unplaced record, whatever its type, and it promotes records whose module lies outside the sample to roots beside the modules. The record gives high confidence for these verdicts.

Only the sampled modules receive children at all. The record calls this sampling, not outline logic, but a reader cannot tell the difference. Use cases need a precedence that the specification never gives: the aggregate where the model assigns one, the module otherwise, and never a file path. The record gives high confidence for this verdict.

Roles, permissions and entry points are correctly flat. Nesting permissions under roles would repeat every shared permission. Only 4 of 23 actors record any `holds`, so an outline built on `holds` would orphan most actors. The record gives high confidence for this verdict.

Governance has the right shape and the wrong label. Waivers list separately with the right entries. The builder emits that list as a third layer beside Intent and Behavior. The provision places governance beside all 4 layers, attached by subject links that never nest. The record gives high confidence for this verdict.

Implementation and Proof have no place in the outline, which the record calls the largest single defect. The builder constructs both outlines and then deletes every code, route, test and finding record before it writes the book. The totals still count entries that no reader can open. The deleted groupings rest on invented nodes such as `architecture-layer`, `folder`, `test-project` and `finding-code`, and none is a permitted label. The record gives high confidence for this verdict.

LiteSpec already defines both parents in its code marks. Code sits under the record it implements, tests sit under the lowest record they prove, and findings sit under their subject. Source links on each entry cover file locations. The record gives high confidence for this claim.

The outline has 5 gaps that belong to LiteSpec, not to the builder. Feature needs a primary-parent rule. Requirement needs a stated tree home, the feature that holds it as an entry, with links kept as trace. Journey needs a goal identifier field. Rule needs its identifier home resolved as a typed reference. Governance needs its attach-by-link rule stated once. The record gives no confidence for these gaps.

A sixth and smaller gap is that a platform-wide setting still carries a module-shaped identifier. LiteSpec must either name the module value that setting carries or admit a platform group. The record gives no confidence for this gap.

The investigation proposes 11 outline rules. They set 4 layer roots with governance beside them, and they place each record once: by identifier, then by required link, then by type list. Intent is ordered with requirements under their feature, and decisions are split by home. Features nest by a stated precedence, and extra links count as trace.

The other proposed rules order Behavior by module, access, published set and operations. They place rules and events by identifier home and state the records outside the book in one note. They keep one undivided glossary and place Implementation and Proof under the records their marks name. They link governance without nesting it. A record that cannot be placed becomes a build finding, and no stray sweep remains. The record states no confidence for these rules.

## Source Notes

The record names 2 repository sources read on 30 October 2026: the core model page (`docs/core/model.md`) and the book tool page (`docs/tools/book.md`). It also reads the accepted mockup builder at `.agents/mockups/book-v6/build-data.js` on the same day. That folder is untracked working material, so it is not in the sources list. The record does not say which claim each source supports.

The sample counts come from the mockup's `data.json`. The sample holds 1,381 of 21,548 records, with 1,988 more outside stubs. The record measured placement in the sample only, so it cannot say what the full outline would show.

The record reports that the builder disagrees with the provisions, and it treats the provisions as the standard. It names no disagreement between the 2 sources.
