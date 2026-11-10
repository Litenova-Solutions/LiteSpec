# Adopt LiteSpec in Steps

## Intent

Adopting LiteSpec means pinning one release, claiming the record layers the team uses, and building to those claims with their checks in place. A team starts with Intent and Behavior, adds Implementation and Proof when it is ready, and works in either order. Each departure from a single provision stays recorded until it is resolved.

## Fit Check

LiteSpec fits a team building one business application: one domain, one API, and one database, with one or more frontends, or a client-only app with no backend of its own. Check the [scope page](../core/scope.md) first. If the system is several services, or something other than a business application, LiteSpec is not built for it.

## Choose the Layers to Start

The `layers` block in `.litespec/project.yaml` names the record layers the project claims and the practice it follows in each. Each layer is written from the one above it, so a team adds layers from the bottom: Behavior needs Intent, and Implementation and Proof each need Behavior. The [scope page](../core/scope.md#layer-claims) states the five combinations a project can claim and what each one covers.

| Step | Layers claimed | The team is ready when |
|:---|:---|:---|
| 1 | `intent` and `behavior`, both `litespec` | It wants the records, the book and the checks, and keeps its own code layout and tests |
| 2 | `implementation`, as `own` or `litespec` | It wants code marks that point at Behavior records, in its own layout or in LiteSpec's |
| 3 | `proof`, as `own` or `litespec` | It wants passing tests to prove Behavior scenarios, in its own test practice or in LiteSpec's |

Implementation and Proof each need only Behavior, so a team may add Proof before Implementation, or stop after either one. A team that keeps its own layout and its own tests can stop at step 3 with both `own`. Claimwick, the running example, claims all four layers with `litespec`.

Extensions work the same way at every step. A team selects an extension when its criterion holds, whatever layers it claims.

## Step 1: Claim Intent and Behavior

At this step the project claims `intent` and `behavior` alone. Its code keeps its own layout, its tests keep their own shape, and no analyzer rule applies. In return the team gets checked records, a book of Intent, Behavior and governance, and `rules` answers about records. The [records-only guide](../guide/start-with-records-only.md) takes a team through this step.

## Step 2: Add Implementation

Set `implementation` to `own` or `litespec` in `layers`, and name the stacks the project uses, since a stack selects the code reader that checks its code. With `own`, the team keeps its own code layout, and each code mark names the Behavior record that the code implements. With `litespec`, LiteSpec's stack, workspace and code layout pages apply, along with the analyzer rules for that code, as the [scope page](../core/scope.md#layer-claims) states.

The step is done when the code marks the claim requires are in place and the model check reports no blocking finding for them.

## Step 3: Add Proof

Set `proof` to `own` or `litespec` in `layers`. Tests prove scenarios: a passing test that points to a scenario proves it, and where a runner's steps cannot carry a mark, the test result file carries the proof instead. With `own`, the team keeps its own test practice. With `litespec`, the cross-stack testing page, the stack testing pages and LiteSpec's test projects apply.

The step is done when every scenario the team claims has passing proof.

## Departures for a Single Provision

Some provisions cannot hold on the day a team adopts them. A team records each one as a departure in `.litespec/governance/departures/`, naming the provision it covers and the reason. A departure for a gap the team means to close also carries an expiry date, and a departure with no expiry date is a lasting choice. A departure past its expiry date reports `departure-expired`. A layer the project does not claim needs no departure, since no provision about it applies. The [departure page](../core/departures.md) states the fields.

## Next Steps

[Start with Records Only](../guide/start-with-records-only.md) takes a team through step 1. [Start a Consumer Repository](../tutorial/getting-started.md) builds a first working project step by step. The [scope page](../core/scope.md) states the layer claims and what each one covers.
