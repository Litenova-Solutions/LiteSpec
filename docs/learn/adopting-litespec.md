# Adopt LiteSpec in Steps

Start with Intent and Behavior: write your product's records and check them while your code and tests stay as they are. Add Implementation and Proof when your team is ready, in either order. In both, you can keep your existing code layout and test practice and still connect them to your records.

Your project records the LiteSpec release it adopts and the layers it claims in `.litespec/project.yaml`. When it cannot yet meet a provision that applies to a layer it claims, it records a waiver.

## Check the Fit

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends. A client-only app with no backend of its own fits too. A system of several services, or anything other than a business application, falls outside what LiteSpec is built for. [Supported Scope](../core/scope.md) states the boundary.

## Choose Your Layers

LiteSpec's records sit in four layers, each written from the one above it:

- **Intent** holds the outcomes and problems the product answers.
- **Behavior** holds what the software does, holds, and enforces.
- **Implementation** is the code that serves the Behavior records.
- **Proof** is the tests and runs that prove the Behavior scenarios.

To claim a layer, name it in the `layers` block of `.litespec/project.yaml`. `litespec model check`, the book, `litespec rules`, and the analyzers then report the provisions and findings that apply to the layers you claim, and nothing about the others. The book shows a layer you do not claim as not claimed, never as missing.

You leave layers out from the bottom. Behavior needs Intent, and Implementation and Proof each need Behavior but not each other, so you can claim either one without the other. [Layer Claims](../core/scope.md#layer-claims) lists the five combinations a project can claim, from `intent` alone to all four layers.

Each claimed layer takes a value that says whose practice it follows:

- `litespec`: the layer follows LiteSpec's pages for it. Intent and Behavior take only this value.
- `own`: offered for Implementation and Proof only. The code or tests still point to the Behavior records, but the team keeps its own architecture or test practice.

Claimwick, the running example, claims all four layers with `litespec`.

Extensions are chosen separately from layers. An extension is a bundle of standards that applies only when its activation criterion holds, and the project selects it whenever that criterion holds, whatever layers it claims. For example, a project that runs the product in production with a formal release, deployment, rollback, or alerting regime selects the `delivery` extension. The [extension catalog](../ext/README.md) lists each extension with its criteria.

## Start with Intent and Behavior

Set `intent` and `behavior` to `litespec` in `layers`, and leave `implementation` and `proof` out. With only these two layers claimed, `stacks` may be empty, because no code reader runs. Your code keeps its own layout, your tests keep their own practice, and no analyzer rule applies.

This claim gives you:

- `litespec model check`, which checks your records.
- The book, which shows your Intent, Behavior, and governance records.
- `litespec rules`, which lists the provisions that govern a record.

[Start with Records Only](../guide/start-with-records-only.md) walks through this claim, from the first record files to a passing check.

## Add Implementation

Implementation connects your code to your Behavior records. To claim it:

- Set `implementation` to `own` or `litespec` in `layers`.
- Name the project's stacks in `stacks`: one backend stack unless the shape is client-only, and one stack per frontend. Each stack selects the code reader that reads its code.
- Give each code element that the [marks table](../core/model.md#marks) assigns a mark one that names the record it serves. A handler marks exactly one record.

The value you choose decides what else applies:

- With `own`, the team keeps its own code layout and architecture. The marks are the whole obligation.
- With `litespec`, the stack, workspace, and code layout pages apply, along with the analyzers' architecture rules. `litespec model check` adds the findings about LiteSpec's code layout, such as `code-placement-drift` and `mark-folder-drift`.

Implementation is in place when the marks the claim requires are present and `litespec model check` reports no blocking finding for them.

## Add Proof

Proof connects your tests to the scenarios in your Behavior records. To claim it:

- Set `proof` to `own` or `litespec` in `layers`.
- Name the project's stacks in `stacks`, as for Implementation, if the project does not name them already.
- Write a test for each scenario that carries a proves mark naming the scenario, calls the use case through its public boundary, and asserts the result the scenario names.

The model builder confirms each proves mark from the test body, and the latest test run marks it passing or failing. A passing proof is evidence for the case its scenario states; it says nothing about behavior no scenario covers. [Scenario Proof](../quality/testing.md#scenario-proof) states the rule in full.

When a test runner's steps cannot carry a mark, or a stack has no code reader, the test result file can carry the proof instead: a JUnit test case names the scenario in a `litespec.proves` property. The [model command page](../tools/model.md#underneath) shows the format.

The value you choose decides what else applies:

- With `own`, the team keeps its own test practice.
- With `litespec`, the cross-stack testing page, the stack testing pages, and LiteSpec's test projects apply.

Proof is in place when every scenario has a passing proof, or a gap waiver covers the scenarios no test proves yet. Until then, `litespec model check` reports `use-case-unverified` for each use case with a scenario that has no passing proof. That finding is advisory: it blocks only under `litespec model check --release`.

## Record Waivers for Adoption Gaps

A waiver sets a provision, a page, or a finding code aside for your project, with a reason. Record one for each provision the project cannot yet meet in a layer it claims. A layer you do not claim needs no waiver, since no provision about it applies.

Each waiver sits in its own folder under `.litespec/governance/waivers/`, or under `.litespec/governance/modules/<module>/waivers/` when it covers one module. Every waiver states what it `covers` (provision IDs, pages, or finding codes), its `reason`, and the `subject` records or `paths` it applies to. Its `kind` decides the remaining fields:

| `kind` | Use it for | Required fields |
|:---|:---|:---|
| `gap` | Work the team means to finish | `expiresOn`, `exit`, and `closedBy`, the records whose change closes the gap |
| `lasting` | A choice the project keeps | `consequences`; no `expiresOn`, `exit`, or `closedBy` |

A gap waiver holds through its `expiresOn` date. After that date, the findings it covered report again, and `litespec model check` reports `waiver-expired`, which blocks the run. [Waivers](../core/waivers.md) states every field and how a waiver covers a finding.

## Next Steps

[Start with Records Only](../guide/start-with-records-only.md) walks a team through claiming Intent and Behavior. [Start a Consumer Repository](../tutorial/getting-started.md) builds a first working project step by step. [Supported Scope](../core/scope.md) states the layer claims and what each one covers.
