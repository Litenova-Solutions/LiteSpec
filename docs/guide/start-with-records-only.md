# Start with Records Only

Start with records alone: claim `intent` and `behavior` in the project file, write the first Intent and Behavior records, and run the model check and the book, while the code and tests keep their own layout and practice.

The [adoption page](../learn/adopting-litespec.md) states the claims that come next, `implementation` and `proof`.

## Prerequisites

Start with:

- A consumer repository with a `.litespec/` folder at its root, or an empty repository where you are about to write the folder.
- The .NET SDK, or the standalone `litespec` binary from the GitHub release.
- The LiteSpec release the team adopts, which the project file records in `reviewedLiteSpecVersion`.

## Procedure

### Install the Release

Install the command line at the release the team adopts, following [Install the CLI](install-the-cli.md). Prefer the local .NET tool, because it pins the release in the repository; when the team has no .NET SDK, install the standalone binary from the GitHub release instead. The installed release is the contract: it carries the schemas, docs, and templates for that version, and it changes only when the team moves the version.

### Write the Project File

Write `.litespec/project.yaml` with the size, the shape, and the layers the project claims. The size is required in every claim, because it sets which record types have to exist. With `intent` and `behavior` alone, `stacks` may be empty, since no code reader runs for this claim.

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

Add a `selectedExtensions` entry, with its criterion, for each extension the project selects. The [project file reference](../reference/project-file.md) lists every field, and the [scope page](../core/scope.md#layer-claims) states what the claim covers.

### Tell the Agents

Keep the `agent-protocol` region in the project `AGENTS.md` current, so every agent in the repository reads the same rules. For a new project, `litespec init` writes that region with the release, size, shape, stacks, layers, extensions, and selection index, as the [init command reference](../tools/init.md) describes. Later, `litespec model render` rewrites it, as the [model command reference](../tools/model.md) describes. Run `litespec model check` to report a missing or drifted region.

### Write the First Records

Write `.litespec/product.yaml` first. It states what the product is, who it is for, and what it never does. Then scaffold each further record with `dotnet litespec new <type> <id>`, which writes the record in its legal folder and reserves its identifier before any prose exists. Write the Intent of each slice before its Behavior, from the problem to the Behavior scenario that proves it, as the [model page](../core/model.md) describes. Independent slices may run at the same time.

When the team cannot meet a rule yet, record a gap waiver in `.litespec/governance/waivers/`, as the [waiver page](../core/waivers.md) states.

### Check the Records

Run the model check, then serve the book:

```bash
dotnet litespec model check
dotnet litespec book
```

The model check runs the check level G findings alone, so no finding names code layout, marks, or proof. The book serves Intent, Behavior, and governance on localhost, and it shows Implementation and Proof as not claimed.

## Check

- `dotnet litespec model check` reports no blocking finding from the consumer root.
- The served book shows the first slice of Intent and Behavior, and marks Implementation and Proof as not claimed.
- `dotnet litespec rules` on a record identifier returns the provisions about records, and on a code path returns no provision, because the claim covers no code.
