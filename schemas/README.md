# Schemas

## Intent

You maintain the LiteSpec repository and change a JSON contract, or you build a consumer tool that reads one. After reading this page you know what each schema owns, which command reads it, and how to change it without breaking either side.

## What It Is

`schemas/` holds the seven JSON Schema contracts of the release. Each file validates one artifact: the release manifest, the consumer project file, the record file, the glossary, the design contract, the pattern catalog, or the source lock. Every schema `$id` names the release under `https://litespec.dev/<version>/schemas/`, so a pinned consumer keeps resolving the shape it reviewed.

## Quick Start

Check every schema and its consumers from the repository root:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
```

The command checks each `$id` against the manifest version, validates the manifest and the shipped project template against their schemas, and reports every schema, template, and keyword problem with its diagnostic code.

## How It Is Organized

| Schema | Validates | Read by |
|:---|:---|:---|
| `litespec-manifest.schema.yaml` | `litespec.manifest.yaml` | `litespec-maintain validate` |
| `litespec-project.schema.yaml` | `.litespec/project.yaml` | `litespec model check`, `litespec init` |
| `record.schema.yaml` | Record file of every consumer record page | `litespec model check` |
| `glossary.schema.yaml` | `docs/glossary.yaml` | `litespec model check` |
| `design-contract.schema.yaml` | Metadata block of `DESIGN.md` | `litespec model check` |
| `ui-pattern-catalog.schema.yaml` | `docs/ui/pattern-catalog.json` | `litespec model check` |
| `ui-source-lock.schema.yaml` | `packages/ui/ui-source-lock.yaml` | `litespec model check` |

The shared evaluator lives in `apps/model/src/LiteSpec.Standards/Schemas/`: `JsonSchema.cs` is the entry every reader above calls, `SchemaGate.cs` rejects any keyword the evaluator does not implement, and `SchemaEvaluator.cs` validates, so one validation path serves all seven schemas.

### Litespec Manifest Schema

`litespec-manifest.schema.yaml` (`schemaVersion` 5) owns the shape of [litespec.manifest.yaml](../litespec.manifest.yaml): the version, the stacks with their folders and pairing rules, the shapes, the extensions with their paths and activation scopes, and the toolchain, package, and UI baseline pins. `litespec-maintain validate` reads the manifest through it, and the `$id` check ties every schema file to the manifest version.

### Litespec Project Schema

`litespec-project.schema.yaml` owns the shape of the consumer project file: the project name, the size, the stacks, the reviewed LiteSpec release, the documentation and code paths, and the selected extensions. `litespec model check` validates the consumer file through it, `litespec init` writes that file from a preset, and `litespec-maintain validate` proves the shipped template in [templates/consumer/project.yaml](../templates/consumer/project.yaml) meets it.

### Record Schema

`record.schema.yaml` owns the record file of every consumer record page: 38 page shapes from product brief through feature, requirement, aggregate, use case, reaction, job, notification, setting, screen, and integration event to reference. A requirement is a page of its own in its feature's folder. The other entry records, for permissions, rules, results, scenarios, events, entry points, and settings, sit on the page that holds them. `litespec model check` validates each record file through it and then checks the sections the schema cannot see.

### Language Schema

`glossary.schema.yaml` (`schemaVersion` 2) owns the shape of the consumer glossary: one Term entry per concept with its meaning, labels, rejected synonyms, broader and narrower terms, and closed-set values, plus the mannered terms no page may use. `litespec model check` validates the record through it and then checks prose against the terms.

### Design Contract Schema

`design-contract.schema.yaml` (`schemaVersion` 1, `kind` design-contract) owns the metadata block that opens the `DESIGN.md` a shared UI package ships: the package name, the density profile, the shell, and the floorplans it provides. `litespec model check` validates the block through it and then checks that the package implements what the block promises.

### UI Pattern Catalog Schema

`ui-pattern-catalog.schema.yaml` (`schemaVersion` 1, `kind` ui-pattern-catalog) owns the shape of the consumer pattern catalog: the breakpoints, the groups, and one decision per recurring interface question with its options, its choice, its compact behavior, and its implementations per platform. `litespec model check` validates the catalog through it and then checks the references the schema cannot see across properties.

### UI Source Lock Schema

`ui-source-lock.schema.yaml` (`schemaVersion` 1) owns the shape of the lock beside the one copy of the registry source: the generator (`shadcn/ui` or `shadcn-svelte`), the CLI version, the decoded preset, the registry, and the digest of every installed primitive. `litespec model check` validates the lock through it, so a hand edit to copied source reports instead of passing unreviewed.

## How It Works

Each reader loads its schema file and passes values to the shared evaluator. The evaluator validates the value against JSON Schema draft 2020-12 and reports each mismatch with its location. The readers add the checks JSON Schema cannot express: the manifest `$id` home and version, the project selection rules, the explanation and document sections, the glossary prose check, the design contract implementation check, the catalog cross-property references, and the lock digest check.

## How to Change One Safely

- Read the readers first: the validator files named in the table and their tests under `apps/cli/tests/LiteSpec.Cli.Tests/` (`Consumer/`, `Ui/`) and `apps/maintain/tests/LiteSpec.Maintain.Tests/` (`Authoring/StandardsValidatorTests.Schemas.cs`), with fixtures under `Fixtures/`.
- Use the draft 2020-12 keywords `JsonSchema.Keywords` lists: every assertion and applicator except `unevaluatedProperties`, `unevaluatedItems` and `contentSchema`, with `$ref` to a local `#/` pointer and no `$anchor` or `$dynamicRef`. `litespec-maintain validate` fails a schema that uses any other keyword (`SCHEMA_UNSUPPORTED_KEYWORD`). A `pattern` is a .NET regular expression, which names a Unicode category by its short name, such as `\p{L}`.
- Update every consumer of the shape in the same change: the shipped template in [templates/](../templates/README.md), the CLI reader, and the fixtures that cover it.
- Keep the `$id` on the published home with the manifest version. Change the `schemaVersion` const only together with the template, reader, and fixture updates the new shape needs.
- Run the checks in the [Verification section of AGENTS.md](../AGENTS.md#verification).

## How to Test It

- `litespec-maintain validate` covers the manifest, the shipped project template, the schema IDs, and the template index.
- `dotnet test --solution LiteSpec.slnx` runs the Consumer, Ui, Model, and Init suites with their fixtures, and the Maintain suite for the authoring checks.
- Add one passing case and one failing case for a new rule, in both directions like the existing schema tests.

## Limits

- Validation proves shape only. Cross-file references stay the job of the owning validator, as the reader column shows.
- Schemas never migrate a consumer. A consumer pins a release and adopts the next whole contract at once.
