# Record Layers

**Stack:** any

**Concern:** records

## Intent

LiteSpec records form four record layers: Intent states the promises, Behavior states how the system keeps them, Implementation builds that behavior in code, and Proof shows that it holds. This page states the translation rules between the record layers: Specify, Implement, and Verify. Agents apply it at every record-layer boundary, and readers use it to judge whether each record layer says only what belongs to it.

## Concepts

### The Four Record Layers

LiteSpec work moves through four record layers. Each record layer answers one question, and each has its own writers and judges.

| Record layer | Question | Writes | Judged by |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | Agents draft | The owner, in the pull request |
| Behavior | How does the system behave to keep the promises? | Agents | The pull request, with G checks |
| Implementation | How is it built? | Agents | S checks, where the reader is qualified |
| Proof | How do we know it holds? | The builder | G and S checks |

The [intent page](intent.md) states the Intent records, and the [behavior page](behavior.md) states the Behavior records. Implementation holds code and technical decisions, and Proof holds tests, check results, computed completion, and the rendered book. Governance is a group of records beside the four record layers, not a fifth layer: the departure record covers any record layer.

### Translation Rules

Three rules join the record layers. Specify translates Intent into Behavior: it adds the behavior detail the promises leave unstated. Implement translates Behavior into Implementation: it adds the code shapes the stack pages fix. Verify translates Implementation into Proof: one test proves each scenario through the public boundary.

### The Specification Is Closed

The specification is closed: an agent builds what the specification states and nothing more. A need a lower record layer meets that the upper record layer leaves unstated becomes a question to the owner or a backlog item, never code.

### World Facts Enter as Cited Records

A fact learned from the world enters Intent as research or a constraint that cites its source and date. The citation shows where the fact came from and the date shows when it held, so a later change rechecks the source instead of the memory.

## Standards

### Write Each Layer from the Layer Above It

**ID:** `litespec/core/layers#write-each-layer-from-the-layer-above-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each record below Intent MUST be written from a record in the record layer above it.

**Rationale:** Each record layer rests on the one above it: Behavior fulfills Intent, Implementation builds Behavior, and Proof proves Implementation. Derivation between records is part of that direction, so generic refusals derive from `access` and `input` without new records. An agent either writes the missing record above such code or removes the code, so the specification wins where Intent has spoken.

**Example:** An agent that finds a guard in code with no record above it writes the requirement or rule, or removes the guard.

**Checked by:** `litespec model check` emits no `behavior-unrecorded` finding for a refusal literal or a screen call to no endpoint, and reports `mark-missing` for a code element that carries no mark.

### Add Behavior Detail When Specifying Intent

**ID:** `litespec/core/layers#add-behavior-detail-when-specifying-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Behavior that fulfills an Intent record MUST add the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access the Intent record leaves unstated.

**Rationale:** Intent promises outcomes in product words, and Behavior turns each promise into the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that keep it. Only Behavior records fulfill a requirement (`litespec/core/intent#restrict-requirement-fulfillment-to-behavior`). Without that detail each implementer guesses the states and the access scope, and two guesses diverge. The checks `requirement-fulfillment-missing`, `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, and `setting-requirement-missing` report the gaps.

**Example:** A reimbursement promise gains an entity with Approved and Reimbursed states, a refusal for an expense that is already reimbursed, and atomic consistency over one aggregate.

**Checked by:** `litespec model check` emits no `requirement-fulfillment-missing`, `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, or `setting-requirement-missing` finding across Behavior records.

### Send Commercial and Observable Figures to the Owner

**ID:** `litespec/core/layers#send-commercial-and-observable-figures-to-the-owner`

**Strength:** required

**Applies to:** artifact: record:setting, record:rule

**Statement:** Behavior that meets a figure the owner sets MUST send it to the owner instead of fixing it.

**Rationale:** The [model page](model.md) holds the one list of figures the owner sets: prices, fees, rates, terms, markets, payers, retention periods, freshness, and figures that differ by tenant, market, or date. Each figure is a business choice, and only an accountable person makes it. An agent that fixed one would bury a commitment in a Behavior record nobody chose. The figure becomes a question to the owner or a backlog item, never a silent default.

**Example:** A receipt threshold of EUR 25 waits for the owner instead of entering the setting silently.

**Reviewed for:** Which figures the owner sets did Behavior fix without asking? Look at the settings and rules the change adds, beside the list of owner-set figures on the model page.

### Implement Only Declared Behavior

**ID:** `litespec/core/layers#implement-only-declared-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job; layer: implementation; trace: yes

**Statement:** Implementation MUST add only the code shapes its stack pages fix and no observable outcome, edge, write, or access grant its Behavior records leave undeclared.

**Rationale:** The stack pages fix the code shapes, so the handler, the repository call, and the published event each have one prescribed form. An observable outcome, edge, write, or access grant outside the records splits the project into a specification that says one thing and code that does another. The checks `behavior-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, and `refusal-unimplemented` report the excess, and analyzer `LS2012` reports a handler that authorizes itself.

**Example:** A handler that returns a code no result declares fails `result-undeclared`; the fix declares the result or removes the code.

**Checked by:** `litespec model check` emits no `behavior-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, or `refusal-unimplemented` finding across the implementation.

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/core/layers#prove-each-scenario-through-the-public-boundary`

**Strength:** required

**Applies to:** artifact: record:scenario; layer: proof; trace: yes

**Statement:** At large size, or with the `bdd` extension, each scenario MUST be proved by one test through the public boundary.

**Rationale:** Small projects carry no scenarios unless the `bdd` extension switches them on, so the rule binds large projects and small projects with the extension. A scenario is the leaf of the record tree, so one test through the public boundary ties each leaf to running behavior. The checks `use-case-scenario-missing` and `proves-unreached` report the missing proof.

**Example:** A large project carries a scenario for each refusal, and one test proves it through the public boundary.

**Checked by:** `litespec model check` emits no `use-case-scenario-missing` or `proves-unreached` finding across scenarios and tests.

### Raise Open Needs as Owner Questions or Backlog Items

**ID:** `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A need a lower record layer meets that the upper record layer leaves unstated MUST travel up as a question to the owner or a backlog item, never as code.

**Rationale:** The lower record layer does not decide what the upper record layer left unstated, so the need travels up with its context instead of turning into a guess. A question to the owner settles what only an accountable person decides, and a backlog item holds the rest where it stays visible. The specification is closed: an agent builds what it states and nothing more.

**Example:** A missing reimbursement policy becomes a question to the owner, not a rule the agent writes.

**Reviewed for:** Which unstated needs lack a question to the owner or a backlog item? Look at the added records from a lower record layer, beside the owner questions and the backlog.

### Enter World Facts as Cited Research or Constraints

**ID:** `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact learned from the world MUST enter Intent as research, or as a constraint whose `sources` cite its source with the date it was read.

**Rationale:** Commitments flow down, and facts from the world flow up: a provider's behavior, a regulation, or an incident enters Intent where people can weigh it. The citation and the `readOn` date show where the fact came from and when someone read it, so a later change rechecks the source instead of the memory. A constraint names the research it rests on in `basedOn`, as [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule) and the constraint fields state. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Example:** An observed two-day payout settlement window enters as research, then as a constraint citing that research.

**Reviewed for:** Which world facts entered without a cited and dated research or constraint record? Look at the provider, regulation, and incident facts the change relies on.

### Record Changed Provider Behavior as Observed Research

**ID:** `litespec/core/layers#record-changed-provider-behavior-as-observed-research`

**Strength:** required

**Applies to:** artifact: record:research, record:constraint

**Statement:** A changed provider behavior MUST enter as research recording the observed behavior, then as a constraint citing that research.

**Rationale:** The provider's behavior is a fact from the world, and research is where such facts enter. The constraint that follows names the research in `basedOn`, so the constraint carries its evidence. Code is not the source: a failed reimbursement in production points at the world, and the world is cited.

**Example:** A payout provider that starts settling every two days instead of daily enters as research with the observed dates, and a settlement-window constraint cites that research.

**Reviewed for:** Which provider changes lack observed research and a citing constraint? Look at the provider facts the change relies on, beside the research records.

### Build Ahead of a Regulation Only Under Its Constraint

**ID:** `litespec/core/layers#build-ahead-of-a-regulation-only-under-its-constraint`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Behavior that anticipates a regulation with a deadline MUST follow that regulation's constraint.

**Rationale:** A regulation with a deadline needs working behavior before its requirement exists, so the constraint carries the rule in the meantime, with its statutory date in `effectiveFrom`. The release stays shippable until the statutory date, and only then does the missing behavior block it. Without this path teams either ship late or build the rule where no record owns it.

**Example:** A privacy regulation that takes effect next year enters as a constraint with its statutory date, and the anticipatory behavior follows that constraint until the requirement exists.

**Reviewed for:** Which anticipatory behavior lacks a citing constraint, or blocks a release before the statutory date? Look at the regulation-driven Behavior records beside the constraint and its date.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`, `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`, and `litespec/core/layers#record-changed-provider-behavior-as-observed-research`.

Top-down: the owner sets EUR 50 instead of EUR 25 for the receipt threshold. The requirement's statement changes, and an agent updates the policy setting default and the scenario where an expense without a receipt is refused in the same change. The test changes, Proof goes green, and the book chapter shows the new promise.

Bottom-up: a late reimbursement in production shows the payout bank settles batches every two days instead of daily. An agent records the observed behavior as research, then changes the settlement-window constraint citing it. The per-batch deadline rule and its scenarios follow in the same change. The test changes, Proof goes green, and the book chapter shows the cited source and date.
