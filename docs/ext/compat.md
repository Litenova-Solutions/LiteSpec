# API Compatibility

**Stack:** any
**Concern:** web-api

Some consumers ship on their own schedule: public clients, partner clients, mobile apps, and readers of published events. You cannot deploy them together with the API, so every contract change lands in code you do not control. These rules sort each change into breaking, compatible, or needs-review, put the breaking ones behind a new version, keep supported versions running, and retire old versions with notice, so those consumers keep working across your releases.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

Enable `compat` when any consumer deploys on its own schedule: a public API, a partner client, a mobile client, or a reader of published events. An API whose consumers always release together with it stays under `litespec/backend/api#treat-openapi-as-a-generated-contract`. A project whose first deployment holds no customer data yet defers these rules through the stage extension.

## Baseline Relationship

No baseline provision changes under this extension.

A domain event never leaves the product: any fact an outside consumer reads travels as an integration event, as the [behavior rules](../core/behavior.md) state.

The extension adds no optional record type.

## Standards

### Classify Changes That Reach Independent Consumers

**ID:** `litespec/ext/compat#classify-changes-that-reach-independent-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify each contract change that affects an independently deployed consumer.

**Rationale:** A consumer the project does not deploy still receives every change it ships. Classifying each change decides whether consumers need a new version, a review, or nothing at all. Without a verdict, that decision reaches consumers as a surprise.

**Reviewed for:** Does every contract change that reaches an independently deployed consumer carry a classification? Check the change reviews.

### Classify Breaking Contract Changes

**ID:** `litespec/ext/compat#classify-breaking-contract-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify removal, rename, required input, narrowed value, response, status, or authentication changes as breaking.

**Rationale:** A consumer can depend on any element of the contract: a field it reads, a status it branches on, or an authentication scheme it implements. Removing, renaming, narrowing, or retyping any of them breaks the consumer that depended on it. Calling each such change breaking routes it to versioning instead of silent release.

**Reviewed for:** Does every removal, rename, narrowed value, retyped value, or authentication change carry a breaking verdict? Check the change reviews.

### Treat Removed Behavior Fields as Breaking Changes

**ID:** `litespec/ext/compat#treat-removed-behavior-fields-as-breaking-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify removal of a query output field, a refusal code, or an integration-event payload field as breaking.

**Rationale:** Consumers read query output fields, branch on refusal codes, and parse integration-event payloads, so a removed field breaks every consumer that relied on it while the route stays green. Calling each such removal breaking routes it to versioning and notice instead of silent release.

**Reviewed for:** Does every removed output field, refusal code, or integration-event payload field carry a breaking verdict? Check the change reviews.

### Classify Compatible Additions

**ID:** `litespec/ext/compat#classify-compatible-additions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify an optional field, endpoint, or query parameter as compatible when existing clients retain behavior.

**Rationale:** An optional field, endpoint, or query parameter asks nothing of existing callers: they send and interpret the same values as before. Classifying these as compatible keeps routine growth cheap. Treating them as breaking would version the API for changes nobody has to adopt.

**Reviewed for:** Does every optional addition record why existing behavior stays unchanged? Check the change reviews.

### Review New Enum Values and Subtypes with Consumers

**ID:** `litespec/ext/compat#review-new-enum-values-and-subtypes-with-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST require consumer review for an added enum value or polymorphic subtype.

**Rationale:** Generated clients often switch over every enum value and discriminator case. A new value that the server sends reaches a client with no branch for it, which rejects a valid response. Consumer review checks that each client handles the new case before it ships.

**Reviewed for:** Did every added enum value or discriminator case pass consumer review before it shipped? Check the consumer reviews.

### Preserve Stable Error Codes

**ID:** `litespec/ext/compat#preserve-stable-error-codes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify removal or reassignment of a stable Problem Details code as breaking.

**Rationale:** Consumers branch on stable Problem Details codes the way they branch on statuses. Removing or reassigning a code breaks that branch while the status stays green. Treating the code as breaking keeps error handling stable across versions.

**Reviewed for:** Does every removed or reassigned stable code carry a breaking verdict? Check the change reviews.

### Classify Unclear Changes as Breaking

**ID:** `litespec/ext/compat#classify-unclear-changes-as-breaking`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify an unclear contract change as breaking and require consumer review.

**Rationale:** A structural comparison cannot prove every change compatible: some changes need a person to judge their effect on consumers. Classifying the unclear ones as breaking routes them to consumer review instead of release. The cost of a needless review is smaller than the cost of a broken consumer.

**Reviewed for:** Does every unclear change carry a breaking verdict with consumer review? Check the change reviews.

### Record Breaking Behavior Changes as Decisions

**ID:** `litespec/ext/compat#record-breaking-behavior-changes-as-decisions`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** After production, a breaking Behavior change MUST travel as a decision naming its consumer impact.

**Rationale:** Once the product is live, consumers release against the promises published. A breaking change breaks those promises, so a developer review alone cannot accept it. The decision names the impact instead: which consumers break, what they must change, and when the old shape leaves.

**Reviewed for:** Does every breaking Behavior change past production travel as a decision naming its consumer impact? Check the decisions.

### Retain Release Baselines

**ID:** `litespec/ext/compat#retain-release-baselines`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API owner MUST store each release OpenAPI document as a retained baseline artifact.

**Rationale:** Each release stores its OpenAPI document as an immutable baseline. Later changes compare against it instead of against memory or a mutable file. Without a retained baseline, the project cannot prove what changed between two releases.

**Reviewed for:** Does every release keep a retained OpenAPI baseline? Check the release artifacts.

### Compare Release Contracts Against Baselines

**ID:** `litespec/ext/compat#compare-release-contracts-against-baselines`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The project MUST compare the current OpenAPI document with the selected supported baseline, both locally and in continuous integration.

**Rationale:** The comparison against the supported baseline runs both locally and in continuous integration. The local run catches a breaking change while it is cheap to rework. The continuous integration run catches it when the local run was skipped.

**Reviewed for:** Did every change pass the diff against the baseline both locally and in continuous integration? Check the local runs and the CI runs.

### Block Breaking Changes Without Authorization

**ID:** `litespec/ext/compat#block-breaking-changes-without-authorization`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Continuous integration MUST fail a breaking diff unless a new API version or a recorded consumer decision authorizes it.

**Rationale:** A breaking change needs a compatibility decision while versioning is still possible: a new API version, or a recorded consumer decision that accepts it. Failing the build forces that decision before release. A warning nobody has to answer authorizes nothing.

**Reviewed for:** Did every breaking diff fail unless a new version or a recorded decision authorized it? Check the CI runs.

### Assign Operation IDs

**ID:** `litespec/ext/compat#assign-operation-ids`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** Every operation exposed to an independent consumer MUST have a deliberate `operationId`, except an endpoint whose shape a specification outside the project fixes.

**Rationale:** Generated clients turn each `operationId` into a method name, and tools use it to reference the operation. A deliberate identifier keeps those names stable and meaningful. An endpoint whose shape an outside specification fixes is exempt, because the project does not own that contract.

**Example:** An OAuth redirect endpoint, an OIDC discovery document, a provider webhook receiver, and a well-known resource each follow a shape the other party defines.

**Reviewed for:** Does every operation exposed to an independent consumer carry a deliberate `operationId`, unless an outside specification fixes its shape? Check the OpenAPI document.

### Retain Compatible Operation IDs

**ID:** `litespec/ext/compat#retain-compatible-operation-ids`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A compatible API change MUST retain the existing `operationId`.

**Rationale:** A compatible change promises existing consumers keep working. Renaming the `operationId` breaks that promise for every generated client and every stored operation reference. The identifier stays until a new version takes over.

**Reviewed for:** Did every compatible change keep the existing `operationId`? Check the OpenAPI diff.

### Assign Versioned Operation IDs

**ID:** `litespec/ext/compat#assign-versioned-operation-ids`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A new API version MUST assign a new operation ID when its operation contract changes.

**Rationale:** The new version needs its own contract names so the retained version keeps its own. A new `operationId` for each changed operation separates the two. Sharing one identifier across versions would let the new contract rename the old one.

**Reviewed for:** Does every changed operation in a new version carry a new operation ID? Check the versioned OpenAPI documents.

### Version Breaking Contracts

**ID:** `litespec/ext/compat#version-breaking-contracts`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An API owner MUST use a new route or documented media-type version for a breaking contract.

**Rationale:** Breaking changes need a boundary: a new route or a documented media-type version. Both contracts then serve at once, and each consumer moves on its own schedule. Without the boundary, one contract serves two incompatible expectations.

**Reviewed for:** Does every breaking contract ship behind a new route or a documented media-type version? Check the versioned API surface.

### Retain Supported Versions

**ID:** `litespec/ext/compat#retain-supported-versions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API owner MUST keep the previous version available for its declared support window.

**Rationale:** A consumer the project does not deploy moves on its own release cadence. The declared support window states how long the previous version stays available for that move. Removing it early strands consumers that kept to the published plan.

**Reviewed for:** Did every previous version stay available for its declared support window? Check the deployed versions beside the declared window.

### Record How Each Version Retires

**ID:** `litespec/ext/compat#record-how-each-version-retires`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A versioning decision MUST name each affected consumer, the deadline, and the removal condition, with the decision recorded before any removal.

**Rationale:** A removal touches consumers the project does not control, so the decision names each affected consumer, the deadline, and the removal condition. Recording happens before any removal, while the plan can still change. An unrecorded retirement removes first and notifies whoever complains.

**Reviewed for:** Does every version retirement rest on a decision that names the affected consumers, the deadline, and the removal condition, recorded before any removal? Check the versioning decisions.

### Signal Planned Public Removal

**ID:** `litespec/ext/compat#signal-planned-public-removal`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A public operation scheduled for removal MUST expose a documented deprecation signal.

**Rationale:** Consumers discover a planned removal only when the API tells them. A documented deprecation signal, such as `Deprecation` and `Sunset` headers, announces the removal on the operation itself. A removal with no signal arrives as an outage to every consumer that missed the release notes.

**Reviewed for:** Does every public operation scheduled for removal expose a documented deprecation signal? Check the public operations.

### Announce Planned Removals in the Changelog

**ID:** `litespec/ext/compat#announce-planned-removals-in-the-changelog`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A public operation scheduled for removal MUST appear in the consumer `CHANGELOG.md`.

**Rationale:** Headers reach callers at the operation, but teams plan from release notes. Naming each operation scheduled for removal in the consumer `CHANGELOG.md` puts it where consumers already look when they plan. A deprecation missing there reaches only the callers that inspect headers.

**Reviewed for:** Does every operation scheduled for removal appear in the consumer changelog? Check the consumer `CHANGELOG.md`.

### Record Deprecation Conditions

**ID:** `litespec/ext/compat#record-deprecation-conditions`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A deprecation decision MUST record the sunset date and replacement operation, with nothing removed before the decision is recorded.

**Rationale:** Each consumer needs to know when to move and where to go. Recording the sunset date and the replacement operation before removing anything gives both answers. A removal without a recorded replacement strands the consumer that depended on the old operation.

**Reviewed for:** Does every deprecation rest on a decision that records the sunset date and the replacement operation before any removal? Check the deprecation decisions.

### Treat Errors as Contracts

**ID:** `litespec/ext/compat#treat-errors-as-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API owner MUST treat Problem Details type, code, field-error codes, and documented statuses as versioned contract elements.

**Rationale:** Error handling is consumer code, and it branches on Problem Details type, code, field-error codes, and documented statuses. When those elements change outside versioning, the consumer failure path breaks, and that path has the least test coverage. Versioning error shapes with the contract keeps that handling working across releases.

**Reviewed for:** Did every type, code, field-code, or status change ship inside versioning? Check the versioned error surface.

### Add Error Outcomes Only When Old Clients Cope

**ID:** `litespec/ext/compat#add-error-outcomes-only-when-old-clients-cope`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API owner MUST add an error outcome only when existing consumers safely handle an unknown code.

**Rationale:** A new error outcome reaches consumers written before it existed. Adding it only when existing consumers handle an unknown code safely keeps old clients working against the new contract. An outcome old clients cannot parse turns their error path into a crash.

**Reviewed for:** Does every added error outcome leave existing consumers safe on unknown codes? Check the consumer error handling.

### Test Generated Clients Against Changed Contracts

**ID:** `litespec/ext/compat#test-generated-clients-against-changed-contracts`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An API owner MUST test each first-party generated client against the changed OpenAPI document, with each client compiling and exercising its changed operations.

**Rationale:** A structural comparison never compiles a client. Compiling each first-party generated client against the changed document, and running its changed operations, lets the client compiler and its exhaustive switches catch breaks the comparison cannot see.

**Reviewed for:** Did every first-party generated client compile and exercise its changed operations against the changed contract? Check the client test runs.

### Classify Integration Event Changes

**ID:** `litespec/ext/compat#classify-integration-event-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API change reviewer MUST classify each integration-event change as breaking, compatible, or needing consumer review under `litespec/ext/compat#classify-breaking-contract-changes`, `litespec/ext/compat#classify-compatible-additions`, and `litespec/ext/compat#review-new-enum-values-and-subtypes-with-consumers`.

**Rationale:** Consumers you do not deploy read integration events as contracts, just as they read HTTP responses. They need the same three verdicts: breaking, compatible, or needs review. Using one shared classification keeps those words meaning the same thing on both surfaces. A separate event verdict would let one change read as compatible for events and breaking for HTTP.

**Reviewed for:** Does every integration-event change carry a breaking, compatible, or needs-review verdict? Check the event reviews.

### Version Breaking Integration Events

**ID:** `litespec/ext/compat#version-breaking-integration-events`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** An API owner MUST carry a breaking integration-event change in a new event version beside the retained version.

**Rationale:** Outside consumers move on their own release cadence. Shipping the breaking change in a new event version, beside the retained version, lets each consumer move on its own schedule. Without the retained version, one event has to serve two incompatible expectations.

**Reviewed for:** Does every breaking event change ship in a new version beside the retained one? Check the integration event versions.

### Retire Integration Events with Notice

**ID:** `litespec/ext/compat#retire-integration-events-with-notice`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An API owner MUST retire an integration event version only after its declared support window with a deprecation notice to its consumers.

**Rationale:** A retired version strands every consumer still reading it, so the declared support window states how long the version stays available and the notice tells consumers when to move. Retiring early or silently turns a planned removal into an outage for the consumers that kept to the published plan.

**Reviewed for:** Did every retired event version stay for its support window and go out with a deprecation notice? Check the retirement records beside the event consumers.

## Conventions

### Store Current OpenAPI Source

**ID:** `litespec/ext/compat#store-current-openapi-source`

**Strength:** default

**Applies to:** artifact: any

**Default:** Store the current source artifact at `apps/api/openapi/{ProjectName}.json`.

**Replacement:** A consumer records a contract-source convention naming where its current OpenAPI artifact lives.

**Rationale:** Source generation, contract review, and the baseline comparison all read the current OpenAPI artifact. One predictable path keeps all three reading the same file. A second copy drifts from the first.

**Reviewed for:** Does every current OpenAPI artifact live at the documented path or the recorded local convention? Check `apps/api/openapi/`.

### Keep Baseline References Immutable

**ID:** `litespec/ext/compat#keep-baseline-references-immutable`

**Strength:** default

**Applies to:** artifact: any

**Default:** Store each supported baseline beside its contract owner or under an immutable retained-release reference.

**Replacement:** A consumer records a baseline convention naming where its immutable baselines live.

**Rationale:** A comparison proves nothing when its target can move. Storing each baseline beside its contract owner, or under an immutable release reference, keeps the target fixed. A mutable baseline lets a breaking change hide by moving the target it is compared against.

**Reviewed for:** Is every baseline immutable and stored under the recorded local convention? Check the baseline references.

### Use One Diff Tool

**ID:** `litespec/ext/compat#use-one-diff-tool`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use the same OpenAPI diff tool in local verification and continuous integration.

**Replacement:** A consumer records a diff-tool convention naming the OpenAPI diff tool its local run and continuous integration share.

**Rationale:** The local run and continuous integration need one shared meaning of breaking. Using one diff tool in both places gives each change one classification. Two tools disagree on edge cases, and developers learn to distrust a check that answers differently in each place.

**Reviewed for:** Do the local runs and the CI runs share the diff tool the local convention records? Check the local runs and the CI runs.

### Keep Problem Details Codes Stable

**ID:** `litespec/ext/compat#keep-problem-details-codes-stable`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep Problem Details error codes stable across compatible versions.

**Replacement:** A consumer records an error-code convention naming how its Problem Details codes evolve.

**Rationale:** Consumer error handling branches on these codes for years. Keeping them stable across compatible versions lets that handling survive routine releases. A renamed code forces every consumer to update its failure path for no change in behavior.

**Reviewed for:** Did every Problem Details code stay stable across compatible versions under the recorded local convention? Check the versioned error surface.

## Dependencies

Packages: None. When a project introduces an OpenAPI diff tool, it pins the tool version in the manifest.

Related extensions: `stage`, which defers these rules until the first deployment holds customer data.

Guides: None.
