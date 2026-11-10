# AI Assistance

**Stack:** any

**Concern:** application

## Intent

Some queries are answered through a model that picks tools to gather facts and compose the answer. This page states the closed tool set, the structure-only scenarios, the pinned stub, and the provenance those queries carry. A project selects it for each query a model mediates.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

A project selects `ai` for a query when a model mediates the answer by calling tools. A query answered by fixed code does not select it, even when a model wrote that code.

## Baseline Relationship

This extension replaces no baseline provision. It switches on no record type: it adds model mediation detail to query records.

A model-mediated query names its closed `tools` set, proves itself with structure-only scenarios, pins a deterministic stub in Proof, and returns provenance with its output.

## Standards

### Name the Closed Tool Set

**ID:** `litespec/ext/ai#name-the-closed-tool-set`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST name the closed set of tools the model calls.

**Rationale:** A model with an open tool set invents tools, reaches past its grant, and surprises every review. A closed set names exactly what the model touches, so the grant, the tests, and the audit all read the same list.

**Example:** The support answer query names `orders.lookup` and `docs.search` as its only tools.

**Reviewed for:** Which mediated queries call a tool outside their closed tool set? Look at each query's tool set beside the tool calls its tests record.

### Refuse Unknown Tools

**ID:** `litespec/ext/ai#refuse-unknown-tools`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST refuse a run that calls a tool outside its closed set.

**Rationale:** Models drift: a prompt tweak or a version change can add a call nobody declared. Refusing the run at the first unknown tool keeps the closed set closed in production, not just on paper.

**Example:** A run that calls `billing.refund` outside its set is refused before the call leaves the product.

**Reviewed for:** Which mediated query tests allow a run to call a tool outside the closed set? Look at the refused-run tests.

### Assert Structure and Never Wording

**ID:** `litespec/ext/ai#assert-structure-and-never-wording`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A scenario for a model-mediated query MUST assert the output structure and never its wording.

**Rationale:** Model wording shifts between runs: the same facts arrive in different sentences. A scenario asserting exact words fails on a paraphrase, while a scenario asserting structure, fields, codes, and cited sources passes on every true answer.

**Example:** The scenario asserts the answer carries `orderId` and one cited source, not the sentence around them.

**Reviewed for:** Which mediated query scenarios assert exact words instead of fields, codes, and cited sources? Look at the scenario assertions.

### Pin a Deterministic Stub in Proof

**ID:** `litespec/ext/ai#pin-a-deterministic-stub-in-proof`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Proof for a model-mediated query MUST run against a deterministic stub pinned by version.

**Rationale:** A live model answers differently each run, so a test against it proves nothing twice. A deterministic stub pinned by version answers the same way every run, so Proof checks the product's handling instead of the model's mood.

**Example:** The support answer tests run against `stub-answers v4`, which returns one fixed tool trace per prompt.

**Reviewed for:** Which mediated query tests call a live model instead of the pinned stub version? Look at the test configuration beside the stub version.

### Record Provenance in the Output

**ID:** `litespec/ext/ai#record-provenance-in-the-output`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST return provenance naming the model, the tools called, and the sources read.

**Rationale:** An answer without provenance cannot be checked: nobody knows which model wrote it, which tools fed it, or which sources back it. Provenance names the three, so every answer carries its own audit trail.

**Example:** The answer returns `model: support-7, tools: [orders.lookup], sources: [order-8841]`.

**Reviewed for:** Which mediated query answers lack the model, the tools called, or the sources read? Look at the provenance on each answer.

### Bound Tool Calls per Query

**ID:** `litespec/ext/ai#bound-tool-calls-per-query`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST bound how many tool calls one run makes.

**Rationale:** A model in a loop calls tools until the budget or the patience runs out. A bound on calls per run caps the cost and the latency up front, so one curious run cannot spend the month's budget.

**Example:** The support answer query allows eight tool calls per run and refuses the ninth.

**Reviewed for:** Which mediated queries state no per-run tool-call bound, or exceed it in the run tests? Look at the query definitions beside the run tests.

## Conventions

### Record Every Model Run

**ID:** `litespec/ext/ai#record-every-model-run`

**Strength:** default

**Applies to:** artifact: query-handler

**Default:** Each model run writes its prompt hash, the tools called, and the outcome to the run log.

**Replacement:** A project names its model-run evidence in its AI documentation.

**Rationale:** Production answers need after-the-fact review: what was asked, what the model touched, and what came back. The run log holds the three without storing full prompts.

**Reviewed for:** Which run log entries lack a prompt hash, the tools called, or an outcome, with no other evidence named in the AI documentation? Look at the run log.

## Dependencies

Packages: None.

Related extensions: `mcp` for the server shape when the product also serves tools to outside models.

Guides: None.
