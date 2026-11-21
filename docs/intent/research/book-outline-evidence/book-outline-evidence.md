# Book Outline Evidence

The book is the generated reference that LiteSpec builds from its records. The book outline states where each record appears in the book and under which parent. This research record checks where the current outline builder places each record and where it invents a parent that LiteSpec never defined. It matters because readers learn a product's structure from the book, so an invented grouping teaches a second hierarchy beside the real one.

## Findings

LiteSpec allows 3 ways for a record to receive a parent, and the book may use no fourth (`model`, `book`). The current outline places most Behavior records correctly (`model`, `book`). It invents Intent groupings from file paths and build order, and it splits the glossary by layer (`model`, `book`). It drops the Implementation and Proof records that the builder constructs (`model`, `book`).

The 3 ways are identifier containment, a required single link, and no parent, which places the record as a top-level list under its own layer (`model`, `book`). The record states 2 corollaries (`model`, `book`). A reader must read a parent from the child's own identifier or fields, never by inverting another record's link without saying so (`model`, `book`). A book label must be a record type, a field name, a status, or a glossary term, so any other outline node is an invented grouping (`model`, `book`).

Product, goal, problem, constraint, research, flow, and actor stand correctly as top-level lists (`model`, `book`). Nothing in their identifiers names a parent, and none carries a required parent link (`model`, `book`). It measures each verdict against the governing provision, the identifier table, the Links table, and the containment paragraph (`model`, `book`).

Feature has 2 candidate parents, and LiteSpec names neither (`model`, `book`). The builder nests a feature under the first problem that claims it (`model`, `book`). Its placed-once guard drops any second parent without a finding, which leaves 71 features with no `solves` as unlabeled roots beside the live features (`model`, `book`).

The builder invents a `requirement-page/<file path>` node from the source file (`model`, `book`). A requirement page is a page style, not a record type, so the grouping breaks both the outline provision and the label provision (`model`, `book`). The grouping also hides the 719 requirements that name neither a feature nor a `basedOn` entry (`model`, `book`). In the sample, 32 requirement pages and 32 files coincide, so grouping by page and grouping by file produce the same grouping (`model`, `book`). At any other size, only the feature entry decides the grouping (`model`, `book`).

The builder places all 74 decisions in one Intent list (`model`, `book`). Of them, 71 are technical decisions that belong under Implementation (`model`, `book`). A reader therefore meets implementation choices inside a layer that must never contain mechanism (`model`, `book`).

The builder splits Term across Intent and Behavior by its `layer` field (`model`, `book`). That field states which clock the word follows, not where the entry stands (`model`, `book`). The glossary is one list, and the split places it in 2 homes (`model`, `book`). Flow stands correctly as a top-level list, because its `goal` is a sentence, not an identifier (`model`, `book`). Nesting flows under goals needs a model change before the builder chooses it (`model`, `book`).

Most Behavior placements are correct (`model`, `book`). Aggregates stand under modules by identifier prefix (`model`, `book`). The builder groups scenarios under their result through the scenario's own `result` field (`model`, `book`). Screens nest under the entry point that both their field and their identifier name (`model`, `book`). The builder correctly ignores `host` for placement (`model`, `book`).

The outline has 4 placements that are wrong or fragile (`model`, `book`). The builder files rules under the first aggregate they govern, not under their identifier home (`model`, `book`). A cross-aggregate rule therefore stands under an aggregate it only governs (`model`, `book`). The record notes that `governs` is trace, not parentage (`model`, `book`). The builder collects domain events onto aggregates through use-case `raises` lists, not through the aggregate their identifier names (`model`, `book`). The builder therefore misplaces a domain event that no use case raises (`model`, `book`).

The builder attaches results and scenarios through shared files (`model`, `book`). A scenario on a reaction page has no outlined parent, although the specification allows it there (`model`, `book`). Reactions, jobs, notifications, and settings name a module only when their source path has a module segment (`model`, `book`). The stray sweep makes any other record a Behavior root (`model`, `book`). The sweep appends every unplaced record, whatever its type, and it promotes records whose module lies outside the sample to roots beside the modules (`model`, `book`).

The builder gives children only to the sampled modules (`model`, `book`). The record calls this sampling, not outline logic, but a reader sees no difference (`model`, `book`). Use cases need a precedence that the specification never gives: the aggregate where the model assigns one, the module otherwise, and never a file path (`model`, `book`).

Roles, permissions, and entry points stand correctly as top-level lists (`model`, `book`). Nesting permissions under roles would repeat every shared permission (`model`, `book`). Only 4 of 23 actors record any `holds`, so an outline that uses `holds` would orphan most actors (`model`, `book`).

Governance has the correct shape and an incorrect label (`model`, `book`). The builder lists waivers separately with the correct entries (`model`, `book`). The builder emits that list as a third layer beside Intent and Behavior (`model`, `book`). The provision places governance beside all 4 layers, and subject links attach it without nesting (`model`, `book`).

Implementation and Proof have no place in the outline, which the record calls the largest single defect (`model`, `book`). The builder constructs both outlines and then deletes every code, route, test, and finding record before it writes the book (`model`, `book`). The totals still count entries that open for no reader (`model`, `book`). The deleted groupings use invented nodes such as `architecture-layer`, `folder`, `test-project`, and `finding-code`, and none is a permitted label (`model`, `book`).

LiteSpec defines both parents in its code marks (`model`, `book`). Code stands under the record it implements, tests stand under the lowest record they prove, and findings stand under their subject (`model`, `book`). Source links on each entry cover file locations (`model`, `book`).

The outline has 5 gaps that belong to LiteSpec, not to the builder (`model`, `book`). Feature needs a primary-parent rule (`model`, `book`). Requirement needs a stated tree home in the feature that contains it as an entry, and it keeps links as trace (`model`, `book`). Flow needs a goal identifier field (`model`, `book`). Rule needs its identifier home resolved as a typed reference (`model`, `book`). Governance needs one statement of its attach-by-link rule (`model`, `book`).

A sixth and smaller gap is that a platform-wide setting still carries a module-shaped identifier (`model`, `book`). LiteSpec must either name the module value that setting carries or admit a platform group (`model`, `book`).

The investigation proposes 11 outline rules (`model`, `book`). They define 4 layer roots with governance beside them, and they place each record once: by identifier, then by required link, then by type list (`model`, `book`). The rules order Intent with requirements under their feature, and they split decisions by home (`model`, `book`). Features nest by a stated precedence, and extra links count as trace (`model`, `book`).

The other proposed rules order Behavior by module, access, published set, and operations (`model`, `book`). They place rules and domain events by identifier home and state the records outside the book in one note (`model`, `book`). They keep one undivided glossary and place Implementation and Proof under the records their marks name (`model`, `book`). They link governance without nesting it (`model`, `book`). A record that the builder leaves unplaced becomes a build finding, and no stray sweep remains (`model`, `book`).

## Source Notes

The record names 2 repository sources read on 30 October 2026: the core model page (`docs/core/model.md`) and the book tool page (`docs/tools/book.md`). It also read the accepted mockup builder at `.agents/mockups/book-v6/build-data.js` on the same day. That folder is untracked working material, so it is not in the sources list. Each finding cites the sources that support it.

The sample counts come from the mockup's `data.json`. The sample contains 1,381 of 21,548 records, with 1,988 more outside stubs. The record measured placement in the sample only, so it states nothing about the full outline.

The record reports that the builder disagrees with the provisions, and it treats the provisions as the standard. It names no disagreement between the 2 sources.
