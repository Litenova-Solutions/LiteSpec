# Release Stage

**Stack:** any

**Concern:** operations

## Intent

The release stage tells whether any deployment holds customer data yet, because several provisions assume a running service that the first builds do not have. This page states how a project declares its stage, which provisions the early stage defers and until when, and how a recorded decision ends the deferral. A project selects it while no deployment holds customer data, or afterwards to record a past move to production, so relaxed early provisions expire on a date instead of lingering by accident.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `stage` before its first deployment holds customer data, or afterwards to put a past promotion on record. The selection declares the current stage and bounds what the early stage defers.

## Baseline Relationship

This extension replaces no baseline provision. A `pre-production` consumer defers named provisions through this extension, while the compat extension still governs versioning once selected.

It switches on no record type: the promotion decision is a core record every project requires.

## Standards

### Declare the Release Stage

**ID:** `litespec/ext/stage#declare-the-release-stage`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A consumer MUST declare `pre-production` or `production` as its release stage in `.litespec/project.yaml`.

**Rationale:** The release stage is a project fact, not a departure from any rule, so it lives as data in the `stage` selection: `releaseStage` holds `pre-production` or `production`, while each deferral lives in its own exception record. Without the declared stage, tools and reviewers cannot tell which provisions the project owes today.

**Example:** A consumer with no customer data in any deployment declares the early stage.

```json
{"id": "stage", "criterion": "no deployment holds customer data", "releaseStage": "pre-production"}
```

**Checked by:** `litespec model check` reports `project-invalid` for a `stage` selection with a missing or unknown `releaseStage`.

### Defer Named Provisions Until Promotion

**ID:** `litespec/ext/stage#defer-named-provisions-until-promotion`

**Strength:** rule

**Applies to:** artifact: record:exception

**Statement:** A `pre-production` consumer MUST record each deferred provision as a departure exception expiring at promotion, with a `production` consumer carrying no deferred provision.

**Rationale:** An early-stage deferral without a named end becomes permanent by neglect. Each exception names the deferred provision in `covers` with the date its deferral ends in `expiresOn`, and the promotion decision names the same provisions, so promotion closes every deferral at once.

**Example:** Each deferral is a departure exception covering its provision until promotion.

```json
{"type": "exception", "id": "exception/compat-classification-until-promotion", "title": "Compat Classification Until Promotion", "kind": "departure", "covers": "litespec/ext/compat#classify-breaking-contract-changes", "paths": ["apps/api/**"], "reason": "No consumer reads the contract before the first deployment.", "expiresOn": "2026-12-01"}
```

**Checked by:** `litespec model check` refuses a deferral exception naming an unknown provision, and any deferral in `production`; `litespec model check` reports `exception-expired` for an exception past its `expiresOn`.

### Promote Through a Recorded Decision

**ID:** `litespec/ext/stage#promote-through-a-recorded-decision`

**Strength:** rule

**Applies to:** artifact: record:decision

**Statement:** A consumer MUST record its promotion to `production` as a decision before its first deployment holds customer data, ending every deferred provision.

**Rationale:** Promotion is the moment relaxed provisions end, and an unrecorded moment never arrives: the relaxed provisions last until somebody notices they never ended. The decision stands in git before the first deployment holds customer data, so the data sits under full rules from day one. The decision also covers the migration and cutover work that moves customer data into production.

**Reviewed for:** The promotion decision under `.litespec/intent/decisions/` stands before the first deployment holds customer data and ends every deferred provision.

## Conventions

None.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
