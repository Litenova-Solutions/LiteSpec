# Check Rule Citations Across Sibling Commands and Scenarios

A command can skip the rules its sibling commands obey, and a scenario can exercise a rule its use case never applies. This decision reports the first gap as blocking where most writers already cite the rule, and the second as advisory.

## Context

A consumer review found six commands posting entry lines. Five cited the entry rules for private money, VAT, business share, proof of costs, and stored VAT. The sixth cited none of them, so one form posted without the checks the others applied. The model check saw no gap: each rule's `governs` matched the links that named it, and nothing compared the sixth command with its siblings.

The same review found scenarios exercising rules their use cases never apply, such as a payment scenario exercising a no-reversal rule. Each entry inflates the coverage report and sends a test author looking for behavior the scenario never shows. Nothing compared an `exercises` entry with the use case holding its scenario.

## Options

LiteSpec compared 3 options for the two gaps. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Report a command that misses an invariant most writers of its aggregate cite, blocking; report a scenario exercising a rule its use case applies nowhere, advisory | The blocking finding fires only where the omission is almost surely a gap: an invariant holds on every change, and most writers already cite it. The advisory finding keeps scenarios honest without blocking on judgment calls. | The sibling finding stays silent while only a minority cites the rule, so two missing commands out of four report nothing. |
| Report every missing citation as advisory | No new blocking finding; every gap is a suggestion the author may set aside. | The entry-form gap ships exactly as specified, since an advisory finding never stops a run. |
| Report a command missing any rule any sibling cites | The simplest rule: one citing sibling is enough to report every command that misses the rule. | Commands that cannot break the rule report anyway, such as a submit command missing a reimburse-once invariant, so authors waive the finding instead of reading it. |

## Decision

A command cites a rule in its steps' cites, its input rules, its consistency `boundBy`, or a result carrying the rule's refusal code. A scenario's `exercises` claims the rule without applying it, so `exercises` never counts as a citation.

The sibling finding covers invariants only, since any other kind constrains single commands, while an invariant holds on every change. It fires only when fewer writers miss the rule than cite it, so a lone citing command never reports its siblings.

The scenario finding reports an `exercises` entry naming a rule the use case applies nowhere. It stays advisory, since relatedness is a judgment call the author answers.

## Consequences

The new provisions are `litespec/core/use-cases#cite-the-invariants-most-writers-cite` and the extended `litespec/core/use-cases#exercise-rules-from-scenarios`, checked by `rule-sibling-uncited` at level G and `exercises-uncited` at level A.

The scenario code reads `exercises-uncited` rather than `exercises-unrelated`, since unrelated is no word of the finding-code grammar: the entry names a rule the use case does not cite back.

## Revisit Conditions

This decision reopens when authors report true gaps the minority gate misses, or when a rule kind beyond invariants needs the same coverage.

Either case means the gate no longer separates omissions from commands that cannot break the rule. The team then decides again where the finding fires and at which level.
