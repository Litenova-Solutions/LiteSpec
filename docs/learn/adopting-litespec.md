# Adopt LiteSpec in Steps

## Intent

Adopting LiteSpec means pinning one release, describing the project, writing the first records, and building to the standard with the checks in place. A team adopts it in steps by record layer: it claims the layers it can meet today and adds the next layer when it is ready for the next part of the contract. Technical leads read this page when they plan the rollout, so the team starts small and every departure stays visible instead of silent.

## Fit Check

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends, or a client-only app with no backend of its own. Check the [scope page](../core/scope.md) first. If the system is several services, or something other than a business application, LiteSpec is not built for it.

## Choose the Layers to Start

The `layers` block in `.litespec/project.yaml` names the record layers the project claims and the practice it follows in each. A team loses nothing by starting low: each layer is written from the one above it, so a team adds the layers below the code one at a time. The [scope page](../core/scope.md#layer-claims) states what each claim covers and what each tool runs for it.

| Starting point | `layers` | Start here when |
|:---|:---|:---|
| The record model | `intent: litespec` and `behavior: litespec` | The team wants the records, the book and the tools, and keeps its own code layout and tests |
| Code traced to records | The record model, plus `implementation: own` and `proof: own` | The team is ready to trace code and tests to the records, still in its own layout and test practice |
| LiteSpec's layers | All four layers, each `litespec` | The team builds in LiteSpec's layout and tests, or moves its layout to it |

Extensions keep working within a claim as they do at every other starting point. A team selects an extension when its criterion holds, whatever layers it claims.

## Step 1: Claim the Record Model

At the record model the project claims `intent` and `behavior` alone. Its code keeps its own layout, its tests keep their own shape, and the analyzer package does nothing. In return the team gets checked records, a book that shows Intent, Behavior and governance, and `rules` answers about records.

### 1. Install the Release

Install the `litespec` command line as a local .NET tool at the release the team adopts, such as `4.1.0`:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 4.1.0
dotnet tool restore
```

Run the first command only when the project has no `.config/dotnet-tools.json` yet. A team without the .NET SDK installs the standalone binary from the GitHub release instead, which `reviewedLiteSpecVersion` pins; the [CLI overview](../tools/README.md#install-the-cli) lists every install method. The installed release is the contract: it carries the manifest, schemas, docs, and templates the version names, and it never changes until the version moves, so a new LiteSpec release never breaks the build by surprise. The team adopts a newer release when it chooses, by moving the version and reading the new release.

### 2. Describe the Project and Claim the Record Model

Write `.litespec/project.yaml` with the size, the shape, and the layers it claims. The size stays required in every claim because it sets which record types must exist. With `intent` and `behavior` alone the stacks may be empty or absent, and `frontends` may be absent; a `server` project with no frontend may omit `frontends` in every claim. A project that names its stacks while it claims no Implementation or Proof names them for information alone, since no code reader runs for it.

```yaml
size: small
shape: server
stacks: []
layers:
  intent: litespec
  behavior: litespec
reviewedLiteSpecVersion: 4.1.0
paths:
  behaviorDocs: .litespec/behavior
```

Fill in the paths, name the refusal codes in `refusals`, and select the extensions the project needs. The [selection page](https://litespec.dev/4.1.0/select/) shows which pages the choices apply. For an existing project, add the `layers` block to the project file it already has and leave its code where it is.

### 3. Tell the Agents

`litespec init` writes the `## LiteSpec` block into the project's `AGENTS.md`, with the release, size, shape, stacks, and selection index of the new project. For an existing project, the selection page gives you the block to paste. From then on, every agent working in the repository loads the same rules.

### 4. Write the First Records

Start with the product: one `.litespec/intent/product.yaml` that says what the product is, who it is for, and what it never does. Then work one slice at a time, one feature from problem to scenario, then the next, as the [model page](../core/model.md) describes: Intent first, then Behavior specified from it. A first module with its features, its aggregates, and the use cases about to be built is enough to start. Each record has a template in the release, and `litespec new` puts each one in its legal folder. The project glossary and its exceptions live beside the records, and both follow the record model in this claim.

### 5. Check the Records

Run the model check and read the book:

```bash
dotnet litespec model check
dotnet litespec book
```

With only `intent` and `behavior` claimed, the model check runs the record checks alone (level G), so no finding names code layout, marks, or proof. The book shows Intent, Behavior and governance, plus how much Intent the Behavior records reach. `rules` on a record identifier returns the provisions about records; `rules` on a code path returns no provisions in this claim and says why. A team runs check in its pipeline when it wants the record claim held on every change; a departure stays visible as an exception record, as Departures at Any Layer below states.

The step is done when the record checks pass and the book shows the first slice. Claimwick, the running example, claims all four layers with `litespec`; a product that takes this step alone claims `intent` and `behavior` and claims nothing about its code.

## Step 2: Trace Code to Records

Code traced to records keeps the project's own code layout and adds the link between code and records. Code and tests point to their records with marks, and test runs prove scenarios. A team takes this step when it wants the builder to show which code serves which record and which scenarios have passing proof.

### 1. Claim Implementation and Proof, and Name the Stacks

Add `implementation: own` and `proof: own` to `layers`, and name the stacks. Stacks are required once Implementation or Proof is claimed, since the stack's code reader reads the code through the stack the project names. A project on a stack with no LiteSpec reader proves through result files only, as step 3 below states, and still claims both layers.

### 2. Add Marks in a Fixed Syntax

Pick one of the two mark styles the release carries, `xml` or `attribute`, and use it everywhere. Mark syntax stays fixed: a project chooses between those two styles and not a syntax of its own, so the analyzers and the reader share one grammar. Code points up to the record it implements, and tests point to the scenario they prove. Existing code keeps its folders and names; only the marks are added.

### 3. Prove Scenarios from Test Runs

A scenario is proven when a test that points to it passes. Where a runner's steps are not methods that can carry a mark, such as a smoke step or a test runner in another language, the test result file carries the proof instead: a JUnit XML test case names the scenarios it proves in a `litespec.proves` property, one property per scenario or several identifiers separated by commas in one value. The same pass, fail, skip and duration rules apply as for a mark on a test method.

### 4. Check Traceability

Run the model check, the book and `rules` again. With Implementation and Proof claimed in their own practice, the model check adds the mark and proof checks (level S) to the record checks, so a finding can name missing code, a missing mark, or an unproved scenario, and never a folder the project never agreed to use. The book adds code and proof status to what the record claim shows. `rules` adds the provisions about marks and proof to the provisions about records. The analyzer package, where the project references it, runs only the traceability rules, `LS5001` to `LS5012`, in this claim.

The step is done when every in-scope code element and test points to its record and every scenario the team claims has passing proof. The library product, with its own layout, can stop here: it traces its lending code to its records and proves its scenarios from its test runs, without moving a folder.

## Step 3: Take LiteSpec's Layers

With `litespec` for Implementation and Proof, the project takes the rest of the contract: LiteSpec's stacks, code layout, analyzers and testing practice. This is the claim a new project usually makes from the start, and the claim a project with its own code layout moves to when it wants the layout and the analyzer rules to hold as well.

### 1. Claim LiteSpec's Layers and Adopt the Layout

Set `implementation` and `proof` to `litespec` in `layers`, and name each stack the project uses. Build each use case the way the standard describes: the modules project ordered by module, aggregate, `UseCases/`, and use case, the handler in its folder, the refusal codes, the tests. A codebase already on five projects selects the `layer-projects` extension instead of moving to the shared kernel and modules projects. Reference `LiteSpec.Analyzers` from the business projects and the hosts, or once in `Directory.Build.props`, so a breach of a .NET provision fails the build at the line that breaks it:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="4.1.0" PrivateAssets="all" />
</ItemGroup>
```

With `implementation: litespec` every analyzer rule runs, the model check runs every check including the layout checks, the book shows everything, and `rules` returns everything that applies to a path.

### 2. Add the Checks to CI

Run `dotnet litespec model check` and the gates the [CI page](../quality/ci.md) lists for each area on every pull request. A gap then shows up in the change that caused it, not months later.

The step is done when the checks for the claimed layers pass and the analyzer build is clean. Claimwick claims all four layers with `litespec`: its records, its code, its layout and its tests all sit inside one claim.

## The Records Path Through the Tutorial

[Start a consumer repository](../tutorial/getting-started.md) follows the claim of all four layers end to end, from project file to one working use case. A team that claims only the record model follows the same tutorial and stops before the code steps, because the record model is the part it claims.

Follow the install and project steps, claiming `intent` and `behavior` as step 2 above states, then the product, module and feature, constraints, decision, people and journey steps, and the Intent check. That check runs on the records alone, before any Behavior record or code exists, and it is the record claim working. Then follow the module specification step, which writes Behavior from Intent, and run the model check again, then render and read the book.

Skip, for now, the steps that implement one use case in LiteSpec's layout, declare the frontend, and add the operating records. The project's own code stays where it is, and its own tests keep proving what they prove outside the model. When the team adds `implementation: own` and `proof: own`, it adds marks and proof to that same code instead of following the layout steps; when it claims `litespec` for both, it follows those steps as written.

## Departures at Any Layer

Existing code rarely meets every rule at the start, at any claim. Record each departure instead of ignoring it:

- An **exception record** of kind `departure` for a provision the project will not follow, with its reason and expiry date.
- An **exception record** of kind `gap` for a check that stays red until the project fixes it, with its reason and expiry date.

Both keep the checks honest. An exception applies while it is present and unexpired; past its expiry date `exception-expired` reports it. An exception names only provisions and findings that apply to the layers the project claims: a project that claims only `intent` and `behavior` needs no exception for a layout rule it never claimed.

## Next Steps

[Start a consumer repository](../tutorial/getting-started.md) builds a first working project step by step, and The Records Path Through the Tutorial above states where a record-only project stops. The [scope page](../core/scope.md) states the layer claims and what each one covers.
