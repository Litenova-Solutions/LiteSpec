# Record Layers

**Stack:** any
**Concern:** records

This page states how the four record layers divide the specification and how each layer builds on the one above it. It is for agents who write records at layer boundaries and for reviewers who judge whether a record says what belongs to its layer. A record written from the wrong layer splits one promise into two guesses.

## Concepts

### The Four Record Layers

A record layer is one group of records answering one question about the product. Intent asks why the product exists and what it promises. Behavior asks how the system acts to keep those promises. Implementation is the code that builds that behavior, with its technical decisions. Proof is the evidence that the behavior holds: tests, check results, computed completion, and the rendered book.

Each layer has its own writers and judges:

| Record layer | Question | Written by | Judged by |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | Agents draft | The owner, in the pull request |
| Behavior | How does the system behave to keep the promises? | Agents | The pull request, with G checks |
| Implementation | How is it built? | Agents | S checks, where the reader is qualified |
| Proof | How do we know it holds? | The builder | G and S checks |

The [Intent page](intent.md) states the Intent records, and the [Behavior page](behavior.md) states the Behavior records. Governance sits beside the four layers. It is not a fifth layer, and the waiver record covers any layer.

Claimwick promises that an expense without a receipt is refused above EUR 25. That promise lives in Intent. Behavior adds the refusal, the states, and the access scope. Implementation builds the handler, and Proof shows the scenario passing.

### Translation Rules

Three rules join the layers, one per boundary. Specify turns an Intent promise into Behavior by adding the behavior detail the promise implies but leaves unstated. Implement turns Behavior into code using the code shapes the stack pages fix. Verify proves Behavior against the built code: one test proves each scenario through the public boundary. Each rule adds what its layer owns. Specify adds no new promise, and Implement adds no new behavior.

A detail counts as implied where every faithful fulfillment of the promise needs it and where no actor meets it as an outcome, limit, or figure the promise leaves unnamed. Specify adds implied detail, and a need the upper layer states without implying, or leaves out entirely, goes up as a question to the owner or an item on the backlog board instead of entering the lower layer as a guess.

The Claimwick requirement "An expense without a receipt is refused above EUR 25" implies the Submitted and Approved states, the `spending.receipt-missing` refusal, and atomic consistency on the expense. A refusal for an expense with a future date is not implied, so it goes up as a question to the owner. Had the requirement left the EUR 25 figure unnamed, that figure goes to the owner too. Implementing the specified behavior writes the handler in the stack shape. Verifying it runs a test that submits an expense without a receipt through the endpoint and checks for the refusal.

### The Specification Is Closed

A closed specification means agents build what the records state, nothing more. Where code needs something the records leave out, agents ask instead of inventing. It becomes a question to the owner, who decides product choices, or an item on the backlog board outside the repository, where it stays visible until specified. It turns into code only after that.

Claimwick code that needs a receipt threshold missing from the records leaves EUR 25 out of the code. It raises an owner question asking for the figure, and waits.

### World Facts Enter as Cited Records

A world fact is something the team learns outside the repository: how a provider behaves, what a regulation says, or what an incident showed. Such facts enter Intent as research, which records what was observed, or as constraints, which state the limit the product lives with. Each entry cites its source and the date it was read, so a later change rechecks the source instead of trusting memory.

Claimwick learns that its payout bank settles batches every two days instead of daily. That observation enters as research with the dates it was seen, and a settlement-window constraint cites that research. Research and constraints carry facts about the world. They leave out guesses about what the world does next, and code serves as their source for nothing.

A regulation with a deadline needs working behavior before its requirement exists. The constraint carries the rule in the meantime, with its statutory date in `effectiveFrom`. The release stays shippable until the statutory date, and only then does the missing behavior block it.

## Standards

### Write Each Layer from the Layer Above It

**ID:** `litespec/core/layers#write-each-layer-from-the-layer-above-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each record below Intent MUST be written from a record in the record layer above it.

**Rationale:** Without a parent above it, a lower record carries a promise from above or invents a new one, and reviewers tell the two apart poorly. Implementers then build from guesses, and two guesses about the same gap diverge. Writing each record from the layer above keeps one chain from promise to proof, so a gap shows as a missing parent instead of silent invention.

**Example:** Claimwick's submit-expense handler carries a positivity guard with no rule above it. The agent writes the missing rule or reports the gap, and the guard stays until the record is written. Behavior with no parent is recorded or reported, not deleted for lacking one. A test with no record above it is a finding the team resolves by writing the missing record or reporting the gap. It is deleted only where a reviewer establishes that the behavior it protects is obsolete, redundant or wrong, and the pull request says so.

**Checked by:** `litespec model check` emits no `behavior-unrecorded` finding for a refusal literal or a screen that calls no endpoint, and reports `mark-missing` for a code element that carries no mark.

### Add Behavior Detail When Specifying Intent

**ID:** `litespec/core/layers#add-behavior-detail-when-specifying-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Behavior that fulfills an Intent record MUST add the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that the Intent record implies but leaves unstated.

**Rationale:** An Intent promise says what the product owes in product words, without saying how the system keeps it. Behavior turns each promise into the states, invariants, refusals, consistency, erasure positions, freshness, and tenant-scoped access that keep it. A detail counts as implied where every faithful fulfillment of the promise needs it and where no actor meets it as an outcome, limit, or figure the promise leaves unnamed. Without that detail, each implementer fills the gap with a guess, and two guesses about the same promise diverge. Settled by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule), world facts enter Intent as cited research or constraints before Specify adds detail.

**Example:** Claimwick's reimbursement promise gains an entity with Approved and Reimbursed states, a refusal for an expense that is already reimbursed, and atomic consistency over one aggregate.

**Checked by:** `litespec model check` emits no `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, or `setting-requirement-missing` finding across Behavior records.

### Implement Only Declared Behavior

**ID:** `litespec/core/layers#implement-only-declared-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job, record:read-model; layer: implementation; trace: yes

**Statement:** Implementation MUST add only the code shapes its stack pages fix and no observable outcome, edge, write, or access grant its Behavior records leave undeclared.

**Rationale:** Stack pages fix the code shapes: the handler, the repository call, and the published domain event each have one prescribed form. An extra outcome, edge, write, or access grant splits the project into a specification saying one thing and code doing another. A record sentence describing a possible future specifies nothing: code builds no interface, provider list, or hook for a feature no record specifies.

**Example:** Claimwick's submit-expense handler returns a code no result declares. The run fails `result-undeclared`, and the fix declares the result or removes the code. A record sentence saying a country pack adds a national audit file specifies no feature: the code builds no audit file provider for it, and the national audit file arrives as its own specified feature.

**Checked by:** `litespec model check` emits no `behavior-unrecorded`, `use-case-unrecorded`, `reaction-unrecorded`, `job-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, or `refusal-unimplemented` finding across the implementation.

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/core/layers#prove-each-scenario-through-the-public-boundary`

**Strength:** required

**Applies to:** artifact: record:scenario; layer: proof; trace: yes

**Statement:** At large size or with the `bdd` extension, each scenario MUST be proved by one test through the public boundary.

**Rationale:** Small projects carry no scenarios unless the `bdd` extension enables them, so the rule binds large projects and small projects with the extension. A scenario is the leaf of the record tree, so one test through the public boundary joins each leaf to running behavior.

**Example:** Claimwick, a large project, carries a scenario for its missing-receipt refusal, and one test proves it by submitting an expense without a receipt through the endpoint.

**Checked by:** `litespec model check` emits no `use-case-scenario-missing` or `proves-unreached` finding across scenarios and tests.

## Conventions

### Raise Open Needs as Owner Questions or Backlog Items

**ID:** `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`

**Strength:** required

**Applies to:** artifact: any

**Default:** Where the upper record layer neither states nor implies a need, such as a refusal, eligibility condition, limit, or a figure the owner sets, travel up as a question to the owner with a recommendation or as a backlog item, never as a record or code.

**Replacement:** A consumer records an open-needs convention naming where such needs go.

**Rationale:** The lower layer decides nothing the upper layer leaves unstated and unexpressed, so the need travels up with its context instead of turning into a guess. A question carries product choices to the person answering for them. A backlog item keeps the rest where it stays visible until specified. The specification is closed: an agent builds what it states and nothing more.

A figure the owner sets is one such need: the [Project Model](model.md) carries the one list of owner-set figures, and each one binds the product to buyers, markets, or law, so an accountable person decides it. A figure the business changes while the system runs is aggregate data, not a setting: its range is a rule with a refusal, and its change is a use case with access and audit. A setting is operator configuration the running system reads.

A question carries a recommendation with each figure: a figure without one asks the owner to research from nothing, while a recommendation gives a default to accept or correct. Grouping figures by subject lets the owner answer one set at a time.

**Example:** Claimwick meets a need for a refusal of expenses with a future date, which the receipt promise leaves unstated and unexpressed. The need becomes a question to the owner instead of a rule an agent writes. A receipt threshold the promise leaves unnamed travels up as a question with a recommended figure the same way.

**Reviewed for:** Does every need the upper layer neither states nor implies have a question to the owner with a recommendation or a backlog item? Check the added records from a lower record layer beside the owner questions and the backlog.

## Reference Example

This informative example demonstrates `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`. It follows one owner decision from the top down to passing proof.

Top-down: the owner sets EUR 50 instead of EUR 25 for the receipt threshold. The requirement statement changes, and in the same change an agent updates the threshold on the policy aggregate and the scenario where an expense without a receipt is refused. The test changes, Proof shows passing, and the book chapter shows the new promise.
