# How LiteSpec Works

Software teams write down what a product should do, then build it, and the two drift apart: the code answers questions the records never asked, and the records promise behavior no test proves. LiteSpec connects the two and checks that they still agree. The team writes short records about the product, marks the code and tests that serve each record, and runs commands that report the gaps.

The example throughout is Claimwick, the expense product in `examples/claimwick/`, introduced in [The Claimwick Product](../example/the-product.md).

## The Parts

LiteSpec has four parts, and each answers one question.

| Part | What it is | The question it answers |
|:---|:---|:---|
| The standard | The pages of one LiteSpec release, carried by the installed tool | How do we build? |
| Records | Short YAML record files in your project, each with an optional Markdown explanation beside it | What do we build, and why? |
| The model | What `litespec model check` and `litespec model query` compute from the records and the marks in code and tests | Does what we built match what we wrote? |
| The tools | The `litespec` command line, the analyzers, and the published site | Is everything consistent, and what is missing? |

A project installs one LiteSpec release, as a local .NET tool or as a standalone binary, and that release stays fixed until the team moves it.

Each rule in the standard is called a provision. It carries one ID and states how binding it is, as [Read the Standard](reading-the-standard.md) explains. Before you edit a file, run `dotnet litespec rules <path>` to list the provisions that govern it.

## The Four Layers

Every record belongs to the layer that owns it, and each layer answers one question. Each layer is written from the one above it: Behavior fulfills Intent, Implementation builds Behavior, and Proof proves Implementation keeps Behavior.

| Layer | Question | Records |
|:--|:--|:--|
| Intent | Why build it, and what do we promise? | goal, direction, problem, feature, requirement, constraint, decision, research, flow, actor, role |
| Behavior | How does the system behave to keep the promises? | module, aggregate, rule, domain event, module event, integration event, use case, result, scenario, reaction, job, read model, notification, setting, permission, refusal set, entry point, screen, external system |
| Implementation | How is it built and operated? | Code, technical decisions, runbooks, operating procedures |
| Proof | How do we know it holds? | Tests, check results, the computed completion, the rendered book |

Above the four layers stands the product record at `.litespec/product.yaml`: what is built, for whom, and where it stops. A project claims the layers it uses in `layers` in `.litespec/project.yaml`, as [Supported Scope](../core/scope.md#layer-claims) states. Both Intent and Behavior also use glossary terms, which live in the one glossary file at the top of `.litespec/` rather than as record files of their own.

Three rules join the layers:

- **Specify** turns Intent into Behavior. It adds the behavior detail the promises leave unstated: states, refusals, data with its retention, and access with its tenant scope. It never invents a value Intent does not state, such as a price, a rate, or a retention period; the agent takes that question to the owner.
- **Implement** turns Behavior into code. It adds no outcome, edge, or access that no record declares.
- **Verify** turns code into Proof: one test proves each scenario through the public boundary.

Records state software only. Company matters such as prices, markets, or hiring stay out, except as a constraint or as dated research, as [Record Layers](../core/layers.md) states.

When a project cannot follow a rule, a waiver sets that provision, a page, or a finding code aside; [Read the Standard](reading-the-standard.md#waivers-from-the-rules) explains the waiver record.

## Record Types and Where They Live

A record's folder is part of the record: it names the record's parent. The other folder names come from a fixed set per layer. The Claimwick tree shows the pattern:

```text
.litespec/
  product.yaml                 the product, above the record layers
  intent/
    goals/  constraints/  decisions/  research/  actors/  roles/  flows/
    problems/
      expenses-wait-weeks/
        expenses-wait-weeks.yaml
        features/
          approval/
            approval.yaml        the feature, answering its problem
            requirements/
              decided-within-five-days/
                decided-within-five-days.yaml   a requirement, in its own folder
  behavior/
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

Each record has one identifier, short and unique across the product, such as `use-case/spending.submit-expense`. Every link between records uses it. When a record moves to another module, its identifier changes, and every citation changes in the same move.

## Links Between Records

Records point at each other by identifier, and each kind of link has one meaning. Access shows how links chain together: an actor holds a role, each permission names the roles that hold it, and the use case declares one grant.

```yaml
# intent/actors/employee/employee.yaml
holds:
- role/employee

# behavior/modules/spending/permissions.yaml
records:
- id: permission/request-expense
  grantedTo:
  - role/employee

# behavior/.../submit-expense/submit-expense.yaml
access:
  grant: permission/request-expense
  tenant: own
```

## A Use Case from Record to Code

Claimwick lets employees submit work expenses with receipts. The use case record at `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml` states its access grant, the requirement it fulfills, and the results and scenarios that end and prove it.

The handler `SubmitExpenseCommandHandler.cs` sits in the module's own project, in a folder ordered by module, `Application/`, aggregate, and use case. It carries the mark `<implements>use-case/spending.submit-expense</implements>`, which names the record it serves.

From the project root, `dotnet litespec model check` reports each gap as a finding, and `dotnet litespec model query use-case/spending.submit-expense` shows the record with its links. To narrow the work, `model query --search <text>` finds records by words, `model query --list <type>` lists every record of a type, and `model check --module <module>` limits a run to one module.

The model computes one completion stage for the use case from the marks, as [Completion Stages](../core/model.md#completion-stages) defines: `coded` when the handler exists, `verified` only when every scenario has a passing proof. [A Use Case in Code](../example/a-use-case-in-code.md) walks the same record line by line, with the rule, the endpoint, the handler, and the tests.

Each finding carries a level. Level G findings read records only and always block. Level S findings read code through a stack reader and block where the reader is qualified, and level R findings are prose quality a person weighs through `litespec model review`, which never blocks. A finding about a missing, unknown, or unused link always blocks: level G on records, level S on code. The [Model Builder](../tools/model.md) states the levels, and [The Model and Its Checks](../example/model-and-checks.md) shows the full Claimwick output.

## How to Read a Specification

A specification is a tree, and nobody reads the whole tree at once. Three ways in work well: take the layers in order, follow one flow from reason to proof, or follow one module from aggregates to proof.

### Why Before How

Read Intent before Behavior. Start at the product record, with its goals, problems, features, constraints, decisions, and research. Then read the Behavior modules, with their aggregates, rules, and use cases, and the reactions, jobs, read models, notifications, and settings around them. Each record names what sits above it, which is why it exists, and what sits below it, which is how it is fulfilled.

### One Flow End to End

Follow one flow from its reason to its proof: the goal or problem with its requirements, the flow with its start and its steps with their branches, the use cases that serve each step, and the tests the model links to each scenario. When every scenario on the flow is proven, the flow holds.

### One Module End to End

Follow one module from its aggregates to its proof: the module itself, each aggregate with its states and rules, each use case with its access and scenarios, the reactions, jobs, read models, notifications, and settings around them, and the tests the model links to each scenario. When every scenario in the module is proven, the module holds.

### Which Track to Take

Product people take the flow track, and developers and agents take the module track. Both start with why before how, and both end at Proof, where passing tests show the promises hold.

## The Tools

The `litespec` command line reads a consumer project and reports what it finds. The [command index](../tools/README.md) lists every command.

| Command | Repository | Reads | Writes |
|:---|:---|:---|:---|
| `litespec init` | Consumer | Release template and preset | `.litespec/project.yaml` |
| `litespec new` | Consumer | Templates and folder rules | One record file, with its identifier |
| `litespec rename` | Consumer | Records, marks and citations | Rewritten citations |
| `litespec rules` | Consumer | One path or record ID | The provisions that apply |
| `litespec model check` | Consumer | Records, code marks, routes and test runs | Findings with levels |
| `litespec model query` | Consumer | One record with its links | The record and its links |
| `litespec model edges` | Consumer | Record links between modules | Link counts per kind and module |
| `litespec model render` | Consumer | The model | The generated regions in your pages |
| `litespec book` | Consumer | Records, marks and findings | A live book on localhost, or one static site with `--out` |
| `litespec context` | Consumer | Records and explanation files | One plain-text file for an AI chat |
| `litespec-maintain validate` | LiteSpec | Pages, templates, manifest and links | Diagnostics |
| `litespec-maintain provisions` | LiteSpec | The active standards | The provision index |

## Next

[Read the Standard](reading-the-standard.md) shows how the standard is organized and how to find the rules that apply to your project.
