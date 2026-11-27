# Agent Operating Protocol

**Stack:** any
**Concern:** workspace

This page states the operating rules for an agent working in a LiteSpec project. It is for every agent run in a LiteSpec project and for every review of an agent change. A run outside its authorized scope rewrites decisions nobody asked it to touch.

## Concepts

### The Project Model and the Agent Context

A consumer project describes itself with records of fixed types in four record layers. Intent states the promises, Behavior states how the system keeps them, Implementation carries code and technical decisions, and Proof shows the checks. Governance is a group of records beside the four record layers, not a fifth layer. The [model page](model.md) states the record types and their links.

An agent queries that model for the records a task touches: `litespec model query <id>` prints one record with its links in both directions, such as a use case with its rules, results, and scenarios. The query output is the task context: the smallest record set covering the request, with the links the builder joined between records. Code, screens, and tests point up to the records they serve.

Reading the query output before editing starts the run from decided Intent instead of repository guesswork.

Every task follows one order: query the model for the records, run `litespec rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code.

### Extensions and Their Boundaries

An extension is a conditional standards bundle a project selects where its criterion holds. A selected project extension applies to the whole project, while a selected local extension applies only inside its own boundary, such as one frontend or one module.

The [scope page](scope.md) states how a project selects extensions. Editing inside an extension boundary without loading the extension follows baseline guidance the extension replaced.

### Guidance Precedence

Guidance overlaps by design: a project waiver narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. The precedence order decides which one wins: the project waiver first, then the extension, then the deeper page, then the shallower page.

A deeper page is one sitting further down the documentation tree, such as a stack page under its area page. The most local decision wins, and the broader baseline fills the gaps it leaves.

### Agents Write Below Decided Intent

The [layers page](layers.md) states the closed specification: the owner decides Intent, and agents write the records below those decisions, through Behavior down to code and tests, as `litespec/core/layers#write-each-layer-from-the-layer-above-it` states.

The Specify rule stops agents from deciding the figures the owner sets on their own. The [Intent page](intent.md) states the Specify boundary. Specify adds the detail the promise implies but leaves unstated: a detail is implied where every faithful fulfillment of the promise needs it and no actor meets it as an outcome, limit, or figure the promise leaves unnamed. Something the upper layer states without implying, or leaves out entirely, turns into a question to the owner with a recommendation or a backlog item, not code, as `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items` states.

### Side Effects That Need an Explicit Task Instruction

Some changes reach beyond an ordinary implementation edit: they move project boundaries, money or data, or change state outside the repository. Each such change needs an explicit task instruction naming the action and its bounds before the agent acts, because the task text alone justifies none of it.

A project grants wider authority in its `AGENTS.md` where it chooses to. The side-effect provision below lists the changes it covers.

### Ask the Tool Which Provisions Apply

`litespec rules <path or record id>` prints the record's contract card first, then the provisions applying to a change, grouped by strength, with project conventions and waivers merged in. Checked provisions collapse into one summary line, since the tools already enforce them. `litespec rules <...> --all` lists each one, and `litespec rules <...> --full` adds rationale and example where a statement is short for the reader.

An agent runs `litespec rules <path>` before editing and answers each judgment provision `litespec rules --since <base>` lists before reporting done, and the completion report names the provisions the agent judged and how.

A convention states a judgment rule. A list of words belongs in the glossary, where the checks read it.

### Git Is the Decision Record

The [principles page](principles.md) states git as the decision record. An agent reads who changed what and when from git, not from record fields.

### Work in Progress

Every link between records holds in every run, and `litespec model check` blocks on reachability locally and in the pull-request check alike.

Work in progress leaving a record unreached carries a gap waiver with `expiresOn` and `closedBy`, parking reachability for that record until it expires.

### New Intent in a Running Project

A running project takes new Intent through one sequence. Other pages link to this sequence instead of stating their own order.

1. Scaffold Intent. Load the records the change touches with `dotnet litespec model query <id>`. Add or confirm the problem and the feature, then the requirement and the flow step. Raise every undecided figure, such as a price, a term, a retention period, or a date-dependent default, to the owner with a recommendation, or place it on the backlog board, as `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items` states.
2. Specify Behavior. Scaffold the use case, its results, and its scenarios with `dotnet litespec new <type> <id>`, `dotnet litespec new result <use-case-id> <name>`, and `dotnet litespec new scenario <use-case-id> <name>`. Name the rules, access, aggregates, consistency, and effects the use case carries.
3. Validate the records. Run `dotnet litespec model check`. Fix every finding before code moves.
4. Load provisions and choose patterns. Run `dotnet litespec rules <path>` for each path the change touches before editing it. Build the pattern Default and record it with `source: recommended`. Wait for an owner only where the route departs from the Default or no Default exists. Scaffold each screen with `dotnet litespec new screen <entry-point>.<name>`.
5. Implement. Build the Domain change, the handler, the endpoint, the persistence, and the frontend, in that default layer order. Use `dotnet build` for the backend code. Approval gate: an explicit task instruction naming the action and its bounds approves before this step adds a package, a schema migration, an authentication change, a public API change, production data work, a release, spending, or an outside call changing state.
6. Write proofs from Then. Write one test for each scenario through the public boundary, taking every expected value from the scenario Then, not from code runs. The scenario Then states the decided Intent, so the test checks the behavior instead of copying it. Refusals and calculations carrying the money risk also run against example rows held back from the agent writing the code, written by a different author and run in CI, as `litespec/core/agent#protect-proofs-from-implementation-edits` states. Other refusals and calculations verify against held-back rows as good practice. The suites in the next step run the proofs.
7. Run the gates and the model commands in order. Run the boundary-selected gates and the test suites first, then `dotnet litespec model check`, then `dotnet litespec model render`.
8. Check the change against its base and merge complete. Run `dotnet litespec model check --since <base>` for the owner review. In a consumer, `<base>` is the consumer default branch, such as `main`, unless it names a consumer folder or another git revision the review names. The report covers every breaking contract change since the base. A branch stays incomplete. What merges is complete. The check blocks on unreached records in every run, and a record staying unreached past a merge carries a gap waiver with `expiresOn` and `closedBy`.
9. Write the release record. Create the release record for the immutable artifact. Pass the release gate: green checks, every scenario proven by a passing test, and a gap waiver with `expiresOn` and `closedBy` for each check held red.

### Findings Are Advice with Evidence

Agent output records findings as advice with evidence, not as decisions. Decisions belong to the owner: the pull request changing a record is its review. An agent recording a decision spends authority it was given nowhere. Advice with evidence lets the owner decide faster without moving the decision to the agent.

Test expected values come only from the scenario Then, not from code runs. Money-risk refusals and calculations run against rows held back from the implementing author, under `proof/withheld/`, written by a different author and run in CI.

## Standards

### Select Task Context

**ID:** `litespec/core/agent#select-task-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST query the project model for the records that cover the request, and read the query output, before changing behavior, access, a contract, or a trace link; a prose-only correction of an existing record needs only the record and its explanation file.

**Rationale:** Without the query, an agent reads too little and invents the missing rules, or reads half the repository and buries the task in unrelated guidance. The query returns the smallest record set covering the request, with the links the builder joined between records, so the agent starts from decided Intent. A prose-only correction of an existing record needs no joined links: the record and its explanation file show the decided shape, and a query slows ordinary work without adding information. Settled by decision/0086-set-one-agent-authority-boundary.

**Example:** `litespec model query use-case/spending.submit-expense` returns the use case with its rules, results, and scenarios. A spelling fix in a setting record reads the record and its explanation file, then edits without querying the model.

**Reviewed for:** Does the completion report name the model query that selected the cited records, or the prose-only correction and the record it read? Check the report beside the cited records.

### Load Applicable Extensions

**ID:** `litespec/core/agent#load-applicable-extensions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST load selected project extensions and selected applicable local extensions before editing code or prose inside the extension's boundary.

**Rationale:** An extension replaces baseline guidance inside its boundary. An agent editing there without loading the extension follows the baseline the extension replaced, and the change contradicts the project's own selection.

**Example:** A change to the cached catalog reads the selected caching extension before touching the code its boundary covers.

**Reviewed for:** Does the change name each extension loaded before editing inside its boundary? Check the change record beside the extensions the project selects.

### Load Applicable Provisions Before Editing

**ID:** `litespec/core/agent#load-applicable-provisions-before-editing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST run `litespec rules <path>` before editing a file and answer each judgment provision `litespec rules --since <base>` lists before reporting done.

**Rationale:** Provisions stay usable only where retrieved by situation: the query prints the record's contract card first, then what governs the change, grouped by strength, with project conventions and waivers merged in. Checked provisions collapse into one summary line that `litespec rules <...> --all` expands, and `litespec rules <...> --full` adds rationale and example where a statement is short for the reader. Settled by decision/0125-deliver-standards-through-five-agent-layers.

**Example:** `litespec rules use-case/spending.submit-expense` prints the use case's contract card first, then the provisions governing the change, grouped by strength.

**Reviewed for:** Does the completion report answer every judgment provision `litespec rules --since <base>` listed? Compare the rules output with the report.

### Keep the Agent Protocol Current

**ID:** `litespec/core/agent#keep-the-agent-protocol-current`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep its agent protocol region and its project-scope skill copies equal to what its LiteSpec release generates.

**Rationale:** The region carries the commands and rules every session loads first. A stale region points agents at an old release, an old selection, or commands that changed, while the tool output stays current. The project-scope skill copies carry the same procedure to tools loading skills on demand, and a stale copy points at the same old release. Settled by decision/0037-generate-the-agent-protocol and decision/0039-ship-a-litespec-skill.

**Example:** Claimwick upgrades its LiteSpec release and runs `litespec model render`. The region in its `AGENTS.md` and its project-scope skill copies match what the new release generates.

**Checked by:** `litespec model check` reports `agent-protocol-drift` (level S) when the region differs from what `render` would write, `agent-protocol-missing` (level S) when `AGENTS.md` holds no region, and `skill-drift` (level S) when a project-scope skill copy differs from what the carried release generates.

### Write Only Rules You Have Seen Hold

**ID:** `litespec/core/agent#write-only-rules-you-have-seen-hold`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST write a rule into the project's agent file only after seeing that rule hold, and a rule about a library's behavior only after running the library.

**Rationale:** A rule in the agent file reads as decided guidance on every later task, so a guess about how a library behaves turns into an instruction every later agent follows. Running the behavior first gives the rule the evidence the agent names, and leaving an unverified rule out keeps the file to what the project knows.

**Example:** Before adding a line to Claimwick's `AGENTS.md` about how its orchestrator starts the stack, an agent runs the start command and sees the hosts come up. It leaves out a line about a library's behavior until it has run that library and seen the result.

**Reviewed for:** Does every rule the agent added to the agent file name the run or observation that showed it hold, with none written from assumption? Look at the added lines beside the session log.

### Apply Guidance Precedence

**ID:** `litespec/core/agent#apply-guidance-precedence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST apply the project waiver, the extension, the deeper page, and the shallower page in that order.

**Rationale:** Overlapping guidance is normal: a project waiver narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. Without a fixed order, two agents resolve the same overlap differently and the workspace drifts. The order puts the most local decision first, so the broader baseline fills the gaps it leaves.

**Reviewed for:** Does the design apply the project waiver, the extension, the deeper page, and the shallower page in that order? Check the design beside the overlapping guidance.

### Replace Only Declared Conventions

**ID:** `litespec/core/agent#replace-only-declared-conventions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST apply a replacement for a convention only when the project documentation records that replacement.

**Rationale:** Conventions exist to be replaced, but only through a recorded decision. An undocumented local preference silently replacing a baseline default reads exactly like a mistake, so the next agent fixes it back. The recorded convention keeps the replacement visible and reviewable.

**Reviewed for:** Does each convention replacement resolve to a recorded local convention? Check the replacements beside the project documentation.

### Scope Every Convention to Files It Governs

**ID:** `litespec/core/agent#scope-every-convention-to-files-it-governs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project convention MUST state an Applies to that matches at least one file in the project tree or one record the project writes.

**Rationale:** `rules` returns a convention only where its Applies to meets the queried path or record. A convention meeting nothing reaches no agent, so its rule stays unread while the project believes it is stated. One match keeps every stated convention live.

**Example:** Claimwick records that every command handler logs its outcome, with `stack: dotnet; artifact: command-handler`. The convention meets each handler file, so `rules` returns it before an agent edits one.

**Checked by:** `litespec model check` reports `convention-unreached` (level G) for a project convention whose Applies to matches no file in the project tree and no record.

### Stop on Unresolved Conflict

**ID:** `litespec/core/agent#stop-on-unresolved-conflict`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST stop when applicable provisions disagree without declared precedence and quote both provision IDs and paths.

**Rationale:** Where two provisions disagree and no precedence decides, any continuation invents a compromise neither provision states. Stopping with both provision identifiers and paths hands the decision to a person with exactly the evidence the decision needs.

**Reviewed for:** Does the stopped work quote the conflict with both provision IDs and paths? Check the handoff beside the disagreeing provisions.

### Inspect Existing Work Before Editing

**ID:** `litespec/core/agent#inspect-existing-work-before-editing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST read `git status` before editing and preserve user changes, untracked files, and unrelated generated output.

**Rationale:** A working tree carries the user's own uncommitted edits, untracked files, and generated output from an unrelated task. An agent editing without looking overwrites or deletes work outside its scope. Reading `git status` first marks that work as untouchable.

**Reviewed for:** Does `git status` output precede the edits, with user changes, untracked files, and unrelated generated output untouched? Check the session log beside the working tree.

### Restrict Unapproved Side Effects

**ID:** `litespec/core/agent#restrict-unapproved-side-effects`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST stay inside its authorized task and take no consequential action beyond it without an explicit task instruction that names the action and its bounds.

**Rationale:** Inside the task, the agent applies an approved standard or decision, drafts records and proposed decisions, implements the change, and reports findings, including a failed check or a conflict between provisions. It approves no product-policy proposal of its own, widens no scope, invents no business value, and takes no consequential outside action beyond the task. Packages, migrations, authentication, a public API, production data, releases, spending, and outside calls changing state each need an explicit task instruction naming the action and its bounds, because none of them follows from the task text alone. Settled by decision/0086-set-one-agent-authority-boundary.

**Example:** A task naming the migration and its scope approves it. A task silent on releases leaves a release unmade.

**Reviewed for:** Does an explicit task instruction naming the action and its bounds cover each consequential action beyond the task, or does the project's `AGENTS.md` grant the wider authority? Check the task text beside the actions taken.

### Make Coherent Scoped Changes

**ID:** `litespec/core/agent#make-coherent-scoped-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST make the smallest coherent change that satisfies the authorized request.

**Rationale:** A change stopping at code and skipping its records, tests, or operating evidence leaves the workspace claiming behavior nothing proves. A change sprawling past the request drags unrelated files into review. The smallest coherent change keeps the records, code, tests, and evidence belonging together in one reviewable unit.

**Reviewed for:** Does every changed artifact trace to the authorized request? Check the changed files beside the task text.

### Exercise Changed Use Cases Through the Running Stack

**ID:** `litespec/core/agent#exercise-changed-use-cases-through-the-running-stack`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Before reporting done, an agent MUST exercise each use case the change affects through the running stack, by the seed, the API walker or a direct call, and state in the completion report what it ran and what it saw, reporting a change verified only by build as unexercised.

**Rationale:** A build and a passing test show that code compiles and a chosen case passes, not that the endpoint, the handler and the screen agree at runtime. A change leaves an event with no handler, a command needing an id no read returns, or a screen reading less than it shows, and none of those fails a build. Running the changed use case on the seeded stack puts the whole path under the report evidence, and saying what ran and what was seen lets the owner tell an exercised change from a build-only one.

**Example:** After changing Claimwick's submit-expense use case, an agent seeds the client-visit scenario, calls the endpoint with Ava's 48.90 EUR fare and reads the approval list back, then reports the request and the returned expense in its completion report. An agent running only `dotnet build` reports the change as unexercised.

**Reviewed for:** Does the completion report name the seed, walker or direct call for each changed use case and what it showed, and does it call a build-only change unexercised? Look at the report beside the changed use cases.

### Protect Proofs from Implementation Edits

**ID:** `litespec/core/agent#protect-proofs-from-implementation-edits`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An agent MUST NOT delete, skip, or weaken a test that proves a scenario, or change its expected values, unless the scenario changed in the same change; it reports a test it believes is wrong instead of editing it.

**Rationale:** A proof changing with the implementation stops proving the decided behavior and starts restating the code. The proof ledger records each scenario's hash and the hash of each test proving it, so `model check --since` reports, as a blocking finding, each scenario whose proving test changed while the scenario stayed put. Settled by decision/0033-agents-never-weaken-proving-tests.

**Example:** A handler change makes a proving test fail. The agent reports the failing test and the mismatch instead of changing the expected value to fit the new result.

**Checked by:** `litespec model check --since <ref>` reports `proof-hash-drift` (level S) for each scenario whose proving test method changed since the base while the scenario did not.

### Name Judged Provisions in the Completion Report

**ID:** `litespec/core/agent#name-judged-provisions-in-the-completion-report`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A completion report MUST name each provision the agent judged and how it judged it.

**Rationale:** The report shows which binding provisions, defaults, and advisory provisions the change met and the judgment for each, so the owner checks the reasoning without re-reading the change. A provision the query returned but the report skips is a judgment nobody checks.

**Example:** The report names each returned provision with its judgment: followed, replaced by a named convention, or parked by a waiver.

**Reviewed for:** Does the completion report name every judged provision with its judgment? Check the report beside the provisions the query returned.

### Raise a Spec Gap Instead of Bending the Code

**ID:** `litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** When a check, a record type, or a provision forces a shape the domain does not have, an agent MUST stop, name the missing concept, and raise it as an owner question with a recommendation or a backlog item instead of writing code around it.

**Rationale:** A check accepting a rate table only as an external system, or a refusal with no record to hold it, pushes the agent to a substitute shape. The substitute compiles and passes the check while the real concept stays unmodelled, so the next agent copies it and the gap spreads. A missing concept is an open need: it travels up as a question to the owner with a recommendation or as a backlog item, and the agent writes no code around it.

**Example:** Claimwick's checks accept the per-diem rate table only as an external system, but the rates are figures the business changes while the system runs. The agent stops, names the missing concept of dated reference data, and raises a question to the owner with a recommendation instead of adding an external port the domain lacks.

**Reviewed for:** Does each forced shape the agent met have a named concept and an owner question with a recommendation or a backlog item, with no code written around it? Check the completion report beside the gap and the code.

### Check Your Own Change Before Handing It Back

**ID:** `litespec/core/agent#check-your-own-change-before-handing-it-back`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Before reporting done, an agent, including a worker under another agent, MUST run the narrowest build and model check for the paths it changed, let the configured analyzers run in that build, exercise each behavior change once as `litespec/core/agent#exercise-changed-use-cases-through-the-running-stack` states, and report each command with its result.

**Rationale:** A worker handing back code without running its own narrow gates returns compile errors and failing checks for the caller to find. The caller then spends its review on mechanical failures instead of the change. Running the gates on the changed paths catches those failures where the context is freshest, and naming each command and result lets the caller see what actually ran.

**Example:** After changing one Claimwick command handler, an agent builds the backend project, runs `litespec model check`, submits an expense through the seeded stack, and lists each command with its result in the completion report.

**Reviewed for:** Does the completion report name the build, model check and analyzers run for the changed paths, each result, and the behavior the agent exercised? Check the report beside the changed paths and the commands it names.

### Follow the Record, Not the Neighbour

**ID:** `litespec/core/agent#follow-the-record-not-the-neighbour`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST shape new code from the record and the provisions, use a neighbouring file only for style, and follow the record and report the neighbour when the two contradict.

**Rationale:** A neighbouring file shows the names and idioms this project chose, but it carries an older or local rule. Copying it spreads one rule into several homes, and the copies drift from the record. The neighbour is not authority: the record is decided Intent, and a contradiction is a defect for the owner to see.

**Example:** A Claimwick handler's neighbour reads a rate from configuration while the rule record cites dated reference data. The agent reads the rate from the cited reference data and reports the neighbour as out of date.

**Reviewed for:** Does each new code shape trace to a record and its provisions, with the neighbour used only for style and each contradiction reported? Check the change beside the cited records and the neighbours it read.

### Review Changed Use Cases from an Independent Packet

**ID:** `litespec/core/agent#review-changed-use-cases-from-an-independent-packet`

**Strength:** advisory

**Applies to:** artifact: any

**Statement:** A change to a use case SHOULD get a semantic review by a reader other than its author, working from the use case packet that holds the record, its judgment provisions, its neighbours and the changed code.

**Deviation:** A change that alters no rule, refusal, permission or calculated value, such as a renamed label, where the author's own model check covers the change.

**Rationale:** The author of a change checks it against what the author meant, so a rule that never fires, a wrong unit, or a screen that skips a permission passes the author's own review. A second reader working from the record and the changed code alone sees the change as the specification states it.

**Example:** After a change to Claimwick's submit-expense handler, `litespec audit semantic --since main` writes the submit-expense packet, and a second agent reads it and reports that the receipt threshold rule is never consulted for foreign-currency expenses.

**Checked by:** `litespec audit semantic` writes the packet of each changed use case and reports `code-unimplemented` for a changed line outside the tests that still holds a stub marker.

## Conventions

### Read the Full Record Before Changing a Boundary

**ID:** `litespec/core/agent#read-the-full-record-before-changing-a-boundary`

**Strength:** default

**Applies to:** artifact: any

**Default:** Read the full record, the record file with its explanation file, before generating a file, changing a public boundary, or choosing between patterns.

**Replacement:** A consumer records a boundary-reading convention naming which changes need the full record.

**Rationale:** Generating a file or changing a public boundary from a summary risks baking a paraphrase into a contract. The full record carries the exact fields, links, and wording the new surface matches. A small edit following an existing local pattern leans on neighboring code instead.

**Reviewed for:** Does each file generation or boundary change cite the full record it matched? Check the change beside the cited records.

### Inspect Local Examples After Standards

**ID:** `litespec/core/agent#inspect-local-examples-after-standards`

**Strength:** default

**Applies to:** artifact: any

**Default:** Inspect neighboring consumer files after loading applicable standards.

**Replacement:** A consumer records an inspection convention naming which neighboring files an agent reads.

**Rationale:** Standards state the needed shape, while neighboring files show the names, folders, and idioms this project chose for it. An agent skipping them writes compliant but foreign-looking code that the next edit rewrites.

**Reviewed for:** Does the change name the neighboring files shaping it after the standards? Check the change beside its neighbors.

### Keep Agent Working Files Under Agents

**ID:** `litespec/core/agent#keep-agent-working-files-under-agents`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep agent working files under `.agents/`, with tracked `skills/` and `records/` folders and ignored `scratch/` and `delegate/` folders.

**Replacement:** A consumer records a working-files convention naming where agents keep working files.

**Rationale:** Agent work with no agreed home lands beside authored records, where it pollutes review, or vanishes with the session, where nobody audits it. The split keeps durable work in `skills/` and `records/` and machine-local work in the ignored `scratch/` and `delegate/` folders.

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

**Rationale:** Many technical choices are already decided: a provision names the shape, the baseline, or the boundary the code follows. A decision restating such a choice splits authority between the provision and the record, and the two drift. Searching first keeps one home for each decided choice.

**Example:** An agent about to record how Claimwick declares integration edges queries `litespec rules` for the subject. The provision declares every edge in a record, so the code follows it and no decision is recorded.

**Reviewed for:** Does any technical decision restate a choice a provision already makes? Compare each decision with the `rules` output for its subject.

## Reference Example

This informative example demonstrates `litespec/core/agent#select-task-context`, `litespec/core/agent#load-applicable-provisions-before-editing`, `litespec/core/agent#load-applicable-extensions`, `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`, and `litespec/core/agent#inspect-local-examples-after-standards`. It follows one handler change from query to completion report.

A command-handler change for `use-case/spending.submit-expense` queries the model for its use case, reads the query output, runs `litespec rules` for the handler path, and loads the selected extensions whose boundary the change touches. It then reads the neighboring handler for local names and shapes. Open figures go to the owner with recommendations, and the completion report names each judged provision with its judgment. It leaves unrelated frontend and container guidance unread.
