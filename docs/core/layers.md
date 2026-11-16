# Record Layers

**Stack:** any

**Concern:** records

A project promises outcomes in Intent, keeps them with system behavior in Behavior, builds that behavior in code in Implementation, and shows evidence in Proof. Specify turns an Intent promise into Behavior, Implement turns Behavior into code, and Verify proves Behavior against the built code. Agents apply these rules at every record-layer boundary, and readers use them to judge whether each record layer says only what belongs to it.

## Concepts

### The Four Record Layers

A record layer is one group of records that answers one question about the product. Intent asks why the product exists and what it promises. Behavior asks how the system acts to keep those promises. Implementation is the code that builds that behavior, with its technical decisions. Proof is the evidence that the behavior holds: tests, check results, computed completion, and the rendered book.

Each layer has its own writers and judges:

| Record layer | Question | Written by | Judged by |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | Agents draft | The owner, in the pull request |
| Behavior | How does the system behave to keep the promises? | Agents | The pull request, with G checks |
| Implementation | How is it built? | Agents | S checks, where the reader is qualified |
| Proof | How do we know it holds? | The builder | G and S checks |

The [Intent page](intent.md) states the Intent records, and the [Behavior page](behavior.md) states the Behavior records.

Governance sits beside the four layers; it is not a fifth layer: the waiver record covers any layer.

For example, Claimwick promises that an expense without a receipt is refused above EUR 25. That promise lives in Intent. Behavior adds the refusal, the states, and the access scope. Implementation builds the handler, and Proof shows the scenario passing.

### Translation Rules

Three rules join the layers, one per boundary. Specify turns an Intent promise into Behavior by adding the behavior detail the promise implies but leaves unstated. Implement turns Behavior into Implementation using the code shapes the stack pages fix. Verify proves Behavior against the built code: one test proves each scenario through the public boundary. Each rule adds only what its layer owns: Specify adds no new promise, and Implement adds no new behavior.

A detail is implied when every faithful fulfillment of the promise needs it and no actor meets it as an outcome, limit, or figure the promise does not name. Specify adds implied detail, and a need the upper layer neither states nor implies goes up as a question to the owner or a backlog item instead of entering the lower layer as a guess.

For example, the Claimwick requirement "An expense without a receipt is refused above EUR 25" implies the Submitted and Approved states, the `spending.receipt-missing` refusal, and atomic consistency on the expense. A refusal for an expense with a future date is not implied, so it goes up as a question to the owner. Had the requirement not named the EUR 25 figure, that figure would go to the owner too. Implementing the specified behavior writes the handler in the stack's shape. Verifying it runs a test that submits an expense without a receipt through the endpoint and checks for the refusal.

### The Specification Is Closed

A closed specification means agents build only what the records state, nothing more. When code needs something the records leave out, agents do not invent it. It becomes a question to the owner, who decides product choices, or an item on the backlog board outside the repository, where it stays visible until it is specified; it never becomes code first.

For example, Claimwick code that needs a receipt threshold it cannot find does not pick EUR 25. It raises an owner question asking for the figure, and waits.

### World Facts Enter as Cited Records

A world fact is something the team learns outside the repository: how a provider behaves, what a regulation says, or what an incident showed. Such facts enter Intent as research, which records what was observed, or as constraints, which state the limit the product lives with. Each entry cites its source and the date it was read, so a later change rechecks the source instead of trusting memory.

For example, Claimwick learns that its payout bank settles batches every two days instead of daily. That observation enters as research with the dates it was seen, and a settlement-window constraint cites that research.

Research and constraints hold facts about the world only. They never hold guesses about what the world might do, and code never serves as their source.

## Standards

### Write Each Layer from the Layer Above It

**ID:** `litespec/core/layers#write-each-layer-from-the-layer-above-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each record below Intent MUST be written from a record in the record layer above it.

**Rationale:** Without a parent above it, a lower record could carry a promise from above or invent a new one, and reviewers cannot tell which. Implementers then build from guesses, and two guesses about the same gap diverge. Writing each record from the layer above keeps one chain from promise to proof, so a gap shows as a missing parent instead of silent invention. Derivation between records follows the same direction: generic refusals derive from `access` and `input` without new records.

**Example:** Claimwick's submit-expense handler holds a positivity guard with no rule above it. The agent writes the missing rule or reports the gap, and the guard stays until the record is written. Behavior with no parent is recorded or reported, never deleted for lacking one. A test with no record above it is a finding the team resolves by writing the missing record or reporting the gap. It is deleted only when a reviewer establishes that the behavior it protects is obsolete, redundant or wrong, and the pull request says so.

**Checked by:** `litespec model check` emits no `behavior-unrecorded` finding for a refusal literal or a screen that calls no endpoint, and reports `mark-missing` for a code element that carries no mark.

### Add Behavior Detail When Specifying Intent

**ID:** `litespec/core/layers#add-behavior-detail-when-specifying-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Behavior that fulfills an Intent record MUST add the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that the Intent record implies but leaves unstated.

**Rationale:** An Intent promise says what the product owes in product words, without saying how the system keeps it. Behavior turns each promise into the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that keep it. A detail counts as implied when every faithful fulfillment of the promise needs it and no actor meets it as an outcome, limit, or figure the promise does not name. Only Behavior records fulfill a requirement, as `litespec/core/intent#restrict-requirement-fulfillment-to-behavior` states. Without that detail, each implementer fills the gap with a guess, and two guesses about the same promise diverge.

**Example:** Claimwick's reimbursement promise gains an entity with Approved and Reimbursed states, a refusal for an expense that is already reimbursed, and atomic consistency over one aggregate.

**Checked by:** `litespec model check` emits no `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, or `setting-requirement-missing` finding across Behavior records.

### Implement Only Declared Behavior

**ID:** `litespec/core/layers#implement-only-declared-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job, record:read-model; layer: implementation; trace: yes

**Statement:** Implementation MUST add only the code shapes its stack pages fix and no observable outcome, edge, write, or access grant its Behavior records leave undeclared.

**Rationale:** Stack pages fix the code shapes: the handler, the repository call, and the published domain event each have one prescribed form. An extra outcome, edge, write, or access grant splits the project into a specification that says one thing and code that does another. A handler that authorizes itself is one such extra grant, which analyzer `LS2012` reports.

**Example:** Claimwick's submit-expense handler returns a code no result declares. The run fails `result-undeclared`, and the fix declares the result or removes the code.

**Checked by:** `litespec model check` emits no `behavior-unrecorded`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, or `refusal-unimplemented` finding across the implementation.

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/core/layers#prove-each-scenario-through-the-public-boundary`

**Strength:** required

**Applies to:** artifact: record:scenario; layer: proof; trace: yes

**Statement:** At large size or with the `bdd` extension, each scenario MUST be proved by one test through the public boundary.

**Rationale:** Small projects carry no scenarios unless the `bdd` extension enables them, so the rule binds large projects and small projects with the extension. A scenario is the leaf of the record tree, so one test through the public boundary connects each leaf to running behavior.

**Example:** Claimwick, a large project, carries a scenario for its missing-receipt refusal, and one test proves it by submitting an expense without a receipt through the endpoint.

**Checked by:** `litespec model check` emits no `use-case-scenario-missing` or `proves-unreached` finding across scenarios and tests.

### Raise Open Needs as Owner Questions or Backlog Items

**ID:** `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A need that the upper record layer neither states nor implies, such as a refusal, eligibility condition, limit, or a figure the owner sets, MUST travel up as a question to the owner with a recommendation or as a backlog item, never as a record or code.

**Rationale:** The lower layer does not decide what the upper layer neither states nor implies, so the need travels up with its context instead of turning into a guess. A question carries product choices to the person who answers for them. A backlog item holds the rest where it stays visible until it is specified. The specification is closed: an agent builds what it states and nothing more.

A figure the owner sets is one such need: the [Project Model](model.md) holds the one list of owner-set figures, and each one binds the product to buyers, markets, or law, so only an accountable person decides it. A figure the business changes while the system runs is aggregate data, not a setting: its range is a rule with a refusal, and its change is a use case with access and audit. A setting is operator configuration the running system reads. An agent that fixed a figure would hide a commitment in a Behavior record nobody chose.

A question carries a recommendation with each figure: a figure without one asks the owner to research from nothing, while a recommendation gives a default to accept or correct. Grouping figures by subject lets the owner answer one set at a time.

**Example:** Claimwick's agent meets a need for a refusal of expenses with a future date, which the receipt promise neither states nor implies, so the need becomes a question to the owner instead of a rule the agent writes. A receipt threshold the promise leaves unnamed travels up as a question with a recommended figure the same way.

**Reviewed for:** Does every need the upper layer neither states nor implies have a question to the owner with a recommendation or a backlog item? Check the added records from a lower record layer beside the owner questions and the backlog.

### Enter World Facts as Cited Research or Constraints

**ID:** `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact learned from the world MUST enter Intent as research, or as a constraint whose `sources` cite its source with the date it was read.

**Rationale:** Commitments flow down, and facts from the world flow up: a provider's behavior, a regulation, or an incident enters Intent where people can weigh it. The citation and the `readOn` date show where the fact came from and when someone read it, so a later change rechecks the source instead of trusting memory. A constraint names the research it rests on in `basedOn`, as the constraint fields state. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Example:** Claimwick's observed two-day payout settlement window enters as research, then as a constraint citing that research.

**Reviewed for:** Did every world fact enter as a cited and dated research or constraint record? Check the provider, regulation, and incident facts the change relies on.

### Build Ahead of a Regulation Only Under Its Constraint

**ID:** `litespec/core/layers#build-ahead-of-a-regulation-only-under-its-constraint`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Behavior that anticipates a regulation with a deadline MUST follow that regulation's constraint.

**Rationale:** A regulation with a deadline needs working behavior before its requirement exists, so the constraint carries the rule in the meantime, with its statutory date in `effectiveFrom`. The release stays shippable until the statutory date, and only then does the missing behavior block it. Without this path, teams either ship late or build the rule where no record owns it.

**Example:** A privacy regulation taking effect next year enters as a Claimwick constraint with its statutory date, and the anticipatory erasure behavior follows that constraint until the requirement exists.

**Reviewed for:** Does every anticipatory behavior cite its constraint without blocking a release before the statutory date? Check the regulation-driven Behavior records against the constraint and its date.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items` and `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`.

Top-down: the owner sets EUR 50 instead of EUR 25 for the receipt threshold. The requirement's statement changes, and in the same change an agent updates the threshold on the policy aggregate and the scenario where an expense without a receipt is refused. The test changes, Proof shows passing, and the book chapter shows the new promise.

Bottom-up: a late reimbursement in production shows the payout bank settles batches every two days instead of daily. An agent records the observed behavior as research, then changes the settlement-window constraint to cite it. In the same change, the per-batch deadline rule and its scenarios change to match. The test changes, Proof shows passing, and the book chapter shows the cited source and date.
