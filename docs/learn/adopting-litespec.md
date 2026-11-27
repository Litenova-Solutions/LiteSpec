# Adopt LiteSpec in Steps

Start with Intent and Behavior: write your product's records and check them while your code and tests stay as they are. Add Implementation and Proof when your team is ready, in either order. In both steps you keep your existing code layout and test practice and still connect them to your records.

Your project records the LiteSpec release it adopts and the layers it claims in `.litespec/project.yaml`. When it cannot yet meet a provision that applies to a layer it claims, it records a waiver.

## Check the Fit

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends. A client-only app with no backend of its own fits too. A system of several services, or anything other than a business application, falls outside what LiteSpec is built for. [Supported Scope](../core/scope.md) states the boundary.

For example, a team running one expense product with one API and one database is in scope. A platform team running five services behind one gateway is not.

## Choose Your Layers

Name each record layer the project follows in the `layers` block of `.litespec/project.yaml`; [Supported Scope](../core/scope.md#layer-claims) states the layers, the values each takes, and the combinations you can claim. Claimwick, the running example, claims all four layers with `litespec`.

Extensions are chosen separately from layers. An extension is a bundle of standards that applies only when its activation criterion holds, and your project selects it whenever that criterion holds, whatever layers it claims. For example, a project that runs the product in production with a formal release, deployment, rollback, or alerting regime selects the `delivery` extension. The [extension catalog](../ext/README.md) lists each extension with its criteria.

## Start with Intent and Behavior

Set `intent` and `behavior` to `litespec` in `layers`, and leave `implementation` and `proof` out. With only these two layers claimed, `stacks` may be empty, because no code reader runs. Your code keeps its own layout, your tests keep their own practice, and no analyzer rule applies.

This claim gives you:

- `litespec model check`, which checks your records.
- The book, which shows your Intent, Behavior, and governance records.
- `litespec rules`, which lists the provisions that govern a record.

For example, a team with a running expense service writes the problem, feature, and use-case records first, runs the model check, and reads the book, all without touching the service code.

[Start with Records Only](../guide/start-with-records-only.md) walks through this claim, from the first record files to a passing check.

## Add Implementation

Implementation connects your code to your Behavior records. To claim it:

1. Set `implementation` to `own` or `litespec` in `layers`.
2. Name the project's stacks in `stacks`: one backend stack unless the shape is client-only, and one stack per frontend. Each stack selects the code reader that reads its code.
3. Give each code element that the [marks table](../core/model.md#marks) assigns a mark one that names the record it serves. A handler marks exactly one record.

The value you choose decides what else applies:

- With `own`, your team keeps its own code layout and architecture. The marks are the whole obligation.
- With `litespec`, the stack, workspace, and code layout pages apply, along with the analyzers' architecture rules. `litespec model check` adds the findings about LiteSpec's code layout, such as `code-placement-drift` and `mark-folder-drift`.

For example, the Claimwick submit handler carries `<implements>use-case/spending.submit-expense</implements>`. With `own`, that mark is enough. With `litespec`, the handler also sits in the use case folder the layout pages name.

Implementation is in place when the marks the claim requires are present and `litespec model check` reports no blocking finding for them.

## Add Proof

Proof connects your tests to the scenarios in your Behavior records. To claim it:

1. Set `proof` to `own` or `litespec` in `layers`.
2. Name the project's stacks in `stacks`, as for Implementation, if the project does not name them already.
3. Write a test for each scenario that carries a proves mark naming the scenario, calls the use case through its public boundary, and asserts the result the scenario names.

The model builder confirms each proves mark from the test body, and the latest test run marks it passing or failing. A passing proof is evidence for the case its scenario states; it says nothing about behavior no scenario covers.

When a test runner's steps cannot carry a mark, or a stack has no code reader, the test result file can carry the proof instead: a JUnit test case names the scenario in a `litespec.proves` property. The [model command page](../tools/model.md#underneath) shows the format.

The value you choose decides what else applies:

- With `own`, your team keeps its own test practice.
- With `litespec`, the cross-stack testing page, the stack testing pages, and LiteSpec's test projects apply.

For example, the Claimwick test that submits an expense above 25 EUR without a receipt asserts the refusal the scenario names for the missing receipt. That passing test is the proof of that scenario, and of nothing else.

Proof is in place when every scenario has a passing proof, or a gap waiver covers the scenarios no test proves yet. Until then, `litespec model check` blocks on `use-case-unverified` for each use case with a scenario that has no passing proof.

## Record Waivers for Adoption Gaps

When your project cannot yet meet a provision that applies to a layer it claims, record a waiver for it; [Waivers](../core/waivers.md) states the kinds, the fields each kind carries, and how a waiver covers a finding. A layer you do not claim needs no waiver, since no provision about it applies.

For example, a team claiming Proof whose receipt test is still red records a gap waiver on that scenario, so the check reports it as covered instead of blocking while the test is fixed.

## Next Steps

[Start with Records Only](../guide/start-with-records-only.md) walks your team through claiming Intent and Behavior. [Start a Consumer Repository](../tutorial/getting-started.md) builds a first working project step by step. [Supported Scope](../core/scope.md) states the layer claims and what each one covers.
