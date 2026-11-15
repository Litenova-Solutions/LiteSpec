# Cover the Behavior Reviewers Found Missing

An outside review read one consumer's Behavior records and found behavior LiteSpec could not express: signed-in acts, verified claims, structured rows, replays, unknown outcomes, and more. This decision gives each proved gap a field or a stated pattern.

## Context

Provider claims were recorded as caller inputs a caller could forge. Three operations needing a signed-in person declared an anonymous grant. Row inputs stayed free text. Creating commands stated no replay behavior. An external send that timed out had no outcome between accepted and rejected. Work spanning modules raised completion before every module finished. Sessions and codes stayed valid until a job removed them. Scenarios asserted operations after their own. Reads disclosing personal data carried no risk. The audit record mixed operation, outcome, and denial in one shape.

The same review claimed the consumer's folder layout differed from the standard. That claim was checked against the Placement Table and found wrong, so only the proved gaps below landed.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A field or pattern for each proved gap | Every gap has a stated shape. | Nine shapes land with provisions, checks, examples. |
| Guidance with no fields | Smaller release. | Claims stay forgeable; scenarios overreach. |
| Signed-in and claims only | The forgery holes close. | Replays and completion stay in prose. |

## Decision

`access.grant` takes `signed-in` beside `anonymous`, `self`, and permissions: the operation requires an authenticated person and no permission, refused with a derived `unauthenticated`. An input's `origin` takes `verified-claim`, naming in `verifiedBy` the entry point or external system that validated it, and a caller can never supply such an input. An input takes `of` a shape: a list of rows with typed fields, `required` per field, `minItems`, and `unique` keys.

`idempotency` on a command states how a repeat is recognized and what a recognized replay does: returns the first result, refuses with a named refusal, or acts again. An external operation's failure separates `rejected`, `accepted`, and `unknown`, and the unknown case names its handling: provider deduplication key, reconciliation, or a retry the receiver tolerates.

The process pattern adds its completion rule: work spanning modules raises its completion fact only after every module's part is done, progress lives in a record that outlives the deleted data, and the requesting state blocks new writes. Validity and storage split: a credential is invalid from its expiry, checked when used, and a cleanup job removes it within a stated interval.

A scenario's Then asserts only its own use case's results, state, and raised domain events; an effect of a later operation belongs to a flow test. Each rule boundary named in `exercises` gets its own scenario at the boundary. A query or permission disclosing data carries its risk like a write. The audit extension separates the operation, its outcome, the refusal code, the actor type, and the target, so a validation refusal is never recorded as a denied access.

## Consequences

Workspace erasure is the worked example for cross-module completion: each module's reaction erases its part and reports done, and the workspace completes only when every module has reported. A claim-shaped input with `origin: caller` on a provider-authenticated entry point is reported. A creating command with `idempotency: state` and no recognition rule is reported.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) surveyed the outside practice the new shapes follow. [research/check-coverage-evidence](research/check-coverage-evidence) mapped the unchecked links the new checks cover. The forged claims and anonymous grants were read from live records with high confidence; the folder-layout claim was checked and rejected.

## Revisit Conditions

This decision reopens when a behavior needs a shape the additions cannot carry. It also reopens when a landed addition proves too narrow for its gap.
