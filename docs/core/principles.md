# Principles

**Stack:** any

**Concern:** records

Principles states the ideas that settle choices the narrower pages leave open: how the record layers relate, where names come from, and what counts as evidence. LiteSpec pairs a strict model of linked records with graded engineering provisions, and git holds the decision record. Apply these principles only after the narrower provisions, and read the [layers page](layers.md) for the boundaries they govern.

## Concepts

### Top-Down Writing

Intent states the promises, Behavior states how the system keeps them, Implementation builds what Behavior states, and Proof shows that the behavior holds. Each layer is written from the one above it, so a reader can trace code up to the promise it keeps. Code and tests point up to the record they serve through a mark that names that record.

Nothing above Implementation is computed from code, because a record derived from code would turn whatever exists into what was promised. The [layers page](layers.md) states the translation rules at each boundary. For example, Claimwick writes its receipt rule in Behavior from the reimbursement promise in Intent, and the handler carries a mark naming that rule. This covers decided promises only: an undocumented habit in code never becomes a record.

### Commitments Flow Down and Facts Flow Up

A commitment made in Intent travels down through Behavior and Implementation to Proof. Facts learned from the world travel the other way: a provider behavior, a regulation, or an incident enters Intent as research or as a constraint, cited and dated, as the [layers page](layers.md#enter-world-facts-as-cited-research-or-constraints) states.

What a lower record layer needs, and the upper record layer does not say, goes to the owner as a question or to the backlog board outside the repository. It never slips quietly into the lower record layer, as the [layers page](layers.md#raise-open-needs-as-owner-questions-or-backlog-items) states, and nothing travels up from code. For example, Claimwick's two-day settlement observation enters as research and flows down into a deadline rule, while a missing receipt threshold goes up as an owner question. Only decided commitments flow down, and only cited facts flow up.

### The Spec Wins Where Intent Has Spoken

Where Intent states a promise, the record wins over the code, and code that disagrees is the defect. Where Intent is silent, working code is evidence for a question to the owner, and the answer lands in Intent first. Silence is a gap to ask about: neither a defect to delete nor a source to copy quietly.

For example, Claimwick code that reimburses without a stated promise keeps running while its owner question is open, and code that reimburses twice against a stated once-only promise is fixed. The record wins only where Intent has spoken, and nowhere else.

### Derivation Between Records Only

One record derives from another, never from code. Generic refusals derive from access and input, and read models derive from the records they list. Derivation runs one way, from the authored source to what cites it.

For example, Claimwick's reporting read model derives its reimbursed-expense rows from the published expense events it lists, not from the spending store. Derivation copies values only, and never invents a promise the source never stated.

### Declared Edges, Not Mechanism Bans

LiteSpec declares edges instead of banning mechanisms. Every edge between use cases, reactions, jobs, notifications, integration events, and external systems is declared on the record that holds it, and an undeclared edge is the defect, as the [system page](system.md#declare-every-edge-between-records) states.

The effect graph holds no cycles except through domain events with a stated end condition, and no edge shares a transaction: a use case commits its own writes, and what follows runs from a reaction to the fact it raised. A project that wants stricter rules keeps them as a cited convention. For example, Claimwick's reimbursement commits its expense and raises its fact, and reporting follows through a reaction in its own transaction. Declared edges cover record edges only, and never excuse a hidden call between handlers.

### Honest Opinion

LiteSpec openly favors modules, aggregates, domain events, use cases, reactions, and jobs over other mechanisms. Those other mechanisms live in extensions, and each extension states how its mechanism composes with the core fields. A project that needs one selects the extension that holds it.

For example, Claimwick runs its nightly erasure as a job, and a project that needs durable dispatch selects the outbox extension. Favored mechanisms cover the core only, and never block a selected extension.

### One Source for Each Fact

Each fact is authored once, in its source home: a requirement on its requirement page, a rule on its home page, a use case on its page, a term in the glossary, and stack versions in the manifest. A record repeats a fact from another record only when a reader needs it there, and a check holds each stated copy equal to its source. Completion derives from the model, and nobody types it by hand. The [model page](model.md) defines the common fields and the computed links.

For example, the library states the bound group once in its hold rule, and `atomicity-drift` reports a use case whose `writes` differ. One source covers authored facts only, and never asks a reader to chase a value across pages.

### Mechanical Proof of Boundaries

A boundary stated only in prose holds until the first change that ignores it. Structural boundaries carry mechanical proof instead: a project or package reference, compiler visibility, an architecture test, a behavior test, a permission guard, a store constraint, or a lint rule. Specifications explain the decisions that need judgment, and tools prove the constraints that do not.

For example, a project-reference test proves Claimwick's Domain has no Infrastructure reference, and fails the build the day one appears. Mechanical proof covers enforceable boundaries only, and never replaces judgment about product choices.

### Restraint About New Machinery

Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts every later reader learns. New machinery arrives only for a current documented requirement. Preference and possible future need justify nothing.

For example, Claimwick adds no cache until a use case names the freshness it needs, and a direction toward future scale adds none. Restraint covers machinery only, and never blocks shaping a record so a later requirement stays reachable.

### Naming Boundary Intent

Boundaries are where components meet, and where future readers orient themselves. Boundary names state the business action they guard, so a newcomer places the interaction without opening either side. Generic names gather unrelated responsibility behind them with every change.

For example, Claimwick names its payout port for submitting payouts, not a generic sender, so a reader places it without opening either side. Boundary naming covers component edges only.

### Git Is the Decision Record

A record in the repository is decided, and the pull request that changes it is its review. A record nobody wants is deleted, and git keeps its history. Who changed what, and when, comes from git, so records carry no history of their own.

For example, a Claimwick feature nobody wants is deleted from the repository, and its past stays in git. Git holds history only, and never decides what the records promise.

### The Specification Is Closed

The specification is closed: an agent builds what the specification states and nothing more, as the [layers page](layers.md) states. What seems missing becomes a question to the owner or a backlog item, never code. Ideas live on the backlog board outside the repository until they are specified.

For example, a Claimwick agent that needs an unstated receipt threshold asks the owner instead of picking EUR 25.

### Every Record Is About the Software

Every record states behavior the application performs, data it holds, a refusal it raises at runtime, infrastructure it runs on, or a limit it enforces, as [Write Every Record About the Software](#write-every-record-about-the-software) states. Goals, problems, features, research, and constraints all qualify when they translate into code, results, tests, or infrastructure. None qualifies when it is about running the company: a goal is a software goal, never a business goal.

For example, a Claimwick goal that translates into payout code qualifies, while a sales target does not. Records cover the application only, and company business enters solely as a constraint or as dated research.

### Completion Is Computed

What a reader asks first is how much of a thing is coded and verified. The model computes that from code, tests, and passing scenarios, and nobody types it. The book shows completion first, derived from the runs.

For example, Claimwick's book marks a use case complete when its handler exists and its scenarios pass. Computed completion covers coded and verified work only.

### Guidance States Its Strength

The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve, so a violation is a defect. Every provision beside the model says whether it is required, a default, or advisory, and where it applies. A check is an aid where one is cheap and sure, never the condition for a provision to exist.

For example, Claimwick treats an unmarked handler as a defect, while an advisory naming suggestion stays guidance with or without a check. Stated strength covers provisions only, and never grades a check result by itself.

### Tools Stay Small

A command, check, generated file, or package exists because the owner approved it, and is removed when nothing uses it. Each tool adds maintenance, failure modes, and concepts every later reader learns, so preference and possible future need justify nothing.

For example, a Claimwick check nobody runs goes away with its command. Small tools cover approved and used tooling only, and never keep a spare command for a possible future.

### Work in Progress

Every link between records holds in every run: `litespec model check` blocks on reachability locally and in the pull-request check alike. Work in progress that leaves a record unreached carries a gap waiver with `expiresOn` and `closedBy`, which parks reachability for that record until the waiver expires.

For example, a Claimwick use case with no endpoint yet stays on its branch until the endpoint lands or a gap waiver parks it. Parking covers reachability only, and never excuses a broken link elsewhere.

## Standards

### Take Every Name from the Intent Above It

**ID:** `litespec/core/principles#take-every-name-from-the-intent-above-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every layer below a record MUST carry the name that record gives an act, a name chosen for what the act does in the business.

**Rationale:** A name states what happens, so the right name comes from asking what the act really does, including how it can end. The record that states the act decides the name, and the use case, the aggregate method, the event, the permission, the route, and the screen label repeat it. A check that reports two names for one act reports drift, not the answer: the fix brings every layer to the name the intent states, and the intent changes first when its own word is wrong. A rename that only clears the check finding leaves one act with two names, as does a name kept in one layer while the others move. Settled by [decision/0026-one-name-for-each-concept](decision/0026-one-name-for-each-concept).

**Example:** In Claimwick, finance staff send an approved expense's payout to the bank, and the bank can refuse it when the company account cannot cover it. The act is a submission, not a payment, so the use case is `submit-payout`, the method `SubmitPayout`, the event `PayoutSubmitted`, the permission `submit-payout`, and the screen button reads "Submit payout". A later command records whether the bank paid or refused.

**Reviewed for:** Does every act carry the name the record above it states in its use case, method, event, permission, route, and screen label? Compare the names of one act across its record, its handler, its endpoint, and its screen.

### Compute Nothing Above Implementation from Code

**ID:** `litespec/core/principles#compute-nothing-above-implementation-from-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST NOT compute a record above Implementation from code.

**Rationale:** A record computed from code turns what exists into what is promised: drift becomes the plan, and the record restates what the code already does. Records above Implementation state decided promises instead, and code points up to the record it serves through a mark that names that record.

**Reviewed for:** Does every record above Implementation state a decided promise rather than restating code? Compare each new record with the code beside it.

### Derive Records Only from Records

**ID:** `litespec/core/principles#derive-records-only-from-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record MUST take each derived value from another record.

**Rationale:** Derivation between records keeps one authored source behind every value: the source changes once, and each derived read model follows. Generic refusals derive from access and input, and each read model derives from the records it lists, so readers trace every derived value to the record that states it.

**Reviewed for:** Can every derived value be traced to the record that authors it? Trace each derived value to the record it cites.

### Follow Intent Where It States the Promise

**ID:** `litespec/core/principles#follow-intent-where-it-states-the-promise`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Behavior and Implementation MUST follow Intent where Intent states the promise.

**Rationale:** Intent holds the decided promises, and Behavior and code keep one chain from promise to product by following them. Where Intent states the promise, code that disagrees is the defect, and the fix changes the code.

**Reviewed for:** Does the changed code agree with the Intent records it serves? Compare the changed code with the stated promises.

### Treat Code Beyond Silent Intent as Evidence for an Owner Question

**ID:** `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-an-owner-question`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST treat working code that Intent does not cover as evidence for a question to the owner, not as a defect to delete and not as a source to copy into Intent.

**Rationale:** When Intent is silent and working code exists, a decision went unrecorded. Deleting the code destroys behavior users rely on, and copying it quietly into Intent turns code into a promise nobody stated. A question carries the behavior to the owner with the code as evidence, and the answer lands in Intent first.

**Example:** A positivity guard in Claimwick's submit-expense handler that no rule states becomes an owner question citing the guard, while the code stays until the answer lands in Intent.

**Reviewed for:** Does every piece of working code outside Intent have an owner question carrying it? Check the owner questions beside the uncovered code.

### End Every Effect Cycle in a Domain Event

**ID:** `litespec/core/principles#end-every-effect-cycle-in-a-domain-event`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An effect graph MUST NOT hold a cycle except through a domain event with a stated end condition.

**Rationale:** A cycle without an end condition runs until something outside the records stops it, so no reader can say when the work finishes. Routing each cycle through a domain event with a stated end condition names the loop and its exit in the records the graph checks.

**Checked by:** `litespec model check` reports no `effect-loop-invalid` finding on the changed records.

### Share No Transaction Across an Edge

**ID:** `litespec/core/principles#share-no-transaction-across-an-edge`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An edge between records MUST NOT share a transaction, so each use case commits only its own writes.

**Rationale:** An edge that shares its caller's transaction makes two use cases one unit of work: the called use case depends on a commit it does not own, a failure in either rolls back both, and neither use-case record says what it commits anymore. Running every follow-on act from a reaction to the fact the first act raised gives each use case its own commit, its own failure handling, and its own record. The schema offers no field for transaction sharing, so a record cannot declare a shared transaction. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** Claimwick's reimbursement commits the expense and raises its reimbursed fact, and a reaction in the reporting module answers the published fact in its own transaction.

**Checked by:** `litespec model check` reports `record-invalid` for an effect entry that names `sharesTransaction` or a use case that `runs` another use case; `dotnet build` reports `LS2021` for a command or event mediator held outside an endpoint, event handler, or job, and `LS2022` for an event published by user code.

### Confine Atomic Consistency to One Use Case

**ID:** `litespec/core/principles#confine-atomic-consistency-to-one-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Atomic consistency MUST cover only the writes of one use case, never an edge to another record.

**Rationale:** Every edge crosses a commit boundary, so work past it can fail after the caller succeeded. Atomic consistency promises one shared outcome, and that promise holds only inside the one commit a use case owns. A rule that spans two use cases is held by a state guard instead: the later act refuses until the earlier fact arrived.

**Example:** The library's hold request waits for its copy in an awaiting-copy state, and lending the copy refuses until the hold fact arrives.

**Reviewed for:** Does every atomic use case rely only on its own writes, with every rule across use cases held by a state guard? Check the consistency field and the rules it cites beside the reactions that follow.

### Record Stricter Edge Rules as Conventions

**ID:** `litespec/core/principles#record-stricter-edge-rules-as-conventions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project that keeps a stricter edge rule MUST record it as a cited convention.

**Rationale:** LiteSpec checks declared edges and leaves stricter taste to each project. A stricter rule recorded as a cited convention stays visible and checkable beside the records it constrains, instead of surviving as habit in a few heads.

**Reviewed for:** Does every stricter edge rule have a cited convention? Check the project conventions beside the changed records.

### Cover Other Mechanisms with Extensions

**ID:** `litespec/core/principles#cover-other-mechanisms-with-extensions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A mechanism outside modules, aggregates, domain events, use cases, reactions, and jobs MUST live in an extension.

**Rationale:** LiteSpec favors one set of mechanisms and states that choice openly instead of claiming neutrality. An extension states how its mechanism composes with the core fields, so a project that needs it selects one place holding the whole contract.

**Reviewed for:** Does every mechanism outside the favored set have an extension? Check the selected extensions beside the changed records.

### Keep One Authored Source

**ID:** `litespec/core/principles#keep-one-authored-source`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST author each fact once as its source and state it elsewhere only under a check that compares the copy with the source.

**Rationale:** One source behind every fact lands each change in one place: the source changes once, and each stated copy follows under its check. A record repeats a fact from another record only when a reader needs it there, and the check reports a copy that drifts from its source. Judgments stay computed: completion derives from the model, and nobody types it by hand. Settled by [decision/0069-state-each-copy-under-a-check](decision/0069-state-each-copy-under-a-check).

**Reviewed for:** Does every fact on the changed pages trace to one authored source, with every stated copy equal to its source under its check? Check the changed pages against their sources and checks.

### Locate Package Versions in the Manifest

**ID:** `litespec/core/principles#locate-package-versions-in-the-manifest`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST resolve the standards-selected stack versions from `litespec.manifest.yaml`.

**Rationale:** The manifest is the version authority for the stacks the standards select, so every consumer resolves the same tested combination. A version pinned in prose or remembered by an agent drifts from that authority with the next manifest change. Consumer dependencies still live in package manifests and lockfiles: only the standards-selected stack versions resolve from `litespec.manifest.yaml`.

**Checked by:** `litespec model check` reports `dependency-version-drift` for a package whose version differs from its manifest pin, and `stack-invalid` for a named stack the manifest does not ship.

### Keep Git as the Decision Record

**ID:** `litespec/core/principles#keep-git-as-the-decision-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST treat a record in the repository as decided, with the pull request that changes it as its review.

**Rationale:** A record in git is the decision, and the pull request that changes it is its review. A record nobody wants is deleted, while git keeps who changed what and when, so records carry no history of their own.

**Example:** A Claimwick feature nobody wants is deleted from the repository, and its history stays in git.

**Reviewed for:** Did every changed record go through its pull request? Check the pull request behind the change.

### Write Every Record About the Software

**ID:** `litespec/core/principles#write-every-record-about-the-software`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record MUST state behavior the application performs, data it holds, a refusal it raises at runtime, infrastructure it runs on, or a limit it enforces.

**Rationale:** A goal, problem, feature, research record, or constraint qualifies when it translates into code, results, tests, or infrastructure. Running the company stays out: it enters only as a constraint or as dated research.

**Example:** A Claimwick goal that translates into payout code and tests qualifies, while a sales target does not.

**Reviewed for:** Does every new record state the software rather than company business? Compare each new record with the code, results, tests, or infrastructure it translates into.

### Compute Completion from Code and Tests

**ID:** `litespec/core/principles#compute-completion-from-code-and-tests`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** A consumer MUST NOT write completion by hand, because the model computes it from code, tests, and passing scenarios.

**Rationale:** A reader's first question is how much of a thing is coded and verified. Completion typed by hand answers with a report the next commit falsifies. The [model page](model.md) defines what the model computes.

**Example:** Claimwick's book marks a use case complete when its handler exists and its scenarios pass, with the model deriving that from the runs.

**Reviewed for:** Does any record state its own completion? Compare the record fields with the completion the model computes.

### State the Strength of Every Provision

**ID:** `litespec/core/principles#state-the-strength-of-every-provision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every provision MUST state whether it is required, a default, or advisory, and where it applies.

**Rationale:** Strength words work only when each one means exactly one thing. With the strength stated, an agent tells binding from advisory without a check existing, and a check stays an aid where one is cheap and sure, never the condition for a provision to exist.

**Reviewed for:** Does every added provision state its strength and scope? Check the provisions the change adds.

### Keep Tools Small

**ID:** `litespec/core/principles#keep-tools-small`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A command, check, generated file, or package MUST exist only while the owner approves it and something uses it.

**Rationale:** Each command, check, generated file, or package adds maintenance, failure modes, and concepts every later reader learns. A tool nothing uses is removed instead of kept beside its replacement. Settled by [decision/0029-delete-the-unwired-language-checker](decision/0029-delete-the-unwired-language-checker), [decision/0031-remove-only-the-timing-variable](decision/0031-remove-only-the-timing-variable), and [decision/0034-measure-mutation-before-gating](decision/0034-measure-mutation-before-gating), and the same rule removes project fields in [decision/0025-remove-fields-nothing-reads](decision/0025-remove-fields-nothing-reads).

**Example:** A Claimwick check nobody runs goes away with its command.

**Reviewed for:** Does every added command, check, generated file, or package have an owner approval and a user? Check the tooling the change adds beside its users.

### Complete Work Before Merging

**ID:** `litespec/core/principles#complete-work-before-merging`

**Strength:** required

**Applies to:** artifact: any

**Statement:** What merges MUST be complete.

**Rationale:** A branch may be incomplete, but what merges is complete. A record that must stay unreached past a merge carries a gap waiver with `expiresOn` and `closedBy`, which parks reachability for that record until the waiver expires.

**Example:** A Claimwick use case with no endpoint stays on its branch until the endpoint lands, or until a gap waiver with `expiresOn` and `closedBy` parks it.

**Checked by:** `litespec model check` blocks on unreached records, such as `intent-unreached` and `constraint-unreached`, in every run, locally and in the pull-request check alike.

### Prove Enforceable Boundaries Mechanically

**ID:** `litespec/core/principles#prove-enforceable-boundaries-mechanically`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** A consumer MUST prove each enforceable boundary mechanically: a reference, visibility, an architecture test, a behavior test, or a lint rule.

**Rationale:** A boundary stated only in prose holds until the first commit that ignores it. Mechanical proof fails the build or the check the day the boundary breaks. Specifications then explain the decisions that need judgment, while tools guard the constraints that do not. Enforceable boundaries include project and package references, compiler visibility, permission guards, store constraints, and lint rules.

**Example:** A project-reference test proves Claimwick's Domain has no Infrastructure reference.

**Reviewed for:** Does each changed enforceable boundary name a reference, test, or lint rule that proves it? Check the changed boundaries beside their references, tests, and lint rules.

### Add New Machinery Only for a Current Requirement

**ID:** `litespec/core/principles#add-new-machinery-only-for-a-current-requirement`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST add packages, projects, wrappers, background processes, caches, queues, or distributed patterns only for a current documented requirement, and never for a direction alone.

**Rationale:** Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts every later reader learns. A current use case, decision, or requirement justifies that cost by naming the activation criterion. Preference and possible future need justify nothing, so speculative machinery stays out. A direction may justify the shape of a record or a model, such as a field that keeps a later way of growing reachable, but it activates no machinery: that cost waits for the requirement that cites the direction.

**Reviewed for:** Does each added package, project, wrapper, process, cache, queue, or pattern cite a current use case, decision, or requirement? Check the added machinery beside the use cases, decisions, and requirements it cites.

### Name Boundaries for Their Business Action

**ID:** `litespec/core/principles#name-boundaries-for-their-business-action`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST use business operation names, specific command and query mediators, and business-action external ports at boundaries.

**Rationale:** Components meet at boundaries, and future readers orient themselves there. Business operation names, specific command and query mediators, and business-action external ports state the purpose at that meeting point, so a newcomer places the interaction without opening either side.

**Reviewed for:** Does each changed boundary name state its business action? Check the changed boundary names against the actions they guard.

### Avoid Generic Boundary Names

**ID:** `litespec/core/principles#avoid-generic-boundary-names`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST NOT use generic `bus`, `manager`, `helper`, `processor`, or `service` names when a narrower name exists.

**Rationale:** Names such as `bus`, `manager`, `helper`, `processor`, and `service` say nothing about the business action, so unrelated responsibility collects behind them with every change. The narrower name keeps each boundary to the action it names.

**Example:** Claimwick's receipt-reminder notifier names its action, while one named only `service` does not.

**Reviewed for:** Does each changed boundary name carry the narrowest accurate business name, with no bare `bus`, `manager`, `helper`, `processor`, or `service`? Check the changed names for bare mechanism words.

## Conventions

### Prefer Direct Owned Dependencies

**ID:** `litespec/core/principles#prefer-direct-owned-dependencies`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use a selected framework type directly inside the architecture layer that owns it until an architectural boundary exists.

**Replacement:** A consumer records a wrapping convention naming the architecture layers that use project-owned abstractions.

**Rationale:** Using a selected framework type directly keeps one concept instead of two: the abstraction and what it hides. A project-owned abstraction earns its place for provider replacement, a test boundary, a stable domain concept, or an architecture boundary. Wrapping the framework everywhere else doubles the API the team maintains without isolating any real change.

**Reviewed for:** Does each new abstraction name the boundary that justifies it, or the wrapping convention that permits it? Check the new abstractions beside their boundaries and the wrapping convention.

### Prefer Local Code Until Reuse Is Real

**ID:** `litespec/core/principles#prefer-local-code-until-reuse-is-real`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep operation-specific code in its operation folder until two real consumers need the same precise behavior.

**Replacement:** A consumer records a sharing convention naming when operation code moves to shared units.

**Rationale:** Operation-specific code kept beside its operation stays easy to change with the operation. Shared code needs a name and a responsibility that stay accurate for both consumers, but the first consumer rarely knows what the second needs. Holding out for two real consumers means the shared unit covers behavior both actually need.

**Reviewed for:** Does each new shared unit serve two real consumers under one accurate name, or fall under the sharing convention? Check the new shared units beside their consumers and the sharing convention.

## Reference Example

This informative example demonstrates `litespec/core/principles#prefer-direct-owned-dependencies` and `litespec/core/principles#prefer-local-code-until-reuse-is-real`.

- A query handler uses the selected stack data-access type directly, because no architectural boundary separates the handler from the stack and no wrapper would isolate a change.
- Repository interfaces stay project-owned where the architecture keeps persistence out of the domain layer, since the boundary justifies the abstraction.
- A two-line mapping used once stays beside its endpoint until a second consumer needs the same behavior.
