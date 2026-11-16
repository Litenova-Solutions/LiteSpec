# Book Outline Evidence

The book is the generated reference that LiteSpec builds from its records. The book outline states where each record appears in the book and under which parent. This research record checks where the current outline builder places each record and where it invents a parent that LiteSpec never defined. It matters because readers learn a product's structure from the book, so an invented grouping teaches a second hierarchy beside the real one.

## Findings

LiteSpec allows 3 ways for a record to receive a parent, and the book uses no fourth. The current outline places most Behavior records correctly. It invents Intent groupings from file paths and build order, and it splits the glossary by layer. It drops the Implementation and Proof records that the builder constructs.

The 3 ways are identifier containment, a required single link, and no parent, which places the record as a top-level list under its own layer. The record states 2 corollaries. A reader reads a parent from the child's own identifier or fields, never by inverting another record's link without saying so. A book label is a record type, a field name, a status, or a glossary term, so any other outline node is an invented grouping. The record gives no confidence for these rules.

Product, goal, problem, constraint, research, flow, and actor stand correctly as top-level lists. Nothing in their identifiers names a parent, and none carries a required parent link. The record gives high confidence for this verdict. It measures each verdict against the governing provision, the identifier table, the Links table, and the containment paragraph.

Feature has 2 candidate parents, and LiteSpec names neither. The builder nests a feature under the first problem it names. Its placed-once guard drops any second parent without a finding, which leaves 71 features with no `solves` as unlabeled roots beside the live features. The record gives high confidence for this verdict.

The builder invents a `requirement-page/<file path>` node from the source file. A requirement page is a page style, not a record type, so the grouping breaks both the outline provision and the label provision. The grouping also hides the 719 requirements that name neither a feature nor a `basedOn` entry. In the sample, 32 requirement pages and 32 files coincide, so grouping by page and grouping by file produce the same grouping. At any other size, only the feature entry decides the grouping. The record gives high confidence for this verdict.

The builder places all 74 decisions in one Intent list. Of them, 71 are technical decisions that belong under Implementation. A reader therefore meets implementation choices inside a layer that contains no mechanism. The record gives high confidence for this verdict.

The builder splits Term across Intent and Behavior by its `layer` field. That field states which clock the word follows, not where the entry stands. The glossary is one list, and the split places it in 2 homes. Flow stands correctly as a top-level list, because its `goal` is a sentence, not an identifier. Nesting flows under goals needs a model change before the builder chooses it. The record gives high confidence for these verdicts.

Most Behavior placements are correct. Aggregates stand under modules by identifier prefix. The builder groups scenarios under their result through the scenario's own `result` field. Screens nest under the entry point that both their field and their identifier name. The builder correctly ignores `host` for placement. The record gives high confidence for this verdict.

The outline has 4 placements that are wrong or fragile. The builder files rules under the first aggregate they govern, not under their identifier home. A cross-aggregate rule therefore stands under an aggregate it only governs. The record notes that `governs` is trace, not parentage. The builder collects domain events onto aggregates through use-case `raises` lists, not through the aggregate their identifier names. The builder therefore misplaces a domain event that no use case raises. The record gives high confidence for these verdicts.

The builder attaches results and scenarios through shared files. A scenario on a reaction page has no outlined parent, although the specification allows it there. Reactions, jobs, notifications, and settings name a module only when their source path has a module segment. The stray sweep makes any other record a Behavior root. The sweep appends every unplaced record, whatever its type, and it promotes records whose module lies outside the sample to roots beside the modules. The record gives high confidence for these verdicts.

The builder gives children only to the sampled modules. The record calls this sampling, not outline logic, but a reader sees no difference. Use cases need a precedence that the specification never gives: the aggregate where the model assigns one, the module otherwise, and never a file path. The record gives high confidence for this verdict.

Roles, permissions, and entry points stand correctly as top-level lists. Nesting permissions under roles repeats every shared permission. Only 4 of 23 actors record any `holds`, so an outline that uses `holds` orphans most actors. The record gives high confidence for this verdict.

Governance has the correct shape and an incorrect label. The builder lists waivers separately with the correct entries. The builder emits that list as a third layer beside Intent and Behavior. The provision places governance beside all 4 layers, and subject links attach it without nesting. The record gives high confidence for this verdict.

Implementation and Proof have no place in the outline, which the record calls the largest single defect. The builder constructs both outlines and then deletes every code, route, test, and finding record before it writes the book. The totals still count entries that open for no reader. The deleted groupings use invented nodes such as `architecture-layer`, `folder`, `test-project`, and `finding-code`, and none is a permitted label. The record gives high confidence for this verdict.

LiteSpec defines both parents in its code marks. Code stands under the record it implements, tests stand under the lowest record they prove, and findings stand under their subject. Source links on each entry cover file locations. The record gives high confidence for this claim.

The outline has 5 gaps that belong to LiteSpec, not to the builder. Feature needs a primary-parent rule. Requirement needs a stated tree home in the feature that contains it as an entry, and it keeps links as trace. Flow needs a goal identifier field. Rule needs its identifier home resolved as a typed reference. Governance needs one statement of its attach-by-link rule. The record gives no confidence for these gaps.

A sixth and smaller gap is that a platform-wide setting carries a module-shaped identifier. LiteSpec either names the module value that setting carries or admits a platform group. The record gives no confidence for this gap.

The investigation proposes 11 outline rules. They define 4 layer roots with governance beside them, and they place each record once: by identifier, then by required link, then by type list. The rules order Intent with requirements under their feature, and they split decisions by home. Features nest by a stated precedence, and extra links count as trace.

The other proposed rules order Behavior by module, access, published set, and operations. They place rules and domain events by identifier home and state the records outside the book in one note. They keep one undivided glossary and place Implementation and Proof under the records their marks name. They link governance without nesting it. A record that the builder leaves unplaced becomes a build finding, and no stray sweep remains. The record states no confidence for these rules.

## Source Notes

The record names 2 repository sources read on 30 October 2026: the core model page (`docs/core/model.md`) and the book tool page (`docs/tools/book.md`). It also read the accepted mockup builder at `.agents/mockups/book-v6/build-data.js` on the same day. That folder is untracked working material, so it is not in the sources list. The record does not say which claim each source supports.

The sample counts come from the mockup's `data.json`. The sample contains 1,381 of 21,548 records, with 1,988 more outside stubs. The record measured placement in the sample only, so it states nothing about the full outline.

The record reports that the builder disagrees with the provisions, and it treats the provisions as the standard. It names no disagreement between the 2 sources.
