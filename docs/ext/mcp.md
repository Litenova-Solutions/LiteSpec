# Model Context Protocol

**Stack:** any

**Concern:** web-api

## Intent

A Model Context Protocol server lets a large language model (LLM) client on the user's own machine call the product's public API. This page states how that server presents itself, describes its tools, and words its refusals; the API pages own the behavior the server calls. A project selects it when it ships such a server, so the model plans from accurate tool descriptions and every write waits for the person's confirmation.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `mcp` when it ships a Model Context Protocol server: a local process that an LLM client starts on the user's own machine so the model can call the product's public API. An integration the product hosts itself does not select it. The server calls the API as one authenticated user or API key and holds no permission of its own, so authentication and authorization stay with the baseline API and security pages. A team that adds an MCP SDK records a Decision for it.

## Baseline Relationship

This extension replaces no baseline provision.

This extension switches on no record type. Authentication, authorization, and permissions stay with the baseline API and security pages, so selection adds no record type.

## Standards

### Record the Server as an Entry Point

**ID:** `litespec/ext/mcp#record-the-server-as-an-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A project shipping an MCP server MUST record it as an entry point of kind `mcp` whose title names the MCP server.

**Rationale:** The entry point record names how callers arrive: `litespec/core/edges#give-each-entry-point-a-kind` keeps one closed set of kinds, and an MCP server is a channel a local model client calls, so it takes the kind `mcp` and its title says which protocol it speaks. Without the record, the server's reach stays invisible to reviewers of the access records.

**Reviewed for:** Which shipped MCP servers have no `mcp` entry point titled for them? Look at the entry point list.

### Pass Server Instructions at Initialize

**ID:** `litespec/ext/mcp#pass-server-instructions-at-initialize`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An MCP server MUST pass an `instructions` value at initialize stating its transport, its audience, its write surface, and its refusal behavior.

**Rationale:** The MCP initialize response carries server instructions that the client loads once per session, while each tool description loads per call. Scope and policy therefore belong at the server level, where they hold for every call: the transport, the audience, the write surface, and the refusal behavior. Tool mechanics stay in the tool descriptions, which the model reads when it chooses a tool. Without server-level instructions, each tool repeats the policy differently or omits it, and the model plans calls the server refuses.

**Example:** "Runs over stdio, one account per process, against the hosted API. Reads cover the account's own data, and three tools create or change records. A policy refusal returns the refusal code, and a write needing a person's confirmation refuses until they give it."

**Reviewed for:** Which initialize responses carry no `instructions` value, or one that omits transport, audience, write surface, or refusal behavior? Look at the server's initialize handler and its test.

### Write One Tool Description for One Decision

**ID:** `litespec/ext/mcp#write-one-tool-description-for-one-decision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An MCP tool MUST title its action imperatively, describe its behavior declaratively, use positive parameter polarity, and state its side effects.

**Rationale:** The [MCP specification](https://modelcontextprotocol.io/specification/2025-06-18) carries the title and description to the model, and the model plans its calls from those words. An imperative title names the action, while a declarative description states what happens instead of commanding the caller, so the server's guidance and the model's own plan do not compete. A positive boolean such as `paused` avoids the inverse polarity that a model answers wrongly, and a stated side effect answers the reviewer who asks what the call changed.

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

**Reviewed for:** Which tools break the title, behavior, parameter polarity, or side-effect rules? Look at each entry in the declared tools directory.

### Scope Each Tool to Named Use Cases

**ID:** `litespec/ext/mcp#scope-each-tool-to-named-use-cases`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each tool MUST name the use cases and permissions it reaches.

**Rationale:** Each tool opens product behavior to a model acting for the user, so the team names exactly which use cases and permissions each tool reaches. Without the named scope, a reviewer cannot tell whether a tool reads one account or writes across many, and a broad tool ships without the scrutiny its reach needs.

**Reviewed for:** Which tool descriptions name no use case or permission they reach? Look at each tool description beside its use case grant.

### Confirm Every Writing Tool

**ID:** `litespec/ext/mcp#confirm-every-writing-tool`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every tool that writes MUST refuse until the person confirms the write.

**Rationale:** A model misreads intent the way any agent does: it can pause the wrong subscription or delete the record the user asked about. Refusing each write until the person confirms keeps the person behind every change while leaving reads fast. Without confirmation, one misunderstood sentence becomes a changed record nobody confirmed.

**Reviewed for:** Which writing tools perform a write without the person's confirmation? Look at each writing tool's handler and its tests.

### Exclude Secrets and Personal Data

**ID:** `litespec/ext/mcp#exclude-secrets-and-personal-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Tool descriptions and logs MUST exclude secrets, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, and personal data from every field they carry.

**Rationale:** Tool descriptions travel to the model on every call, and logs persist for operators long after the call ends. A secret copied into either grants access to whoever reads it next, and personal data in either spreads beyond the consent that collected it. Descriptions and logs name the fields without carrying the values.

**Reviewed for:** Which tool descriptions or log lines carry a secret or personal value? Look at the tool descriptions and the server's log calls.

### State the Distribution and Update Path

**ID:** `litespec/ext/mcp#state-the-distribution-and-update-path`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The server documentation MUST state how a user installs and updates the server.

**Rationale:** The server runs on the user's machine, not on the product's servers, so no deployment pipeline updates it. Without documented install and update steps, users run stale servers with stale tool descriptions, and a security fix waits on users who never learn it exists.

**Reviewed for:** Which server documentation pages omit install or update steps? Look at the server's documentation.

## Conventions

### State the Refusing Side on Each Refusal

**ID:** `litespec/ext/mcp#state-the-refusing-side-on-each-refusal`

**Strength:** default

**Applies to:** artifact: any

**Default:** Prefix an input-shape refusal with `Needs:`, a human-confirmation refusal with `Needs confirmation:`, and a server-side refusal with `Refused:`.

**Replacement:** A project names another refusal vocabulary for its tools in its documentation.

**Rationale:** A refusal without a source sends the model guessing: it rephrases input the server rejected on policy, or it waits for a person when its own input was malformed. The prefix names the failing side, so the model repairs its own input after `Needs:`, asks the person after `Needs confirmation:`, and reports the server after `Refused:`, instead of retrying a refusal no retry satisfies.

**Reviewed for:** Which refusals carry no `Needs:`, `Needs confirmation:`, or `Refused:` prefix, with no other vocabulary named in the documentation? Look at the tools' refusal messages.

## Dependencies

Packages: `@modelcontextprotocol/server`.

Related extensions: `None.`

Guides: `None.`
