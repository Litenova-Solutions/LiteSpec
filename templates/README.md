# Templates

You maintain the LiteSpec repository and change a seed file, or you author a consumer repository and copy one. After reading, you know which folder holds the file you need, who copies it, and how a change is proved.

## What It Is

`templates/` holds three folders of seed files. Consumer authors copy them by hand or through `litespec init`. The seeds for LiteSpec pages live here too, and the validator parses them as the pages they produce, so each seed always passes the contract it teaches.

## Quick Start

From the repository root, write a starting project file into a new consumer folder:

```bash
dotnet run --project apps/cli/src/LiteSpec.Cli -- init --preset dotnet-react --size small --root ../sample-store
```

The command merges the preset over `consumer/project.yaml`, stamps the release version and the project name, and writes `.litespec/project.yaml`. An existing project file is never overwritten.

## How It Is Organized

### Consumer Templates

The `consumer/` folder holds one starting point per record type and page style, plus the project file, the language file, the design contract, the UI catalog, and the source lock. The [consumer template index](consumer/README.md) maps each file to the consumer path it targets, from `.litespec/product.yaml` to `packages/ui/ui-source-lock.yaml`.

Each record template is a YAML record file shaped like [record.schema.yaml](../schemas/record.schema.yaml) with working example values. An article type's template also ships its explanation file, and a document style's template is a Markdown document. A value in `<...>` marks a placeholder for the author to write; `litespec-maintain validate` skips placeholders when validating shape. Replace every `<placeholder>` and `__PROJECT__` value before committing. `litespec init` fills the project placeholders in `.litespec/project.yaml` and lists the ones it leaves behind, and `litespec model check` rejects a project file that still holds one. Do not edit [consumer/README.md](consumer/README.md): it is the template index the validator checks.

### Presets

The `presets/` folder holds five YAML files: `android-client.yaml`, `blazor-client.yaml`, `dotnet-react.yaml`, `dotnet-svelte.yaml`, and `records.yaml`. Each names the shape, stacks, and frontends a new project starts from. The `android-client` and `blazor-client` presets select the `client-only` shape, while `dotnet-react` and `dotnet-svelte` select `server`. Running `litespec init --preset <name> --size <small|large>` reads the preset, merges it over `consumer/project.yaml`, and stamps the release version. A preset adds no rule: switching presets changes only the starting file.

### Standard Seeds

The `standard/` folder holds the seeds for LiteSpec pages: `topic.md`, `stack.md`, `extension.md`, `pattern.md`, `command.md`, `reference.md`, `how-to.md`, `tutorial.md`, `explanation.md`, and `index.md`. The validator parses each seed as the page class it produces, so every seed always passes the contract it teaches: `topic.md` as a topic page, `stack.md` as a stack page, `extension.md` as an extension page, `pattern.md` as a pattern page, `command.md` as a tools page, `reference.md` as a reference page, `how-to.md` as a guide page, `tutorial.md` as a tutorial page, `explanation.md` as an explanation page, and `index.md` as a section index. The glossary lives at exactly one path, `docs/reference/glossary.md`, so it has no seed.

## How It Works

`litespec init` accepts only the five shipped preset names and the two sizes, `small` and `large`. It reads `templates/presets/<name>.yaml` and `templates/consumer/project.yaml` from the release the tool carries, or from `LITESPEC_HOME` when set. It fills the project name from `--name` or from the target folder name, stamps `reviewedLiteSpecVersion` with the carried release, writes the file, and copies the scaffold with the `## LiteSpec` block of `AGENTS.md` filled in and the `__LITESPEC_COMMAND__` placeholder replaced by the command that runs the CLI. For `--install tool`, the default, it also writes `.config/dotnet-tools.json` pinning the tool at the release. With `--install standalone` it writes no tool manifest and names `litespec` as the command. An unreadable template, preset, or scaffold file exits 1, while an existing project file or an invalid option exits 2.

Record authors copy templates by hand into the paths the [consumer template index](consumer/README.md) maps. The standard seeds never get copied into consumers: the validator reads them directly as virtual pages.

## How to Change One Safely

- For a record template, keep the frontmatter shaped like [record.schema.yaml](../schemas/record.schema.yaml), with each author-written value in the `<...>` placeholder form the validator skips, and the body sections matching the page style the [authoring standard](../docs/core/authoring.md) defines. Copy the changed file into a scratch consumer and run `litespec model check --root <folder>` there.
- For `.litespec/project.yaml` or a preset, keep both valid against the project schema. `litespec-maintain validate` checks the shipped template. Keep the [consumer/README.md](consumer/README.md) rows in sync: every row names a file that exists, and every shipped file appears in a row.
- For a standard seed, keep it passing its virtual page contract. `litespec-maintain validate` parses each one.
- When the shape changes, update the CLI reader and its tests, with one passing case and one failing case.
- Run the checks in the [Verification section of AGENTS.md](../AGENTS.md#verification).

## How to Test It

- `litespec-maintain validate` covers the shipped project template, the template index in both directions, every record template against the record schema with placeholders skipped, and each standard seed as a virtual page.
- `litespec model check --root <folder>` proves a changed record template validates in place.
- `dotnet test --solution LiteSpec.slnx` runs the Init, Model, and Maintain suites with their fixtures.

## Limits

- Templates carry example values, not a migration. Adopting a new release means re-reading the templates, not diffing them.
- `litespec init` never overwrites an existing project file. Delete the existing file before running it again.
- Placeholders mark author input, not defaults. A committed file still holding one is unfinished.
