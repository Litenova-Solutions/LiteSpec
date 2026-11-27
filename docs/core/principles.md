# Principles

**Stack:** any

**Concern:** records

Principles states the ideas that settle choices the narrower pages leave open: how the record layers relate, where names come from, and what counts as evidence. It is for readers who meet a conflict between a rule and a product need. The record layers carry the obligations; these ideas only explain them.

## Concepts

### Top-Down Writing

Intent states the promises, Behavior states how the system keeps them, Implementation builds what Behavior states, and Proof shows that the behavior holds. Each layer is written from the one above it, so a reader traces code up to the promise it keeps. Code and tests point up to the record they serve through a mark naming that record.

Nothing above Implementation comes from code: a record derived from code turns whatever exists into what was promised. The [layers page](layers.md) states the translation rules at each boundary. Claimwick writes its receipt rule in Behavior from the reimbursement promise in Intent, and the handler carries a mark naming that rule. This covers decided promises only: an undocumented habit in code stays out of the records.

### Commitments Flow Down and Facts Flow Up

A commitment made in Intent travels down through Behavior and Implementation to Proof. Facts learned from the world travel the other way: a provider behavior, a regulation, or an incident enters Intent as research or as a constraint, cited and dated, as the [layers page](layers.md) states.

What a lower record layer needs, and the upper record layer leaves unsaid, goes to the owner as a question or to the backlog board outside the repository. It slips quietly into the lower record layer at no point, as the [layers page](layers.md#raise-open-needs-as-owner-questions-or-backlog-items) states, and nothing travels up from code. Claimwick's two-day settlement observation enters as research and flows down into a deadline rule, while a missing receipt threshold goes up as an owner question. Decided commitments flow down, and cited facts flow up.

### The Spec Wins Where Intent Has Spoken

Where Intent states a promise, the record wins over the code, and code that disagrees is the defect. Where Intent is silent, working code is evidence for a question to the owner, and the answer lands in Intent first. Silence is a gap to ask about: neither a defect to delete nor a source to copy quietly.

Claimwick code that reimburses without a stated promise keeps running while its owner question is open, and code that reimburses twice against a stated once-only promise is the defect. The record wins only where Intent has spoken, and nowhere else.

### Derivation Between Records Only

One record derives from another, and none derives from code. Generic refusals derive from access, input, and the knowledge the use case reads, and read models derive from the records they list. Derivation runs one way, from the authored source to what cites it.

Claimwick's reporting read model derives its reimbursed-expense rows from the published expense events it lists, not from the spending store. Derivation copies values only, and invents no promise the source leaves unstated.

### Declared Edges, Not Mechanism Bans

LiteSpec declares edges instead of banning mechanisms. Each edge between use cases, reactions, jobs, notifications, integration events, and external systems is declared on the record holding it, and an undeclared edge is the defect, as the [system page](system.md#declare-every-edge-between-records) states.

The effect graph holds no cycle except through domain events with a stated end condition, and no edge shares a transaction: a use case commits its own writes, and what follows runs from a reaction to the fact it raised. A project that wants stricter rules keeps them as a cited convention. Claimwick's reimbursement commits the expense and raises its fact, and reporting follows through a reaction in its own transaction. Declared edges cover record edges only, and excuse no hidden call between handlers.

### Honest Opinion

LiteSpec openly favors modules, aggregates, domain events, use cases, reactions, and jobs over other mechanisms. Those other mechanisms live in extensions, and each extension states how its mechanism composes with the core fields. A project that needs one selects the extension that holds it.

Claimwick runs its nightly erasure as a job, and a project that needs durable dispatch selects the outbox extension. Favored mechanisms cover the core only, and block no selected extension.

### One Source for Each Fact

Each fact is authored once, in its source home: a requirement on its requirement page, a rule on its home page, a use case on its page, a term in the glossary, and stack versions in the manifest. A record repeats a fact from another record only when a reader needs it there, and a check holds each stated copy equal to its source. Completion derives from the model, and nobody types it by hand. The [model page](model.md) defines the common fields and the computed links.

The library states the bound group once in its hold rule, and `atomicity-drift` reports a use case whose `writes` differ. One source covers authored facts only, and asks no reader to chase a value across pages.

### Mechanical Proof of Boundaries

A boundary stated only in prose holds until the first change that ignores it. Structural boundaries carry mechanical proof instead: a project or package reference, compiler visibility, an architecture test, a behavior test, a permission guard, a store constraint, or a lint rule. Specifications explain the decisions that need judgment, and tools prove the constraints that need none.

A project-reference test proves Claimwick's Domain has no Infrastructure reference, and fails the build the day one appears. Mechanical proof covers enforceable boundaries only, and replaces no judgment about product choices.

### Restraint About New Machinery

Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts each later reader learns. New machinery arrives only for a current documented requirement. Preference and possible future need justify nothing.

Claimwick adds no cache until a use case names the freshness it needs, and a direction toward future scale adds none. Restraint covers machinery only, and blocks no shaping of a record that keeps a later requirement reachable.

### Naming Boundary Intent

Boundaries are where components meet, and where future readers orient themselves. Boundary names state the business action they guard, so a newcomer places the interaction without opening either side. Generic names gather unrelated responsibility behind them with each change.

Claimwick names its payout port for submitting payouts, not a generic sender, so a reader places it without opening either side. Boundary naming covers component edges only.

### Git Is the Decision Record

A record in the repository is decided, and the pull request changing it is its review. A record nobody wants leaves the repository through deletion, and git keeps who changed what and when, so records carry no history of their own.

A Claimwick feature nobody wants leaves the repository, and its past stays in git. Git holds history only, and decides no promise the records state.

### The Specification Is Closed

The specification is closed: an agent builds what the specification states and nothing more, as the [layers page](layers.md) states. What seems missing becomes a question to the owner or a backlog item, not code. Ideas live on the backlog board outside the repository until they are specified.

A Claimwick agent that needs an unstated receipt threshold asks the owner instead of picking EUR 25. Ideas stay outside the repository until they are specified.

### Every Record Is About the Software

Each record states behavior the application performs, data it holds, a refusal it raises at runtime, infrastructure it runs on, or a limit it enforces. Goals, problems, features, research, and constraints all qualify when they translate into code, results, tests, or infrastructure. Company business stays out: it enters solely as a constraint or as dated research.

A Claimwick goal that translates into payout code and tests qualifies, while a sales target stays out. Records cover the application only.

### Completion Is Computed

What a reader asks first is how much of a thing is coded and verified. The model computes that from code, tests, and passing scenarios, and nobody types it by hand. The book shows completion first, derived from the runs.

Claimwick's book marks a use case complete when its handler exists and its scenarios pass. Computed completion covers coded and verified work only.

### Guidance States Its Strength

The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve, so a violation is a defect. Each provision beside the model says whether it is required, a default, or advisory, and where it applies. A check is an aid where one is cheap and sure, not the condition for a provision to exist.

Claimwick treats an unmarked handler as a defect, while an advisory naming suggestion stays guidance with or without a check. Stated strength covers provisions only, and grades no check result by itself.

### Tools Stay Small

A command, check, generated file, or package exists while the owner approves it and something uses it. Each tool adds maintenance, failure modes, and concepts each later reader learns, so preference and possible future need justify nothing.

A Claimwick check nobody runs goes away with its command. Small tools cover approved and used tooling only, and keep no spare command for a possible future.

### Work in Progress

Each link between records holds in each run: `litespec model check` blocks on reachability locally and in the pull-request check alike. Work in progress that leaves a record unreached carries a gap waiver with `expiresOn` and `closedBy`, which parks reachability for that record until the waiver expires.

A Claimwick use case with no endpoint yet stays on its branch until the endpoint lands or a gap waiver parks it. Parking covers reachability only, and excuses no broken link elsewhere.

## Standards

### Take Every Name from the Intent Above It

**ID:** `litespec/core/principles#take-every-name-from-the-intent-above-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every layer below a record MUST carry the name that record gives an act, a name chosen for what the act does in the business.

**Rationale:** A name states what happens, so the right name comes from asking what the act really does, including how it ends. The record that states the act decides the name, and the use case, the aggregate method, the event, the permission, the route, and the screen label repeat it. A check that reports two names for one act reports drift, not the answer: the fix brings each layer to the name the intent states, and the intent changes first when its own word is wrong. A rename that only clears the check finding leaves one act with two names, as does a name kept in one layer while the others move. Settled by [decision/0026-one-name-for-each-concept](decision/0026-one-name-for-each-concept).

**Example:** In Claimwick, finance staff send an approved expense's payout to the bank, and the bank refuses it when the company account falls short. The act is a submission, not a payment, so the use case is `submit-payout`, the method `SubmitPayout`, the event `PayoutSubmitted`, the permission `submit-payout`, and the screen button reads "Submit payout". A later command records whether the bank paid or refused.

**Reviewed for:** Does every act carry the name the record above it states in its use case, method, event, permission, route, and screen label? Compare the names of one act across its record, its handler, its endpoint, and its screen.

### End Every Effect Cycle in a Domain Event

**ID:** `litespec/core/principles#end-every-effect-cycle-in-a-domain-event`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An effect graph MUST NOT hold a cycle except through a domain event with a stated end condition.

**Rationale:** A cycle with no end condition runs until something outside the records stops it, so no reader says when the work finishes. Routing each cycle through a domain event with a stated end condition names the loop and its exit in the records the graph checks.

**Checked by:** `litespec model check` reports no `effect-loop-invalid` finding on the changed records.

### Share No Transaction Across an Edge

**ID:** `litespec/core/principles#share-no-transaction-across-an-edge`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An edge between records MUST NOT share a transaction, so each use case commits only its own writes.

**Rationale:** An edge that shares its caller's transaction makes two use cases one unit of work: the called use case depends on a commit it never owns, a failure in either rolls back both, and neither use-case record says what it commits anymore. Running each follow-on act from a reaction to the fact the first act raised gives each use case its own commit, its own failure handling, and its own record. The schema offers no field for transaction sharing, so a record declares no shared transaction. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** Claimwick's reimbursement commits the expense and raises its reimbursed fact, and a reaction in the reporting module answers the published fact in its own transaction.

**Checked by:** `litespec model check` reports `record-invalid` for an effect entry that names `sharesTransaction` or a use case that `runs` another use case; `dotnet build` reports `LS2021` for a command or event mediator held outside an endpoint, event handler, or job, and `LS2022` for an event published by user code.

### Confine Atomic Consistency to One Use Case

**ID:** `litespec/core/principles#confine-atomic-consistency-to-one-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Atomic consistency MUST cover only the writes of one use case, never an edge to another record.

**Rationale:** Each edge crosses a commit boundary, so work past it fails after the caller succeeded. Atomic consistency promises one shared outcome, and that promise holds only inside the single commit a use case owns. A rule that spans two use cases is held by a state guard instead: the later act refuses until the earlier fact arrived.

**Example:** The library's hold request waits for its copy in an awaiting-copy state, and lending the copy refuses until the hold fact arrives.

**Reviewed for:** Does every atomic use case rely only on its own writes, with every rule across use cases held by a state guard? Check the consistency field and the rules it cites beside the reactions that follow.

### Keep One Authored Source

**ID:** `litespec/core/principles#keep-one-authored-source`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST author each fact once as its source and state it elsewhere only under a check that compares the copy with the source.

**Rationale:** One source behind each fact lands each change in one place: the source changes once, and each stated copy follows under its check. A record repeats a fact from another record only when a reader needs it there, and the check reports a copy that drifts from its source. Judgments stay computed: completion derives from the model, and nobody types it by hand. Settled by [decision/0069-state-each-copy-under-a-check](decision/0069-state-each-copy-under-a-check).

**Reviewed for:** Does every fact on the changed pages trace to one authored source, with every stated copy equal to its source under its check? Check the changed pages against their sources and checks.

### Locate Package Versions in the Manifest

**ID:** `litespec/core/principles#locate-package-versions-in-the-manifest`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST resolve the standards-selected stack versions from `litespec.manifest.yaml`.

**Rationale:** The manifest is the version authority for the stacks the standards select, so each consumer resolves the same tested combination. A version pinned in prose or remembered by an agent drifts from that authority with the next manifest change. Consumer dependencies still live in package manifests and lockfiles: only the standards-selected stack versions resolve from `litespec.manifest.yaml`.

**Checked by:** `litespec model check` reports `dependency-version-drift` for a package whose version differs from its manifest pin, and `stack-invalid` for a named stack the manifest does not ship.

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

**Rationale:** Strength words work only when each one means exactly one thing. With the strength stated, an agent tells binding from advisory without a check existing, and a check stays an aid where one is cheap and sure, not the condition for a provision to exist.

**Reviewed for:** Does every added provision state its strength and scope? Check the provisions the change adds.

### Complete Work Before Merging

**ID:** `litespec/core/principles#complete-work-before-merging`

**Strength:** required

**Applies to:** artifact: any

**Statement:** What merges MUST be complete.

**Rationale:** A branch stays incomplete, but what merges is complete. A record that stays unreached past a merge carries a gap waiver with `expiresOn` and `closedBy`, which parks reachability for that record until the waiver expires.

**Example:** A Claimwick use case with no endpoint stays on its branch until the endpoint lands, or until a gap waiver with `expiresOn` and `closedBy` parks it.

**Checked by:** `litespec model check` blocks on unreached records, such as `intent-unreached` and `constraint-unreached`, in every run, locally and in the pull-request check alike.

### Add New Machinery Only for a Current Requirement

**ID:** `litespec/core/principles#add-new-machinery-only-for-a-current-requirement`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST add packages, projects, wrappers, background processes, caches, queues, or distributed patterns only for a current documented requirement, and never for a direction alone.

**Rationale:** Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts each later reader learns. A current use case, decision, or requirement justifies that cost by naming the activation criterion. Preference and possible future need justify nothing, so speculative machinery stays out. A direction justifies the shape of a record or a model, such as a field that keeps a later way of growing reachable, but it activates no machinery: that cost waits for the requirement that cites the direction. Unwired or unmeasured machinery leaves the repository instead of staying as guidance, by [decision/0029-delete-the-unwired-language-checker](decision/0029-delete-the-unwired-language-checker), [decision/0031-remove-only-the-timing-variable](decision/0031-remove-only-the-timing-variable), and [decision/0034-measure-mutation-before-gating](decision/0034-measure-mutation-before-gating).

**Reviewed for:** Does each added package, project, wrapper, process, cache, queue, or pattern cite a current use case, decision, or requirement? Check the added machinery beside the use cases, decisions, and requirements it cites.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/principles#take-every-name-from-the-intent-above-it` and `litespec/core/principles#keep-one-authored-source`.

Finance staff send an approved Claimwick expense payout to the bank, and the bank refuses it when the company account falls short. The act is a submission, so the use case is `submit-payout`, the method `SubmitPayout`, the event `PayoutSubmitted`, and the screen button reads "Submit payout": each layer below the record carries the name the record gives the act.

The once-only reimbursement rule lives in its rule record alone. The book, the handler mark, and the scenario cite that record instead of restating it, and the check holds each stated copy equal to its source.
