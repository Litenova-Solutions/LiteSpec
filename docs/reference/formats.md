# Formats

Every file a LiteSpec tool reads or writes has one format. Every format a person writes has one schema, a contract stating its exact shape, which the checking command and the editor both validate. The format list names each file, who writes it, its envelope, the command checking it, and its schema with a real example.

A consumer author uses the list to wire editor validation and to look up a field. A tool author uses it to find which command owns each file.

## Reference

People write YAML, and programs exchange JSON. A file a person edits is YAML: it states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment, as `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml` states. A file one program writes for another stays JSON.

Every schema lives in `schemas/` at the repository root, written as YAML in JSON Schema draft 2020-12, with an `$id` equal to its published URL. The copy at `https://litespec.dev/<version>/schemas/<file>` and the file in `schemas/` state the same contract, and the [validate](../maintain/validate.md) and [model](../tools/model.md) pages list the commands checking each file.

### Format List

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

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at [litespec-manifest.schema.yaml](../../schemas/litespec-manifest.schema.yaml).

### Consumer Project Schema

| Item | Value |
|:---|:---|
| File | `schemas/litespec-project.schema.yaml` |
| Validates | `.litespec/project.yaml` in a consumer repository |
| Checked by | `litespec model check`, and `litespec-maintain validate` for the template copy |
| Published URL | `https://litespec.dev/<version>/schemas/litespec-project.schema.yaml` |

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at [litespec-project.schema.yaml](../../schemas/litespec-project.schema.yaml). The [project file](project-file.md) shows how the file is written and validated, with a complete example and the validation messages.

### Record Schema

| Item | Value |
|:---|:---|
| File | `schemas/record.schema.yaml` |
| Validates | The record file of each consumer record page and the frontmatter of each document |
| Checked by | `litespec model check` |
| Published URL | `https://litespec.dev/<version>/schemas/record.schema.yaml` |

The schema carries one page variant per record type and page style, with the identifier patterns and shared shapes beside the variants in `$defs`. Read the full shape at [record.schema.yaml](../../schemas/record.schema.yaml). The [record folders](folders.md) list where each record type lives, and the [consumer templates](../../templates/consumer/README.md) show every field of each type.

### Glossary Schema

| Item | Value |
|:---|:---|
| File | `schemas/glossary.schema.yaml` |
| Validates | The project glossary, by default `.litespec/glossary.yaml` |
| Checked by | `litespec model check` |
| Published URL | `https://litespec.dev/<version>/schemas/glossary.schema.yaml` |

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at [glossary.schema.yaml](../../schemas/glossary.schema.yaml).

### Design Contract Schema

| Item | Value |
|:---|:---|
| File | `schemas/design-contract.schema.yaml` |
| Validates | The YAML metadata block that opens the `DESIGN.md` a shared UI package ships |
| Checked by | `litespec model check` (`litespec/packs/svelte/ui-primitives#declare-one-visual-reference`) |
| Published URL | `https://litespec.dev/<version>/schemas/design-contract.schema.yaml` |

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at [design-contract.schema.yaml](../../schemas/design-contract.schema.yaml).

### UI Pattern Catalog Schema

| Item | Value |
|:---|:---|
| File | `schemas/ui-pattern-catalog.schema.yaml` |
| Validates | The workspace UI pattern catalog, `.litespec/implementation/patterns/catalog.yaml` unless `paths.uiPatterns` names another file |
| Checked by | `litespec model check` |
| Published URL | `https://litespec.dev/<version>/schemas/ui-pattern-catalog.schema.yaml` |

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at `ui-pattern-catalog.schema.yaml`.

### UI Source Lock Schema

| Item | Value |
|:---|:---|
| File | `schemas/ui-source-lock.schema.yaml` |
| Validates | The source lock beside the one copy of the registry source |
| Checked by | `litespec model check` (`litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream`) |
| Published URL | `https://litespec.dev/<version>/schemas/ui-source-lock.schema.yaml` |

The schema states every field, and the table above names the file, the target, the checker, and the URL. Read the full shape at [ui-source-lock.schema.yaml](../../schemas/ui-source-lock.schema.yaml).

### Check Result Schema

| Item | Value |
|:---|:---|
| File | `schemas/check-result.schema.yaml` |
| Validates | Each JSON file a drill or harness writes under `.litespec/proof/checks/`, or under `paths.checkResults` |
| Checked by | `litespec model check`, which reports `proof-invalid` for a file that fails it |
| Published URL | `https://litespec.dev/<version>/schemas/check-result.schema.yaml` |

A check result is program output, so it stays JSON, while the schema is YAML like every other schema. The [model](../tools/model.md) page shows how a result proves the record it names.

## Notes

A schema states one exact shape per release, with no compatible range between releases. A consumer adopting a later release takes in every difference at that moment, under `litespec/core/authoring#publish-each-release-as-a-complete-contract` and `litespec/core/authoring#exclude-cross-release-compatibility-work`. When the published copy and the repository copy disagree, the repository copy wins, since every check reads `schemas/` from the checkout its own tool belongs to.
