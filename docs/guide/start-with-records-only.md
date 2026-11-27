# Start with Records Only

Start with records alone: claim `intent` and `behavior` in the project file, write the first Intent and Behavior records, and run the model check and the book, while your code and tests keep their own layout and practice. You get checked records and a readable book before you change any code.

The [adoption page](../learn/adopting-litespec.md) states the claims that come next, `implementation` and `proof`.

## Prerequisites

Start with:

- A consumer repository with a `.litespec/` folder at its root, or an empty repository where you are about to write the folder.
- The .NET SDK, or the standalone `litespec` binary from the GitHub release.
- The LiteSpec release your team adopts, which the project file records in `reviewedLiteSpecVersion`.

## Procedure

1. Install the release your team adopts. Follow [Install the CLI](install-the-cli.md), and prefer the local .NET tool, because it pins the release in the repository. When your team has no .NET SDK, install the standalone binary from the GitHub release instead. For example, a team adopting 7.0.0 commits the local tool pin, so every machine restores 7.0.0. The installed release is the contract: it carries the schemas, docs, and templates for that version, and it changes only when your team moves the version.
2. Write `.litespec/project.yaml` with the size, the shape, and the layers your project claims. The size is required in every claim, because it sets which record types have to exist. With `intent` and `behavior` alone, `stacks` may be empty, since no code reader runs for this claim.

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

   Add a `selectedExtensions` entry, with its criterion, for each extension your project selects. The [project file reference](../reference/project-file.md) lists every field, and the [scope page](../core/scope.md#layer-claims) states what the claim covers.
3. Keep the `agent-protocol` region in the project `AGENTS.md` current, so every agent in the repository reads the same rules. For a new project, `litespec init` writes that region with the release, size, shape, stacks, layers, extensions, and selection index, as the [init command reference](../tools/init.md) describes. Later, `litespec model render` rewrites it, as the [model command reference](../tools/model.md) describes. Run `litespec model check` to report a missing or drifted region.
4. Write `.litespec/product.yaml` first. It states what the product is, who it is for, and what it never does. Then scaffold each further record with `dotnet litespec new <type> <id>`, which writes the record in its legal folder and reserves its identifier before any prose exists. Write the Intent of each slice before its Behavior, from the problem to the Behavior scenario that proves it. For example, a shop team writes the problem of late refunds, the feature that answers it, and the decide-refund use case with its scenario, in that order. Independent slices may run at the same time.
5. When your team cannot meet a rule yet, record a gap waiver in `.litespec/governance/waivers/`, as the [waiver page](../core/waivers.md) states. For example, a team that has not written every flow yet waives the finding on the missing flow until the next slice lands.
6. Run the model check, then serve the book:

   ```bash
   dotnet litespec model check
   dotnet litespec book
   ```

   The model check runs the check level G findings alone, so no finding names code layout, marks, or proof. The book serves Intent, Behavior, and governance on localhost, and it shows Implementation and Proof as not claimed.

Agents editing the records read the warm model from `litespec serve` and the path-scoped rules from `litespec agent sync`, as the [Serve](../tools/serve.md) and [Agent Sync](../tools/agent-sync.md) pages state.

## Check

- `dotnet litespec model check` reports no blocking finding from the consumer root.
- The served book shows the first slice of Intent and Behavior, and marks Implementation and Proof as not claimed.
- `dotnet litespec rules` on a record identifier returns the provisions about records, and on a code path returns no provision, because the claim covers no code.
