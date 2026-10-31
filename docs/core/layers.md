# Record Layers

## Intent

LiteSpec records form four record layers: Intent states the promises, Behavior states how the system keeps them, Implementation builds that behavior in code, and Proof shows that it holds. This page states the translation rules between the record layers: Specify, Implement, and Verify. Agents apply it at every record-layer boundary, and reviewers use it to judge whether each record layer says only what belongs to it.

## Concepts

### The Four Record Layers

LiteSpec work moves through four record layers. Each record layer answers one question, and each has its own writers and reviewers.

| Record layer | Question | Writes | Reviews |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | Agents draft, the decider decides | The decider and product people |
| Behavior | How does the system behave to keep the promises? | Agents | Developers; the security reviewer for access, outside calls, and personal-data flows |
| Implementation | How is it built? | Agents | Check level S; developers sample |
| Proof | How do we know it holds? | The builder | Check levels G and S |

The [intent page](intent.md) states the Intent records, and the [behavior page](behavior.md) states the Behavior records. Implementation holds code only, and Proof holds tests, check results, computed statuses, and the rendered book. Governance records, `deviation` and `known-gap`, attach to any record layer.

### Translation Rules

Three rules join the record layers. Specify translates Intent into Behavior: it adds the behavior detail the promises leave unstated. Implement translates Behavior into Implementation: it adds the code shapes the stack pages fix. Verify translates Implementation into Proof: one test proves each scenario through the public boundary.

### Questions and World Facts

A need a lower record layer meets that the upper record layer leaves unstated travels up as a draft question. The question carries the question itself, a decider, a decide-by date, a recommendation, and a ballot group. The [model page](model.md) states the one question lifecycle (`litespec/core/model#answer-each-question-on-its-record`). A fact learned from the world enters Intent as research or a constraint that cites its source and date. A change at the top marks every record whose parent changed, agents update those records, and review and proof follow.

## Standards

### Write Each Layer from the Layer Above It

**ID:** `litespec/core/layers#write-each-layer-from-the-layer-above-it`

**Statement:** Each record below Intent MUST be written from a record in the record layer above it.

**Rationale:** Each record layer rests on the one above it: Behavior fulfills Intent, Implementation builds Behavior, and Proof proves Implementation. Derivation between records is part of that direction, so generic refusals derive from `access` and `input` without new records. Code with no parent record stays evidence for a question instead of becoming a record by silent transcription, so the spec wins where Intent has spoken.

**Example:** A guard in code with no parent record raises a question; the agent drafts the requirement or rule and keeps the code.

**Checked by:** `litespec model check` emits no `behavior-unrecorded` finding for a refusal literal, a screen call to no endpoint, or an engine step mapping to no use case, and reports `mark-missing` for a code element that carries no mark.

### Add Behavior Detail When Specifying Intent

**ID:** `litespec/core/layers#add-behavior-detail-when-specifying-intent`

**Statement:** Behavior that fulfills an Intent record MUST add the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access the Intent record leaves unstated.

**Rationale:** Intent promises outcomes in product words, and Behavior turns each promise into the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that keep it. Only Behavior records fulfill a requirement (`litespec/core/intent#restrict-requirement-fulfillment-to-behavior`). Without that detail each implementer guesses the states and the access scope, and two guesses diverge. The checks `requirement-fulfillment-missing`, `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, and `setting-requirement-missing` report the gaps.

**Example:** A reimbursement promise gains an entity with Approved and Reimbursed states, a refusal for an expense that is already reimbursed, and atomic consistency over one aggregate.

**Checked by:** `litespec model check` emits no `requirement-fulfillment-missing`, `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, or `setting-requirement-missing` finding across Behavior records.

### Raise Commercial and Observable Figures as Questions

**ID:** `litespec/core/layers#raise-commercial-and-observable-figures-as-questions`

**Statement:** An agent that meets a figure the decider sets MUST raise it as a draft question instead of deciding it.

**Rationale:** The [model page](model.md) holds the one list of figures the decider sets. Each figure is a business choice, and only an accountable person makes it. An agent that decided one would bury a commitment in a Behavior record nobody approved. Each open figure becomes a draft question with a decider and a decide-by date.

**Example:** A receipt threshold of EUR 25 goes to the ballot as a draft question with a recommendation instead of entering the setting silently.

**Reviewed for:** Which figures the decider sets did Behavior decide without a draft question? Look at the settings and rules the change adds, beside the question list and the list of decider figures on the model page.

### Implement Only Declared Behavior

**ID:** `litespec/core/layers#implement-only-declared-behavior`

**Statement:** Implementation MUST add only the code shapes its stack pages fix and no observable outcome, edge, write, or access grant its Behavior records leave undeclared.

**Rationale:** The stack pages fix the code shapes, so the handler, the repository call, and the published event each have one prescribed form. An observable outcome, edge, write, or access grant outside the records splits the project into approved fiction and unreviewed fact. The checks `behavior-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, and `refusal-unimplemented` report the excess, and analyzer `LS2012` reports a handler that authorizes itself.

**Example:** A handler that returns a code no result declares fails `result-undeclared`; the fix declares the result or removes the code.

**Checked by:** `litespec model check` emits no `behavior-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, or `refusal-unimplemented` finding across the implementation.

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/core/layers#prove-each-scenario-through-the-public-boundary`

**Statement:** Each scenario MUST be proved by one test through the public boundary.

**Rationale:** A scenario is the leaf of the record tree, so one test through the public boundary ties each leaf to running behavior. The checks `use-case-scenario-missing` and `proves-unreached` report the missing proof. A proof goes stale when its scenario's semantic hash changes, while rewording prose leaves the proof green.

**Checked by:** `litespec model check` emits no `use-case-scenario-missing` or `proves-unreached` finding across scenarios and tests.

### Raise Open Needs as Draft Questions

**ID:** `litespec/core/layers#raise-open-needs-as-draft-questions`

**Statement:** A need a lower record layer meets that the upper record layer leaves unstated MUST travel up as a draft question carrying a decider and a decide-by date.

**Rationale:** The lower record layer does not decide what the upper record layer left unstated, so the need travels up with its context instead of turning into a guess. The question carries a recommendation and a ballot group. The [model page](model.md) states the one question lifecycle: which questions auto-approve and how an aged question is reported (`litespec/core/model#answer-each-question-on-its-record`).

**Example:** A missing reimbursement policy becomes a ballot item due 2026-11-01 with a named decider.

**Reviewed for:** Which unstated needs lack a draft question with a decider and a decide-by date? Look at the added records from a lower record layer, beside the ballot inbox.

### Enter World Facts as Cited Research or Constraints

**ID:** `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`

**Statement:** A fact learned from the world MUST enter Intent as research or a constraint that cites its source and date.

**Rationale:** Commitments flow down, and facts from the world flow up: a provider's behavior, a regulation, or an incident enters Intent where people can weigh it. The citation and date show where the fact came from and when it held, so a later change rechecks the source instead of the memory. Nothing travels up from code.

**Example:** An observed two-day payout settlement window enters as research, then as a constraint draft citing that research.

**Reviewed for:** Which world facts entered without a cited and dated research or constraint record? Look at the provider, regulation, and incident facts the change relies on.

### Flag Records When a Parent Record Changes

**ID:** `litespec/core/layers#flag-records-when-a-parent-record-changes`

**Statement:** Each record whose parent record changes MUST be flagged for update, and its review and proof stay stale until an agent updates it.

**Rationale:** A change at the top reaches every record that fulfills or serves the changed parent, so the builder flags each one as parent-reopened. Agents update the flagged records, and review and proof follow the update. Without the flag a Behavior record keeps proving a promise nobody makes anymore.

**Reviewed for:** Which records with a changed parent still show a passing review and proof? Look at the flagged records beside their updated parents.

### Close Incident Deviations Within Seven Days

**ID:** `litespec/core/layers#close-incident-deviations-within-seven-days`

**Statement:** A production incident fix that ships before its question is answered MUST carry a time-boxed incident deviation that becomes a draft question within seven days.

**Rationale:** A production incident ships its fix before any question is answered, at 02:00 if that is when the page fires. The time-boxed incident deviation records what shipped and why, and turning it into a draft question within seven days returns the choice to an accountable person. The release gate counts aged incident deviations, so a forgotten deviation stays visible.

**Reviewed for:** Which incident fixes lack a deviation, or carry one older than seven days without a draft question? Look in the deviations records beside the release gate.

### Record Changed Provider Behavior as Observed Research

**ID:** `litespec/core/layers#record-changed-provider-behavior-as-observed-research`

**Statement:** A changed provider behavior MUST enter as research recording the observed behavior, then as a constraint draft citing that research.

**Rationale:** The provider's behavior is a fact from the world, and research is where such facts enter. The constraint draft that follows cites the research, so the ballot item carries its evidence. Code is not the source: a failed reimbursement in production points at the world, and the world is cited.

**Reviewed for:** Which provider changes lack observed research and a citing constraint draft? Look at the provider facts the change relies on, beside the research records.

### Build Ahead of a Regulation Only Under Its Constraint

**ID:** `litespec/core/layers#build-ahead-of-a-regulation-only-under-its-constraint`

**Statement:** Behavior that anticipates a regulation with a deadline MUST follow that regulation's constraint while its requirement stays a draft, blocking the release only after the statutory date.

**Rationale:** A regulation with a deadline needs working behavior before its requirement passes review, so the constraint carries the rule in the meantime. The release stays shippable until the statutory date, and only then does the missing approval block it. Without this path teams either ship late or build the rule where no record owns it.

**Reviewed for:** Which anticipatory behavior lacks a citing constraint, or blocks a release before the statutory date? Look at the regulation-driven Behavior records beside the constraint and its date.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/layers#flag-records-when-a-parent-record-changes`, `litespec/core/layers#raise-open-needs-as-draft-questions`, `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`, and `litespec/core/layers#record-changed-provider-behavior-as-observed-research`.

Top-down: the decider answers the ballot with EUR 50 instead of EUR 25 for the receipt threshold. The requirement's statement changes, the builder marks the policy setting and the receipt rule as parent-reopened, and an agent updates the setting default and the scenario where an expense without a receipt is refused. The developer reviews one two-line Behavior diff, the test changes, Proof goes green, and the book chapter shows the new promise.

Bottom-up: a late reimbursement in production shows the payout bank settles batches every two days instead of daily. An agent records the observed behavior as research, then drafts a change to the settlement-window constraint citing it. The decider sees one ballot item on a tier 1 record, and on approval the per-batch deadline rule and its scenarios follow.
