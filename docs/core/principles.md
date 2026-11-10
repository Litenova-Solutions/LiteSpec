# Principles

**Stack:** any
**Layer:** records

## Intent

These principles settle a design choice when no narrower provision prefers one design. LiteSpec defines a strict specification model of linked records beside graded engineering provisions, with git as the decision record. Apply this page when two designs both satisfy the narrower pages, and read the [layers page](layers.md) for the boundaries these principles govern.

## Concepts

### Top-Down Writing

Each record layer is written from the record layer above it: Intent states the promises, Behavior states how the system keeps them, Implementation builds what Behavior states, and Proof shows that it holds. Code and tests point up to the record they serve, through a mark naming that record. Nothing above Implementation is computed from code.

### Commitments Flow Down and Facts Flow Up

A commitment made in Intent travels down: Behavior fulfills it, Implementation builds it, and Proof checks it. A fact learned from the world, such as a provider behavior, a regulation, or an incident, enters Intent as research or a constraint, cited and dated. What a lower record layer needs, and the upper record layer does not say, goes to the owner as a question or to the backlog board outside the repository, never quietly into the lower record layer. Nothing travels up from code.

### The Spec Wins Where Intent Has Spoken

Where Intent states a promise, the record wins over the code: code that disagrees is the defect. Where Intent is silent, working code is evidence for a question to the owner, and the answer lands in Intent first. Silence is a gap to ask about, not a defect to delete and not a source to copy quietly.

### Derivation Between Records Only

One record derives from another: generic refusals derive from access and input, and views derive from the records they list. Derivation runs between records, in one direction, from the authored source to what cites it. Code is not a source for a record.

### Declared Edges, Not Mechanism Bans

Every edge between use cases, reactions, jobs, notifications, integration events, and external systems is declared on the record that holds it, and an undeclared edge is the defect. The effect graph holds no cycles except through domain events with a stated end condition. An inline edge states whether it shares the caller's transaction, and atomic consistency spans no async edge. A workflow engine carries a saga only through the workflow-engine extension, where each engine step maps to exactly one use case with its compensation. A project that wants stricter rules keeps them as a cited convention.

### Honest Opinion

LiteSpec favors modules, aggregates, domain events, use cases, reactions, and jobs, and it does not claim to be neutral about that choice. Other mechanisms live in extensions, which state how they compose with the core fields. A project that needs another mechanism selects the extension that holds it.

### One Source for Each Fact

Each fact lives in exactly one home: a requirement on its requirement page, a rule on its home page, a use case on its page, a term in the glossary, stack versions in the manifest. Completion derives from the model; nobody types it by hand. The [model page](model.md) defines the common fields and the computed links.

### Mechanical Proof of Boundaries

A boundary stated only in prose holds until the first change that ignores it. Structural boundaries carry mechanical proof: a project or package reference, compiler visibility, an architecture test, a behavior test, a permission guard, a store constraint, or a lint rule. Specifications explain the decisions that need judgment; tools prove the constraints that do not.

### Restraint About New Machinery

Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts every later reader learns. New machinery arrives only for a current documented requirement. Preference and possible future need justify nothing.

### Naming Boundary Intent

Boundaries are where components meet and where future readers orient themselves. Boundary names state the business action they guard, so a newcomer places the interaction without opening either side. Generic names collect unrelated responsibility behind them with every change.

### Git Is the Decision Record

A record in the repository is decided, and the pull request that changes it is its review. A record nobody wants is deleted; git keeps its history. Who changed what and when comes from git, so records carry no history of their own.

### The Specification Is Closed

An agent builds what the specification states and nothing more. Something that seems missing becomes a question to the owner or a backlog item, never code. Ideas live on the backlog board outside the repository until they are specified.

### Every Record Is About the Software

Every record states what the application does, holds, refuses at runtime, or runs on. Goals, problems, features, research, and constraints all qualify when they translate into code, results, tests, or infrastructure, and none qualifies when it is about running the company. A goal is a software goal that translates into code, results, and infrastructure, never a business goal.

### Completion Is Computed

What a reader asks first is how much of a thing exists and is proven. The model computes it from code, tests, and passing scenarios, and nobody types it. The book shows completion first, derived from the runs.

### Guidance States Its Strength

The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve, and a violation is a defect. Every provision beside the model says whether it is a rule, a default, or advice, and where it applies. A check is an aid where one is cheap and sure, never the condition for a provision to exist.

### Tools Stay Small

A command, check, generated file, or package exists because the owner approved it, and it is removed when nothing uses it. Each tool adds maintenance, failure modes, and concepts every later reader learns. Preference and possible future need justify nothing.

### Work in Progress

A branch may be incomplete; what merges is complete. `litespec model check` reports reachability locally and blocks on it in the pull-request check. A record that must stay unreached past a merge carries an exception with `expiresOn`, which parks reachability for that record until it expires.

## Standards

### Write Each Layer from the Layer Above

**ID:** `litespec/core/principles#write-each-layer-from-the-layer-above`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST write each record layer from the record layer above it, from Intent down to Proof.

**Rationale:** A record layer written from the record layer above inherits decided promises instead of inventing its own: Behavior fulfills Intent, Implementation builds Behavior, and Proof checks Implementation. A record layer written from below restates guesses as rules, and the next upper record-layer change leaves two versions of one promise.

**Example:** A use case names the requirement it fulfills before its handler exists.

**Checked by:** `litespec model check` reports no `record-link-missing` finding on the changed records.

### Take Every Name from the Intent Above It

**ID:** `litespec/core/principles#take-every-name-from-the-intent-above-it`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** Every layer below a record MUST carry the name that record gives an act, a name chosen for what the act does in the business.

**Rationale:** A name is a claim about what happens. The right name comes from asking what the act really does, including how it can end, and the record that states the act decides it; the use case, the aggregate method, the event, the permission, the route and the screen label then repeat it. A check that reports two names for one act reports drift, not the answer: the fix brings every layer to the name the intent states, and the intent changes first when its own word is wrong. A rename made only to quiet a check, or a name kept in one layer while the others move, leaves one act with two names.

**Example:** In the Claimwick product, finance staff send an approved expense's payout to the bank, and the bank can refuse it when the company account cannot cover it. The act is a submission, not a payment: the use case is `submit-payout`, the method `SubmitPayout`, the event `PayoutSubmitted`, the permission `submit-payout`, and the screen's button reads "Submit payout". A later command records whether the bank paid or refused.

**Reviewed for:** Which act carries a different name in its use case, method, event, permission, route or screen label than the record above it states? Compare the names of one act across its record, its handler, its endpoint and its screen.

### Compute Nothing Above Implementation from Code

**ID:** `litespec/core/principles#compute-nothing-above-implementation-from-code`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST NOT compute a record above Implementation from code.

**Rationale:** A record computed from code turns what exists into what is promised, so drift becomes the plan and the record restates what the code already does. Records above Implementation state decided promises, and code points up to the record it serves through a mark naming that record.

**Reviewed for:** Which records above Implementation restate code instead of a decided promise? Compare each new record with the code beside it.

### Derive Records Only from Records

**ID:** `litespec/core/principles#derive-records-only-from-records`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A record MUST take each derived value from another record.

**Rationale:** Derivation between records keeps one authored source behind every value: the source changes once and each derived view follows. Generic refusals derive from access and input, and each view derives from the records it lists, so readers trace every derived value to the record that states it.

**Reviewed for:** Which derived values lack an authored source record? Trace each derived value to the record it cites.

### Enter Facts from the World as Research or Constraints

**ID:** `litespec/core/principles#enter-facts-from-the-world-as-research-or-constraints`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST enter each fact learned from the world as research or a constraint, cited and dated.

**Rationale:** A provider behavior, a regulation, or an incident is evidence from outside the project, and entering it as research or a constraint keeps the citation and the date beside the fact. A fact typed straight into Behavior or code loses its source, and later readers treat an observation as a decision.

**Example:** An agent records a slower payout settlement as research, then writes the constraint that cites it.

**Reviewed for:** Which outside facts lack a cited and dated source? Look at the research and constraint records beside the change.

### Raise What a Lower Layer Needs to the Owner

**ID:** `litespec/core/principles#raise-what-a-lower-layer-needs-to-the-owner`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST raise what a lower record layer needs, and the upper record layer does not state, to the owner as a question or to the backlog board, never quietly in the lower record layer.

**Rationale:** A need that goes to the owner reaches the person who decides it, and a need placed on the backlog board stays visible outside the repository. A need filled in quietly at the lower record layer becomes an undecided promise that later readers trust.

**Example:** A use case that needs a price its requirements do not state raises an owner question naming the missing price.

**Reviewed for:** Which needs from a lower record layer lack an owner question or a backlog item? Look at the owner questions and the backlog board beside the records that need them.

### Follow Intent Where It Has Spoken

**ID:** `litespec/core/principles#follow-intent-where-it-has-spoken`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** Behavior and Implementation MUST follow Intent where Intent states the promise.

**Rationale:** Intent holds the decided promises, so Behavior that fulfills them and code that builds them keep one chain from promise to product. Where Intent states the promise, code that disagrees is the defect, and the fix changes the code.

**Reviewed for:** Which code disagrees with a stated promise? Compare the changed code with the Intent records it serves.

### Treat Code Beyond Silent Intent as Evidence for an Owner Question

**ID:** `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-an-owner-question`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST treat working code that Intent does not cover as evidence for a question to the owner, not as a defect to delete and not as a source to copy into Intent.

**Rationale:** Silent Intent plus working code means a decision nobody recorded: deleting the code destroys behavior users rely on, and copying it quietly into Intent turns code into a promise nobody stated. A question carries the behavior to the owner with the code as evidence, and the answer lands in Intent first.

**Example:** A positivity guard in code that no rule states becomes an owner question citing the guard, and the code stays until the answer lands in Intent.

**Reviewed for:** Which working code outside Intent lacks an owner question carrying it? Look at the owner questions beside the uncovered code.

### Declare Every Edge

**ID:** `litespec/core/principles#declare-every-edge`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST declare every edge between use cases, reactions, jobs, notifications, integration events, and external systems on the record that holds it.

**Rationale:** An edge declared on its record is checked: the builder verifies its target, its timing, and its failure handling. An undeclared edge is the defect, because it runs behavior no record names and no review saw.

**Example:** A use case that reminds a manager declares a sends edge to the notification with its timing and failure handling.

**Reviewed for:** Which edges lack a declaration on their record? Look at the `effects` entries with `runs`, `sends`, `emits`, or `calls` beside the changed records.

### End Every Effect Cycle in a Domain Event

**ID:** `litespec/core/principles#end-every-effect-cycle-in-a-domain-event`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An effect graph MUST NOT hold a cycle except through a domain event with a stated end condition.

**Rationale:** A cycle without an end condition runs until something outside the records stops it, so no reader can state when the work finishes. Routing each cycle through a domain event with a stated end condition names the loop and its exit in the records the graph checks.

**Checked by:** `litespec model check` reports no `effect-loop-invalid` finding on the changed records.

### State Transaction Sharing on Every Inline Edge

**ID:** `litespec/core/principles#state-transaction-sharing-on-every-inline-edge`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An inline edge MUST state whether it shares the caller's transaction.

**Rationale:** Two inline edges that look alike run in one transaction or two, and the difference decides what a failure leaves behind. Stating the sharing on the edge keeps transaction honesty where the edge is declared.

**Example:** An inline runs edge states `sharesTransaction: true`, so a failure in the use case it runs rolls back the caller.

**Checked by:** `litespec model check` reports no `record-invalid` finding on the changed inline edges.

### Confine Atomic Consistency to Synchronous Edges

**ID:** `litespec/core/principles#confine-atomic-consistency-to-synchronous-edges`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** Atomic consistency MUST NOT span an async edge.

**Rationale:** An async edge crosses a commit boundary, so work past it can fail after the caller succeeded. Atomic consistency promises one shared outcome, which holds only while every edge stays synchronous.

**Reviewed for:** Which atomic use cases span an async edge? Look at the consistency and effects fields beside the changed use cases.

### Map Each Engine Step to One Use Case

**ID:** `litespec/core/principles#map-each-engine-step-to-one-use-case`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A saga carried by a workflow engine MUST map each engine step to exactly one use case with its compensation.

**Rationale:** The engine carries the saga while the records state it: one use case per step keeps the promise, the refusal codes, and the undo in the records the checks read. An engine step without its use case runs behavior no record names, and a step without its compensation leaves a failure with no stated undo.

**Example:** An engine step that reimburses an expense maps to the reimburse-expense use case, whose compensation voids the payout.

**Reviewed for:** Which engine steps map to no use case, or to a use case with no compensation? Look at the engine step definitions beside the use case records.

### Record Stricter Edge Rules as Conventions

**ID:** `litespec/core/principles#record-stricter-edge-rules-as-conventions`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A project that keeps a stricter edge rule MUST record it as a cited convention.

**Rationale:** LiteSpec checks declared edges and leaves stricter taste to each project. A stricter rule recorded as a cited convention stays visible and checkable beside the records it constrains, instead of living as habit in a few heads.

**Reviewed for:** Which stricter edge rules lack a cited convention? Look at the project conventions beside the changed records.

### Cover Other Mechanisms with Extensions

**ID:** `litespec/core/principles#cover-other-mechanisms-with-extensions`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A mechanism outside modules, aggregates, domain events, use cases, reactions, and jobs MUST live in an extension.

**Rationale:** LiteSpec favors one set of mechanisms and states that choice openly instead of claiming neutrality. An extension states how its mechanism composes with the core fields, so a project that needs it selects one place that holds the whole contract.

**Reviewed for:** Which mechanisms outside the favored set lack an extension? Look at the selected extensions beside the changed records.

### Keep One Authored Source

**ID:** `litespec/core/principles#keep-one-authored-source`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST author each record once in its home file and derive every other appearance from the model.

**Rationale:** Two homes for one fact become two facts with the next edit: each copy changes on its own schedule while readers guess which one rules. Deriving every other appearance from the model keeps the authored record as the single place a change lands. Completion computed from the model stays true without anyone typing it by hand.

**Reviewed for:** Each fact on the changed pages traces to one authored record, with every other appearance derived from the model.

### Locate Package Versions in the Manifest

**ID:** `litespec/core/principles#locate-package-versions-in-the-manifest`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST resolve the standards-selected stack versions from `litespec.manifest.json`.

**Rationale:** The manifest is the version authority for the stack the standards select, so every consumer resolves the same tested combination. A version pinned in prose or remembered by an agent drifts from that authority with the next manifest change. Consumer dependencies still live in package manifests and lockfiles; only the standards-selected stack versions resolve from `litespec.manifest.json`.

**Checked by:** `litespec model check` fails a project whose stack versions do not resolve from the manifest.

### Prove Scenarios with Tests

**ID:** `litespec/core/principles#prove-scenarios-with-tests`

**Strength:** rule

**Applies to:** artifact: test

**Statement:** A test MUST prove a scenario only by calling its use case and asserting its result.

**Rationale:** A passing test that skips the use case proves nothing about the behavior it claims. Calling the use case and asserting its result ties the proof to the shipped path. A scenario without such a test carries a gap exception with an expiry date, not silent coverage. The [model page](model.md) defines the `proves` link.

**Reviewed for:** Each scenario in the model links a passing test that calls its use case and asserts its result.

### Keep Git as the Decision Record

**ID:** `litespec/core/principles#keep-git-as-the-decision-record`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST treat a record in the repository as decided, with the pull request that changes it as its review.

**Rationale:** A record in git is the decision, and the pull request that changes it is its review. A record nobody wants is deleted, and git keeps who changed what and when. Records carry no history of their own.

**Example:** A feature nobody wants is deleted from the repository; its history stays in git.

**Reviewed for:** Which changed records bypass their pull request? Look at the pull request behind the change.

### Build Only What the Specification States

**ID:** `litespec/core/principles#build-only-what-the-specification-states`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST build what the specification states and nothing more.

**Rationale:** The specification is closed: something that seems missing becomes a question to the owner or a backlog item, never code. Code without a record behind it is a promise nobody stated.

**Example:** An agent that finds no payout rule asks the owner instead of coding a guessed guard.

**Reviewed for:** Which code has no specification record behind it? Compare the changed code with the records it serves.

### Write Every Record About the Software

**ID:** `litespec/core/principles#write-every-record-about-the-software`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** Every record MUST state what the application does, holds, refuses at runtime, or runs on.

**Rationale:** Goals, problems, features, research, and constraints all qualify when they translate into code, results, tests, or infrastructure. Running the company stays out; it enters only as a constraint or as dated research.

**Example:** A goal that translates into payout code and tests qualifies; a sales target does not.

**Reviewed for:** Which records state company business instead of the software? Compare each new record with the code, results, tests, or infrastructure it translates into.

### Compute Completion from Code and Tests

**ID:** `litespec/core/principles#compute-completion-from-code-and-tests`

**Strength:** rule

**Applies to:** artifact: any; conformance: traced

**Statement:** A consumer MUST NOT write completion by hand, because the model computes it from code, tests, and passing scenarios.

**Rationale:** What a reader asks first is how much of a thing exists and is proven. Completion typed by hand turns the plan into a completion report that the next commit falsifies. The [model page](model.md) defines what the model computes.

**Example:** The book shows a use case complete when its handler exists and its scenarios pass, with the model deriving that from the runs.

**Reviewed for:** Which records state their own completion? Compare the record fields with the completion the model computes.

### State the Strength of Every Provision

**ID:** `litespec/core/principles#state-the-strength-of-every-provision`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** Every provision MUST state whether it is a rule, a default, or advice, and where it applies.

**Rationale:** Strength words work only when each one means exactly one thing: an agent tells binding from advisory without a check existing. A check is an aid where one is cheap and sure, never the condition for a provision to exist.

**Reviewed for:** Which provisions leave their strength or scope unstated? Look at the provisions the change adds.

### Keep Tools Small

**ID:** `litespec/core/principles#keep-tools-small`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A command, check, generated file, or package MUST exist only while the owner approves it and something uses it.

**Rationale:** Each command, check, generated file, or package adds maintenance, failure modes, and concepts every later reader learns. A tool nothing uses is removed instead of kept beside its replacement.

**Example:** A check nobody runs is removed with its command.

**Reviewed for:** Which commands, checks, generated files, or packages lack an owner approval or a user? Look at the tooling the change adds beside its users.

### Complete Work Before Merging

**ID:** `litespec/core/principles#complete-work-before-merging`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** What merges MUST be complete.

**Rationale:** A branch may be incomplete; what merges is complete. A record that must stay unreached past a merge carries an exception with `expiresOn`, which parks reachability for that record until it expires.

**Example:** A use case with no endpoint stays on its branch until the endpoint lands or an exception with `expiresOn` parks it.

**Checked by:** `litespec model check` reports unreached records locally and blocks on them in the pull-request check.

### Prove Enforceable Boundaries Mechanically

**ID:** `litespec/core/principles#prove-enforceable-boundaries-mechanically`

**Strength:** rule

**Applies to:** artifact: any; conformance: traced

**Statement:** A consumer MUST prove each enforceable boundary mechanically: a reference, visibility, an architecture test, a behavior test, or a lint rule.

**Rationale:** A boundary stated only in prose holds until the first commit that ignores it. Mechanical proof, such as a reference, a visibility rule, an architecture test, a behavior test, or a lint rule, fails the build or the check the day the boundary breaks. Specifications then explain the decisions that need judgment, while tools guard the constraints that do not. Enforceable boundaries include project and package references, compiler visibility, permission guards, store constraints, and lint rules.

**Example:** A project-reference test proves Domain has no Infrastructure reference.

**Reviewed for:** Each changed enforceable boundary names a reference, test, or lint rule that proves it.

### Require Current Complexity Activation

**ID:** `litespec/core/principles#require-current-complexity-activation`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST add packages, projects, wrappers, background processes, caches, queues, or distributed patterns only for a current documented requirement.

**Rationale:** Each package, project, wrapper, background process, cache, queue, or distributed pattern adds maintenance, failure modes, and concepts every later reader must learn. A current use case, decision, or requirement names the activation criterion that justifies that cost. Preference and possible future need justify nothing, so speculative machinery stays out.

**Reviewed for:** Each added package, project, wrapper, process, cache, queue, or pattern cites a current use case, decision, or requirement.

### Name Boundary Intent

**ID:** `litespec/core/principles#name-boundary-intent`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST use business operation names, specific command and query mediators, and business-action external ports at boundaries.

**Rationale:** Boundaries are where components meet and where future readers orient themselves. Business operation names, specific command and query mediators, and business-action external ports state the purpose at that meeting point. A precise name lets a newcomer place the interaction without opening either side.

**Reviewed for:** Each changed boundary name states its business action.

### Avoid Generic Boundary Names

**ID:** `litespec/core/principles#avoid-generic-boundary-names`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST NOT use generic `bus`, `manager`, `helper`, `processor`, or `service` names when a narrower name exists.

**Rationale:** Names such as `bus`, `manager`, `helper`, `processor`, and `service` say nothing about the business action, so unrelated responsibility collects behind them with every change. The narrower name keeps each boundary to the action it names.

**Example:** A notifier named for post publication names its action; one named only `service` does not.

**Reviewed for:** Each changed boundary name carries the narrowest accurate business name, with no bare `bus`, `manager`, `helper`, `processor`, or `service`.

## Conventions

### Prefer Direct Owned Dependencies

**ID:** `litespec/core/principles#prefer-direct-owned-dependencies`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use a selected framework type directly inside the architecture layer that owns it until an architectural boundary exists.

**Replacement:** A consumer records a wrapping convention naming the architecture layers that use project-owned abstractions.

**Rationale:** Using a selected framework type directly keeps one concept where two would add nothing: the abstraction and what it hides. A project-owned abstraction earns its place for provider replacement, a test boundary, a stable domain concept, or an architecture boundary. Wrapping the framework everywhere else doubles the API the team maintains without isolating any real change.

**Reviewed for:** Each new abstraction names the boundary that justifies it, or the wrapping convention that permits it.

### Prefer Local Code Until Reuse Is Real

**ID:** `litespec/core/principles#prefer-local-code-until-reuse-is-real`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep operation-specific code in its operation folder until two real consumers need the same precise behavior.

**Replacement:** A consumer records a sharing convention naming when operation code moves to shared units.

**Rationale:** Operation-specific code kept beside its operation stays easy to change with the operation. Shared code needs a name and responsibility that stay accurate for both consumers, and the first consumer rarely knows what the second needs. Waiting for two real consumers means the shared unit covers behavior both actually need.

**Reviewed for:** Each new shared unit serves two real consumers under one accurate name, or the sharing convention permits it.

## Reference Example

This informative example demonstrates `litespec/core/principles#prefer-direct-owned-dependencies` and `litespec/core/principles#prefer-local-code-until-reuse-is-real`.

- A query handler uses the selected stack data-access type directly, because no architectural boundary separates the handler from the stack, so no wrapper isolates a change.
- Repository interfaces stay project-owned where the architecture keeps persistence out of the domain layer: the boundary justifies the abstraction.
- A two-line mapping used once stays beside its endpoint until a second consumer needs the same behavior.
