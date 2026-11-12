# Project File

## Intent

`.litespec/project.yaml` is the one record of what a consumer project (an application repository built against one pinned LiteSpec release) selected: its size, shape, technology stacks, extensions, paths, and pinned release. The team edits it when the selection changes and validates it with the `litespec model check` command before relying on it. This page lists every field the file holds, states what reads each field, and shows the errors a wrong value produces.

## Reference

The [supported scope](../core/scope.md) page states which sizes, shapes, stacks, and extensions a project can select. The [stacks](../core/stacks.md) page states what selecting a stack applies. The [init](../tools/init.md) page states how a new file is written, the [model](../tools/model.md) page states how it is validated and how paths feed the project model. Terms such as stack, shape, size, extension, and preset follow the [glossary](glossary.md).

### File Location and Schema

| Item | Value |
|:---|:---|
| Path | `.litespec/project.yaml` at the consumer root |
| Schema | `schemas/litespec-project.schema.yaml` in the pinned release |
| Template | `templates/consumer/project.yaml` in the release the tool carries |
| Presets | `templates/presets/android-client.yaml`, `templates/presets/blazor-client.yaml`, `templates/presets/dotnet-react.yaml`, `templates/presets/dotnet-svelte.yaml` |

`litespec init` merges the template with one preset and stamps the size, name, and reviewed release. `litespec model check` validates the written file against the schema and then runs the selection checks below. The model builder reads the paths, stack config, size, shape, and stacks from the same file.

### Top-Level Fields

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `schemaVersion` | integer | yes | none |
| `project` | object | yes | none |
| `size` | string | yes | none |
| `layers` | object | yes | none |
| `explanations` | object | no | none |
| `shape` | string | no | `"server"` |
| `stacks` | array of string | when `implementation` or `proof` is claimed | none |
| `proof` | object | no | none |
| `marks` | string | no | `"xml"` |
| `dependencies` | object | no | none |
| `screenshots` | object | no | none |
| `refusals` | object | no | the default codes |
| `reviewedLiteSpecVersion` | string | yes | none |
| `paths` | object | yes | none |
| `selectedExtensions` | array of object | yes | none |
| `stackConfig` | object | no | none |
| `frontends` | array of object | for a `client-only` project | none |
| `ui` | object | no | none |

What each field means and what reads it:

- `schemaVersion` is the shape of this file, `1` in this release. The editor finds the schema from the first-line comment `# yaml-language-server: $schema=<url>`, which `litespec init` writes; no key in the data names it.
- `project` names the project. `litespec model check` checks it against the schema. See the `project` object below.
- `size` is `small` or `large`. Small requires the core record types with Architecture and Integration test projects, and large adds goals, problems, features, journeys, and scenarios with the Acceptance project. `litespec model check` checks the value, and the model builder sizes the record set it expects.
- `layers` names the record layers the project claims and the practice each follows. See the `layers` object below.
- `explanations` names the record layers whose every record page carries an explanation file. See the `explanations` object below.
- `shape` is `server` or `client-only`. A server names exactly one backend stack, and a client-only project names no backend stack. `litespec init` writes it from the preset, `litespec model check` checks the backend count, and the model builder applies the shape.
- `stacks` lists the selected stack ids: `dotnet` for the backend and one entry per frontend from its platform. It is required when the project claims `implementation` or `proof`; otherwise a stack is informational. `litespec model check` refuses an id the pinned release does not ship.
- `proof` tunes how the model builder confirms proof claims for the project. See the `proof` object below.
- `marks` is the mark style: `xml` for documentation tags, `attribute` for generated attributes. The model builder, the analyzers and the source generator read it, and each reports a mark in the other style as `mark-style-invalid`.
- `dependencies` names the dependency gate: the licenses every shipped package may carry without an admitting decision. See the `dependencies` object below.
- `screenshots` names the screenshot galleries the model builder reads for screen captures. See the `screenshots` object below.
- `refusals` names the codes the derived generic results carry. See the `refusals` object below.
- `reviewedLiteSpecVersion` records the LiteSpec release whose complete contract the project last reviewed, such as `"5.1.0"`. `litespec model check` compares it with the release of the installed `litespec` tool and fails the run when the two differ.
- `paths` declares every location the tools read. See the `paths` object below.
- `selectedExtensions` lists the extensions the project selected, each with its criterion. See the extension entry below.
- `stackConfig` holds per-stack configuration keyed by a stack id the manifest ships. See the `stackConfig.dotnet` object below.
- `frontends` lists every frontend the project ships. See the frontend entry below.
- `ui` holds `package`, the one shared UI package of the workspace. See the `ui.package` object below.

### The `layers` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `intent` | `litespec` | yes | none |
| `behavior` | `litespec` | no | not claimed |
| `implementation` | `litespec` or `own` | no | not claimed |
| `proof` | `litespec` or `own` | no | not claimed |

What each field means and what reads it:

- A key that is present claims its record layer; a key left out is not claimed, and no check reads that layer.
- `litespec` follows LiteSpec's practice for the layer. `own` keeps the project's own practice for `implementation` or `proof` and claims only that code and tests mark the records they serve.
- A lower layer needs the one above it: `behavior` needs `intent`, and `implementation` and `proof` each need `behavior`. The schema refuses a file that breaks this.
- `litespec model check`, the book, `litespec rules` and the analyzers report only the claimed layers.

### The `explanations` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `layers` | array of string | yes | none |

What each field means and what reads it:

- `layers` lists the record layers whose every record page carries an explanation file: `intent`, `behavior` or both, each once: the layers that hold record pages, with each record type on the layer the [Project Model](../core/model.md) gives it. A page on a listed layer without its `<name>.md` beside its record file, or with a required H2 section missing, is an `explanation-missing` finding. A listed layer the project does not claim in `layers` is a `project-invalid` finding.
- When the object is omitted, explanation files stay optional, except the article types, which always require one. `litespec model check` reads the list from the project file.

### The `project` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `name` | string | yes | none |

What each field means and what reads it:

- `name` is the project name, such as `"shop"`. `litespec init` sets it from `--name` or from the root folder name.

### The `proof` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `jobTriggerSteps` | array of string | no | none |
| `outcomeOnlySteps` | array of string | no | none |

What each field means and what reads it:

- `jobTriggerSteps` names the steps classes whose `when` steps run a job, such as one that moves a simulated clock. Each entry matches `^[A-Z][A-Za-z0-9]*$`.
- `outcomeOnlySteps` names the steps classes whose `then` steps state only that a request succeeded or was refused. Each entry matches `^[A-Z][A-Za-z0-9]*$`.
- The model builder reads both lists when it confirms proof claims for the project.

### The `dependencies` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `licenses` | array of strings | no | none |

- `licenses` lists the SPDX license identifiers the project accepts, such as `MIT` and `Apache-2.0`. A shipped package under any other license needs an admitting decision that names it.

### The `screenshots` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `galleries` | array of string | no | none |
| `pattern` | string | no | `"{route}--{theme}--{width}.png"` |

What each field means and what reads it:

- `galleries` lists the gallery folders holding screenshot captures, one per frontend that photographs its routes, such as `"apps/web/test-results/gallery"`. The model builder reads the captures from these folders.
- `pattern` is the capture file-name pattern, with `{route}` for the route slug, `{theme}` for the theme, and `{width}` for the width in pixels. When omitted, the default pattern applies.

### The `refusals` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `forbidden` | string | no | `"authorization.forbidden"` |
| `invalidInput` | string | no | `"request.validation-failed"` |
| `composed` | string array | no | none |

What each field means and what reads it:

- `forbidden` is the code the derived `result/<use-case>.forbidden` carries, derived for a use case whose `access.grant` is a permission.
- `invalidInput` is the code the derived `result/<use-case>.invalid-input` carries, derived for a use case with any `input` field carrying `rules`.
- `composed` lists the refusal codes the project builds at run time from a key, each a template with one `{key}` placeholder after a prefix and a dot, such as `"uniqueness.{key}-taken"` for the conflict a commit answers when an aggregate loses a uniqueness key. The `refusal-unimplemented` check counts a code matching a template as raised when its key is a kebab-case literal in the code, such as the key scope `copy-barcode` on the book aggregate for `uniqueness.copy-barcode-taken`.
- The model builder reads `forbidden` and `invalidInput` when it derives the generic results. When the object is omitted, the defaults apply.

### The `paths` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `docs` | string | no | `.litespec` |
| `glossary` | string | no | `.litespec/glossary.yaml` |
| `behaviorDocs` | string | yes | none |
| `testRoots` | array of string | no | none |
| `testResults` | string | no | `TestResults` |
| `contract` | string | no | none |
| `alerts` | array of strings | no | none |
| `operations` | array of strings | no | none |
| `checkResults` | string | no | `.litespec/proof/checks` |
| `sbom` | array of strings | no | none |
| `apiClient` | string | no | `packages/api` |
| `uiPatterns` | string | no | `.litespec/implementation/patterns/catalog.yaml` |

What each field means and what reads it:

- `docs` is the root of the specification tree. Every record file under it holds one record page, with an explanation file beside it where one exists, and every other Markdown file is a document. When `docs` is omitted, the tools use `.litespec`.
- `glossary` names the project glossary. `litespec model check` validates it and scans the pages against its rejected words and mannered terms.
- `behaviorDocs` names the behavior documentation root. It is required.
- `testRoots` lists the directories that hold automated test source. The model builder reads scenarios from these roots.
- `testResults` names where the latest test run results live, so passing and failing proof is read from them.
- `contract` names the committed OpenAPI document, so `litespec model check` compares each operation's `x-litespec-use-case` with its endpoint's mark.
- `alerts` names the alert rule files or folders, in the YAML shape Prometheus rule files and Grafana alert provisioning share, so each rule's `requirement` and `runbook` labels are checked and the alert inventory is rendered from them.
- `operations` names the deployment, proxy, host, and CI workflow files or folders that carry `follows` marks in their `#` comments; below a named folder every file counts except Markdown documentation.
- `checkResults` names where drill and harness check results live, read beside the measured-check results under `testResults`.
- `sbom` names the bill of materials files or folders, SPDX or CycloneDX JSON, checked against the manifest pins, the admissions, and `dependencies.licenses`.
- `apiClient` names the generated API client the frontends import, so route calls resolve to the use cases they reach. The model builder appends `src` to the configured path, and the reader takes operations only from the files the client generator writes (`*.gen.ts`), so a hand-written helper beside them is not an operation.
- `uiPatterns` names the workspace UI pattern catalog when it is not at the default path.

### The `stackConfig.dotnet` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `modules` | string | no | `apps/api/src/*.Modules` |
| `sharedKernel` | string | no | `apps/api/src/*.SharedKernel` |
| `domain` | string | no | `apps/api/src/*.Domain` under `layer-projects`, none otherwise |
| `infrastructure` | string | no | `apps/api/src/*.Infrastructure` under `layer-projects`, none otherwise |
| `webapi` | array of string | no | `apps/api/src/*.WebApi` |
| `worker` | array of string | no | `apps/api/src/*.Worker` |
| `templateStore` | string | no | none |

What each field means and what reads it:

- `modules` is the code root the model builder reads: the modules project holding every module. A `*` in one segment expands to the first matching folder.
- `sharedKernel` names the shared kernel project holding what every module builds on, where reasonless names are allowed. When omitted, the default applies and `litespec model check` reports `name-reason-missing` for every reasonless folder or type outside it.
- `domain` and `infrastructure` name the Domain and Infrastructure projects the model builder reads, under `layer-projects` only. Without that extension they default to none.
- `webapi` and `worker` name the web and worker host projects the model builder reads: a list with one path per host.
- `templateStore` names the one C# file holding the notification template store: its top-level dictionary keys are the template keys, and its texts write parameters as `{{PascalCaseName}}`. When omitted, the template checks stay silent.

### The `ui.package` Object

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `name` | string | yes | none |
| `path` | string | yes | none |
| `componentsJson` | string | yes | none |
| `primitives` | string | yes | none |
| `tokens` | string | yes | none |
| `sourceLock` | string | yes | none |
| `designContract` | string | yes | none |
| `publicExports` | array of string | yes | none |

What each field means and what reads it:

- `name` is the package name, such as `"@shop/ui"`. `path` is the package root, such as `"packages/ui"`.
- `componentsJson` is the registry configuration, `primitives` is the directory that holds the registry source the pinned CLI copied in, `tokens` is the token stylesheet, `sourceLock` is the source lock, and `designContract` is the design contract.
- `publicExports` lists the entry points applications import, with at least one entry. A primitive the composites are built from is not one of them.
- `litespec model check` reports a missing `ui.package` when a controlled frontend is declared without one, and validates every listed file. A controlled frontend is one whose platform is `react` or `svelte`.

### A `frontends` Entry

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `name` | string | yes | none |
| `path` | string | yes | none |
| `platform` | string | yes | none |
| `entryPoint` | string | no | the entry points of its screens |
| `refusalDictionary` | string | no | none |
| `routeSuite` | string | no | none |
| `visualGoldens` | string | no | none |
| `ui` | object | no | none |

What each field means and what reads it:

- `name` is the frontend name, such as `"web"`. It matches `^[a-z][a-z0-9-]*$`.
- `path` is the frontend directory, such as `"apps/web"`. `litespec model check` reports a frontend whose directory does not exist.
- `platform` is `react`, `svelte`, `blazor`, or `android`. Every entry declares it, so a frontend outside the controlled UI contract still declares that position instead of omitting the field. A `react` or `svelte` frontend is controlled and requires a `ui` block and the shared package. A `blazor` or `android` frontend is skipped by the controlled UI check.
- `entryPoint` names the browser or device entry point the frontend serves, such as `"entry-point/claimwick-app"`. The model builder reads the frontend's screens, use cases and records file through it. Without it, the entry points its screens name decide, and before any screen exists every browser entry point does.
- `refusalDictionary` names the file or folder holding that frontend's refusal dictionary: one sentence per refusal code, keyed by the refusal `code`. The model builder reads it for `refusal-sentence-missing`, so a refusal a screen can reach is shown from that frontend's own words.
- `routeSuite` names the file or folder holding that frontend's route suite. The model builder reads it for the route-suite `proves` check, so the suite has a named subject instead of a convention.
- `visualGoldens` names the folder holding that frontend's visual goldens. The model builder reads it for the same check, so a golden outside the named folder proves nothing.
- `ui` holds `profile`, which is required, and `globalCss`, which is optional. The profile is one of `public-light`, `application-balanced`, or `admin-dense`. The `globalCss` value is the one stylesheet the frontend declares, and it imports the shared package tokens. `litespec model check` checks the profile value and the stylesheet path.

### A `selectedExtensions` Entry

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `id` | string | yes | none |
| `criterion` | string | yes | none |
| `releaseStage` | string | for `stage` | none |
| `decision` | string | no, only on `audit` | none |

What each field means and what reads it:

- `id` is the extension id in the pinned manifest, such as `"locale"`. `litespec model check` refuses an id the pinned release does not ship and lists the known ids.
- `criterion` states the criterion that was met, such as `"The product ships in English and Dutch."`. `litespec/core/scope#record-selected-extensions` requires the entry, and `litespec/core/scope#select-conditional-extensions-explicitly` requires selecting an extension only when its criteria apply.
- `releaseStage` is `pre-production` or `production`, and only the `stage` extension carries it, which the schema enforces.
- `decision` names the audit adoption decision by its identifier, and only the `audit` extension carries it. `litespec rules` returns `litespec/ext/audit#record-the-audit-adoption-decision` for that decision alone.

### Placeholders and the Init Fill

The template names the project as `__PROJECT__` in paths and package names. `litespec init` fills every `__PROJECT__` value with the `--name` value, or with the root folder name when `--name` is omitted; in an npm package scope such as `@__PROJECT__/ui` it writes that name in lowercase kebab case, because npm names are lowercase. After it writes the file it prints `Next step: dotnet litespec model check`. `litespec model check` treats a leftover placeholder as an error and names each one:

```text
.litespec/project.yaml: template placeholder '__PROJECT__' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]
```

A preset without a controlled web frontend, such as `blazor-client` or `android-client`, declares no package that a frontend uses, so `litespec init` removes `ui` and `paths.uiPatterns` from the written file.

### A Complete Example

This example shows the file `litespec init --preset dotnet-react --size small --name shop` writes from the template and the `dotnet-react` preset. It ran against release `5.1.0`. The preset selects shape `server`, stacks `dotnet` and `react`, and one `react` frontend `web` at `apps/web`.

```yaml
# yaml-language-server: $schema=https://litespec.dev/5.1.0/schemas/litespec-project.schema.yaml
schemaVersion: 1
project:
  name: shop
size: small
shape: server
stacks:
- dotnet
- react
layers:
  intent: litespec
  behavior: litespec
  implementation: litespec
  proof: litespec
reviewedLiteSpecVersion: 5.1.0
screenshots:
  galleries:
  - apps/web/test-results/gallery
  pattern: '{route}--{theme}--{width}.png'
paths:
  docs: .litespec
  behaviorDocs: .litespec/behavior
  testRoots:
  - apps/api/tests
stackConfig:
  dotnet:
    modules: apps/api/src/shop.Modules
    sharedKernel: apps/api/src/shop.SharedKernel
frontends:
- name: web
  path: apps/web
  platform: react
ui:
  package:
    name: '@shop/ui'
    path: packages/ui
    componentsJson: packages/ui/components.json
    primitives: packages/ui/src/components
    tokens: packages/ui/src/styles/tokens.css
    sourceLock: packages/ui/ui-source-lock.yaml
    designContract: packages/ui/DESIGN.md
    publicExports:
    - '@shop/ui/floorplans'
selectedExtensions: []
```

The other three presets differ only in shape, stacks, frontends, and the shared package. `dotnet-svelte` selects shape `server`, stacks `dotnet` and `svelte`, and one `svelte` frontend `web` at `apps/web`. `blazor-client` selects shape `client-only`, stack `blazor`, and one `blazor` frontend `app` at `apps/app`, with no `ui` and no `uiPatterns`. `android-client` selects shape `client-only`, stack `android`, and one `android` frontend `android` at `apps/mobile/src/{ProjectName}.Android`, with no `ui` and no `uiPatterns`. The [init](../tools/init.md) page states all five presets. The records preset selects shape `server`, the `intent` and `behavior` layers, and no stacks or frontends.

### Validation Errors

`litespec model check` validates the project record, the pinned release, the stacks, the extensions, and the frontends. The messages below are quoted from the validator. The schema check runs first, so a wrong type or an unknown field reports a schema problem before these messages appear.

| Mistake | Message |
|:---|:---|
| A placeholder is left in the file | `.litespec/project.yaml: template placeholder '__PROJECT__' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| The installed release and the reviewed release differ | `.litespec/project.yaml: reviewedLiteSpecVersion '3.0.0' does not match the installed LiteSpec tool release 5.1.0; install the reviewed release with 'dotnet tool update LiteSpec.Cli --version 3.0.0' and 'dotnet tool restore' for the .NET tool, or rerun the install script in the project root for the standalone binary, or re-review the contract and record 5.1.0` |
| The reviewed release is missing | `.litespec/project.yaml: missing 'reviewedLiteSpecVersion'; the installed LiteSpec tool release is 5.1.0` |
| A stack id is unknown | `.litespec/project.yaml: stack 'rails' is not a stack the pinned LiteSpec release ships [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A client-only project names a backend stack | `.litespec/project.yaml: shape 'client-only' names backend stack 'dotnet'; a client-only project names no backend stack [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A server names zero or two backend stacks | `.litespec/project.yaml: stacks name 0 backend stacks; name exactly one of dotnet [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| An extension id is unknown | `.litespec/project.yaml: extensions 'search' is not an extension in the pinned LiteSpec release; known ids are ai, audit, bdd, cache, compat, concurrency, customer-rules, delivery, devices, integrations, jobs, layer-projects, lifecycle, locale, mcp, outbox, plans, realtime, stage, tenancy` |
| A frontend declares no platform | `.litespec/project.yaml: frontend 'web' declares no 'platform'; one of android, blazor, react, svelte is required [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A frontend names a stack the project does not select | `.litespec/project.yaml: frontend 'web' platform 'react' is not a stack the project names in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A controlled frontend has no shared package | `.litespec/project.yaml: 1 controlled frontend(s) and no 'ui.package'; the shared UI package every controlled web frontend composes is undeclared` |
| A configured path does not exist | `.litespec/project.yaml: paths.docs 'docs' does not exist; the page scan finds no record to check` |

The last row stands for every configured path: the message names the field, the missing path, and the check that goes quiet. The docs root, the glossary, each frontend directory, each `ui.package` file, and each frontend stylesheet follow this shape.

## Notes

The field lists follow `schemas/litespec-project.schema.yaml` and the consumer template. The init behavior follows `apps/cli/src/LiteSpec.Cli/Init/InitCommand.cs`. The validation messages follow `apps/cli/src/LiteSpec.Cli/Model/Consumer/ProjectFileChecks.cs` and `apps/cli/src/LiteSpec.Cli/Model/Consumer/GlossaryChecks.cs`. The path defaults follow `apps/cli/src/LiteSpec.Cli/Model/Configuration/ConsumerConfig.cs`. The presets are `templates/presets/dotnet-react.yaml`, `templates/presets/dotnet-svelte.yaml`, `templates/presets/blazor-client.yaml`, and `templates/presets/android-client.yaml`, read with `templates/consumer/project.yaml`. Example sections stay within 120 words; the project file carries no setting for the bound.
