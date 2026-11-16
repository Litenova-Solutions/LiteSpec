# AI Assistance

**Stack:** any

**Concern:** application

Some queries are answered through a model that picks tools to gather facts and composes the answer. Because the model's tool calls and wording vary between runs, those queries name a closed tool set, prove structure instead of wording, and return provenance with each answer.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

Select `ai` for a query when a model mediates the answer by calling tools. A query answered by fixed code does not select it, even when a model wrote that code.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type. It adds model mediation detail to query records: the query names its closed `tools` set, proves itself with structure-only scenarios, pins a deterministic stub in Proof, and returns provenance with its output.

## Standards

### Name the Closed Tool Set

**ID:** `litespec/ext/ai#name-the-closed-tool-set`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST name the closed set of tools the model calls.

**Rationale:** Without a closed set, the model can call tools beyond its grant, including tools nobody declared. Naming exactly what the model may call gives the grant, the tests, and the audit one list to read.

**Example:** The support answer query names `orders.lookup` and `docs.search` as its only tools.

**Reviewed for:** Does every mediated query name its closed tool set, with no calls outside it? Read each query's tool set beside the tool calls its tests record.

### Refuse Unknown Tools

**ID:** `litespec/ext/ai#refuse-unknown-tools`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST refuse a run that calls a tool outside its closed set.

**Rationale:** A prompt change or a model version change can add a call nobody declared. Refusing the run at the first unknown tool enforces the closed set in production as well as in the record.

**Example:** A run that calls `billing.refund` outside its set is refused before the call leaves the product.

**Reviewed for:** Does every mediated query test refuse a run that calls a tool outside the closed set? Read the refused-run tests.

### Assert Output Structure Instead of Wording

**ID:** `litespec/ext/ai#assert-output-structure-instead-of-wording`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A scenario for a model-mediated query MUST assert the output structure and never its wording.

**Rationale:** Model wording shifts between runs: the same facts arrive in different sentences. A scenario asserting exact words fails on a paraphrase, while a scenario asserting structure, fields, codes, and cited sources passes on every true answer.

**Example:** The scenario asserts the answer carries `orderId` and one cited source, not the sentence around them.

**Reviewed for:** Does every mediated query scenario assert fields, codes, and cited sources instead of exact words? Read the scenario assertions.

### Pin a Deterministic Stub in Proof

**ID:** `litespec/ext/ai#pin-a-deterministic-stub-in-proof`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Proof for a model-mediated query MUST run against a deterministic stub pinned by version.

**Rationale:** A live model answers differently each run, so a test against it cannot repeat its result. A deterministic stub pinned by version answers the same way every run, so Proof checks the product's handling instead of the model's varying output.

**Example:** The support answer tests run against `stub-answers v4`, which returns one fixed tool trace per prompt.

**Reviewed for:** Does every mediated query test run against the pinned stub version instead of a live model? Read the test configuration beside the stub version.

### Record Provenance in the Output

**ID:** `litespec/ext/ai#record-provenance-in-the-output`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST return provenance naming the model, the tools called, and the sources read.

**Rationale:** Without provenance, a reader cannot check an answer: nobody knows which model wrote it, which tools fed it, or which sources back it. Naming the model, the tools called, and the sources read puts that audit trail on the answer itself.

**Example:** The answer returns `model: support-7, tools: [orders.lookup], sources: [order-8841]`.

**Reviewed for:** Does every mediated query answer carry the model, the tools called, and the sources read? Read the provenance on each answer.

### Bound Tool Calls per Query

**ID:** `litespec/ext/ai#bound-tool-calls-per-query`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST bound how many tool calls one run makes.

**Rationale:** A model in a loop can call tools repeatedly without stopping. A bound on calls per run caps the cost and the latency up front, so one looping run cannot consume the whole budget.

**Example:** The support answer query allows eight tool calls per run and refuses the ninth.

**Reviewed for:** Does every mediated query state a per-run tool-call bound, and do the run tests honor it? Read the query definitions beside the run tests.

## Conventions

### Record Every Model Run

**ID:** `litespec/ext/ai#record-every-model-run`

**Strength:** default

**Applies to:** artifact: query-handler

**Default:** Each model run writes its prompt hash, the tools called, and the outcome to the run log.

**Replacement:** A project names its model-run evidence in its AI documentation.

**Rationale:** Production answers need review after the fact: what was asked, what the model touched, and what came back. The run log holds the prompt hash, the tools called, and the outcome, without storing full prompts.

**Reviewed for:** Does every run log entry hold a prompt hash, the tools called, and an outcome, or does the AI documentation name other evidence? Read the run log.

## Dependencies

Packages: None.

Related extensions: `mcp` for the server shape when the product also serves tools to outside models.

Guides: None.
