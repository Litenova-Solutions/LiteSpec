# Formats

## Intent

Every file a LiteSpec tool reads or writes has one format, and every format a person writes has one schema (a contract stating its exact shape) that the checking command and the editor both validate. This page lists each format: the file, who writes it, its envelope, the command that reads or checks it, and its schema with a real example. An author in a consumer project uses it to wire editor validation and to look up a field, and a tool author uses it to find which command owns which file.

## Reference

People write YAML; programs exchange JSON. A file a person edits is YAML that states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment, as `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml` requires. A file a program writes for another program stays JSON. Every schema lives in `schemas/` at the repository root as YAML in JSON Schema draft 2020-12, and carries an `$id` equal to its published URL, so the copy at `https://litespec.dev/<version>/schemas/<file>` and the file in `schemas/` state the same contract. The [validate](../maintain/validate.md) and [model](../tools/model.md) pages state the commands that check each file.

### Every Format

| File | Written by | Syntax | Envelope | Schema |
|:---|:---|:---|:---|:---|
| `.litespec/project.yaml` | a person, first by `litespec init` | YAML | `schemaVersion: 1` | `litespec-project.schema.yaml` |
| Record files under `.litespec/` | a person | YAML | `type` | `record.schema.yaml` |
| `.litespec/glossary.yaml` | a person | YAML | `schemaVersion: 2` | `glossary.schema.yaml` |
| `.litespec/implementation/patterns/catalog.yaml` | a person | YAML | `schemaVersion: 2` | `ui-pattern-catalog.schema.yaml` |
| The `DESIGN.md` metadata block of the shared UI package | a person | YAML | `schemaVersion: 2` | `design-contract.schema.yaml` |
| `<ui package>/ui-source-lock.yaml` | a person records each registry component; `litespec model render` writes its digests, `preset` and `cli` | YAML | `schemaVersion: 2` | `ui-source-lock.schema.yaml` |
| `litespec.manifest.yaml` in the LiteSpec repository | a LiteSpec maintainer | YAML | `schemaVersion: 6` | `litespec-manifest.schema.yaml` |
| `.litespec/proof/checks/*.json` | a drill or harness | JSON | one check result | `check-result.schema.yaml` |
| `--format json` output, the book data, `docs/reference/provisions.json` and `docs/reference/pages.json` | the tools | JSON | the command's own shape | none |

### Manifest Schema

| Item | Value |
|:---|:---|
| File | `schemas/litespec-manifest.schema.yaml` |
| Validates | `litespec.manifest.yaml` in the LiteSpec repository |
| Checked by | `litespec-maintain validate` |
| Published URL | `https://litespec.dev/<version>/schemas/litespec-manifest.schema.yaml` |

The top-level properties name the release and what it ships. `schemaVersion` is the constant `6`. `version` is the release. `name` and `agentsEntry` name the standard and its agent entry point. `stacks`, `shapes`, and `extensions` list what a consumer project selects from. `stack` pins the toolchain versions a consumer targets, and `packages` pins every NuGet and npm package. `uiBaseline` and `uiBaselineSvelte` pin the controlled UI presets, both in the one `$defs/uiBaseline` shape with their own platform, system and base.

The release manifest opens with these fields:

```yaml
# yaml-language-server: $schema=./schemas/litespec-manifest.schema.yaml
schemaVersion: 6
version: 5.1.0
name: litespec
agentsEntry: AGENTS.md
```

### Consumer Project Schema

| Item | Value |
|:---|:---|
| File | `schemas/litespec-project.schema.yaml` |
| Validates | `.litespec/project.yaml` in a consumer repository |
| Checked by | `litespec model check`, and `litespec-maintain validate` for the template copy |
| Published URL | `https://litespec.dev/<version>/schemas/litespec-project.schema.yaml` |

The top-level properties describe the project. `schemaVersion` is the constant `1`. `project` holds the project name. `size` is `small` or `large`. `layers` names the claimed record layers and the practice each follows. `shape` is `server` or `client-only`. `stacks` lists the selected stacks, `stackConfig` holds per-stack configuration, `frontends` lists the frontends, and `ui` names the shared UI package. `reviewedLiteSpecVersion` records the LiteSpec release the project last reviewed. `paths` locates the specification tree, the glossary, the tests and the operations files. `selectedExtensions` lists the active extensions with their criterion. `refusals`, `marks`, `proof`, `dependencies`, and `screenshots` carry further project choices. The [project file](project-file.md) states every field.

The consumer template opens with these fields:

```yaml
# yaml-language-server: $schema=https://litespec.dev/5.1.0/schemas/litespec-project.schema.yaml
schemaVersion: 1
project:
  name: __PROJECT__
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
```

### Record Schema

| Item | Value |
|:---|:---|
| File | `schemas/record.schema.yaml` |
| Validates | The record file of each consumer record page and the frontmatter of each document |
| Checked by | `litespec model check` |
| Published URL | `https://litespec.dev/<version>/schemas/record.schema.yaml` |

The schema holds no single property list. Instead it carries a `oneOf` across the page variants, one per record type and page style, so each record file and each document frontmatter matches exactly one variant, such as `productPage`, `useCasePage`, `screenPage`, `ruleSetPage` or `howToPage`. Each variant requires `type` plus its own fields, such as `id` and `title`. A record present in git is decided; no variant carries approval, status, or history fields.

A list page carries its entries in `records`: requirement, permission, rule, result, scenario, event, entry-point, and setting entries. The identifier patterns sit beside the variants in `$defs`, such as `useCaseId` for `use-case/<module>.<name>`, `reactionId` for `reaction/<module>.<name>`, `jobId` for `job/<module>.<name>`, `journeyStepId` for `journey/<name>#s1`, and `derivedResultId` for the derived generic results. Shared shapes sit there too: the `access` object with `grant` and `tenant`, the query `output` groups, `paging`, `consistency`, `effectEntry` with exactly one verb of `runs`, `sends`, `emits`, or `calls`, the reaction trigger, the job kind and schedule, `idempotency`, `audit`, the one `onFailure` shape, and `dataClassEntry`.

A use case requires `kind`, which is `command` or `query`, and the `access` object. The entry point the use case is reached through states the caller `auth`; the use case states only `grant` and `tenant`:

```yaml
type: use-case
id: use-case/<module>.<name>
title: <Use case display title>
kind: command
access:
  grant: permission/<name>
  tenant: own
```

### Glossary Schema

| Item | Value |
|:---|:---|
| File | `schemas/glossary.schema.yaml` |
| Validates | The project glossary, by default `.litespec/glossary.yaml` |
| Checked by | `litespec model check` |
| Published URL | `https://litespec.dev/<version>/schemas/glossary.schema.yaml` |

The top-level properties hold the closed vocabulary. `schemaVersion` is the constant `2`. `terms` holds one entry per concept, each with its `id`, `term`, `meaning`, `layer` of `intent` or `behavior`, `aliases` naming the established synonyms, `labels`, `rejected` synonyms, `broader` and `narrower` terms, and closed-set `values`. `mannered` holds the metaphor terms the project rejects everywhere, each with its `term` and its `instead`.

The consumer glossary template carries real mannered entries:

```yaml
mannered:
- term: load-bearing
  instead: required
- term: one-way door
  instead: irreversible
```

### Design Contract Schema

| Item | Value |
|:---|:---|
| File | `schemas/design-contract.schema.yaml` |
| Validates | The YAML metadata block that opens the `DESIGN.md` a shared UI package ships |
| Checked by | `litespec model check` (`litespec/frontend/ui#publish-a-design-contract`) |
| Published URL | `https://litespec.dev/<version>/schemas/design-contract.schema.yaml` |

The properties bind the contract to its package. `schemaVersion` is the constant `2`. `package` names the shared UI package. `profile` names the density the worked example uses. `patterns` lists the floorplans the package provides.

The consumer template opens its design contract with these fields:

```yaml
schemaVersion: 2
package: "@__PROJECT__/ui"
profile: application-balanced
patterns:
  - list-page
  - entity-page
```

### UI Pattern Catalog Schema

| Item | Value |
|:---|:---|
| File | `schemas/ui-pattern-catalog.schema.yaml` |
| Validates | The workspace UI pattern catalog, `.litespec/implementation/patterns/catalog.yaml` unless `paths.uiPatterns` names another file |
| Checked by | `litespec model check` (`litespec/frontend/patterns#keep-one-pattern-catalog`) |
| Published URL | `https://litespec.dev/<version>/schemas/ui-pattern-catalog.schema.yaml` |

The top-level properties hold one decision per recurring UI question. `schemaVersion` is the constant `2`. `breakpoints` names the viewport widths in CSS pixels. `groups` holds the catalog groups. `patterns` holds the decisions, each with its `options`, chosen `decision`, `compact` behavior, and per-platform `implementations`.

The consumer template catalog opens with these fields:

```yaml
# yaml-language-server: $schema=https://litespec.dev/5.1.0/schemas/ui-pattern-catalog.schema.yaml
schemaVersion: 2
breakpoints:
  compact: 768
```

### UI Source Lock Schema

| Item | Value |
|:---|:---|
| File | `schemas/ui-source-lock.schema.yaml` |
| Validates | The source lock beside the one copy of the registry source |
| Checked by | `litespec model check` (`litespec/frontend/web/ui#track-source-changes`) |
| Published URL | `https://litespec.dev/<version>/schemas/ui-source-lock.schema.yaml` |

The team records each primitive it installs or changes in `components`, with its `name`, `address`, `status`, `paths`, and `dependencies`. `litespec model render` writes each `digest` from the primitive's source, and writes `cli` and `preset` from the manifest, so no person computes a digest or copies a preset. `schemaVersion` is the constant `2`. `generator` is `shadcn/ui` or `shadcn-svelte`, the CLI that installed the package. `registry` names where the source came from. `formatter` names the code formatter.

The consumer template lock opens with these fields:

```yaml
# yaml-language-server: $schema=https://litespec.dev/5.1.0/schemas/ui-source-lock.schema.yaml
schemaVersion: 2
generator: shadcn/ui
```

### Check Result Schema

| Item | Value |
|:---|:---|
| File | `schemas/check-result.schema.yaml` |
| Validates | Each JSON file a drill or harness writes under `.litespec/proof/checks/`, or under `paths.checkResults` |
| Checked by | `litespec model check`, which reports `proof-invalid` for a file that fails it |
| Published URL | `https://litespec.dev/<version>/schemas/check-result.schema.yaml` |

A check result is program output, so it is JSON; the schema is YAML like every other schema. The [model](../tools/model.md) page states how a result proves the record it names.

## Notes

A schema states one exact shape per release, with no compatible range between releases. A consumer adopting a later release absorbs every difference at that moment, under `litespec/core/authoring#publish-each-release-as-a-complete-contract` and `litespec/core/authoring#exclude-cross-release-compatibility-work`. When the published copy and the repository copy disagree, the repository copy wins, because every check reads `schemas/` from the checkout its tool belongs to.
