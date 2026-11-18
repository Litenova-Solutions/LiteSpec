# Project File

`.litespec/project.yaml` is the single record of what a consumer project selected: its size, shape, stacks, extensions, paths, and pinned release. A consumer project is an application repository built against one pinned LiteSpec release. The team edits the file when the selection changes, and validates it with `litespec model check` before relying on it.

## Reference

The [supported scope](../core/scope.md) page states which sizes, shapes, stacks, and extensions a project can select. The [stacks](../core/stacks.md) page states what selecting a stack applies. The [init](../tools/init.md) page states how a new file is written, and the [model](../tools/model.md) page states how the file is validated and how paths feed the project model. Terms such as stack, shape, size, extension, and preset follow the [glossary](glossary.md).

### File Location and Schema

| Item | Value |
|:---|:---|
| Path | `.litespec/project.yaml` at the consumer root |
| Schema | `schemas/litespec-project.schema.yaml` in the pinned release |
| Template | `templates/consumer/project.yaml` in the release the tool carries |
| Presets | `templates/presets/android-client.yaml`, `templates/presets/blazor-client.yaml`, `templates/presets/dotnet-react.yaml`, `templates/presets/dotnet-svelte.yaml`, `templates/presets/records.yaml` |

`litespec init` merges the template with one preset, and fills the size, name, and reviewed release. `litespec model check` validates the written file against the schema, then runs the selection checks below. The model builder reads the paths, `stackConfig`, size, shape, and stacks from the same file.

### Top-Level Fields

The schema states every top-level field, its type, and whether it is required; read it at [litespec-project.schema.yaml](../../schemas/litespec-project.schema.yaml). `litespec model check` checks the selection, and the model builder reads the size, shape, stacks, layers, and paths from the same file.

### The `layers` Object

The schema states the layers a project claims and the practice each follows; the [supported scope](../core/scope.md#layer-claims) states what each claim covers. `litespec model check`, the book, `litespec rules`, and the analyzers report only the claimed layers.

### The `explanations` Object

The schema states the layers whose record pages carry explanation files. A page on a listed layer without its explanation file, or with a required section missing, is an `explanation-missing` finding, and a listed layer the project does not claim is a `project-invalid` finding.

### The `project` Object

The schema states the project name. `litespec init` sets it from `--name`, or from the root folder name when `--name` is omitted.

### The `proof` Object

The schema states the step classes that tune how the model builder confirms proof claims. The model builder reads both lists when it confirms the project's proof claims.

### The `dependencies` Object

The schema states the licenses the project accepts. A shipped package under any other license needs an admitting decision naming it.

### The `screenshots` Object

The schema states the gallery folders and the capture file-name pattern. The model builder reads the captures from those folders.

### The `refusals` Object

The schema states the codes the derived generic results carry and the statuses, aliases, and composed templates the project declares. The model builder reads them when it derives the generic results and when proof confirmation matches a test's asserted status or client code to a declared refusal.

### The `paths` Object

The schema states every location the tools read, with its default. The model builder reads the records, tests, contracts, and operations files from these paths.

### The `stackConfig.dotnet` Object

The schema states the .NET projects the model builder reads, with their defaults. The `modules` pattern expands to every matching folder, so `apps/api/src/Modules/*` reads one project per module; a single project holding every module stays allowed as `apps/api/src/*.Modules`. Every other `*` in one segment expands to the first folder matching it. The `kernel` project holds the business values every module speaks, and the `platform` project holds the technical capabilities.

### The `ui.package` Object

The schema states the files of the workspace's one shared UI package. `litespec model check` reports a missing `ui.package` when a controlled frontend is declared without one, and validates every listed file.

### A `frontends` Entry

The schema states every field of a frontend entry. The model builder reads each frontend's directory, route suite, and visual goldens from it, and `litespec model check` checks the platform value, the shared package a controlled frontend needs, and the stylesheet path.

### A `selectedExtensions` Entry

The schema states the extension id, the criterion that was met, and the `releaseStage` and `decision` a selection carries. `litespec model check` refuses an id the pinned release does not ship, and lists the known ids.

### Placeholders and the Init Fill

The template names the project as `__PROJECT__` in paths and package names. `litespec init` fills every `__PROJECT__` value with the `--name` value, or with the root folder name when `--name` is omitted. In an npm package scope such as `@__PROJECT__/ui`, it writes that name in lowercase kebab case, because npm names are lowercase. After writing the file, it prints `Next step: dotnet litespec model check`. `litespec model check` treats a leftover placeholder as an error and names each one:

```text
.litespec/project.yaml: template placeholder '__PROJECT__' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]
```

A preset without a controlled web frontend, such as `blazor-client` or `android-client`, declares no package that a frontend uses, so `litespec init` removes `ui` and `paths.uiPatterns` from the written file in that case.

### A Complete Example

This example shows the file `litespec init --preset dotnet-react --size small --name shop` writes from the template and the `dotnet-react` preset, run against release `7.0.0`. The preset selects shape `server`, stacks `dotnet` and `react`, and one `react` frontend `web` at `apps/web`.

```yaml
# yaml-language-server: $schema=https://litespec.dev/7.0.0/schemas/litespec-project.schema.yaml
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
reviewedLiteSpecVersion: 7.0.0
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
    modules: apps/api/src/Modules/*
    kernel: apps/api/src/shop.Kernel
    platform: apps/api/src/shop.Platform
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

The other three presets differ only in shape, stacks, frontends, and the shared package. `dotnet-svelte` selects shape `server`, stacks `dotnet` and `svelte`, and one `svelte` frontend `web` at `apps/web`. `blazor-client` selects shape `client-only`, stack `blazor`, and one `blazor` frontend `app` at `apps/app`, with no `ui` and no `uiPatterns`. `android-client` selects shape `client-only`, stack `android`, and one `android` frontend `android` at `apps/mobile/src/{ProjectName}.Android`, with no `ui` and no `uiPatterns`. The [init](../tools/init.md) page states all five presets; the records preset selects shape `server`, the `intent` and `behavior` layers, and no stacks or frontends.

### Validation Errors

`litespec model check` validates the project record, the pinned release, the stacks, the extensions, and the frontends. The messages below are quoted from the validator. The schema check runs first, so a wrong type or an unknown field reports a schema problem before any of these messages appear.

| Mistake | Message |
|:---|:---|
| A placeholder is left in the file | `.litespec/project.yaml: template placeholder '__PROJECT__' is not filled [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| The installed release and the reviewed release differ | `.litespec/project.yaml: reviewedLiteSpecVersion '3.0.0' does not match the installed LiteSpec tool release 7.0.0; install the reviewed release with 'dotnet tool update LiteSpec.Cli --version 3.0.0' and 'dotnet tool restore' for the .NET tool, or rerun the install script in the project root for the standalone binary, or re-review the contract and record 7.0.0` |
| The reviewed release is missing | `.litespec/project.yaml: missing 'reviewedLiteSpecVersion'; the installed LiteSpec tool release is 7.0.0` |
| A stack id is unknown | `.litespec/project.yaml: stack 'rails' is not a stack the pinned LiteSpec release ships [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A client-only project names a backend stack | `.litespec/project.yaml: shape 'client-only' names backend stack 'dotnet'; a client-only project names no backend stack [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A server names zero or two backend stacks | `.litespec/project.yaml: stacks name 0 backend stacks; name exactly one of dotnet [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| An extension id is unknown | `.litespec/project.yaml: extensions 'search' is not an extension in the pinned LiteSpec release; known ids are ai, audit, bdd, cache, compat, concurrency, customer-rules, delivery, devices, integrations, jobs, layer-projects, lifecycle, locale, mcp, outbox, plans, realtime, stage, tenancy` |
| A frontend declares no platform | `.litespec/project.yaml: frontend 'web' declares no 'platform'; one of android, blazor, react, svelte is required [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A frontend names a stack the project does not select | `.litespec/project.yaml: frontend 'web' platform 'react' is not a stack the project names in stacks [litespec/core/scope#select-a-size-a-shape-and-stacks]` |
| A controlled frontend has no shared package | `.litespec/project.yaml: 1 controlled frontend(s) and no 'ui.package'; the shared UI package every controlled web frontend composes is undeclared` |
| A configured path does not exist | `.litespec/project.yaml: paths.docs 'docs' does not exist; the page scan finds no record to check` |

The last row stands for every configured path: the message names the field, the missing path, and the check that goes quiet. The docs root, the glossary, each frontend directory, each `ui.package` file, and each frontend stylesheet all report in this shape.

## Notes

The fields are stated in `schemas/litespec-project.schema.yaml` and the consumer template. The init behavior follows `apps/cli/src/LiteSpec.Cli/Init/InitCommand.cs`. The validation messages follow `apps/cli/src/LiteSpec.Cli/Model/Consumer/ProjectFileChecks.cs` and `apps/cli/src/LiteSpec.Cli/Model/Consumer/GlossaryChecks.cs`. The path defaults follow `apps/cli/src/LiteSpec.Cli/Model/Configuration/ConsumerConfig.cs`. The presets are `templates/presets/dotnet-react.yaml`, `templates/presets/dotnet-svelte.yaml`, `templates/presets/blazor-client.yaml`, `templates/presets/android-client.yaml`, and `templates/presets/records.yaml`, read with `templates/consumer/project.yaml`. Example sections stay within 120 words, and the project file carries no setting for that bound.
