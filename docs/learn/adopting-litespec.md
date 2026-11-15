# Adopt LiteSpec in Steps

A team rarely adopts a whole standard on day one: records, code layout, and tests change at different speeds, and a big-bang switch stalls real work. LiteSpec lets a team adopt one part at a time. The project pins one release, claims the record layers it follows, and puts the checks for those layers in place. Later steps add more layers.

A team starts with Intent and Behavior: checked records, a readable book, and rule answers about records, while its code and tests keep their own shape. They add Implementation and Proof later, in either order. While a gap is still open, the team covers each provision it cannot yet meet with a recorded waiver, and closes the waiver when the work lands.

## Check the Fit

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends. A client-only app with no backend of its own fits too. Check [Supported Scope](../core/scope.md) first: it states the boundary. A system of several services, or anything other than a business application, falls outside what LiteSpec is built for.

## Choose the Layers to Start

The `layers` block in `.litespec/project.yaml` names the record layers the project claims and the practice it follows in each one. Layers build from the top down: Behavior needs Intent, and Implementation and Proof each need Behavior. [Supported Scope](../core/scope.md#layer-claims) states the five combinations a project can claim and what each one covers.

| Step | Layers claimed | The team is ready when |
|:---|:---|:---|
| 1 | `intent` and `behavior`, both `litespec` | It wants the records, the book and the checks, and keeps its own code layout and tests |
| 2 | `implementation`, as `own` or `litespec` | It wants code marks that point at Behavior records, in its own layout or in LiteSpec's |
| 3 | `proof`, as `own` or `litespec` | It wants passing tests to prove Behavior scenarios, in its own test practice or in LiteSpec's |

Implementation and Proof each need only Behavior, so a team may add Proof before Implementation, or stop after either one. A team that keeps its own layout and its own tests can stop at step 3 with both layers set to `own`. Claimwick, the running example, claims all four layers with `litespec`.

Extensions work the same way at every step. A team selects an extension whenever its criterion holds, whatever layers it claims.

## Step 1: Claim Intent and Behavior

At this step the project claims `intent` and `behavior` and nothing else. Its code keeps its own layout, its tests keep their own shape, and no analyzer rule applies. In return the team gets checked records, a book covering Intent, Behavior, and governance, and `rules` answers about records. [Start with Records Only](../guide/start-with-records-only.md) walks a team through this step, from the first record files to a passing check.

## Step 2: Add Implementation

When the team wants its code tied to its records, it sets `implementation` to `own` or `litespec` in `layers` and names the stacks the project uses. Naming the stacks matters because each stack selects the code reader that checks its code. With `own`, the team keeps its own code layout, and each code mark names the Behavior record the code implements. With `litespec`, the stack, workspace, and code layout pages apply, along with the analyzer rules for that code, as [Supported Scope](../core/scope.md#layer-claims) states.

The step is done when the marks the claim requires are in place and `litespec model check` reports no blocking finding for them.

## Step 3: Add Proof

When the team wants its tests to prove its records, it sets `proof` to `own` or `litespec` in `layers`. Tests prove scenarios: a passing test that points to a scenario proves it. Where a runner's steps cannot carry a mark, the test result file carries the proof instead. With `own`, the team keeps its own test practice. With `litespec`, the cross-stack testing page, the stack testing pages, and LiteSpec's test projects apply.

The step is done when every scenario the team claims has passing proof.

## Cover Early Gaps with Waivers

Some provisions cannot hold on the day a team adopts them. The team records each one as a waiver in `.litespec/governance/waivers/`, naming the provision it covers and the reason.

Every waiver states its kind. A gap the team means to close carries `kind: gap` with `expiresOn`, an exit, and the records in `closedBy` whose change closes it. A lasting choice carries `kind: lasting` with `consequences` and no end date. A waiver past its `expiresOn` date reports `waiver-expired`. A layer the project does not claim needs no waiver, since no provision about it applies. [Waivers](../core/waivers.md) states the fields.

## Next Steps

[Start with Records Only](../guide/start-with-records-only.md) walks a team through step 1. [Start a Consumer Repository](../tutorial/getting-started.md) builds a first working project step by step. [Supported Scope](../core/scope.md) states the layer claims and what each one covers.
