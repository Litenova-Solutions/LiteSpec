# Agent Operating Protocol

## Intent

An agent is software that reads task context, edits files, and runs checks. This page states the operating rules for an agent working in a LiteSpec project: which records of approved intent to read before editing, which guidance wins when pages overlap, and which changes need approval before it acts. Following them keeps each run inside its authorized scope, preserves unrelated work, and ends it with a report a reviewer can check.

## Concepts

### The Project Model and the Agent Context

A consumer project describes itself with records of fixed types in four record layers: Intent states the promises, Behavior states how the system keeps them, Implementation holds code and technical decisions, and Proof shows the checks. Governance is a group of records beside the four record layers, not a fifth layer. The [model page](model.md) states the record types and their links. An agent queries that model for the records a task touches: `litespec model query <id>` prints one record with its links in both directions, such as a use case with its rules, results, and scenarios.

The generated agent context is the rendered summary of that selection: the smallest record set that covers the request, with the links the builder joined between records. Code, screens, and tests point up to the records they serve. Reading the context before editing starts the run from decided Intent instead of repository guesswork.

### Extensions and Their Boundaries

An extension is a conditional standards bundle a project selects when its criterion holds. A selected project extension applies to the whole project, while a selected local extension applies only inside its own boundary, such as one frontend or one module. The [scope page](scope.md) states how a project selects extensions. Editing inside an extension's boundary without loading the extension follows baseline guidance the extension replaced.

### Guidance Precedence

Guidance overlaps by design: a project exception narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. The precedence order decides which one wins: the project exception first, then the extension, then the deeper page, then the shallower page. A deeper page is one that sits further down the documentation tree, such as a stack page under its area page. The most local approved decision wins, and the broader baseline fills only the gaps it leaves.

### Decider Decisions and Agent Drafts

Agents write every page below the decider's decisions: the decider decides Intent, and agents draft the records that carry those decisions down through Behavior. The Specify translation rule forbids agents from deciding the figures the decider sets on their own; the [model page](model.md) holds the one list of those figures. Each of those stays a draft question with a decider and a date. The [Intent page](intent.md) states the Specify boundary.

### Ballots and Questions

A ballot groups open questions for one decision, by decision identifier or short topic slug, each with a recommendation. The [model page](model.md) states the one question lifecycle: which questions auto-approve and how an aged question is reported (`litespec/core/model#answer-each-question-on-its-record`).

### Transcription Mode

During adoption, agents write Behavior records from working code and mark each one `provenance: transcribed`. A transcribed record stays in draft until the records it fulfills or serves are approved, and transcription mode ends when the decision named in `adoption.transcriptionEndsWith` is approved. The [scope page](scope.md) states the adoption record.

### Side Effects That Need Approval

Some changes reach beyond an ordinary implementation edit: they alter project boundaries, move money or data, or change state outside the repository. Each such change needs the task authorizer's approval before the agent acts, because the task text alone never justifies it. The side-effect provision below lists the changes it covers.

### New Intent in a Running Project

A running project takes new intent through this one sequence. Other pages link to this sequence instead of stating their own order.

1. Scaffold Intent. Load the records the change touches with `dotnet litespec model query <id>`, add or confirm the problem and the feature, then the requirement and the journey step, and open a ballot for every undecided figure, such as a price, a term, a retention period, or a date-dependent default. A question on a tier 1 record waits for its named decider, and a question with a recommendation follows the auto-approval rule the [model page](model.md) states (`litespec/core/model#answer-each-question-on-its-record`).
2. Specify Behavior. Scaffold the use case, its results, and its scenarios with `dotnet litespec new <type> <id>`, `dotnet litespec new result <use-case-id> <name>`, and `dotnet litespec new scenario <use-case-id> <name>`, naming the rules, access, aggregates, consistency, and effects the use case carries.
3. Validate the records. Run `dotnet litespec consumer` and fix every finding before code moves.
4. Choose patterns and scaffold screens. Build the pattern Default and record it with `source: recommended`, and wait for an owner only when the route departs from the Default or no Default exists. Scaffold each screen with `dotnet litespec new screen <entry-point>.<name>`.
5. Implement. Build the Domain change, the handler, the endpoint, the persistence, and the frontend, in that layer order, with `dotnet build` for the backend code. Approval gate: approval lands before this step adds a package, a schema migration, an authentication change, a public API change, production data work, a release, spending, or an outside call that changes state.
6. Write proofs from Then. Write one test for each scenario through the public boundary, taking every expected value only from the scenario Then. Refusals and calculations also run against the example rows held back from the agent that wrote the code, and the suites in the next step run the proofs.
7. Run the gates and the model commands in order. Run the boundary-selected gates and the test suites first, then `dotnet litespec model build`, then `dotnet litespec model check`, then `dotnet litespec model render`.
8. Diff, clear, and batch-approve. Run `dotnet litespec model diff <base> --intent` for the decider review and `dotnet litespec model diff <base> --behavior` for the developer review and the security queue, where `<base>` in a consumer is the consumer default branch, such as `main`, unless it names a consumer folder or another git revision the review names. Clear the security queue, answer the ballots, then approve with `dotnet litespec model review` and `dotnet litespec model approve --batch <selector>`. Batch approval before a final test run is draft review only: the release gate re-reads `verified` after that final run.
9. Write the release record. Create the release record for the immutable artifact and pass the release gate: green check level G checks, every tier 1 draft answered, every scenario verified, and green check level S checks where the reader is qualified.

## Standards

### Select Task Context

**ID:** `litespec/core/agent#select-task-context`

**Statement:** An agent MUST query the project model for the records that cover the request, and read the generated agent context, before editing.

**Rationale:** Without the query, an agent reads either too little and invents the missing rules, or half the repository and buries the task in unrelated guidance. The query returns the smallest record set that covers the request, with the links the builder joined between records, so the agent starts from decided Intent.

**Example:** `litespec model query use-case/spending.submit-expense` returns the use case with its rules, results, and scenarios.

**Reviewed for:** The completion report names the model query that selected the cited records.

### Follow the Local Pattern Before the Model Query

**ID:** `litespec/core/agent#follow-the-local-pattern-before-the-model-query`

**Statement:** A change touching no record that carries `risk` or an approval MAY skip the model query, reading the agent context and neighboring code instead.

**Rationale:** A small edit with no risk and no approval behind it does not need the full record set: the agent context plus the neighboring file already show the approved shape. Requiring a model query for every small change would slow ordinary work without adding information. A change touching a record with `risk` or an approval keeps the full query, because the query shows the tier and the open questions a one-line change would otherwise miss, and it costs seconds.

**Example:** A spelling fix in a draft tier 3 setting that carries no risk reads the agent context and the sibling setting, then edits without querying the model.

**Reviewed for:** The change touches no record carrying `risk` or an approval and names the local pattern it followed.

### Load Applicable Extensions

**ID:** `litespec/core/agent#load-applicable-extensions`

**Statement:** An agent MUST load selected project extensions and selected applicable local extensions before editing code or prose inside the extension's boundary.

**Rationale:** An extension replaces baseline guidance inside its boundary. An agent that edits there without loading the extension follows the baseline the extension replaced, and the change contradicts the project's own selection.

**Example:** A change to the cached catalog reads the selected caching extension before touching the code its boundary covers.

**Reviewed for:** The change names each extension loaded before editing inside its boundary.

### Apply Guidance Precedence

**ID:** `litespec/core/agent#apply-guidance-precedence`

**Statement:** An agent MUST apply the project exception, the extension, the deeper page, and the shallower page in that order.

**Rationale:** Overlapping guidance is normal: a project exception narrows a shared provision, an extension replaces a baseline default, and a stack page refines its area page. Without a fixed order, two agents resolve the same overlap differently and the workspace drifts. The order puts the most local approved decision first, so the broader baseline fills only the gaps it leaves.

**Reviewed for:** The design applies the project exception, the extension, the deeper page, and the shallower page in that order.

### Replace Only Declared Conventions

**ID:** `litespec/core/agent#replace-only-declared-conventions`

**Statement:** An agent MAY apply a replacement for a convention only when the project documentation records that replacement.

**Rationale:** Conventions exist to be replaced, but only through a recorded decision. An undocumented local preference that silently replaces a baseline default looks identical to a mistake, so the next agent fixes it back. The recorded convention keeps the replacement visible and reviewable.

**Reviewed for:** Each convention replacement resolves to a recorded local convention in the project documentation.

### Stop on Unresolved Conflict

**ID:** `litespec/core/agent#stop-on-unresolved-conflict`

**Statement:** An agent MUST stop when applicable provisions disagree without declared precedence and quote both provision IDs and paths.

**Rationale:** When two provisions disagree and no precedence decides, any continuation invents a compromise neither provision approved. Stopping with both provision identifiers and paths hands the decision to a person with exactly the evidence the decision needs.

**Reviewed for:** The stopped work quotes the conflict with both provision IDs and paths.

### Inspect Existing Work Before Editing

**ID:** `litespec/core/agent#inspect-existing-work-before-editing`

**Statement:** An agent MUST read `git status` before editing and preserve user changes, untracked files, and unrelated generated output.

**Rationale:** A working tree can hold the user's own uncommitted edits, untracked files, and generated output from an unrelated task. An agent that edits without looking overwrites or deletes work outside its scope. Reading `git status` first marks that work as untouchable.

**Reviewed for:** `git status` output precedes the edits, with user changes, untracked files, and unrelated generated output untouched.

### Restrict Unapproved Side Effects

**ID:** `litespec/core/agent#restrict-unapproved-side-effects`

**Statement:** An agent MUST NOT act without approval on packages, migrations, authentication, a public API, production data, releases, spending, or outside calls that change state.

**Rationale:** The listed changes alter project boundaries beyond an ordinary implementation edit: they move money or data, open security boundaries, or change state outside the repository. None of them can be judged from the task text alone, so the task authorizer approves each one before the agent acts.

**Reviewed for:** An approval covers each package, migration, authentication, public API, production data, release, spending, or state-changing outside call the change makes.

### Make Coherent Scoped Changes

**ID:** `litespec/core/agent#make-coherent-scoped-changes`

**Statement:** An agent MUST make the smallest coherent change that satisfies the authorized request.

**Rationale:** A change that stops at code and skips its records, tests, or operating evidence leaves the workspace claiming behavior nothing proves. A change that sprawls past the request drags unrelated files into review. The smallest coherent change keeps the records, code, tests, and evidence that belong together in one reviewable unit.

**Reviewed for:** Every changed artifact traces to the authorized request.

### Take Expected Values from the Scenario

**ID:** `litespec/core/agent#take-expected-values-from-the-scenario`

**Statement:** An agent MUST take a test's expected values only from the scenario's Then, never from running the code.

**Rationale:** Expected values read from a passing run restate what the code already does, so the test stays green while behavior drifts. The scenario's Then states the reviewed intent, which makes the test check the behavior rather than copy it.

**Example:** A scenario whose Then names a total of `42.50` keeps `42.50` as the asserted value even when the code returns `42.51`: the mismatch fails the test instead of updating the expectation.

**Reviewed for:** Each test's expected values match the scenario's Then, with none taken from a code run.

### Verify Refusals and Calculations Against Held-Back Examples

**ID:** `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`

**Statement:** An agent MUST verify a refusal or calculation implementation against example rows held back from the agent that wrote it.

**Rationale:** An agent that sees the examples while implementing fits the code to the rows instead of the rule. Held-back rows turn the examples into an independent check: the implementing agent works from the rule and the scenario, and a later check runs the withheld rows against the result.

**Example:** The implementing agent writes a discount calculation from its rule and scenario; a later check runs the withheld example rows against the result.

**Reviewed for:** The check log shows refusal and calculation example rows withheld during implementation and run after.

### Write Pages Below Decided Intent

**ID:** `litespec/core/agent#write-pages-below-decided-intent`

**Statement:** An agent MUST write every page below the decider's decisions.

**Rationale:** The decider decides Intent, and everything below those decisions is drafting: Behavior records, code, and tests that carry the decisions down. An agent that invents promises writes commitments nobody approved, while an agent that waits for dictation wastes the review the decider gives to drafts. Writing below decided Intent keeps authorship with the agent and decisions with the decider.

**Example:** Given an approved requirement for expense reimbursement, an agent drafts the use case, its scenarios, and the handler without asking what the promise means.

**Reviewed for:** Each drafted page traces to a decided Intent record, with no invented promise.

### Escalate Decisions That Specify Forbids

**ID:** `litespec/core/agent#escalate-decisions-that-specify-forbids`

**Statement:** An agent MUST NOT decide a figure the decider sets.

**Rationale:** The Specify translation rule reserves the figures the decider sets for Intent, because each one binds the product to buyers, markets, or law; the [model page](model.md) holds the one list of those figures. An agent that picks a figure silently decides what only the decider approves. Each undecided figure becomes a draft question with a decider, a date, and a recommendation.

**Example:** A setting needs a default queue cap; the agent drafts the question with a recommendation instead of writing the default into the record.

**Reviewed for:** Each figure the decider sets that the change needs sits in a draft question, not in an agent-written decision.

### Raise Questions in Ballots with Recommendations

**ID:** `litespec/core/agent#raise-questions-in-ballots-with-recommendations`

**Statement:** An agent MUST raise each open question in a ballot with a recommendation.

**Rationale:** A question without a recommendation asks the decider to research from nothing, while a recommendation gives a default to accept or correct. Grouping questions in ballots by decision or topic lets each decider answer one set at a time. The [model page](model.md) states which questions wait for explicit approval and which auto-approve (`litespec/core/model#answer-each-question-on-its-record`).

**Example:** Two approval questions land in the "Approval terms" ballot, each with a recommendation and a due date.

**Reviewed for:** Each open question sits in a ballot with a recommendation.

### Mark Transcribed Records During Adoption

**ID:** `litespec/core/agent#mark-transcribed-records-during-adoption`

**Statement:** An agent writing Behavior from code during adoption MUST mark the record `provenance: transcribed`.

**Rationale:** Transcription reverses the normal direction: the record follows working code instead of leading it. The mark tells every reviewer that the record is evidence, not a promise, so it stays in draft until the records it fulfills or serves are approved. When the ending decision is approved, any record still carrying the mark fails, which proves the mode ended cleanly.

**Example:** A handler with no use-case page gains a transcribed draft that waits for its requirement's ballot.

**Reviewed for:** Each record transcribed from code carries `provenance: transcribed` and stays in draft until the records it fulfills or serves are approved.

### Advise Without Gating

**ID:** `litespec/core/agent#advise-without-gating`

**Statement:** An agent MUST report its findings as advice instead of recording approval or review.

**Rationale:** Approval and review belong to people: the decider approves Intent, developers review Behavior, and the security reviewer reads tier 1 access and data where the project names one. An agent that records approval spends authority it was never given. Advice with evidence lets each reviewer decide faster without moving the decision to the agent. A recorded agent reading counts as the tier 1 second reading in a one-person project; the person still approves.

**Reviewed for:** The agent output records findings as advice, with approval and review left to people.

## Conventions

### Read the Full Record Before Changing a Boundary

**ID:** `litespec/core/agent#read-the-full-record-before-changing-a-boundary`

**Default:** Read the full record before generating a file, changing a public boundary, or choosing between patterns.

**Replacement:** A consumer records a boundary-reading convention naming which changes need the full record.

**Rationale:** Generating a file or changing a public boundary from a summary risks baking a paraphrase into a contract. The full record carries the exact fields, links, and wording the new surface matches. A small edit that follows an existing local pattern can rely on the generated agent context instead.

**Reviewed for:** Each file generation or boundary change cites the full record it matched.

### Inspect Local Examples After Standards

**ID:** `litespec/core/agent#inspect-local-examples-after-standards`

**Default:** Inspect neighboring consumer files after loading applicable standards.

**Replacement:** A consumer records an inspection convention naming which neighboring files an agent reads.

**Rationale:** Standards state the required shape, while neighboring files show the names, folders, and idioms this project chose for it. An agent that skips them writes compliant but foreign-looking code that the next edit rewrites.

**Reviewed for:** The change names the neighboring files that shaped it after the standards.

### Keep Agent Working Files Under Agents

**ID:** `litespec/core/agent#keep-agent-working-files-under-agents`

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

**Reviewed for:** Every agent working file sits under `.agents/` or the path the working-files convention names.

## Reference Example

This informative example demonstrates `litespec/core/agent#select-task-context`, `litespec/core/agent#load-applicable-extensions`, `litespec/core/agent#raise-questions-in-ballots-with-recommendations`, and `litespec/core/agent#inspect-local-examples-after-standards`.

A command-handler change for `use-case/spending.submit-expense` queries the model for its use case, reads the generated agent context, and loads the selected extensions whose boundary the change touches. It then reads the neighboring handler for local names and shapes. Open figures go to a ballot with recommendations. It leaves unrelated frontend and container guidance unread.
