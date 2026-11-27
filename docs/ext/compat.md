# API Compatibility

**Stack:** any

**Concern:** web-api

Some consumers ship on their own schedule: public clients, partner clients, mobile apps, and readers of published events. This page is for API owners whose contract changes land in code they do not control. Sort each change, version the breaking ones, and retire old versions with notice, so those consumers keep working across releases.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

Enable `compat` when any consumer deploys on its own schedule: a public API, a partner client, a mobile client, or a reader of published events. An API whose consumers release together with it stays under the [backend API rules](../core/backend-api.md). A project whose first deployment holds no customer data yet defers these rules through the stage extension.

## Baseline Relationship

This extension replaces no baseline provision. It adds no optional record type.

A domain event leaves the product only as an integration event, as the [behavior rules](../core/behavior.md) state.

### Sort Each Change

Each contract change reaching an independently deployed consumer gets a verdict: breaking, compatible, or needs review. The reviewer records the verdict, so the decision reaches consumers announced instead of silent.

Removal, rename, tightened input, narrowed value, response, status, or authentication changes count as breaking. A consumer depends on each element of the contract: a field it reads, a status it branches on, or a scheme it implements. Removing, renaming, narrowing, or retyping any of them breaks the consumer depending on it.

Removed query output fields, refusal codes, and integration-event payload fields count as breaking for the same reason. Consumers read output fields, branch on refusal codes, and parse event payloads, so a removed field breaks every consumer relying on it while the route stays green. Added enum values and polymorphic subtypes go through consumer review first, since generated clients switch over every value and reject a valid response with no branch for the new case.

Optional fields, endpoints, and query parameters count as compatible when existing callers send and interpret the same values as before. Stable Problem Details codes stay under the same verdict scheme: removing or reassigning a stable code counts as breaking, since consumers branch on codes the way they branch on statuses. An unclear change counts as breaking with consumer review, because a structural comparison proves less than a person judging consumer impact, and a spare review costs less than a broken consumer. Integration-event changes use the same three verdicts, keeping one shared meaning across HTTP responses and events.

### Version, Keep, and Retire

A breaking Behavior change past production travels as a decision stating consumer impact: which consumers break, what they change, and when the old shape goes away. The decision puts the impact where a developer review alone falls short.

Each release stores its OpenAPI document as a retained baseline artifact. The current document gets compared with the supported baseline both locally and in continuous integration: the local run catches a breaking change while rework stays cheap, and the integration run catches it when the local run was skipped. Continuous integration fails a breaking diff unless a new API version or a recorded consumer decision authorizes it, forcing the compatibility decision while versioning stays possible.

Each operation for independent consumers carries a deliberate `operationId`, turning into stable method names in generated clients. Outside shapes the project does not own stay exempt, such as OAuth redirects, OIDC discovery documents, provider webhook receivers, and well-known resources. A compatible change keeps its `operationId`, and a new API version assigns a fresh one for each changed operation, so the retained version keeps its own names. A breaking contract ships behind a new route or a documented media-type version, serving both contracts at once while each consumer moves on its own schedule. The previous version stays available for its declared support window.

Each planned removal announces itself twice: a documented deprecation signal on the operation itself, such as `Deprecation` and `Sunset` headers, and an entry in the consumer `CHANGELOG.md` where teams plan. The versioning decision states affected consumers, deadline, and removal condition before anything goes away, and each deprecation decision records the sunset date with the replacement operation. Breaking event changes ship in a new event version beside the retained one, retiring only after the declared support window with a deprecation notice.

### Errors and Generated Clients

Problem Details type, code, field-error codes, and documented statuses are versioned contract elements. Consumer failure paths branch on them with the least test coverage, so changing them outside versioning breaks the path with the thinnest proof. A new error outcome joins the contract only when existing consumers handle an unknown code safely, keeping old clients working against the new contract.

Each first-party generated client compiles and exercises its changed operations against the changed document. The client compiler and its exhaustive switches catch breaking changes a structural comparison misses.

### Where the Contract Lives

The current source artifact lives at `apps/api/openapi/{ProjectName}.json`, giving source generation, contract review, and the baseline comparison one file to read. Each supported baseline stays beside its contract owner or under an immutable retained-release reference, keeping the comparison target fixed. Local verification and continuous integration share the same OpenAPI diff tool, giving each change one classification in both places. Problem Details error codes stay stable across compatible versions, letting consumer error handling survive routine releases.

## Standards

None.

## Conventions

None.

## Dependencies

Packages: None. When a project introduces an OpenAPI diff tool, it pins the tool version in the manifest.

Related extensions: None.

Guides: None.
