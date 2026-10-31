# Release Stage

## Intent

The release stage tells whether any deployment holds customer data yet, because several provisions assume a running service that the first builds do not have. This page states how a project declares its stage, which provisions the early stage defers and until when, which draft intent reviewed Behavior records may rest on, how transcription from working code ends, and how a recorded decision ends the deferral. A project selects it while no deployment holds customer data, or afterwards to record a past move to production, so relaxed early provisions expire on a date instead of lingering by accident.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `stage` before its first deployment holds customer data, or afterwards to put a past promotion on record. The selection declares the current stage and bounds what the early stage defers. The project names the decision that ends transcription in `adoption.transcriptionEndsWith` in `litespec.project.json`.

## Baseline Relationship

This extension replaces no baseline provision. A `pre-production` consumer defers named provisions through this extension, while the compat extension still governs versioning once selected.

It switches on no record type: the promotion decision is a core record every project requires. A reviewed Behavior record that rests on draft intent is covered here instead of by an exception, as the [model page](../core/model.md) states in its authoring order.

## Standards

### Declare the Release Stage

**ID:** `litespec/ext/stage#declare-the-release-stage`

**Statement:** A consumer MUST declare `pre-production` or `production` as its release stage in `litespec.project.json`.

**Rationale:** The release stage is a project fact, not a departure from any rule, so it lives as data in the `stage` selection: `releaseStage` holds `pre-production` or `production`, while each deferral lives in its own exception record. Without the declared stage, tools and reviewers cannot tell which provisions the project owes today.

**Example:** A consumer with no customer data in any deployment declares the early stage.

```json
{"id": "stage", "criterion": "no deployment holds customer data", "reviewBy": "2026-12-01", "releaseStage": "pre-production"}
```

**Checked by:** `litespec consumer` refuses a `stage` selection with a missing or unknown `releaseStage`.

### Defer Named Provisions Until Promotion

**ID:** `litespec/ext/stage#defer-named-provisions-until-promotion`

**Statement:** A `pre-production` consumer MUST record each deferred provision as a departure exception expiring at promotion, with a `production` consumer carrying no deferred provision.

**Rationale:** An early-stage deferral without a named end becomes permanent by neglect. Each exception names the deferred provision in `covers` with the date its deferral ends in `expiresOn`, and the promotion decision names the same provisions, so promotion closes every deferral at once.

**Example:** Each deferral is a departure exception covering its provision until promotion.

```json
{"type": "exception", "id": "exception/compat-classification-until-promotion", "title": "Compat Classification Until Promotion", "owner": "claimwick-team", "status": "approved", "approvedBy": "claimwick-team", "approvedOn": "2026-10-15", "approvedHash": "9f31ac02", "kind": "departure", "covers": "litespec/ext/compat#classify-breaking-contract-changes", "paths": ["apps/api/**"], "reason": "No consumer reads the contract before the first deployment.", "expiresOn": "2026-12-01"}
```

**Checked by:** `litespec consumer` refuses a deferral exception naming an unknown provision, and any deferral in `production`; `litespec model check` reports `exception-expired` for an exception past its `expiresOn`.

### Promote Through a Recorded Decision

**ID:** `litespec/ext/stage#promote-through-a-recorded-decision`

**Statement:** A consumer MUST approve its promotion to `production` as a decision before its first deployment holds customer data, ending every deferred provision.

**Rationale:** Promotion is the moment relaxed provisions end, and an unrecorded moment never arrives: the relaxed provisions last until somebody notices they never ended. The decision carries its owner, status, decider, and decide-by date like any decision, and approving it before the first deployment holds customer data keeps the data under full rules from day one. The decision also covers the migration and cutover work that moves customer data into production.

**Reviewed for:** The promotion decision under `docs/intent/decisions/` carries approval before the first deployment holds customer data and ends every deferred provision.

### Record Rested-On Draft Intent

**ID:** `litespec/ext/stage#record-rested-on-draft-intent`

**Statement:** A `pre-production` consumer MUST name, in its promotion decision, each draft decision and requirement that a reviewed Behavior record rests on.

**Rationale:** A reviewed Behavior record resting on a draft nobody named looks settled while its foundation can still move. Naming each rested-on draft decision and requirement in the promotion decision keeps the open intent visible beside the records built on it, so promotion settles exactly the intent the Behavior assumed.

**Reviewed for:** The promotion decision names each draft decision and requirement that a reviewed Behavior record rests on.

### Resolve Rested-On Draft Intent at Promotion

**ID:** `litespec/ext/stage#resolve-rested-on-draft-intent-at-promotion`

**Statement:** A consumer MUST approve or retire each named draft intent before its first deployment holds customer data.

**Rationale:** Draft intent covered by the early stage expires at promotion: the promotion decision approves or retires each named draft, so no Behavior record enters production resting on a guess. A draft still open at promotion keeps the promotion unapproved with it.

**Reviewed for:** Each named draft intent carries approval or retirement before the first deployment holds customer data.

### Hold Transcribed Records in Draft

**ID:** `litespec/ext/stage#hold-transcribed-records-in-draft`

**Statement:** A Behavior record with `provenance: transcribed` MUST stay in `draft` until its `fulfills` or `serves` parent is approved.

**Rationale:** A transcribed record carries what working code does, not what anyone decided, so reviewing it would approve behavior with no promise behind it. Holding it in draft until its requirement or journey parent is approved keeps every reviewed record a carried promise. The `transcription-status-invalid` check reports a transcribed record that reached reviewed early.

**Reviewed for:** Each transcribed Behavior record stays in draft until its fulfills or serves parent is approved.

### End Transcription at the Named Decision

**ID:** `litespec/ext/stage#end-transcription-at-the-named-decision`

**Statement:** A consumer MUST name the decision ending transcription in `adoption.transcriptionEndsWith`, and carry no transcribed record after that decision is approved.

**Rationale:** Transcription is adoption work with an end: once the named decision is approved, every Behavior record states decided behavior, and any record still marked transcribed is a promise nobody made. Naming the decision in the project file fixes the end where tools read it. The `transcription-expired` check reports a transcribed record left after the end.

**Reviewed for:** The project file names the transcription-ending decision, and no transcribed record remains after its approval.

## Conventions

None.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
