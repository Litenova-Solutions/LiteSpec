# How LiteSpec Works

## Intent

LiteSpec connects what a team writes about a product to the code that implements it, and checks that the two still agree. This page explains the parts, the layers, the homes, and the tools, then states how to read a specification. Claimwick, the expense product in `examples/claimwick/`, is the worked example, walked from [The Claimwick Product](../example/the-product.md).

## The Parts

LiteSpec has four parts. Each one answers a different question.

| Part | What it is | The question it answers |
|:---|:---|:---|
| The standard | The pages of one LiteSpec release, carried by the installed tool | How do we build? |
| Records | Short YAML record files in your project, each with an optional Markdown explanation beside it | What do we build, and why? |
| The model | What `litespec model check` and `litespec model query` compute from the records and the marks in code and tests | Does what we built match what we wrote? |
| The tools | The `litespec` command line, the analyzers and the published site | Is everything consistent, and what is missing? |

Your project installs one LiteSpec release, as a local .NET tool or as a standalone binary, and that release does not change under you until you move the version.

Every rule in the standard is a provision with one ID and one stated strength, as [Read the Standard](reading-the-standard.md) explains; before editing a file, run `dotnet litespec rules <path>` to list the provisions that apply to it.

## The Four Layers

Every record answers one layer's question, and each layer is written from the layer above it.

| Layer | Question | Records |
|:--|:--|:--|
| Intent | Why build it, and what do we promise? | Product, goal, problem, feature, requirement, constraint, decision, research, journey, actor, role |
| Behavior | How does the system behave to keep the promises? | Aggregate, rule, event, integration event, use case, result, scenario, reaction, job, notification, setting, permission, entry point, screen, external system |
| Implementation | How is it built? | Code, technical decisions, runbooks |
| Proof | How do we know it holds? | Tests, check results, the computed completion, the rendered book |

A project claims the layers it uses in `layers` in `.litespec/project.yaml`. Three translation rules join them. **Specify** turns Intent into Behavior: it adds states, refusals, data with its retention, and access with its tenant scope. It never invents a value that Intent does not state, such as a price, a rate or a retention period; the agent takes that question to the owner. **Implement** turns Behavior into code, and it adds no outcome, edge or access that no record declares. **Verify** turns code into Proof: one test proves each scenario through the public boundary.

Records state only software; [Record Layers](../core/layers.md) states how company matters enter. A departure sets a provision, a page, or a finding code aside; [Read the Standard](reading-the-standard.md#departures-from-the-rules) explains the departure record.

## Record Types and Their Homes

The folder is part of the record: it names the parent, and every other folder name comes from a closed set per layer. The Claimwick tree shows the pattern:

```text
.litespec/intent/
  product.yaml
  goals/  constraints/  decisions/  research/  actors/  roles/  journeys/
  problems/
    expenses-wait-weeks/
      expenses-wait-weeks.yaml
      features/
        approval/
          approval.yaml        the feature
          requirements/
            decided-within-five-days/
              decided-within-five-days.yaml   a requirement, in its own folder
.litespec/behavior/
  modules/
    spending/
      aggregates/
        expenses/                an aggregate's folder
          aggregate.yaml
          use-cases/
            submit-expense/
              submit-expense.yaml  a use case
          rules/
            ...
```

Each record has one identifier, short and unique in the product, such as `use-case/spending.submit-expense`, and every link uses it. A record that moves to another module changes its identifier and every citation in the same change.

## Links Between Records

Access runs through three records and one grant: the actor holds a role, the role allows permissions, and the use case declares one grant:

```yaml
# intent/actors/employee/employee.yaml
holds:
- role/employee

# intent/roles/employee/employee.yaml
allows:
- permission/request-expense
- permission/list-expenses

# behavior/.../submit-expense/submit-expense.yaml
access:
  grant: permission/request-expense
  tenant: own
```

## A Use Case from Record to Code

Claimwick lets employees submit work expenses with receipts. The record `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml` states the use case, its access grant, the requirement it fulfills, and the results and scenarios that end and prove it. The handler `SubmitExpenseCommandHandler.cs` sits in a folder ordered by module, aggregate, `UseCases/` and use case, and carries the mark `<implements>use-case/spending.submit-expense</implements>` naming the record it serves. Running `dotnet litespec model check` from the project root reports each gap as a finding, and `dotnet litespec model query use-case/spending.submit-expense` shows the record with its links. The model computes one completion stage for the use case from the marks, as [Completion Stages](../core/model.md#completion-stages) defines: `coded` when the handler exists, `verified` only when every scenario has a passing proof. [A Use Case in Code](../example/a-use-case-in-code.md) walks the same record line by line, with the rule, the endpoint, the handler, and the tests.

Each finding carries a level: level G findings read records only and always block, level S findings read code through a stack reader, and level A findings are advisory. The [Model Builder](../tools/model.md) states the levels, and [The Model and Its Checks](../example/model-and-checks.md) shows the full Claimwick output.

## How to Read a Specification

A specification is a tree, and no reader reads the whole tree at once.

### Why Before How

Read Intent before Behavior: start at the product record with its goals, problems, features, constraints, decisions, and research, then read the Behavior modules with their aggregates, rules, use cases, and surrounding reactions, jobs, notifications, and settings. Each record names what sits above it, which is why it exists, and what sits below it, which is how it is fulfilled.

### One Journey End to End

Follow one journey from its goal to its proof: the goal and its requirements, the journey page with its start, its steps and its branches, each step's serving use cases, and the tests the model links to each scenario. When every scenario on the journey is proven, the journey holds.

### One Module End to End

Follow one module from its aggregates to its proof: the module page, each aggregate with its states and rules, each use case with its access and scenarios, the reactions, jobs, notifications, and settings around them, and the tests the model links to each scenario. When every scenario in the module is proven, the module holds.

### Which Track to Take

A product person takes the journey track and a developer or agent takes the module track; both read why before how, and both end at Proof, where passing tests show the promises hold.

## The Tools

The `litespec` command line reads a consumer project and reports what it finds; the [command index](../tools/README.md) lists every command.

| Command | Repository | Reads | Writes |
|:---|:---|:---|:---|
| `litespec init` | Consumer | Release template and preset | `.litespec/project.yaml` |
| `litespec new` | Consumer | Templates and folder rules | One record file, with its identifier |
| `litespec rename` | Consumer | Records, marks and citations | Rewritten citations |
| `litespec rules` | Consumer | One path or record ID | The provisions that apply |
| `litespec model check` | Consumer | Records, code marks, routes and test runs | Findings with levels |
| `litespec model query` | Consumer | One record with its links | The record and its links |
| `litespec model render` | Consumer | The model | The marked regions of your pages |
| `litespec book` | Consumer | Records, marks and findings | One offline site |
| `litespec-maintain validate` | LiteSpec | Pages, templates, manifest and links | Diagnostics |
| `litespec-maintain provisions` | LiteSpec | The active standards | The provision index |

## Next

[Read the Standard](reading-the-standard.md) explains how the standard is organized and how to find the rules that apply to your project.
