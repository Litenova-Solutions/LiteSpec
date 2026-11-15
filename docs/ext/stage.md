# Release Stage

**Stack:** any

**Concern:** operations

The release stage tells whether any deployment holds customer data yet, because several provisions assume a running service that the first builds do not have.

This page states how a project declares its stage, which provisions the early stage defers and until when, and how a recorded decision ends the deferral.

A project selects it while no deployment holds customer data, or afterwards to record a past move to production, so relaxed early provisions expire on a date instead of lingering by accident.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `stage` before its first deployment holds customer data, or afterwards to put a past promotion on record. The selection declares the current stage and bounds what the early stage defers.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type: the promotion decision is a core record every project requires. A `pre-production` consumer defers named provisions through this extension, while the compat extension still governs versioning once selected.

## Standards

### Declare the Release Stage

**ID:** `litespec/ext/stage#declare-the-release-stage`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer MUST declare `pre-production` or `production` as its release stage in `.litespec/project.yaml`.

**Rationale:** The release stage is a project fact, not a waiver from any rule, so it lives as data in the `stage` selection. `releaseStage` holds `pre-production` or `production`, while each deferral lives in its own waiver record. Without the declared stage, tools and reviewers have no way to tell which provisions the project owes today.

**Example:** A consumer with no customer data in any deployment declares the early stage.

```yaml
selectedExtensions:
  - id: stage
    criterion: no deployment holds customer data
    releaseStage: pre-production
```

**Checked by:** `litespec model check` reports `project-invalid` for a `stage` selection with a missing or unknown `releaseStage`.

### Defer Named Provisions Until Promotion

**ID:** `litespec/ext/stage#defer-named-provisions-until-promotion`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** A `pre-production` consumer MUST record each deferred provision as a gap waiver expiring at promotion with `closedBy` naming the promotion decision, with a `production` consumer carrying no deferred provision.

**Rationale:** An early-stage deferral without a named end becomes permanent by neglect. Each waiver carries `kind: gap`, names the deferred provision in `covers`, sets the date its deferral ends in `expiresOn`, and names the promotion decision in `closedBy`, so promotion closes every deferral at once.

**Example:** Each deferral is a waiver covering its provision until promotion.

```yaml
type: waiver
id: waiver/compat-classification-until-promotion
title: Compat Classification Until Promotion
kind: gap
covers:
  - litespec/ext/compat#classify-breaking-contract-changes
paths:
  - apps/api/**
reason: No consumer reads the contract before the first deployment.
exit: Promotion to production ends the deferral and the classification applies.
expiresOn: 2026-12-01
closedBy:
  - decision/0001-promote-to-production
```

**Checked by:** `litespec model check` refuses a deferral waiver naming an unknown provision, and any deferral in `production`; `litespec model check` reports `waiver-expired` for a waiver past its `expiresOn`.

### Promote Through a Recorded Decision

**ID:** `litespec/ext/stage#promote-through-a-recorded-decision`

**Strength:** required

**Applies to:** artifact: record:decision; when: project

**Statement:** A consumer MUST record its promotion to `production` as a decision before its first deployment holds customer data, ending every deferred provision.

**Rationale:** Promotion is the moment relaxed provisions end, and an unrecorded moment never arrives. The relaxed provisions last until somebody notices they never ended. The decision stands in git before the first deployment holds customer data, so the data sits under full rules from day one. The decision also covers the migration and cutover work that moves customer data into production.

**Reviewed for:** Does the promotion decision stand in git before the first deployment holds customer data, with every deferred provision ended? Read the decision records under `.litespec/intent/decisions/` beside the deferrals they end.

## Conventions

None.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
