# Templates

## Intent

You maintain the LiteSpec repository and change a seed file, or you author a consumer repository and copy one. After reading this page you know which folder holds the file you need, who copies it, and how a change is proved.

## What It Is

`templates/` holds three folders of seed files. Consumer authors copy them by hand or through `litespec init`. The seeds for LiteSpec pages live here too, so the validator parses them as the pages they produce and a seed always passes the contract it teaches.

## Quick Start

From the repository root, write a starting project file into a new consumer folder:

```bash
dotnet run --project apps/cli/src/LiteSpec.Cli -- init --preset dotnet-react --size small --root ../sample-store
```

The command merges the preset over `consumer/project.yaml`, stamps the release version and the project name, and writes `.litespec/project.yaml`. It never overwrites an existing project file.

## How It Is Organized

### Consumer Templates

`consumer/` holds one starting point per record type and page style, plus the project file, the language file, the design contract, the UI catalog, and the source lock. [consumer/README.md](consumer/README.md) maps each file to the consumer path it targets, from `.litespec/intent/product.yaml` to `packages/ui/ui-source-lock.yaml`.

Each record template is a YAML record file shaped like [record.schema.yaml](../schemas/record.schema.yaml) with working example values; an article type's template also has its explanation file, and a document style's template is a Markdown document. A value in `<...>` is a placeholder where the author writes; `litespec-maintain validate` skips placeholders when it validates the shape. Replace every `<placeholder>` and `__PROJECT__` value before committing. `litespec init` fills the project placeholders in `.litespec/project.yaml` and lists the ones it leaves, and `litespec model check` refuses a project file that still holds one. Do not edit [consumer/README.md](consumer/README.md): it is the template index the validator checks.

### Presets

`presets/` holds five YAML files: `android-client.yaml`, `blazor-client.yaml`, `dotnet-react.yaml`, `dotnet-svelte.yaml`, and `records.yaml`. Each one names the shape, the stacks, and the frontends a new project starts from. `android-client` and `blazor-client` select the `client-only` shape, and `dotnet-react` and `dotnet-svelte` select `server`. `litespec init --preset <name> --size <small|large>` reads the preset, merges it over `consumer/project.yaml`, and stamps the release version. A preset adds no rule: switching presets changes the starting file only.

### Standard Seeds

`standard/` holds the seeds for LiteSpec pages: `topic.md`, `stack.md`, `extension.md`, `pattern.md`, `command.md`, `reference.md`, `how-to.md`, `tutorial.md`, `explanation.md`, and `index.md`. The validator parses each seed as the page class it produces, so the seed always passes the contract it teaches: `topic.md` as a topic page, `stack.md` as a stack page, `extension.md` as an extension page, `pattern.md` as a pattern page, `command.md` as a tools page, `reference.md` as a reference page, `how-to.md` as a guide page, `tutorial.md` as a tutorial page, `explanation.md` as an explanation page, and `index.md` as a section index. A glossary has one path, `docs/reference/glossary.md`, so it has no seed.

## How It Works

`litespec init` accepts only the five shipped preset names and the two sizes, `small` and `large`. It reads `templates/presets/<name>.yaml` and `templates/consumer/project.yaml` from the release the tool carries, or from `LITESPEC_HOME` when set. It fills the project name from `--name` or from the target folder name, stamps `reviewedLiteSpecVersion` with the release the tool carries, writes the file, copies the scaffold with the `## LiteSpec` block of `AGENTS.md` filled and the `__LITESPEC_COMMAND__` placeholder replaced by the command that runs the CLI, and, for `--install tool`, the default, writes `.config/dotnet-tools.json` pinning the tool at the release. `--install standalone` writes no tool manifest and names `litespec` as the command. It reports an unreadable template, preset, or scaffold file with exit code 1, and an existing project file or an invalid option with exit code 2.

Record authors copy templates by hand into the paths [consumer/README.md](consumer/README.md) maps. The standard seeds are never copied into consumers: the validator reads them directly as virtual pages.

## How to Change One Safely

- For a record template, keep the frontmatter shaped like [record.schema.yaml](../schemas/record.schema.yaml), with every author-written value in the `<...>` placeholder form the validator skips, and the body sections matching the page style the [authoring standard](../docs/core/authoring.md) defines. Copy the changed file into a scratch consumer and run `litespec model check --root <folder>` there.
- For `.litespec/project.yaml` or a preset, keep both valid against the project schema. `litespec-maintain validate` checks the shipped template. Keep [consumer/README.md](consumer/README.md) rows in sync: every row names a file that exists, and every shipped file appears in a row.
- For a standard seed, keep it passing its virtual page contract. `litespec-maintain validate` parses each one.
- Update the CLI reader and its tests when the shape changes, with one passing case and one failing case.
- Run the checks in the [Verification section of AGENTS.md](../AGENTS.md#verification).

## How to Test It

- `litespec-maintain validate` covers the shipped project template, the template index in both directions, every record template against the record schema with placeholders skipped, and the standard seeds as virtual pages.
- `litespec model check --root <folder>` proves a changed record template validates in place.
- `dotnet test --solution LiteSpec.slnx` runs the Init, Model, and Maintain suites with their fixtures.

## Limits

- Templates carry example values, not a migration. Adopting a new release means re-reading the templates, not diffing them.
- `litespec init` never overwrites an existing project file. Remove the existing file before running it again.
- Placeholders are author input, not defaults. A committed file holding one is unfinished.
