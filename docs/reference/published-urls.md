# Published URLs

## Intent

An agent (software that reads task context, changes files, and runs checks) wires itself to LiteSpec through URLs on `https://litespec.dev`, not through a local copy of the repository. This page lists every URL the site serves: its pattern, a real example for the current release, and what it returns, so an agent or a reader resolves any reference with a fetch.

## Reference

The site is generated from the standard at the repository root, so the site and the release it is built from state the same contract. The release in this contract is `6.0.0`, taken from the `version` field of `litespec.manifest.yaml`, and every release path below starts with that version. [Work with agents](../learn/working-with-agents.md) explains how an agent uses these URLs on a task.

### Page URLs

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/6.0.0/<path>/` | `https://litespec.dev/6.0.0/backend/domain/` | The page as HTML |
| `https://litespec.dev/6.0.0/<path>.md` | `https://litespec.dev/6.0.0/backend/domain.md` | The page as plain Markdown |

`<path>` is the page path under `docs/` without the extension, such as `backend/domain`. A folder index is served at its folder, so `docs/learn/README.md` is served at `https://litespec.dev/6.0.0/learn/`. Raw Markdown answers with `Content-Type: text/markdown`. Relative links between pages are rewritten to absolute raw URLs, so an agent that fetches one page follows its links without a browser. Fenced code blocks keep every byte.

### Release Indexes

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/llms.txt` | `https://litespec.dev/llms.txt` | The root index for agents |
| `https://litespec.dev/6.0.0/llms.txt` | `https://litespec.dev/6.0.0/llms.txt` | Every page of the release |

Each index answers with `Content-Type: text/plain`. The root index explains how to use the site, lists every selection index and every layer bundle, and ends with the agent protocol block: the generated agent protocol region of the consumer template, filled for the default selection and preceded by what each placeholder takes. The release index groups every page by area with its raw Markdown URL and a one-line description.

### Selection Indexes

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/6.0.0/select/` | `https://litespec.dev/6.0.0/select/` | The selection page as HTML |
| `https://litespec.dev/6.0.0/select/<key>/llms.txt` | `https://litespec.dev/6.0.0/select/server.dotnet.react/llms.txt` | The pages one selection applies |

A selection key joins the shape and the stacks with dots: `server.dotnet.react` names a server project using the `dotnet` backend stack and the `react` frontend stack. A server selection names one backend stack and zero or one frontend stack. A client-only selection names no backend stack and one frontend stack, such as `client-only.react`. Extensions never enter the key; the `AGENTS.md` block names the chosen extensions.

A selection index answers with `Content-Type: text/plain`. It groups the pages the selection applies by layer, with each page linking its raw Markdown. The maintainer-only page stays out, and the authoring page stays because it holds consumer provisions. A Tools and Reference group lists the `rules`, `model`, `new` and `skill` tool pages with the Published URLs page. A By Artifact group names, for each artifact kind the release provisions state, the pages of the selection that carry provisions for it, for the selection's stacks. It then lists the extensions the selection may add and, when the selection holds a frontend stack, the UI pattern groups, each as a bare link, and ends with a note that says to fetch only the pages a task touches.

The selection page renders the `AGENTS.md` block from the consumer protocol template: the generated agent protocol region with each placeholder filled for the chosen answers. `litespec init` writes the same region into a new consumer, and an owner pastes the page's block when adopting an existing project. With size `small`, shape `server`, stacks `dotnet` and `react`, and no extensions, the block opens with the region fence and the precedence sentence, carries the protocol lines with the selection's values, and closes with the region fence:

```text
<!-- generated:agent-protocol -->
When this file disagrees with `dotnet litespec rules`, `dotnet litespec model query` or a record, the tool output and the record win; this file only points to them.
...
<!-- /generated:agent-protocol -->
```

The middle lines state the protocol with the release, size, shape, stacks, extensions, layers, selection key, and command of the answers. [Work with agents](../learn/working-with-agents.md) shows the same block in use.

### Layer Bundles

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/6.0.0/layers/<key>.md` | `https://litespec.dev/6.0.0/layers/backend/dotnet.md` | The layer as plain Markdown |

A layer is one folder's pages, concatenated in reading order: each page opens with a comment naming its raw source URL, and pages are separated by a line holding `---`. A layer answers with `Content-Type: text/markdown`. The keys are:

- `core` for the always-applied areas.
- `backend` and `frontend` for the principles.
- One shared folder, such as `frontend/web`.
- One stack folder, such as `backend/dotnet`, `frontend/web/react`, `frontend/web/svelte`, or `frontend/blazor`.
- One UI pattern group, such as `patterns/record-detail`.
- One extension, such as `ext/audit`.

The layers a selection applies, without extensions and pattern groups and in reading order, decide the groups of its selection index.

### Schema Files

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/6.0.0/schemas/<file>` | `https://litespec.dev/6.0.0/schemas/record.schema.yaml` | The schema as YAML |

The eight files are the ones the [Formats](formats.md) page lists. Each answers with `Content-Type: application/yaml`. Each schema `$id` equals its published URL.

### Provision Index

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/6.0.0/provisions.json` | `https://litespec.dev/6.0.0/provisions.json` | Every provision of the release |

The provision index answers with `Content-Type: application/json` and stays out of search results. It carries one object per provision with its ID, heading, owning page, anchor, kind, strength, statement, rationale, example, scope, and verification evidence, so an agent resolves any provision with one fetch instead of reading every page.

A provision ID resolves to its page: `litespec/<page>#<slug>` is `https://litespec.dev/<release>/<page>/#<slug>` as HTML and `https://litespec.dev/<release>/<page>.md` as Markdown. For example, `litespec/backend/application#keep-command-handlers-to-load-call-and-stage` is `https://litespec.dev/6.0.0/backend/application/#keep-command-handlers-to-load-call-and-stage` as HTML and `https://litespec.dev/6.0.0/backend/application.md` as Markdown.

### Latest Redirect

| Pattern | Example | Result |
|:---|:---|:---|
| `https://litespec.dev/latest` | `https://litespec.dev/latest` | Redirect to `https://litespec.dev/6.0.0/` |
| `https://litespec.dev/latest/*` | `https://litespec.dev/latest/backend/domain/` | Redirect to the same path under `6.0.0` |

Both redirects answer `302`. The build writes them to `_redirects` in the output root, which the hosting platform reads.

## Notes

Every plain-text URL exists so an agent resolves it with a fetch, without a browser. Start from the selection index that matches the project shape and stacks, paste the `AGENTS.md` block the selection page produces, and fetch only the pages a task touches. To read the whole release in fewer requests, fetch the layer bundle whose section the task spans: each section heading links the bundle that carries the same text.
