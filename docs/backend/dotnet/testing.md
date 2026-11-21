# .NET Testing

**Stack:** dotnet

**Concern:** tests

Acceptance tests in a .NET backend prove reactions through the outbox they travel: the test drains the outbox in process and asserts the effect the scenario names. This page defines how the shared [Backend Testing](../testing.md) rules apply to .NET: draining the outbox in acceptance tests through the durable-messaging test package.

## Concepts

### Durable Messaging in Tests

An acceptance test asserts what a caller observes: the response, a later read, or a reaction's effect. A reaction's effect travels through the outbox, so the test drains the outbox before asserting it. The durable-messaging test package hosts a transactional outbox in the test and drains it on demand, so a scenario observes the reaction's effect without waiting for the processor's polling delay.

## Standards

### Drain the Outbox in Acceptance Tests

**ID:** `litespec/backend/dotnet/testing#drain-the-outbox-in-acceptance-tests`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An acceptance test that asserts a reaction's effect MUST drain the outbox through the durable-messaging test package before asserting it.

**Rationale:** The test hosts a transactional outbox through `AddTestTransactionalOutbox` and drains it through `ProcessorPassHelpers.DrainOutboxAsync`, both from `LiteBus.Testing.DurableMessaging` at its manifest pin. Draining runs each subscribed handler in process, so the scenario observes the reaction's effect without the processor's polling delay. The suite still runs through the real application host under `litespec/backend/testing#share-one-production-faithful-integration-harness`.

**Example:** In Claimwick, the expense-approval acceptance test sends the approve command, drains the outbox, and then asserts the payout the schedule-payout reaction wrote. Without the drain, the test would assert before the processor delivers the approval.

**Reviewed for:** Does every acceptance test that asserts a reaction's effect drain the outbox before asserting it? Look at each acceptance test beside the outbox test package.

## Conventions

None.
