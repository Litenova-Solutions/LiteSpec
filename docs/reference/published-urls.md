# Published URLs

An agent reaches LiteSpec through URLs on `https://litespec.dev`, not through a local copy of the repository. This page lists every URL the site serves: the pattern, a real example for the current release, and what each returns, so an agent or a reader resolves any reference with one fetch.

## Reference

The site is generated from the standard in this repository, so the site and the release it is built from state the same contract. That release is `7.0.0`, taken from the `version` field of `litespec.manifest.yaml`, and every release path below starts with it. A release candidate, such as `6.0.0-rc.1`, serves no site: its tool writes links naming the release version, and the site serves those links once that release ships. [Work with agents](../learn/working-with-agents.md) shows how an agent uses these URLs on a task.

### Page URLs

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/7.0.0/<path>/` | `https://litespec.dev/7.0.0/core/scope/` | The page as HTML |
| `https://litespec.dev/7.0.0/<path>.md` | `https://litespec.dev/7.0.0/core/scope.md` | The page as plain Markdown |

`<path>` is the page path under `docs/` with no extension, such as `core/scope`. A folder index is served at its folder, so `docs/reference/README.md` is served at `https://litespec.dev/7.0.0/reference/`. Raw Markdown answers with `Content-Type: text/markdown`, and relative links between pages are rewritten to absolute raw URLs, so an agent fetching one page follows its links with no browser. Fenced code blocks keep every byte.

### Release Indexes

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/llms.txt` | `https://litespec.dev/llms.txt` | The root index for agents |
| `https://litespec.dev/7.0.0/llms.txt` | `https://litespec.dev/7.0.0/llms.txt` | Every page of the release |

Each index answers with `Content-Type: text/plain`. The root index explains how to use the site, lists every selection index and every layer bundle, and ends with the agent protocol block: the consumer template's generated agent protocol region, filled for the default selection and preceded by what each placeholder accepts. The release index groups every page by area, with its raw Markdown URL and a one-line description.

### Selection Indexes

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/7.0.0/select/` | `https://litespec.dev/7.0.0/select/` | The selection page as HTML |
| `https://litespec.dev/7.0.0/select/<key>/llms.txt` | `https://litespec.dev/7.0.0/select/server.dotnet.svelte/llms.txt` | The pages one selection applies |

A selection key joins the shape and the stacks with dots: `server.dotnet.svelte` names a server project using the `dotnet` backend stack and the `svelte` frontend stack. A server selection names one backend stack and zero or one frontend stack, while a client-only selection carries no backend stack and one frontend stack, such as `client-only.android`.

A selection index answers with `Content-Type: text/plain`. It groups the pages that apply to the selection by layer, each linking its raw Markdown. A Tools and Reference group lists the `rules`, `model`, `new`, and `skill` tool pages with the Published URLs page. A By Artifact group names, for each artifact kind the release provisions state, the pages of the selection carrying provisions for it, for the selection stacks. The index then lists the extensions for the selection and, when the selection holds a frontend stack, the UI pattern groups, each as a bare link, and ends with a note to fetch only the pages a task touches.

The selection page renders the `AGENTS.md` block from the consumer protocol template: the generated agent protocol region, with each placeholder filled for the chosen answers. `litespec init` writes the same region into a new consumer, and an owner pastes the page's block when adopting an existing project. With size `small`, shape `server`, stacks `dotnet` and `svelte`, and no extensions, the block opens with the region fence and the precedence sentence, carries the protocol lines with the selection's values, and closes with the region fence:

```text
<!-- generated:agent-protocol -->
When this file disagrees with `dotnet litespec rules`, `dotnet litespec model query` or a record, the tool output and the record win; this file only points to them. Between a record and its explanation file, the record wins; a disagreement is a defect in the explanation.
...
<!-- /generated:agent-protocol -->
```

The middle lines state the protocol with the answers' release, size, shape, stacks, extensions, layers, selection key, and command. [Work with agents](../learn/working-with-agents.md) shows the same block in use.

### Layer Bundles

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/7.0.0/layers/<key>.md` | `https://litespec.dev/7.0.0/layers/packs/dotnet.md` | The layer as plain Markdown |

A layer is one folder's pages, concatenated in reading order. Each page opens with a comment naming its raw source URL, and pages are separated by a line holding `---`. A layer answers with `Content-Type: text/markdown`. The keys are:

- One area key, such as `core`, covers the pages every selection applies.
- A stack folder key, such as `packs/dotnet`, covers that stack.
- An extension key, such as `ext/delivery`, covers that extension.

The layers that apply to a selection decide the groups of its selection index, taken in reading order and without extensions.

### Schema Files

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/7.0.0/schemas/<file>` | `https://litespec.dev/7.0.0/schemas/record.schema.yaml` | The schema as YAML |

The eight files are the ones the [Formats](formats.md) page lists. Each answers with `Content-Type: application/yaml`, and each schema `$id` equals its published URL.

### Provision Index

| Pattern | Example | Returns |
|:---|:---|:---|
| `https://litespec.dev/7.0.0/provisions.json` | `https://litespec.dev/7.0.0/provisions.json` | Every provision of the release |

The provision index answers with `Content-Type: application/json` and stays out of search results. It carries one object per provision, with its ID, heading, owning page, anchor, kind, mark, evidence, and strength, so an agent resolves any provision with one fetch instead of reading every page.

Each provision ID resolves to its page: `litespec/<page>#<slug>` is `https://litespec.dev/<release>/<page>/#<slug>` as HTML and `https://litespec.dev/<release>/<page>.md` as Markdown. For example, `litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage` is `https://litespec.dev/7.0.0/core/backend-application/#keep-command-handlers-to-load-call-and-stage` as HTML and `https://litespec.dev/7.0.0/core/backend-application.md` as Markdown.

### Latest Redirect

| Pattern | Example | Result |
|:---|:---|:---|
| `https://litespec.dev/latest` | `https://litespec.dev/latest` | Redirect to `https://litespec.dev/7.0.0/` |
| `https://litespec.dev/latest/*` | `https://litespec.dev/latest/core/scope/` | Redirect to the same path under `7.0.0` |

Both redirects answer `302`. The build writes them to `_redirects` in the output root, which the hosting platform reads.

## Notes

Every plain-text URL exists so an agent resolves it with a fetch, with no browser. Start from the selection index matching the project shape and stacks, paste the `AGENTS.md` block the selection page produces, and fetch only the pages a task touches. To read the whole release in fewer requests, fetch the layer bundle covering the section the task spans.
