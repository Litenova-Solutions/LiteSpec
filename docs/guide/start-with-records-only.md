# Start with Records Only

## Purpose

A team that wants LiteSpec's records, book and checks before it changes any code follows this guide. It claims `intent` and `behavior` in the project file, writes the first Intent and Behavior records, and runs the model check and the book, while its code and tests keep their own layout and practice. The [adoption page](../learn/adopting-litespec.md) states the claims that come next, `implementation` and `proof`.

## Prerequisites

Start with:

- A consumer repository with a `.litespec/` folder at its root, or an empty repository where the folder is about to be written.
- The .NET SDK, or the standalone `litespec` binary from the GitHub release.
- The LiteSpec release the team adopts, which the project file records in `reviewedLiteSpecVersion`.

## Procedure

### Install the Release

Install the `litespec` command line as a local .NET tool at the release the team adopts:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version <release>
dotnet tool restore
```

Run the first command only when the project has no `.config/dotnet-tools.json` yet. A team without the .NET SDK installs the standalone binary instead, and the [CLI overview](../tools/README.md#install-the-cli) lists every install method. The installed release is the contract: it carries the schemas, docs and templates the version names, and it changes only when the team moves the version.

### Write the Project File

Write `.litespec/project.yaml` with the size, the shape and the layers the project claims. The size is required in every claim, because it sets which record types must exist. With `intent` and `behavior` alone, `stacks` may be empty, since no code reader runs for the claim.

```yaml
# yaml-language-server: $schema=https://litespec.dev/<release>/schemas/litespec-project.schema.yaml
schemaVersion: 1
project:
  name: shop
size: small
shape: server
stacks: []
layers:
  intent: litespec
  behavior: litespec
reviewedLiteSpecVersion: <release>
paths:
  docs: .litespec
  behaviorDocs: .litespec/behavior
selectedExtensions: []
```

Add a `selectedExtensions` entry for each extension the project selects, each with its criterion. The [project file reference](../reference/project-file.md) lists every field, and the [scope page](../core/scope.md#layer-claims) states what the claim covers.

### Tell the Agents

Add the `## LiteSpec` block to the project's `AGENTS.md`, so every agent working in the repository reads the same rules. The [init command page](../tools/init.md) states the block that `litespec init` writes for a new project, with the release, size, shape, stacks and selection index.

### Write the First Records

Write `.litespec/intent/product.yaml` first. It states what the product is, who it is for, and what it never does. Then run `dotnet litespec new <type> <id>` for each further record, which writes the record in its legal folder and reserves its identifier before any prose exists. Work one slice at a time, from the problem to the Behavior scenario that proves it, as the [model page](../core/model.md) describes.

A rule the team cannot meet yet gets a departure in `.litespec/governance/departures/`, as the [departure page](../core/departures.md) states.

### Check the Records

Run the model check, then build the book:

```bash
dotnet litespec model check
dotnet litespec book
```

The model check runs the check level G findings alone, so no finding names code layout, marks or proof. The book shows Intent, Behavior and governance, and it shows Implementation and Proof as not claimed.

## Check

- `dotnet litespec model check` reports no blocking finding from the consumer root.
- The book shows the first slice of Intent and Behavior, and marks Implementation and Proof as not claimed.
- `dotnet litespec rules` on a record identifier returns the provisions about records, and on a code path returns no provision, since the claim covers no code.
