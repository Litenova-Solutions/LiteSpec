# AI and Model Context Protocol

**Stack:** any

**Concern:** application

Some queries get answers from a model that calls tools to gather facts and composes the reply. This page is for teams shipping those mediated queries and for teams shipping a Model Context Protocol (MCP) server on the user's own machine. Closed tool sets, structure-only proof, and provenance keep mediated answers checkable, while accurate tool descriptions and confirmed writes keep a shipped server safe to call.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

Select `ai-mcp` for a query when a model mediates the answer by calling tools. A query answered by fixed code leaves it unselected, even when a model wrote that code. Select `ai-mcp` also when the project ships an MCP server: a local process that an LLM client starts on the user's own machine so the model calls the product's public API. An integration the product hosts itself leaves it unselected.

The server calls the API as one authenticated user or API key and holds no permission of its own, so authentication and authorization stay with the baseline API and security pages. A team adding an MCP SDK records a Decision for it.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type. It adds model mediation detail to query records: the query states its closed `tools` set, proves itself with structure-only scenarios, pins a deterministic stub in Proof, and returns provenance with its output. It records a shipped server as an entry point of kind `mcp` whose title states the MCP server.

### Closed Tools and Structure-Only Proof

Each mediated query states the closed set of tools the model calls, for example `orders.lookup` and `docs.search` for a support answer query. The closed list gives the grant, the tests, and the audit one list to read, so a tool outside the grant shows at once. A run calling a tool outside the set stops before the call leaves the product, which holds the boundary in production as well as in the record.

Model wording shifts between runs, so scenarios assert structure instead of sentences: fields, codes, and cited sources, for example `orderId` plus one cited source. Proof runs against a deterministic stub pinned by version, for example `stub-answers v4` with one fixed tool trace per prompt. The stub answers the same way each run, so Proof checks the product handling instead of the model's varying output.

Each answer returns provenance stating the model, the tools called, and the sources read, for example `model: support-7, tools: [orders.lookup], sources: [order-8841]`. Each query also states a per-run bound on tool calls, for example eight calls with the ninth refused. The bound caps cost and latency up front, so one looping run consumes nothing beyond its share.

### What a Shipped Server Presents

A shipped server is recorded as an entry point of kind `mcp` with its title stating the server. The initialize response carries an `instructions` value covering transport, audience, write surface, and refusal behavior. Server-level scope and policy live there, holding for every call, while tool mechanics live in the tool descriptions the model reads when choosing a tool.

Each tool description titles its action in the imperative and states behavior declaratively, with positive parameter polarity and stated side effects. A `paused` boolean beats an inverse shape the model reads wrongly, and a note like "events published while paused are dropped, not queued" tells the reviewer what the call changes. Each tool states the use cases and permissions it reaches, so a reviewer sees whether a tool reads one account or writes across many.

Every writing tool pauses until the person confirms, while reads stay fast. Tool descriptions and logs state field names without values: no secret and no personal data in any field. The server documentation states how a user installs and updates the server, since no deployment pipeline updates a program running on the user's machine.

Each model run writes its prompt hash, the tools called, and the outcome to the run log, giving later review the what-asked, what-touched, and what-returned without storing full prompts. Each refusal carries a prefix stating the refusing side: `Needs:` for input shape, `Needs confirmation:` for a write awaiting the person, and `Refused:` for a server-side policy refusal. The prefix tells the model whether to repair its input, ask the person, or report the policy outcome instead of retrying a refusal no retry satisfies.

## Standards

None.

## Conventions

None.

## Dependencies

Packages: `@modelcontextprotocol/server` where the product ships a server, none where it only mediates queries.

Related extensions: `None`.

Guides: `None`.
