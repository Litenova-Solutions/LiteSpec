# Agent Operating Protocol

**Stack:** any
**Concern:** workspace

This page states the operating rules for an agent working in a LiteSpec project: which records of decided Intent to read before editing, which provisions govern each change, which guidance wins when pages overlap, and which changes need an explicit task instruction before the agent acts. An agent is software that reads task context, edits files, and runs checks. The protocol keeps each run inside its authorized scope, preserves unrelated work, and ends it with a report the owner can check. It applies to every agent run in a LiteSpec project, and to every review of an agent's change.

## Concepts

### The Project Model and the Agent Context

A consumer project describes itself with records of fixed types in four record layers. Intent states the promises, Behavior states how the system keeps them, Implementation holds code and technical decisions, and Proof shows the checks. Governance is a group of records beside the four record layers, not a fifth layer. The [model page](model.md) states the record types and their links.

An agent queries that model for the records a task touches: `litespec model query <id>` prints one record with its links in both directions, such as a use case with its rules, results, and scenarios. The query output is the task context: the smallest record set that covers the request, with the links the builder joined between records. Code, screens, and tests point up to the records they serve.

Reading the query output before editing starts the run from decided Intent instead of repository guesswork.

Every task follows one order: query the model for the records, run `litespec rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code.

### Extensions and Their Boundaries

An extension is a conditional standards bundle a project selects when its criterion holds. A selected project extension applies to the whole project, while a selected local extension applies only inside its own boundary, such as one frontend or one module.

The [scope page](scope.md) states how a project selects extensions. Editing inside an extension's boundary without loading the extension follows baseline guidance the extension replaced.

### Guidance Precedence

Guidance overlaps by design: a project waiver narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. The precedence order decides which one wins: the project waiver first, then the extension, then the deeper page, then the shallower page.

A deeper page is one that sits further down the documentation tree, such as a stack page under its area page. The most local decision wins, and the broader baseline fills only the gaps it leaves.

### Agents Write Below Decided Intent

The [layers page](layers.md) states the closed specification: the owner decides Intent, and agents write the records below those decisions, through Behavior down to code and tests.

The Specify rule forbids agents from deciding the figures the owner sets on their own; the [Intent page](intent.md) states the Specify boundary. Specify adds the detail the promise implies but leaves unstated: a detail is implied when every faithful fulfillment of the promise needs it and no actor meets it as an outcome, limit, or figure the promise does not name. Something the upper layer neither states nor implies becomes a question to the owner or a backlog item, never code.

### Side Effects That Need an Explicit Task Instruction

Some changes reach beyond an ordinary implementation edit: they alter project boundaries, move money or data, or change state outside the repository. Each such change needs an explicit task instruction that names the action and its bounds before the agent acts, because the task text alone never justifies it.

A project may grant wider authority in its `AGENTS.md`. The side-effect provision below lists the changes it covers.

### Ask the Tool Which Provisions Apply

`litespec rules <path or record id>` lists the provisions that apply to a change, grouped by strength, as one-line statements, with project conventions and waivers merged in. `litespec rules <...> --full` adds rationale and example when a statement is not enough.

An agent runs `litespec rules <path>` before editing and answers each judgment provision `litespec rules --since <base>` lists before reporting done, and the completion report names the provisions the agent judged and how.

A convention states a judgment rule; a list of words belongs in the glossary, where the checks read it.

### Git Is the Decision Record

The [principles page](principles.md) states git as the decision record. An agent reads who changed what and when from git, never from record fields.

### Work in Progress

Every link between records holds in every run, and `litespec model check` blocks on reachability locally and in the pull-request check alike.

Work in progress that leaves a record unreached carries a gap waiver with `expiresOn` and `closedBy`, which parks reachability for that record until it expires.

### New Intent in a Running Project

A running project takes new Intent through this one sequence. Other pages link to this sequence instead of stating their own order.

1. Scaffold Intent. Load the records the change touches with `dotnet litespec model query <id>`. Add or confirm the problem and the feature, then the requirement and the flow step. Raise every undecided figure, such as a price, a term, a retention period, or a date-dependent default, to the owner with a recommendation, or place it on the backlog board.
2. Specify Behavior. Scaffold the use case, its results, and its scenarios with `dotnet litespec new <type> <id>`, `dotnet litespec new result <use-case-id> <name>`, and `dotnet litespec new scenario <use-case-id> <name>`. Name the rules, access, aggregates, consistency, and effects the use case carries.
3. Validate the records. Run `dotnet litespec model check`. Fix every finding before code moves.
4. Load provisions and choose patterns. Run `dotnet litespec rules <path>` for each path the change touches before editing it. Build the pattern Default and record it with `source: recommended`. Wait for an owner only when the route departs from the Default or no Default exists. Scaffold each screen with `dotnet litespec new screen <entry-point>.<name>`.
5. Implement. Build the Domain change, the handler, the endpoint, the persistence, and the frontend, in that default layer order. Use `dotnet build` for the backend code. Approval gate: an explicit task instruction naming the action and its bounds approves before this step adds a package, a schema migration, an authentication change, a public API change, production data work, a release, spending, or an outside call that changes state.
6. Write proofs from Then. Write one test for each scenario through the public boundary, taking every expected value only from the scenario Then. Refusals and calculations that carry the money risk also run against example rows held back from the agent that wrote the code, written by a different author and run in CI. Other refusals and calculations verify against held-back rows as advisory practice. The suites in the next step run the proofs.
7. Run the gates and the model commands in order. Run the boundary-selected gates and the test suites first, then `dotnet litespec model check`, then `dotnet litespec model render`.
8. Check the change against its base and merge complete. Run `dotnet litespec model check --since <base>` for the owner review. In a consumer, `<base>` is the consumer default branch, such as `main`, unless it names a consumer folder or another git revision the review names. The run reports every breaking contract change since the base. A branch may be incomplete; what merges is complete. The check blocks on unreached records in every run, and a record that must stay unreached past a merge carries a gap waiver with `expiresOn` and `closedBy`.
9. Write the release record. Create the release record for the immutable artifact. Pass the release gate: green checks, every scenario proven by a passing test, and a gap waiver with `expiresOn` and `closedBy` for each check held red.

## Standards

### Select Task Context

**ID:** `litespec/core/agent#select-task-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST query the project model for the records that cover the request, and read the query output, before changing behavior, access, a contract, or a trace link; a prose-only correction of an existing record needs only the record and its explanation file.

**Rationale:** Without the query, an agent reads either too little and invents the missing rules, or half the repository and buries the task in unrelated guidance. The query returns the smallest record set that covers the request, with the links the builder joined between records, so the agent starts from decided Intent.

A change that touches behavior, access, a contract, or a trace link always needs that set. A prose-only correction of an existing record needs no joined links: the record and its explanation file show the decided shape, and a query would slow ordinary work without adding information.

**Example:** `litespec model query use-case/spending.submit-expense` returns the use case with its rules, results, and scenarios. A spelling fix in a setting record reads the record and its explanation file, then edits without querying the model.

**Reviewed for:** Does the completion report name the model query that selected the cited records, or the prose-only correction and the record it read? Check the report beside the cited records.

### Load Applicable Extensions

**ID:** `litespec/core/agent#load-applicable-extensions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST load selected project extensions and selected applicable local extensions before editing code or prose inside the extension's boundary.

**Rationale:** An extension replaces baseline guidance inside its boundary. An agent that edits there without loading the extension follows the baseline the extension replaced, and the change contradicts the project's own selection.

**Example:** A change to the cached catalog reads the selected caching extension before touching the code its boundary covers.

**Reviewed for:** Does the change name each extension loaded before editing inside its boundary? Check the change record beside the extensions the project selects.

### Load Applicable Provisions Before Editing

**ID:** `litespec/core/agent#load-applicable-provisions-before-editing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST run `litespec rules <path>` before editing a file and answer each judgment provision `litespec rules --since <base>` lists before reporting done.

**Rationale:** Provisions are only usable when retrieved by situation: the query returns what governs the change, grouped by strength, with project conventions and waivers merged in. `litespec rules <...> --full` adds rationale and example when a statement is not enough. Settled by [decision/0038-narrow-rules-output-per-file-and-change-set](decision/0038-narrow-rules-output-per-file-and-change-set).

**Example:** `litespec rules use-case/spending.submit-expense` returns the provisions that govern the change, grouped by strength.

**Reviewed for:** Does the completion report answer every judgment provision `litespec rules --since <base>` listed? Compare the rules output with the report.

### Keep the Agent Protocol Current

**ID:** `litespec/core/agent#keep-the-agent-protocol-current`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep its agent protocol region and its project-scope skill copies equal to what its LiteSpec release generates.

**Rationale:** The region carries the commands and rules every session loads first. A stale region points agents at an old release, an old selection, or commands that changed, while the tool output stays current. The project-scope skill copies carry the same procedure to tools that load skills on demand, and a stale copy points at the same old release.

The release template plus the project file regenerate the region and the project-scope skill copies in one command, so drift is always fixable. Global copies live outside the project and stay outside the check. Settled by [decision/0037-generate-the-agent-protocol](decision/0037-generate-the-agent-protocol) and [decision/0039-ship-a-litespec-skill](decision/0039-ship-a-litespec-skill).

**Example:** Claimwick upgrades its LiteSpec release and runs `litespec model render`; the region in its `AGENTS.md` and its project-scope skill copies match what the new release generates.

**Checked by:** `litespec model check` reports `agent-protocol-drift` (level S) when the region differs from what `render` would write, `agent-protocol-missing` (level S) when `AGENTS.md` holds no region, and `skill-drift` (level S) when a project-scope skill copy differs from what the carried release generates.

### Apply Guidance Precedence

**ID:** `litespec/core/agent#apply-guidance-precedence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST apply the project waiver, the extension, the deeper page, and the shallower page in that order.

**Rationale:** Overlapping guidance is normal: a project waiver narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. Without a fixed order, two agents resolve the same overlap differently and the workspace drifts. The order puts the most local decision first, so the broader baseline fills only the gaps it leaves.

**Reviewed for:** Does the design apply the project waiver, the extension, the deeper page, and the shallower page in that order? Check the design beside the overlapping guidance.

### Replace Only Declared Conventions

**ID:** `litespec/core/agent#replace-only-declared-conventions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST apply a replacement for a convention only when the project documentation records that replacement.

**Rationale:** Conventions exist to be replaced, but only through a recorded decision. An undocumented local preference that silently replaces a baseline default looks identical to a mistake, so the next agent fixes it back. The recorded convention keeps the replacement visible and reviewable.

**Reviewed for:** Does each convention replacement resolve to a recorded local convention? Check the replacements beside the project documentation.

### Scope Every Convention to Files It Governs

**ID:** `litespec/core/agent#scope-every-convention-to-files-it-governs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project convention MUST state an Applies to that matches at least one file in the project tree or one record the project writes.

**Rationale:** `rules` returns a convention only when its Applies to meets the queried path or record. A convention that meets nothing reaches no agent, so its rule stays unread while the project believes it is stated. One match keeps every stated convention live.

**Example:** Claimwick records that every command handler logs its outcome, with `stack: dotnet; artifact: command-handler`; the convention meets each handler file, so `rules` returns it before an agent edits one.

**Checked by:** `litespec model check` reports `convention-unreached` (level G) for a project convention whose Applies to matches no file in the project tree and no record.

### Stop on Unresolved Conflict

**ID:** `litespec/core/agent#stop-on-unresolved-conflict`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST stop when applicable provisions disagree without declared precedence and quote both provision IDs and paths.

**Rationale:** When two provisions disagree and no precedence decides, any continuation invents a compromise neither provision states. Stopping with both provision identifiers and paths hands the decision to a person with exactly the evidence the decision needs.

**Reviewed for:** Does the stopped work quote the conflict with both provision IDs and paths? Check the handoff beside the disagreeing provisions.

### Inspect Existing Work Before Editing

**ID:** `litespec/core/agent#inspect-existing-work-before-editing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST read `git status` before editing and preserve user changes, untracked files, and unrelated generated output.

**Rationale:** A working tree can hold the user's own uncommitted edits, untracked files, and generated output from an unrelated task. An agent that edits without looking overwrites or deletes work outside its scope. Reading `git status` first marks that work as untouchable.

**Reviewed for:** Does `git status` output precede the edits, with user changes, untracked files, and unrelated generated output untouched? Check the session log beside the working tree.

### Restrict Unapproved Side Effects

**ID:** `litespec/core/agent#restrict-unapproved-side-effects`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST stay inside its authorized task and take no consequential action beyond it without an explicit task instruction that names the action and its bounds.

**Rationale:** Inside the task, the agent applies an approved standard or decision, drafts records and proposed decisions, implements the change, and reports findings, including a failed check or a conflict between provisions. It approves no product-policy proposal of its own, widens no scope, invents no business value, and takes no consequential outside action beyond the task.

Packages, migrations, authentication, a public API, production data, releases, spending, and outside calls that change state each need an explicit task instruction that names the action and its bounds, because none of them can be judged from the task text alone. A project may grant wider authority in its `AGENTS.md`. Settled by [decision/0086-set-one-agent-authority-boundary](decision/0086-set-one-agent-authority-boundary).

**Example:** A task that names the migration and its scope approves it; a task silent on releases leaves a release unmade.

**Reviewed for:** Does an explicit task instruction naming the action and its bounds cover each consequential action beyond the task, or does the project's `AGENTS.md` grant the wider authority? Check the task text beside the actions taken.

### Make Coherent Scoped Changes

**ID:** `litespec/core/agent#make-coherent-scoped-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST make the smallest coherent change that satisfies the authorized request.

**Rationale:** A change that stops at code and skips its records, tests, or operating evidence leaves the workspace claiming behavior nothing proves. A change that sprawls past the request drags unrelated files into review. The smallest coherent change keeps the records, code, tests, and evidence that belong together in one reviewable unit.

**Reviewed for:** Does every changed artifact trace to the authorized request? Check the changed files beside the task text.

### Take Expected Values from the Scenario

**ID:** `litespec/core/agent#take-expected-values-from-the-scenario`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An agent MUST take a test's expected values only from the scenario's Then, never from running the code.

**Rationale:** Expected values read from a passing run restate what the code already does, so the test stays green while behavior drifts. The scenario's Then states the decided Intent, which makes the test check the behavior rather than copy it.

**Example:** A scenario whose Then names a total of `42.50` keeps `42.50` as the asserted value even when the code returns `42.51`: the mismatch fails the test instead of updating the expectation.

**Reviewed for:** Do each test's expected values match the scenario's Then, with none taken from a code run? Check the assertions beside the scenario Thens.

### Verify Refusals and Calculations Against Held-Back Examples

**ID:** `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`

**Strength:** advisory

**Applies to:** artifact: any

**Statement:** An agent SHOULD verify a refusal or calculation implementation against example rows held back from the agent that wrote it.

**Deviation:** A rule whose scenarios already pin every boundary in their expected values.

**Rationale:** An agent that sees the examples while implementing fits the code to the rows instead of the rule. Held-back rows turn the examples into an independent check: the implementing agent works from the rule and the scenario, and a later check runs the withheld rows against the result.

For a calculation or refusal rule that carries the money risk, held-back verification is required, as [Verify Money Calculations and Refusals Against Held-Back Examples](#verify-money-calculations-and-refusals-against-held-back-examples) states. Settled by [decision/0085-bind-proof-to-code-and-outcomes](decision/0085-bind-proof-to-code-and-outcomes).

**Example:** The implementing agent writes a discount calculation from its rule and scenario; a later check runs the withheld example rows against the result.

**Reviewed for:** Does the check log show refusal and calculation example rows withheld during implementation and run after? Check the log beside the implementing change.

### Verify Money Calculations and Refusals Against Held-Back Examples

**ID:** `litespec/core/agent#verify-money-calculations-and-refusals-against-held-back-examples`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST verify a refusal or calculation implementation that carries the money risk against example rows held back from the agent that wrote it.

**Rationale:** Money rules decide amounts a customer pays or receives, so fitting the code to the examples instead of the rule moves money. A different author than the implementing agent writes the held-back rows. They sit under `proof/withheld/`, which the implementing agent's instructions exclude. Their expected values cite the scenario's rule, and they run in CI.

The money risk is the risk on the permission guarding the use case. Settled by [decision/0085-bind-proof-to-code-and-outcomes](decision/0085-bind-proof-to-code-and-outcomes).

**Example:** The implementing agent writes a discount calculation from its rule and scenario; a CI check runs the withheld rows from `proof/withheld/` against the result.

**Reviewed for:** Does the check log show money-risk refusal and calculation rows written by a different author, withheld during implementation, and run after? Check the log beside the withheld rows.

### Protect Proofs from Implementation Edits

**ID:** `litespec/core/agent#protect-proofs-from-implementation-edits`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An agent MUST NOT delete, skip, or weaken a test that proves a scenario, or change its expected values, unless the scenario changed in the same change; it reports a test it believes is wrong instead of editing it.

**Rationale:** A proof that changes with the implementation stops proving the decided behavior and starts restating the code.

The proof ledger records each scenario's hash and the hash of each test that proves it, so `model check --since` can report, as an advisory finding, each scenario whose proving test changed while the scenario did not. Hashing the proving test method rather than its file keeps an unrelated edit in the same file from reporting a change. Settled by [decision/0033-agents-never-weaken-proving-tests](decision/0033-agents-never-weaken-proving-tests).

**Example:** A handler change makes a proving test fail; the agent reports the failing test and the mismatch instead of changing the expected value to fit the new result.

**Checked by:** `litespec model check --since <ref>` reports `proof-hash-drift` as advisory for each scenario whose proving test method changed since the base while the scenario did not.

### Write Pages Below Decided Intent

**ID:** `litespec/core/agent#write-pages-below-decided-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST write every page below decided Intent.

**Rationale:** Intent holds the decided promises, and everything below those decisions is written by agents: Behavior records, code, and tests that carry the decisions down. An agent that invents promises writes commitments nobody decided, while an agent that waits for dictation wastes the reading the owner gives to finished work. Writing below decided Intent keeps authorship with the agent and decisions with the owner.

**Example:** Given a requirement for expense reimbursement, an agent writes the use case, its scenarios, and the handler without asking what the promise means.

**Reviewed for:** Does each written page trace to a decided Intent record, with no invented promise? Check the written pages beside the Intent records.

### Escalate Decisions That Specify Forbids

**ID:** `litespec/core/agent#escalate-decisions-that-specify-forbids`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST NOT decide a figure the owner sets.

**Rationale:** The Specify rule reserves the figures the owner sets for Intent, because each one binds the product to buyers, markets, or law; the [Intent page](intent.md) states the Specify boundary. An agent that picks a figure silently takes a decision that belongs to the owner. Each undecided figure becomes a question to the owner with a recommendation, or a backlog item.

**Example:** A setting needs a default queue cap; the agent raises a question to the owner with a recommendation instead of writing the default into the record.

**Reviewed for:** Does each figure the owner sets that the change needs sit in an owner question or a backlog item, not in an agent-written decision? Check the owner questions and the backlog board beside the change.

### Raise Open Figures to the Owner

**ID:** `litespec/core/agent#raise-open-figures-to-the-owner`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST raise each open figure to the owner with a recommendation, or place it on the backlog board.

**Rationale:** A figure without a recommendation asks the owner to research from nothing, while a recommendation gives a default to accept or correct. Grouping figures by subject lets the owner answer one set at a time. A figure never becomes a silent agent decision or code written on a guess.

**Example:** Two open figures land in one owner question set, each with a recommendation.

**Reviewed for:** Does every open figure have an owner question with a recommendation or a backlog item? Check the owner questions and the backlog board beside the change.

### Name Judged Provisions in the Completion Report

**ID:** `litespec/core/agent#name-judged-provisions-in-the-completion-report`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A completion report MUST name each provision the agent judged and how it judged it.

**Rationale:** The report shows which required provisions, defaults, and advisory provisions the change met and the judgment for each, so the owner checks the reasoning without re-reading the change. A provision the query returned but the report skips is a judgment nobody can check.

**Example:** The report names each returned provision with its judgment: followed, replaced by a named convention, or parked by a waiver.

**Reviewed for:** Does the completion report name every judged provision with its judgment? Check the report beside the provisions the query returned.

### Advise Without Gating

**ID:** `litespec/core/agent#advise-without-gating`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST report its findings as advice with evidence, never as a decision.

**Rationale:** Decisions belong to the owner: the pull request that changes a record is its review. An agent that records a decision spends authority it was never given. Advice with evidence lets the owner decide faster without moving the decision to the agent.

**Reviewed for:** Does the agent output record findings as advice with evidence, with decisions left to the owner? Check the output beside the changed records.

## Conventions

### Read the Full Record Before Changing a Boundary

**ID:** `litespec/core/agent#read-the-full-record-before-changing-a-boundary`

**Strength:** default

**Applies to:** artifact: any

**Default:** Read the full record, the record file with its explanation file, before generating a file, changing a public boundary, or choosing between patterns.

**Replacement:** A consumer records a boundary-reading convention naming which changes need the full record.

**Rationale:** Generating a file or changing a public boundary from a summary risks baking a paraphrase into a contract. The full record carries the exact fields, links, and wording the new surface matches. A small edit that follows an existing local pattern can rely on neighboring code instead.

**Reviewed for:** Does each file generation or boundary change cite the full record it matched? Check the change beside the cited records.

### Inspect Local Examples After Standards

**ID:** `litespec/core/agent#inspect-local-examples-after-standards`

**Strength:** default

**Applies to:** artifact: any

**Default:** Inspect neighboring consumer files after loading applicable standards.

**Replacement:** A consumer records an inspection convention naming which neighboring files an agent reads.

**Rationale:** Standards state the required shape, while neighboring files show the names, folders, and idioms this project chose for it. An agent that skips them writes compliant but foreign-looking code that the next edit rewrites.

**Reviewed for:** Does the change name the neighboring files that shaped it after the standards? Check the change beside its neighbors.

### Keep Agent Working Files Under Agents

**ID:** `litespec/core/agent#keep-agent-working-files-under-agents`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep agent working files under `.agents/`, with tracked `skills/` and `records/` folders and ignored `scratch/` and `delegate/` folders.

**Replacement:** A consumer records a working-files convention naming where agents keep working files.

**Rationale:** Agent work with no agreed home lands beside authored records, where it pollutes review, or vanishes with the session, where nobody can audit it. The split keeps durable work in `skills/` and `records/` and machine-local work in the ignored `scratch/` and `delegate/` folders.

**Example:**

```text
.agents/
  skills/
  records/
  delegate/
  scratch/
```

**Reviewed for:** Does every agent working file sit under `.agents/` or the path the working-files convention names? Check the working files beside the convention.

### Search the Provisions Before Recording a Decision

**ID:** `litespec/core/agent#search-the-provisions-before-recording-a-decision`

**Strength:** default

**Applies to:** artifact: any

**Default:** Query `litespec rules` for the decision's subject, and where a provision makes the choice, follow that provision instead of recording a decision.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Many technical choices are already decided: a provision names the shape, the baseline, or the boundary the code follows. A decision that restates such a choice splits authority between the provision and the record, and the two drift. Searching first keeps one home for each decided choice.

**Example:** An agent about to record how Claimwick declares integration edges queries `litespec rules` for the subject; the provision declares every edge in a record, so the code follows it and no decision is recorded.

**Reviewed for:** Does any technical decision restate a choice a provision already makes? Compare each decision with the `rules` output for its subject.

## Reference Example

This informative example demonstrates `litespec/core/agent#select-task-context`, `litespec/core/agent#load-applicable-provisions-before-editing`, `litespec/core/agent#load-applicable-extensions`, `litespec/core/agent#raise-open-figures-to-the-owner`, and `litespec/core/agent#inspect-local-examples-after-standards`.

A command-handler change for `use-case/spending.submit-expense` queries the model for its use case, reads the query output, runs `litespec rules` for the handler path, and loads the selected extensions whose boundary the change touches. It then reads the neighboring handler for local names and shapes. Open figures go to the owner with recommendations, and the completion report names each judged provision with its judgment. It leaves unrelated frontend and container guidance unread.
