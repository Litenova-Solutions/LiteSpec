# Product Release

## Intent

A product release is the verified behavior a team ships after Intent and Behavior review approve it, with the security queue read where the project names a reviewer. This page states who reviews each diff, how tiers and the veto window work, which checks each changed boundary runs, and what the written release record lists for its artifact. A release that follows it ships only reviewed and proven behavior with recovery, visibility, and delivery evidence attached.

## Concepts

### The Review Flow

Each change flows from the builder through checks into three review views: the Intent diff with its ballots for product people, the Behavior diff for developers, and the security queue, a list to read for the security reviewer where the project names one. The Intent diff shows changes in product words, the Behavior diff shows the records developers judge, and the security queue lists cross access, new outside calls, and new personal or sensitive data classes. The release gate admits the change only when every view it touches is reviewed and every check it selects is green; the security queue is read beside the Behavior diff, not gated.

### Intent Review

Product people review the Intent diff and answer its ballots. Questions on tier 1 records call for explicit approval from the decider, while questions on tier 3 records auto-approve after the veto window with the recommendation recorded as the answer, as `litespec/core/model#answer-each-question-on-its-record` states. Each ballot carries a consequences summary rendered from Behavior as context, outside the approval hash, so Intent stays enough to approve.

### Behavior Review

Developers review the Behavior diff. A tier 1 change ships with passing scenarios and a recorded second reading, and it ships on no self review alone. An agent reading counts as that second reading, with its report kept beside the manifest. Tier 3 changes are sampled, so a developer reads a sample rather than every line.

### The Security Queue

The security reviewer is the role a project may name in `review.securityReviewer` in the project file. The queue lists every change to cross tenant access, every new `calls` edge, and every new personal or sensitive data class. The queue is a list to read beside the Behavior diff, not a gate: a record can be reviewed and still wait in the queue, and the read is recorded beside the change rather than in the status.

### Single Operator Mode

A project with one operator runs the same flow with the veto window doing the waiting. Questions on tier 3 records auto-approve after `decideBy` plus `vetoWindowDays`, recorded as `answeredBy: auto (veto window)` on the question, while questions on tier 1 records wait for explicit approval, as `litespec/core/model#answer-each-question-on-its-record` states. Every unanswered question past its `decideBy` that cannot auto-approve is reported by the `question-expired` check, so nothing silently blocks. A project whose owners list holds one name needs no `--second-reviewer`.

### Batch Review

A project with thousands of drafts is reviewed in batches, never record by record. `litespec model review` computes the review queue from the model: every draft and every reopened record, with its decider, tier, `decideBy`, effective status, the drafts awaiting it, and its evidence (scenario proofs and open findings), grouped into batches. A result or a scenario takes the tier of its use case. A batch is one use case with its results and scenarios, one decision with the records it decides, or the tier 3 drafts of one module and one record type. A reopened record carrying a `risk` value, an `access` grant, or requirement `values` is marked `fullReRead`, because the hash says only that its promise moved, not which part.

`litespec model approve --batch` approves one batch in one operation: it promotes each draft, Intent and governance records to approved and Behavior records to reviewed, stamps its approval data, re-stamps each reopened record in the batch, and writes a batch manifest under `docs/proof/approvals/`. A record already approved with an unchanged hash is listed in the manifest and left as it is. The manifest is generated evidence, never a record. A batch is refused whole when one guard fails. Approving a batch is not shipping it: the release gate still asks for passing tier 1 scenarios, with the security queue read beside the Behavior diff. No record auto-approves: only a question auto-approves after its `decideBy` plus the veto window, as `litespec/core/model#answer-each-question-on-its-record` states, because a question carries a recommendation the window can settle, while a record is a promise that carries a person's name.

The second review of a tier 1 batch is a reading recorded beside the approval, never the approval itself. In a project with one person, an independent agent review whose report the manifest cites stands as that reading, while the approval stays the person's. The report is a page in the `review-report` style under `docs/proof/reviews/`.

### The Release Gate

The gate admits a release when its G checks are green, its questions on tier 1 records are answered, its scenarios are verified, and its S checks are green where the reader is qualified. G checks read records only and block always, S checks read code through a stack reader and block where that reader is qualified, and A checks report without blocking. The [model page](model.md) states the check levels, and the boundary table below states which checks each changed boundary selects. `litespec model check --release` makes the release-readiness findings block, so the gate is a command.

### Agents Advise

Agents draft records, propose answers, and review as advisers. An agent reading counts as the tier 1 second reading, with the reading's report kept beside the manifest; approval stays with the named person.

### Checker Qualification

A stack reader is qualified by fixtures: a known good tree it passes and a known bad tree where it reports each planted defect. Only a qualified reader gates its scope with S checks; elsewhere the same checks report without blocking.

### Verified Behavior and Journey Evidence

Implemented and verified are different claims. A use case is implemented when its handler exists, and verified when every one of its scenarios has a passing proof through the public boundary that is not stale, as the proof words table on the [model page](model.md) states. A journey test proves that connected use cases produce one observable product outcome through the deployed public boundary, catching the wiring, permission, and data defects that narrower suites miss.

### The Release Record

Each release creates one release record for its immutable artifact. The record names the source commit and the immutable artifact reference, lists the included journeys, use cases, and extensions, and attaches the evidence and operating readiness the provisions below require. It stays non-authoritative: specifications own behavior and scenarios, so the record links outward instead of restating them. Release records live under `docs/releases/` unless the project records another location.

### Boundaries Select Checks

Each changed boundary adds the checks that cover its risk. A change that touches several boundaries runs every check each one selects.

| Changed boundary | Checks the change runs |
|:---|:---|
| Backend | The release build and the backend test suites |
| Frontend | Frozen installation, lint, type checking, tests, and the production build |
| Journey | Its journey tests through the deployed boundary |
| Intent or Behavior | G checks with the book and diffs |
| Extension | The verification its extension states |

### Debt, Gaps, and the First Release

Accepted debt never sits as an untracked compromise: it lives as an exception of kind `gap` with an owner and an expiry date, whose findings report until the date and whose `exception-expired` reports after it. The [model page](model.md) states the exception shape. Governance is a group of records beside the four record layers, not a fifth layer.

A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. Before its first production release, the consumer keeps a readiness record instead, with dated rehearsals, green gates, and a rehearsal owner who owns the backup and restore rehearsal.

### The Adopted LiteSpec Release

A consumer adopts one LiteSpec release by installing the `LiteSpec.Cli` tool package at that release version and recording the same version in `reviewedLiteSpecVersion`. Each LiteSpec release publishes the tool package and the site at the release version together. A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together, after reading the contract the new release states.

## Standards

### Keep Unfinished Behavior in Draft

**ID:** `litespec/core/release#keep-unfinished-behavior-in-draft`

**Statement:** A use case with unfinished behavior MUST keep its record in draft.

**Rationale:** Draft status is the only signal that separates planned behavior from released behavior. A use case with unfinished behavior that stands reviewed claims a promise the code does not keep, and release gates count it as shippable.

**Reviewed for:** Which use cases with unfinished behavior stand outside draft? Look at the status on each use case record.

### Review Intent Changes Through the Intent Diff and Ballots

**ID:** `litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots`

**Statement:** Product people MUST review each Intent change through the Intent diff and its ballots.

**Rationale:** The Intent diff shows the change in product words with its consequences as context, and ballots group the questions by decider with their recommendations. Reviewing there keeps the business decision with the people who own it, instead of scattering it across Behavior pages they never open.

**Example:** A threshold change shows one product sentence plus the affected promises, and the decider approves it in the Approval terms ballot.

**Reviewed for:** Which Intent changes shipped without product review of the Intent diff and ballots? Look at the Intent diff and ballot decisions beside each change.

### Review Behavior Changes Through the Behavior Diff

**ID:** `litespec/core/release#review-behavior-changes-through-the-behavior-diff`

**Statement:** Developers MUST review each Behavior change through the Behavior diff.

**Rationale:** The Behavior diff shows the records, scenarios, and refusals the change adds or moves, which is the evidence developers judge. Reviewing there keeps the technical decision with the people who own the behavior, instead of asking product people to judge fields they never write.

**Example:** A two-line cap rule change shows its setting default and its refusal scenario in one Behavior diff for sampling.

**Reviewed for:** Which Behavior changes shipped without developer review of the Behavior diff? Look at the Behavior diff decisions beside each change.

### Require Passing Scenarios and Advisory Review for Tier 1 Behavior

**ID:** `litespec/core/release#require-passing-scenarios-and-advisory-review-for-tier-1-behavior`

**Statement:** A tier 1 Behavior change MUST show passing scenarios and an advisory agent review before it ships.

**Rationale:** Tier 1 records carry money, physical-access, personal data, or access risk, so one passing suite is not enough on its own. Passing scenarios prove the behavior through the public boundary, and the advisory agent review checks the records a tired reviewer skips, with both recorded beside the change. The advisory review counts as the tier 1 second reading.

**Example:** A cross tenant search ships only after its scenarios pass and the agent advice on its grant is recorded.

**Reviewed for:** Which tier 1 Behavior changes lack passing scenarios or recorded agent advice? Look at the scenario proofs and advice beside each tier 1 change.

### Refuse Self Review Alone for Tier 1 Behavior

**ID:** `litespec/core/release#refuse-self-review-alone-for-tier-1-behavior`

**Statement:** A tier 1 Behavior change MUST NOT ship on self review alone.

**Rationale:** A reviewer who wrote the change reads what they meant rather than what the records say, which is where tier 1 risk hides. A second reviewer beside the author keeps one pair of eyes from being the whole gate. A project whose owners list holds one name needs no second reviewer.

**Example:** An author who drafts a money rule waits for another developer read before the change leaves the gate. In a project with one person, an independent agent review whose report the batch manifest cites stands as that read.

**Checked by:** `litespec model approve --batch` refuses a batch holding a tier 1 record when `--second-reviewer` is missing or names the approver, or names `agent` with no `--review-report`, unless the owners list holds one name, and records the second review in the batch manifest.

### Sample Tier 3 Behavior Reviews

**ID:** `litespec/core/release#sample-tier-3-behavior-reviews`

**Statement:** Tier 3 Behavior changes MUST face sampled developer review.

**Rationale:** Tier 3 records carry no listed risk, so reading every line costs review time without matching risk. Sampling reads a share of tier 3 changes in full, which catches drift while letting routine work flow.

**Example:** A wording fix and a setting default move through the gate after the sampled read passes.

**Reviewed for:** Which tier 3 samples lack a recorded developer read? Look at the sampled Behavior diffs beside the gate.

### Compute the Review Queue from the Model

**ID:** `litespec/core/release#compute-the-review-queue-from-the-model`

**Statement:** The review queue MUST be computed from the model, listing each draft and each reopened record with its decider, tier, decideBy, effective status, the drafts awaiting it, and its evidence, grouped into batches.

**Rationale:** A queue kept by hand drifts from the records the moment one is added, and a flat list sorted by date says nothing when thousands of drafts share one date. Computing the queue from the model keeps it true on every run, and ordering it by what waits on each batch puts the decisions others need first.

**Example:** When Claimwick drafts `use-case/spending.split-expense` with money risk, `litespec model review --tier 1` lists its batch with its results and scenarios, its scenario proofs, and the drafts awaiting it.

**Checked by:** `litespec model review --format json` writes each batch and each record with the fields the [model command page](../tools/model.md) lists, in the order it states.

### Approve Drafts in Bounded Batches

**ID:** `litespec/core/release#approve-drafts-in-bounded-batches`

**Statement:** A batch approval MUST cover one use case with its results and scenarios, one automation with the scenarios on its page, one decision with the records it decides, one tier 1 record no use case, automation or decision holds, on its own, or the tier 3 drafts of one module and one record type, where an Intent record's module is its identifier home, or for a journey the goal it serves, so an open question holds back only the records of its own home.

**Rationale:** One signature over hundreds of unread records is a rubber stamp. Bounding a batch to what one person can read in one sitting keeps each approval a decision: a tier 1 use case is read with the results and scenarios that state its promise, a decision with what it settles, and tier 3 drafts in sets small enough to sample.

**Example:** `litespec model approve --batch use-case/spending.split-expense --by Cara --second-reviewer Ben` reviews the split-expense use case with its results and scenarios in one act.

**Checked by:** `litespec model approve --batch` accepts only a `use-case/`, `decision/`, or `<module>/<type>` selector, and leaves tier 1 drafts out of a module and type batch.

### Hold Only the Blocked Records of a Type Batch

**ID:** `litespec/core/release#hold-only-the-blocked-records-of-a-type-batch`

**Statement:** A module and type batch MUST approve every record whose guards pass and hold each record whose guard fails, by name and with that guard.

**Rationale:** A module and type batch gathers unrelated drafts of one kind, so one record waiting on an open question says nothing about its siblings; refusing the whole batch would hold every sibling behind a question that is not theirs. A use case, automation, decision or tier 1 record batch is read as one unit, so it stays approved or refused whole. A held record keeps its status and gets no stamp, and the manifest lists it with the action `held` and its guard, so the next run approves it once the guard passes.

**Example:** The `admin/screen` batch holds twelve screens; two serve a journey step whose journey waits on an open pricing question. The run approves the ten others and lists the two as held, each naming `stays draft behind draft journey/choose-a-plan`.

**Checked by:** `litespec model approve --batch` approves the passing records of a module and type batch, writes each blocked record as `held` with its guard in the manifest, and refuses a module and type batch only when none of its records passes.

### Refuse a Batch Without a Named Approver

**ID:** `litespec/core/release#refuse-a-batch-without-a-named-approver`

**Statement:** A batch approval MUST refuse the whole batch unless `--by` names a person from the project's owners.

**Rationale:** A record is a promise, so every approval carries the name of the person who made it, and `litespec/core/model#approve-tier-1-records-by-a-person` requires it outright for tier 1. A batch that falls back to a decider, an unknown, or the veto window would let a signature nobody gave cover the records it promotes.

**Example:** Approving the split-expense batch with no `--by` is refused, and nothing is written.

**Checked by:** `litespec model approve --batch` exits 1, writes nothing, and names each record it would change when `--by` is missing, automatic, or no owner.

### Refuse a Batch Holding an Open Question

**ID:** `litespec/core/release#refuse-a-batch-holding-an-open-question`

**Statement:** A batch approval MUST refuse the whole batch when any record in it carries an open question.

**Rationale:** `litespec/core/model#hold-approval-behind-open-questions` holds a record back while a question on it waits for its decider. A batch must not approve around that rule, or a signature would settle the question nobody answered.

**Example:** A split-expense batch whose use case still asks who approves a split across cost centers is refused until the question is answered.

**Checked by:** `litespec model approve --batch` exits 1, writes nothing, and names each record with an open question.

### Refuse a Record Whose Effective Status Stays Draft

**ID:** `litespec/core/release#refuse-a-record-whose-effective-status-stays-draft`

**Statement:** A batch approval MUST refuse the whole batch when a record in it would keep the effective status draft, because a decision deciding it, a requirement it fulfills, or, for a transcribed record, a journey it serves stays draft and is not earlier in the same batch.

**Rationale:** Effective status already says a record cannot hold while what it rests on is undecided, and a transcribed record waits for its Intent. Approving it in a batch anyway would stamp a promise that still reads draft everywhere the model shows it. Allowing the record it rests on earlier in the same batch lets one decision and what it settles be approved together, in authority order.

**Example:** A batch holding a transcribed `use-case/spending.split-expense` that fulfills a draft requirement is refused until the requirement is approved or a decision batch approves both, the requirement first.

**Checked by:** `litespec model approve --batch` exits 1, writes nothing, and names each record with the draft record it rests on.

### Write a Manifest for Every Batch Approval

**ID:** `litespec/core/release#write-a-manifest-for-every-batch-approval`

**Statement:** Every batch approval MUST write a generated manifest under `docs/proof/approvals/` naming its records, approver, second reviewer, date, approval hashes, sample, scenario proof state, and open findings.

**Rationale:** Approval data on each record says who approved it, but not what else was approved in the same act or what the evidence showed then. The manifest keeps that evidence beside the records as Proof, so a later reader can tell reviewed from verified and see which records a signature covered, without a review record type that would duplicate the approval data. An agent second reading writes its report under `docs/proof/reviews/` for the same reason, and the manifest cites it: a report records what its reader saw when the review ran, so a record renamed or removed later is history in it, not a broken link.

**Example:** Reviewing the split-expense batch on 2026-11-20 writes `docs/proof/approvals/2026-11-20-use-case-spending-split-expense.json`, which records that its scenarios were confirmed but not yet run.

**Checked by:** `litespec model approve --batch` writes the manifest named and shaped as the [model command page](../tools/model.md) states, in the same operation that promotes the records, and refuses the batch when it cannot; `litespec model check` reads no citation in a page under `docs/proof/reviews/`.

### Draw Each Tier 3 Sample from the Sample Hash

**ID:** `litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash`

**Statement:** The sample of a tier 3 batch MUST be drawn from each record's sample hash at the project's `review.sampleShare`, and recorded in the batch manifest.

**Rationale:** A sample chosen by the reviewer drifts toward the records that look easy. Drawing it from the sample hash makes it deterministic: anyone can derive the same sample later from the same records, and a changed record moves in or out of the sample by its content, not by choice.

**Example:** With `review.sampleShare` at 0.1, a tier 3 batch of forty Claimwick rules samples the four rules with the lowest sample hashes.

**Checked by:** `litespec model approve --batch --dry-run` prints the sample, and the batch manifest records it.

### Name the Security Reviewer in the Project File

**ID:** `litespec/core/release#name-the-security-reviewer-in-the-project-file`

**Statement:** A project MAY name its security reviewer in `review.securityReviewer` in the project file.

**Rationale:** The security queue calls for one reader for cross access, new outside calls, and new personal or sensitive data classes. Naming the role in the project file tells every diff where to route the queue, instead of leaving queued access waiting for whoever notices it. The queue is a list to read beside the Behavior diff, not a gate.

**Example:** A project file carrying `"securityReviewer": "cara"` routes the queue to Cara until someone else holds the role.

**Reviewed for:** Which security queues lack a named reader where the project wants one? Look at `review.securityReviewer` beside the behavior diff.

### Review the Security Queue Separately from Reviewed

**ID:** `litespec/core/release#review-the-security-queue-separately-from-reviewed`

**Statement:** Each security queue item MUST be read separately from reviewed status.

**Rationale:** Reviewed status says developers accepted the Behavior, while the queue asks whether cross access, a new outside call, or a new personal or sensitive data class is safe to ship. Judging the queue inside reviewed status would let a developer sample approve access risk, so the security reviewer reads each item beside the change where the project names one. The queue is a list to read, not a gate.

**Example:** A reviewed staff search with cross scope waits in the queue until the security reviewer reads the grant.

**Reviewed for:** Which cross, calls, or data class changes shipped without the queue being read? Look at the security queue beside the Behavior diff.

### Verify Connected Journeys

**ID:** `litespec/core/release#verify-connected-journeys`

**Statement:** A completed journey MUST connect verified use cases to one observable outcome through a deployed public boundary.

**Rationale:** Use cases that pass in isolation can still fail together: wiring, permissions, and data cross boundaries the isolated tests never touch. A journey test that runs verified use cases to one observable outcome through the deployed public boundary proves the connected path customers actually take.

**Reviewed for:** Which completed journeys lack a passing test through the deployed boundary? Look at the journey tests beside each included journey.

### Run Boundary-Selected Checks

**ID:** `litespec/core/release#run-boundary-selected-checks`

**Statement:** A release change MUST run every check selected by its changed backend, frontend, journey, Intent, Behavior, and extension boundaries.

**Rationale:** Each boundary carries risks the others cannot see: frontend checks say nothing about migrations, and backend suites say nothing about rendered screens. Running every check the changed boundaries select keeps each change evidenced where its risk lives. A narrow category that passes alone certifies only part of the change.

**Reviewed for:** Which changes lack a passing result for a check their boundaries select? Look at the change report beside the boundary table.

### Run Backend Release Checks

**ID:** `litespec/core/release#run-backend-release-checks`

**Statement:** A backend change MUST pass the release build and the backend test suites.

**Rationale:** The release build compiles the solution in release mode, catching the conditional and optimization-gated breaks a debug build hides. The architecture, integration, and acceptance suites the size requires prove architecture layering, wiring, and behavior in turn. Skipping any of them ships backend code the project's own suites do not fully prove.

**Reviewed for:** Which backend changes lack a passing release build or suite result? Look at the build and suite reports beside the change.

### Run Frontend Release Checks

**ID:** `litespec/core/release#run-frontend-release-checks`

**Statement:** A frontend change MUST run frozen installation, lint, type checking, tests, and production build checks.

**Rationale:** Each step guards a different failure: frozen installation pins dependencies, lint and type checking catch static defects, tests prove behavior, and the production build proves the shipped bundle compiles. A frontend that skips one arrives at release with that whole class of defect unchecked.

**Reviewed for:** Which frontend changes lack a passing install, lint, type, test, or build result? Look at the frontend reports beside the change.

### Run Journey Checks

**ID:** `litespec/core/release#run-journey-checks`

**Statement:** A journey change MUST run its journey tests.

**Rationale:** A journey change alters the connected path, so only a journey test through the deployed public boundary can prove it still reaches its outcome. Narrower suites keep passing while the journey itself breaks.

**Reviewed for:** Which journey changes lack passing journey tests? Look at the journey reports against the deployed boundary.

### Run Consistency Checks

**ID:** `litespec/core/release#run-consistency-checks`

**Statement:** An Intent or Behavior change MUST pass the G checks with the book and diffs.

**Rationale:** G checks read records only: shape, references, links, drafts, coverage, and the Behavior rules the check list names. Without them, a renamed record or a moved file silently breaks the references other pages rely on, and the book and diffs render a graph that no longer holds. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

**Checked by:** `litespec model check` exits zero with no G finding.

### Run Extension Checks

**ID:** `litespec/core/release#run-extension-checks`

**Statement:** A selected project or applicable local extension MUST run its stated verification when affected.

**Rationale:** An extension adds behavior, tests, and operating surface beyond the baseline. Its stated verification is the only evidence written for that surface, so skipping it releases extended behavior the baseline checks never examine.

**Reviewed for:** Which affected extensions lack their stated verification evidence? Look at the extension evidence beside the change.

### Regenerate Application Contracts

**ID:** `litespec/core/release#regenerate-application-contracts`

**Statement:** A source change MUST regenerate OpenAPI and frontend API types when it changes their source contract.

**Rationale:** OpenAPI and frontend API types derive from a source contract. A source change that leaves the committed copies stale ships clients built against a contract the server no longer honors.

**Reviewed for:** Which committed contracts differ from a fresh generation? Look at the contract diff beside the source change.

### Keep Generated Contracts Stable

**ID:** `litespec/core/release#keep-generated-contracts-stable`

**Statement:** A generated application contract MUST be stable and exclude timestamps, machine paths, and nondeterministic ordering.

**Rationale:** Timestamps, machine paths, and unstable ordering make every regeneration look like a behavior change. Deterministic output keeps contract diffs reviewable, so a reviewer sees only the changes that matter.

**Reviewed for:** Which contract diffs hold timestamps, machine paths, or unstable order? Look at the generated diff beside each contract.

### Meet Release Readiness Gates

**ID:** `litespec/core/release#meet-release-readiness-gates`

**Statement:** A release MUST show green G checks, answered questions on tier 1 records, verified scenarios, and green S checks where the reader is qualified.

**Rationale:** The four parts prove the four risks are closed: G checks prove the record graph holds, answered questions on tier 1 records prove no high-risk question is open, verified scenarios prove each behavior through the public boundary, and S checks prove the code matches the records where a qualified reader exists. A release missing any part ships with that risk unjudged. `litespec model check --release` makes the release-readiness findings block, so the gate is a command.

**Example:** A reimbursement release shows green G checks, no open questions on tier 1 records, every scenario verified, and green S checks on the .NET reader.

**Checked by:** `litespec model check --release` blocks on `use-case-unverified` and on unanswered questions on tier 1 records.

### Advise Without Gating as an Agent

**ID:** `litespec/core/release#advise-without-gating-as-an-agent`

**Statement:** A recorded agent reading MUST count as the tier 1 second reading.

**Rationale:** One-person teams cannot staff a second person, so a recorded agent reading stands as the tier 1 second reading. The reading stays advice: the person approves, and the agent only reads, with the reading's report kept beside the manifest. Approval and review come from the named people and roles.

**Example:** An agent review of a tier 1 grant is recorded as the second reading beside the person's approval, and the report is kept beside the manifest.

**Reviewed for:** Which tier 1 approvals lack a recorded second reading beside the person approval? Look at the second readings and reports beside each approval.

### Qualify the Checker with Known Good and Known Bad Fixtures

**ID:** `litespec/core/release#qualify-the-checker-with-known-good-and-known-bad-fixtures`

**Statement:** A stack reader MUST pass its known good and known bad fixtures before it gates with S checks.

**Rationale:** An unqualified reader blocks on noise or passes on silence, and each mistake trains the team to ignore its findings. Passing a known good tree and reporting each planted defect in a known bad tree proves the reader sees what it claims before it blocks any change.

**Example:** The .NET reader passes the clean tree and reports the planted undeclared write before its S findings block.

**Reviewed for:** Which gating S readers lack passing fixture runs? Look at the fixture results beside the stack reader.

### Apply Release Access Controls

**ID:** `litespec/core/release#apply-release-access-controls`

**Statement:** A release with restricted access MUST provide authentication and authorization.

**Rationale:** Restricted behavior without authentication accepts callers nobody identified, and without authorization it accepts actions nobody permitted. Both decisions call for a trusted identity and a declared permission before the behavior ships.

**Reviewed for:** Which restricted release paths lack authentication or authorization? Look at the guards on each restricted path.

### Provide Release Data Recovery

**ID:** `litespec/core/release#provide-release-data-recovery`

**Statement:** A release MUST provide repeatable schema creation or upgrade, backup instructions, and a tested restore.

**Rationale:** A product that cannot recreate its schema, back up its data, and restore from backup holds customer data it cannot recover. Repeatable setup plus a tested restore turns recovery from a hope into a rehearsed procedure.

**Reviewed for:** Which releases lack repeatable schema setup, backup instructions, or a tested restore? Look at the recovery evidence beside the release.

### Provide Release Operating Visibility

**ID:** `litespec/core/release#provide-release-operating-visibility`

**Statement:** A release MUST provide liveness, readiness, trace-correlated diagnostics, and secrets outside source control.

**Rationale:** Operators cannot keep a product they cannot see: liveness and readiness signals drive restarts and traffic, and trace-correlated diagnostics connect a failure to its cause. Secrets stay outside source control because a committed secret is a published one.

**Reviewed for:** Which releases lack health signals, trace diagnostics, or secrets outside source control? Look at the operating evidence beside the release.

### Provide Release Delivery Controls

**ID:** `litespec/core/release#provide-release-delivery-controls`

**Statement:** A release MUST provide CI, tested deployment and rollback paths, current operating limits, and the runbooks those limits name.

**Rationale:** CI, tested deployment and rollback paths, current limits, and the runbooks those limits name make every release repeatable and every failure recoverable. Without them, each deployment improvises and each incident starts from scratch.

**Reviewed for:** Which releases lack current CI, deployment, rollback, limits, or runbooks? Look at the delivery evidence beside the release.

### Record the Release Artifact

**ID:** `litespec/core/release#record-the-release-artifact`

**Statement:** A release MUST create one release record for its immutable artifact.

**Rationale:** Evidence scattered across builds and reports answers no single question. One release record per immutable artifact binds every result to the exact output it proves.

**Reviewed for:** Which releases lack one record naming the immutable artifact? Look at the release records beside each artifact.

### Resolve Release Blockers

**ID:** `litespec/core/release#resolve-release-blockers`

**Statement:** A release MUST settle every open question on a tier 1 record and external claim it depends on, or exclude the affected behavior from every owning record.

**Rationale:** An open question on a tier 1 record or an unverified external claim behind released behavior is a promise with unsettled terms. Settling each one, or excluding the affected behavior from every owning record, keeps the Intent and release records agreeing on what shipped. Questions on tier 3 records auto-approve after the veto window, and the `question-expired` check reports every question that cannot auto-approve, so undecided work stalls no release. `litespec/core/model#answer-each-question-on-its-record` states the question lifecycle.

**Reviewed for:** Which releases depend on an open question on a tier 1 record or external claim without excluding the behavior? Look at the questions and claims beside the owning records.

### Report Verification Exactly

**ID:** `litespec/core/release#report-verification-exactly`

**Statement:** A completion report MUST name exact commands, outcomes, evidence scope, skipped checks, generated differences, scope limits, and covered use cases.

**Rationale:** A vague report lets unrun checks hide among completed ones. Naming exact commands, outcomes, scope, omissions, and limits tells a reviewer precisely what the evidence covers and what it does not.

**Reviewed for:** Which completion reports lack commands, outcomes, scope, omissions, differences, limits, or covered use cases? Look at the report beside the change.

### Identify Immutable Release Artifacts

**ID:** `litespec/core/release#identify-immutable-release-artifacts`

**Statement:** A release record MUST name source commit and immutable artifact reference.

**Rationale:** The source commit names the reviewed input and the artifact reference names the deployed output. Together they bind the record to one build, so evidence from another build cannot stand in for it.

**Reviewed for:** Which release records lack a source commit or artifact reference? Look at the record header beside each artifact.

### List Included Release Behavior

**ID:** `litespec/core/release#list-included-release-behavior`

**Statement:** A release record MUST list included journeys, use cases, and selected applicable extensions.

**Rationale:** The behavior boundary of an artifact is exactly its journeys, use cases, and extensions. Listing them tells operators and reviewers what the artifact contains and, by omission, what it does not.

**Reviewed for:** Which records omit an included journey, use case, or extension? Look at the behavior list on each release record.

### Record Release Evidence

**ID:** `litespec/core/release#record-release-evidence`

**Statement:** A release record MUST list build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results.

**Rationale:** Build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results together show the artifact works and can be operated. A missing kind is a question nobody answered.

**Reviewed for:** Which records lack a result for a listed evidence kind? Look at the evidence list on each release record.

### Record Operating Readiness

**ID:** `litespec/core/release#record-operating-readiness`

**Statement:** A release record MUST list operating limits, alerts, runbooks, known operating conditions, and skipped checks.

**Rationale:** Limits, alerts, runbooks, known conditions, and skipped checks tell the maintainer what to watch, what to run, and what remains unverified after release. Without them, the release hands over an artifact with no operating instructions.

**Reviewed for:** Which records lack limits, alerts, runbooks, conditions, or omissions? Look at the readiness list on each release record.

### Keep Release Records Non-Authoritative

**ID:** `litespec/core/release#keep-release-records-non-authoritative`

**Statement:** A release record MUST NOT restate a specification or copy a scenario.

**Rationale:** Specifications own behavior and scenarios. A release record that restates them becomes a second copy, and the copies drift until nobody knows which one holds. The record links outward instead.

**Reviewed for:** Which release records restate a specification or copy a scenario? Look at the record body beside the linked specifications.

### Record Accepted Debt as an Exception

**ID:** `litespec/core/release#record-accepted-debt-as-an-exception`

**Statement:** Accepted debt MUST live as an exception of kind `gap` with an owner and an expiry date.

**Rationale:** Accepted debt with no owner and no expiry date is forgotten work. An exception of kind `gap` with both turns it into a dated promise whose findings report until the date and whose `exception-expired` reports after it. The [model page](model.md) states the exception shape. One person may approve it.

**Example:** Claimwick ships month-end export without its archive proof, so the debt lives as an exception of kind `gap` with the finance owner and an `expiresOn` of 2027-03-01; `exception-expired` reports it after that date.

**Reviewed for:** Which accepted debts lack an exception of kind `gap` with owner and expiry date? Look at the exceptions beside each debt.

### Keep a First-Release Readiness Record

**ID:** `litespec/core/release#keep-a-first-release-readiness-record`

**Statement:** A consumer before its first production release MUST keep a readiness record with dated rehearsals, green gates, and a rehearsal owner.

**Rationale:** A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. The readiness record instead states what proves the product may take data: dated rehearsals, green gates, and a rehearsal owner who owns the backup and restore rehearsal.

**Reviewed for:** Which pre-release consumers lack dated rehearsals, green gates, or a rehearsal owner? Look at the readiness record beside the project.

### State Each Release as a Complete Record

**ID:** `litespec/core/release#state-each-release-as-a-complete-record`

**Statement:** A release record MUST state its complete behavior and evidence without depending on an earlier release record.

**Rationale:** A release judged from fragments is judged by archaeology: the reader digs through other records to fill the gaps. A complete record lets one reader judge one release from one page, with no upgrade guide filling in.

**Reviewed for:** Which release records depend on an earlier record to be judged? Look at the record alone beside its links.

## Conventions

### Store Release Records Together

**ID:** `litespec/core/release#store-release-records-together`

**Default:** Keep release records under `docs/releases/`.

**Replacement:** A consumer records a release-path convention naming where its release records live.

**Rationale:** One folder for release records tells a maintainer where to find the evidence for any artifact. A record filed elsewhere looks missing while readers open the folder and find nothing there.

**Example:** `docs/releases/2026-08-21.md` records one immutable artifact and its evidence.

**Reviewed for:** Which release records sit outside `docs/releases/` without a recorded release path convention? Look at the release paths beside the convention.

### Treat Check Warnings as Failures

**ID:** `litespec/core/release#treat-check-warnings-as-failures`

**Default:** Treat build and lint warnings as failures unless the build configuration records a narrow suppression with its reason.

**Replacement:** A consumer records a warnings convention naming which warnings fail its build.

**Rationale:** Warnings that nobody reviews accumulate until a real defect hides among them. Failing on warnings forces each one to be fixed or narrowly suppressed with its reason, so a changed compiler, linter, or dependency boundary surfaces immediately.

**Reviewed for:** Which builds pass with unreviewed warnings outside recorded suppressions? Look at the build logs beside the warnings convention.

## Reference Example

This informative example demonstrates `litespec/core/release#verify-connected-journeys`, `litespec/core/release#identify-immutable-release-artifacts`, and `litespec/core/release#record-release-evidence`.

A reimbursement release includes verified submit, approve, and reimburse use cases. The submit-and-reimburse journey passes against the deployed API and database, and the record names the source commit and the image digest as the immutable artifact reference, with the release evidence attached.

This informative example demonstrates `litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots`, `litespec/core/release#review-behavior-changes-through-the-behavior-diff`, and `litespec/core/release#review-the-security-queue-separately-from-reviewed`.

A threshold change shows one Intent sentence in the decider ballot, a two-line Behavior diff for sampling, and no security queue item, so it releases after the ballot and the sample pass.
