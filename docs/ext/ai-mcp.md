# AI and Model Context Protocol

**Stack:** any

**Concern:** application

Some queries are answered through a model that picks tools to gather facts and composes the answer. Because the model's tool calls and wording vary between runs, those queries name a closed tool set, prove structure instead of wording, and return provenance with each answer. Separately, the product can ship a Model Context Protocol (MCP) server so a large language model client on the user's own machine can call the product's public API. These rules cover both halves: how a mediated query declares its tools and proves its answers, and how a shipped server presents itself, describes its tools, and words its refusals.

The query half applies when a model mediates the answer by calling tools, so the model plans from accurate tool descriptions and every write waits for the person's confirmation. The server half applies when the project ships such a server, so the model plans from accurate tool descriptions and every write waits for the person's confirmation.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

Select `ai-mcp` for a query when a model mediates the answer by calling tools. A query answered by fixed code does not select it, even when a model wrote that code. Select `ai-mcp` also when the project ships an MCP server: a local process that an LLM client starts on the user's own machine so the model can call the product's public API. An integration the product hosts itself does not select it.

The server calls the API as one authenticated user or API key and holds no permission of its own, so authentication and authorization stay with the baseline API and security pages. A team that adds an MCP SDK records a Decision for it.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type. It adds model mediation detail to query records: the query names its closed `tools` set, proves itself with structure-only scenarios, pins a deterministic stub in Proof, and returns provenance with its output. It records a shipped server as an entry point of kind `mcp` whose title names the MCP server.

## Standards

### Name the Closed Tool Set

**ID:** `litespec/ext/ai-mcp#name-the-closed-tool-set`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST name the closed set of tools the model calls.

**Rationale:** Without a closed set, the model can call tools beyond its grant, including tools nobody declared. Naming exactly what the model may call gives the grant, the tests, and the audit one list to read.

**Example:** The support answer query names `orders.lookup` and `docs.search` as its only tools.

**Reviewed for:** Does every mediated query name its closed tool set, with no calls outside it? Read each query's tool set beside the tool calls its tests record.

### Refuse Unknown Tools

**ID:** `litespec/ext/ai-mcp#refuse-unknown-tools`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST refuse a run that calls a tool outside its closed set.

**Rationale:** A prompt change or a model version change can add a call nobody declared. Refusing the run at the first unknown tool enforces the closed set in production as well as in the record.

**Example:** A run that calls `billing.refund` outside its set is refused before the call leaves the product.

**Reviewed for:** Does every mediated query test refuse a run that calls a tool outside the closed set? Read the refused-run tests.

### Assert Output Structure Instead of Wording

**ID:** `litespec/ext/ai-mcp#assert-output-structure-instead-of-wording`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A scenario for a model-mediated query MUST assert the output structure and never its wording.

**Rationale:** Model wording shifts between runs: the same facts arrive in different sentences. A scenario asserting exact words fails on a paraphrase, while a scenario asserting structure, fields, codes, and cited sources passes on every true answer.

**Example:** The scenario asserts the answer carries `orderId` and one cited source, not the sentence around them.

**Reviewed for:** Does every mediated query scenario assert fields, codes, and cited sources instead of exact words? Read the scenario assertions.

### Pin a Deterministic Stub in Proof

**ID:** `litespec/ext/ai-mcp#pin-a-deterministic-stub-in-proof`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Proof for a model-mediated query MUST run against a deterministic stub pinned by version.

**Rationale:** A live model answers differently each run, so a test against it cannot repeat its result. A deterministic stub pinned by version answers the same way every run, so Proof checks the product's handling instead of the model's varying output.

**Example:** The support answer tests run against `stub-answers v4`, which returns one fixed tool trace per prompt.

**Reviewed for:** Does every mediated query test run against the pinned stub version instead of a live model? Read the test configuration beside the stub version.

### Record Provenance in the Output

**ID:** `litespec/ext/ai-mcp#record-provenance-in-the-output`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST return provenance naming the model, the tools called, and the sources read.

**Rationale:** Without provenance, a reader cannot check an answer: nobody knows which model wrote it, which tools fed it, or which sources back it. Naming the model, the tools called, and the sources read puts that audit trail on the answer itself.

**Example:** The answer returns `model: support-7, tools: [orders.lookup], sources: [order-8841]`.

**Reviewed for:** Does every mediated query answer carry the model, the tools called, and the sources read? Read the provenance on each answer.

### Bound Tool Calls per Query

**ID:** `litespec/ext/ai-mcp#bound-tool-calls-per-query`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A model-mediated query MUST bound how many tool calls one run makes.

**Rationale:** A model in a loop can call tools repeatedly without stopping. A bound on calls per run caps the cost and the latency up front, so one looping run cannot consume the whole budget.

**Example:** The support answer query allows eight tool calls per run and refuses the ninth.

**Reviewed for:** Does every mediated query state a per-run tool-call bound, and do the run tests honor it? Read the query definitions beside the run tests.

### Record the Server as an Entry Point

**ID:** `litespec/ext/ai-mcp#record-the-server-as-an-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A project shipping an MCP server MUST record it as an entry point of kind `mcp` whose title names the MCP server.

**Rationale:** Entry point records name how callers arrive, and `litespec/core/edges#give-each-entry-point-a-kind` keeps one closed set of kinds. An MCP server is a channel a local model client calls, so it takes the kind `mcp` with its title naming the protocol it uses. Without that record, what the server reaches stays invisible to anyone reviewing the access records.

**Reviewed for:** Does every shipped MCP server have an `mcp` entry point titled for it? Read the entry point list.

### Pass Server Instructions at Initialize

**ID:** `litespec/ext/ai-mcp#pass-server-instructions-at-initialize`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An MCP server MUST pass an `instructions` value at initialize stating its transport, its audience, its write surface, and its refusal behavior.

**Rationale:** The client loads server instructions once per session from the MCP initialize response, while each tool description loads per call. That split puts scope and policy at the server level, where they hold for every call: transport, audience, write surface, and refusal behavior. Tool mechanics stay in the tool descriptions, which the model reads when it chooses a tool. Without server-level instructions, each tool repeats the policy differently or omits it, and the model plans calls the server then refuses.

**Example:** "Runs over stdio, one account per process, against the hosted API. Reads cover the account's own data, and three tools create or change records. A policy refusal returns the refusal code, and a write needing a person's confirmation refuses until they give it."

**Reviewed for:** Does every initialize response carry an `instructions` value covering transport, audience, write surface, and refusal behavior? Read the server's initialize handler and its test.

### Write One Tool Description for One Decision

**ID:** `litespec/ext/ai-mcp#write-one-tool-description-for-one-decision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An MCP tool MUST title its action imperatively, describe its behavior declaratively, use positive parameter polarity, and state its side effects.

**Rationale:** The model plans its calls from the title and description it receives under the [MCP specification](https://modelcontextprotocol.io/specification/2025-06-18). An imperative title names the action, while a declarative description states what happens instead of commanding the caller, so the server's guidance and the model's own plan do not compete. A positive boolean such as `paused` avoids the inverse polarity a model gets wrong, and a stated side effect tells the reviewer what the call changed.

**Example:**

```json
{
  "name": "pause_webhook_deliveries",
  "description": "Pauses or resumes delivery to one subscription. Events published while paused are dropped, not queued.",
  "input": {
    "type": "object",
    "properties": {
      "paused": { "type": "boolean", "description": "True to pause, false to resume." }
    }
  }
}
```

**Reviewed for:** Does every tool follow the title, behavior, parameter polarity, and side-effect rules? Read each entry in the declared tools directory.

### Scope Each Tool to Named Use Cases

**ID:** `litespec/ext/ai-mcp#scope-each-tool-to-named-use-cases`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each tool MUST name the use cases and permissions it reaches.

**Rationale:** Each tool opens product behavior to a model acting for the user. Naming exactly which use cases and permissions each tool reaches lets a reviewer tell whether a tool reads one account or writes across many. Without the named scope, a broad tool ships without scrutiny matching what it reaches.

**Reviewed for:** Does every tool description name the use cases and permissions it reaches? Read each tool description beside its use case grant.

### Refuse Each Write Until the Person Confirms

**ID:** `litespec/ext/ai-mcp#refuse-each-write-until-the-person-confirms`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every tool that writes MUST refuse until the person confirms the write.

**Rationale:** A model misreads intent the way any agent does: it can pause the wrong subscription or delete the record the user asked about. Refusing each write until the person confirms means every change carries the person's confirmation while reads stay fast. Without confirmation, one misunderstood sentence becomes a changed record nobody confirmed.

**Reviewed for:** Does every writing tool refuse until the person confirms? Read each writing tool's handler and its tests.

### Keep Secrets and Personal Data Out of Descriptions and Logs

**ID:** `litespec/ext/ai-mcp#keep-secrets-and-personal-data-out-of-descriptions-and-logs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Tool descriptions and logs MUST carry no secret, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, and no personal data in any field.

**Rationale:** Tool descriptions travel to the model on every call, and logs persist for operators long after the call ends. A secret copied into either grants access to whoever reads it next, and personal data in either spreads beyond what the consent allowed. Descriptions and logs name the fields without including the values.

**Reviewed for:** Do any tool descriptions or log lines carry a secret or personal value? Read the tool descriptions and the server's log calls.

### State the Distribution and Update Path

**ID:** `litespec/ext/ai-mcp#state-the-distribution-and-update-path`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The server documentation MUST state how a user installs and updates the server.

**Rationale:** The server runs on the user's machine, not on the product's servers, so no deployment pipeline updates it. Without documented install and update steps, users run stale servers with stale tool descriptions, and a security fix waits for users who never learn it exists.

**Reviewed for:** Does the server documentation state how to install and update the server? Read the server's documentation.

## Conventions

### Record Every Model Run

**ID:** `litespec/ext/ai-mcp#record-every-model-run`

**Strength:** default

**Applies to:** artifact: query-handler

**Default:** Each model run writes its prompt hash, the tools called, and the outcome to the run log.

**Replacement:** A project names its model-run evidence in its AI documentation.

**Rationale:** Production answers need review after the fact: what was asked, what the model touched, and what came back. The run log holds the prompt hash, the tools called, and the outcome, without storing full prompts.

**Reviewed for:** Does every run log entry hold a prompt hash, the tools called, and an outcome, or does the AI documentation name other evidence? Read the run log.

### State the Refusing Side on Each Refusal

**ID:** `litespec/ext/ai-mcp#state-the-refusing-side-on-each-refusal`

**Strength:** default

**Applies to:** artifact: any

**Default:** Prefix an input-shape refusal with `Needs:`, a human-confirmation refusal with `Needs confirmation:`, and a server-side refusal with `Refused:`.

**Replacement:** A project names another refusal vocabulary for its tools in its documentation.

**Rationale:** A refusal without a source leaves the model to guess: it rephrases input the server rejected on policy, or it waits for a person when its own input was malformed. The prefix names the failing side. The model repairs its own input after `Needs:`, asks the person after `Needs confirmation:`, and reports the server-side refusal after `Refused:`, instead of retrying a refusal no retry satisfies.

**Reviewed for:** Does every refusal carry a `Needs:`, `Needs confirmation:`, or `Refused:` prefix, or does the documentation name another vocabulary? Read the tools' refusal messages.

## Dependencies

Packages: `@modelcontextprotocol/server` where the product ships a server, none where it only mediates queries.

Related extensions: `None`.

Guides: `None`.
