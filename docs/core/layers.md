# Record Layers

**Stack:** any

**Concern:** records

A project promises outcomes in Intent, keeps them with system behavior in Behavior, builds that behavior in code in Implementation, and shows evidence in Proof. Specify turns an Intent promise into Behavior, Implement turns Behavior into code, and Verify turns code into proof. Agents apply these rules at every record-layer boundary, and readers use them to judge whether each record layer says only what belongs to it.

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

Three rules join the layers, one per boundary. Specify turns an Intent promise into Behavior by adding the behavior detail the promise leaves unstated. Implement turns Behavior into Implementation using the code shapes the stack pages fix. Verify turns Implementation into Proof: one test proves each scenario through the public boundary.

Each rule adds only what its layer owns: Specify adds no new promise, and Implement adds no new behavior. When a lower layer needs something the upper layer left out, that need goes up as a question or a backlog item instead of entering the lower layer as a guess.

For example, specifying Claimwick's receipt promise adds the Approved and Reimbursed states with a refusal for a missing receipt. Implementing it writes the handler in the stack's shape. Verifying it runs a test that submits an expense without a receipt through the endpoint and checks for the refusal.

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

**Example:** Claimwick's submit-expense handler holds a positivity guard with no rule above it. The agent writes the missing rule or reports the gap, and the guard stays until the record is written. Behavior with no parent is recorded or reported, never deleted for lacking one.

**Checked by:** `litespec model check` emits no `behavior-unrecorded` finding for a refusal literal or a screen that calls no endpoint, and reports `mark-missing` for a code element that carries no mark.

### Add Behavior Detail When Specifying Intent

**ID:** `litespec/core/layers#add-behavior-detail-when-specifying-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Behavior that fulfills an Intent record MUST add the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access the Intent record leaves unstated.

**Rationale:** An Intent promise says what the product owes in product words, without saying how the system keeps it. Behavior turns each promise into the states, invariants, refusals, consistency, data classes with erasure positions, freshness, and tenant-scoped access that keep it. Only Behavior records fulfill a requirement, as `litespec/core/intent#restrict-requirement-fulfillment-to-behavior` states. Without that detail, each implementer fills the gap with a guess, and two guesses about the same promise diverge.

**Example:** Claimwick's reimbursement promise gains an entity with Approved and Reimbursed states, a refusal for an expense that is already reimbursed, and atomic consistency over one aggregate.

**Checked by:** `litespec model check` emits no `requirement-fulfillment-missing`, `record-link-missing`, `step-use-case-missing`, `erasure-undeclared`, or `setting-requirement-missing` finding across Behavior records.

### Send Commercial and Observable Figures to the Owner

**ID:** `litespec/core/layers#send-commercial-and-observable-figures-to-the-owner`

**Strength:** required

**Applies to:** artifact: record:setting, record:rule

**Statement:** Behavior that meets a figure the owner sets MUST send it to the owner instead of fixing it.

**Rationale:** The [Project Model](model.md) holds the one list of figures the owner sets: prices, fees, rates, terms, markets, payers, retention periods, freshness, and figures that differ by tenant, market, or date. Each figure is a business choice, and only an accountable person makes it. An agent that fixed one would hide a commitment in a Behavior record nobody chose. The figure becomes a question to the owner or a backlog item, never a silent default.

**Example:** Claimwick's receipt threshold of EUR 25 waits for the owner instead of entering the setting silently.

**Reviewed for:** Did Behavior fix any figure the owner sets without asking? Check the settings and rules the change adds beside the list of owner-set figures on the model page.

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

**Statement:** A need a lower record layer meets that the upper record layer leaves unstated MUST travel up as a question to the owner or a backlog item, never as code.

**Rationale:** The lower layer does not decide what the upper layer left unstated, so the need travels up with its context instead of turning into a guess. A question carries product choices to the person who answers for them. A backlog item holds the rest where it stays visible until it is specified. The specification is closed: an agent builds what it states and nothing more.

**Example:** Claimwick's missing reimbursement policy becomes a question to the owner, not a rule the agent writes.

**Reviewed for:** Does every unstated need have a question to the owner or a backlog item? Check the added records from a lower record layer beside the owner questions and the backlog.

### Enter World Facts as Cited Research or Constraints

**ID:** `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact learned from the world MUST enter Intent as research, or as a constraint whose `sources` cite its source with the date it was read.

**Rationale:** Commitments flow down, and facts from the world flow up: a provider's behavior, a regulation, or an incident enters Intent where people can weigh it. The citation and the `readOn` date show where the fact came from and when someone read it, so a later change rechecks the source instead of trusting memory. A constraint names the research it rests on in `basedOn`, as the constraint fields state. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Example:** Claimwick's observed two-day payout settlement window enters as research, then as a constraint citing that research.

**Reviewed for:** Did every world fact enter as a cited and dated research or constraint record? Check the provider, regulation, and incident facts the change relies on.

### Record Changed Provider Behavior as Observed Research

**ID:** `litespec/core/layers#record-changed-provider-behavior-as-observed-research`

**Strength:** required

**Applies to:** artifact: record:research, record:constraint

**Statement:** A changed provider behavior MUST enter as research recording the observed behavior, then as a constraint citing that research.

**Rationale:** The provider's behavior is a fact from the world, and research is where such facts enter. The constraint that follows names the research in `basedOn`, so the constraint carries its evidence. Code is not the source: a failed reimbursement in production points at the world, and the world is cited.

**Example:** Claimwick's payout provider starts settling every two days instead of daily: that enters as research with the observed dates, and a settlement-window constraint cites that research.

**Reviewed for:** Does every provider change have observed research and a citing constraint? Check the provider facts the change relies on beside the research records.

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

This informative example demonstrates `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`, `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`, and `litespec/core/layers#record-changed-provider-behavior-as-observed-research`.

Top-down: the owner sets EUR 50 instead of EUR 25 for the receipt threshold. The requirement's statement changes, and in the same change an agent updates the policy setting default and the scenario where an expense without a receipt is refused. The test changes, Proof shows passing, and the book chapter shows the new promise.

Bottom-up: a late reimbursement in production shows the payout bank settles batches every two days instead of daily. An agent records the observed behavior as research, then changes the settlement-window constraint to cite it. In the same change, the per-batch deadline rule and its scenarios change to match. The test changes, Proof shows passing, and the book chapter shows the cited source and date.
