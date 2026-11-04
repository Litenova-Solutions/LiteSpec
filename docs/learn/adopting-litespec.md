# Adopt LiteSpec

## Intent

Adopting LiteSpec means pinning one release (building against one published version of the standard), describing the project, writing the first records (short YAML files stating product intent), and building to the standard with the checks in place. This page walks through that sequence in the order teams usually follow it, including what to record when a rule does not fit yet. Technical leads read it when they plan the rollout, so the team starts small and every departure stays visible instead of silent.

## Fit Check

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends, or a client-only app with no backend of its own. Check the [scope page](../core/scope.md) first. If the system is several services, or something other than a business application, LiteSpec is not built for it.

## Adoption Steps

Teams usually adopt LiteSpec in six steps, from the install to the checks. For the exact commands on a new repository, follow [Start a consumer repository](../tutorial/getting-started.md).

### 1. Install the Release

Install the `litespec` command line as a local .NET tool at the release the team adopts, such as `4.0.0`:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 4.0.0
dotnet tool restore
```

Run the first command only when the project has no `.config/dotnet-tools.json` yet. The installed release is the contract: it carries the manifest, schemas, docs, and templates the version names, and it never changes until the version moves, so a new LiteSpec release never breaks the build by surprise. The team adopts a newer release when it chooses, by moving the version and reading the new release.

### 2. Describe the Project

Run `dotnet litespec init` with the preset closest to the stack and the size:

```bash
dotnet litespec init --preset dotnet-react --size small
```

It writes `.litespec/project.json` with the size, shape, and stacks, records the release the team pinned in `reviewedLiteSpecVersion`, and writes the starting agent context, glossary, and example cast beside it. Fill in the paths, name the refusal codes in `refusals`, and select the extensions the project needs. The [selection page](https://litespec.dev/4.0.0/select/) shows which pages the choices apply.

### 3. Tell the Agents

`litespec init` writes the `## LiteSpec` block into the project's `AGENTS.md`, with the release, size, shape, stacks, and selection index of the new project. For an existing project, the selection page gives you the block to paste. From then on, every agent working in the repository loads the same rules.

### 4. Write the First Records

Start with the product: one `.litespec/intent/product.yaml` that says what the product is, who it is for, and what it never does. Then work one slice at a time, one feature from problem to scenario, then the next, as the [model page](../core/model.md) describes: Intent first, then Behavior specified from it, then code, then Proof. A first module with its features, its aggregates, and the use cases about to be built is enough to start. Each record has a template in the release, and `litespec new` puts each one in its legal folder.

### 5. Build to the Standard

Build the first use case the way the standard describes: the application project ordered by module, aggregate, and use case, the handler in its folder, the refusal codes, the tests. A codebase already on five projects selects the `layer-projects` extension instead of moving to one application project. Reference `LiteSpec.Analyzers` from the application project and the hosts, or once in `Directory.Build.props`, so a breach of a .NET provision fails the build at the line that breaks it:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="4.0.0" PrivateAssets="all" />
</ItemGroup>
```

Then check what the builder finds:

```bash
dotnet litespec model check
dotnet litespec rules apps/api/src/Shop/Orders/Order/CancelOrder/CancelOrderCommandHandler.cs
```

The first command lists the gaps with a level beside each finding. The second lists the provisions that apply to the file just written, grouped by strength.

### 6. Add the Checks to CI

Run `dotnet litespec model check` and the gates the [CI page](../quality/ci.md) lists for each area on every pull request. A gap then shows up in the change that caused it, not months later.

## Departures During Adoption

Existing code rarely meets every rule at the start. Record each departure instead of ignoring it:

- An **exception record** of kind `departure` for a provision the project will not follow, with its reason and expiry date.
- An **exception record** of kind `gap` for a check that stays red until the project fixes it, with its reason and expiry date.

Both keep the checks honest. An exception applies while it is present and unexpired; past its expiry date `exception-expired` reports it.

## Next Steps

[Start a consumer repository](../tutorial/getting-started.md) builds a first working project step by step.
