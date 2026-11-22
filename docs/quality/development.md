# Local Development

**Stack:** any
**Concern:** workspace

Local development needs a running stack, representative data, and a way to sign in without the real identity provider. The shortcuts that make this fast must never ship: a development sign-in that reaches a release is a sign-in bypass. This page states how to start the stack, which defaults make local work represent production, and the rule that keeps every shortcut out of release builds.

## Concepts

### Development Shortcuts

A development shortcut is code that exists only to make local development or demonstration faster and that replaces or weakens a production behavior: a sign-in shortcut, a fake of an external system, a seed or reset command, a relaxed guard. A shortcut changes how the product runs locally, not what the product does. The release build does not contain it, and the real behavior is exercised before a release.

### The Cast in Development

Seed data and personas come from the cast record, whose identifier is `cast/<product-name>` and whose fixed home is `.litespec/cast.yaml` beside `product.yaml` and `glossary.yaml`. A person in the cast carries an actor, so a development sign-in becomes that actor in the product's own sign-in use case. A party, occasion or figure gives a seed command the value to write through a public use case. Code that uses a cast entry marks it with the entry's id, so a reader can see which code uses which entry.

## Standards

### Start the Stack with One Local Command

**ID:** `litespec/quality/development#start-the-stack-with-one-local-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST start WebApi, PostgreSQL, configured frontends, and its Worker where one exists, with one local command named in the consumer `AGENTS.md`.

**Rationale:** A stack that needs several terminals and a wiki page starts differently for every developer, and the differences become bug reports nobody can reproduce. One named command brings WebApi, PostgreSQL, the frontends, and the background Worker up together, so every developer and every agent starts from the same running system. The baseline command is the Aspire `AppHost`, with `ServiceDefaults` supplying the shared discovery, health, resilience, and telemetry defaults its resources rely on. A workspace that orchestrates the same set another way satisfies this rule with its own command: the obligation is one command that brings the stack up, not one tool that does it.

**Reviewed for:** Does one command named in the consumer `AGENTS.md` start WebApi, PostgreSQL, the frontends, and the Worker? Look at `AGENTS.md` and the AppHost project.

### Keep Development Shortcuts Out of Release Builds

**ID:** `litespec/quality/development#keep-development-shortcuts-out-of-release-builds`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A development shortcut MUST be excluded from release builds at compile time, refuse to run outside the development environment, and carry the development-shortcut mark.

**Rationale:** A runtime setting leaves the choice to whatever the deployment passes. A missing setting fails open, so the shortcut that makes local sign-in fast becomes the sign-in bypass in a release: a sign-in shortcut that issues a session without the real identity provider lets anyone in. Compile-time exclusion removes the shortcut from the release artifact, so no deployment setting can bring it back. The development-shortcut mark gives a tool one way to find every shortcut and report the one that reached a release compilation. Proposed by [decision/0113-shortcuts-stay-out-of-release-builds](../intent/decisions/0113-shortcuts-stay-out-of-release-builds/0113-shortcuts-stay-out-of-release-builds.md).

**Example:** Claimwick's development sign-in lives in a project the solution references only when `$(Configuration)` is `Debug`, its registration sits behind `#if DEBUG`, and its type carries `/// <development-shortcut>Sign in as Ava locally</development-shortcut>`. A `Release` build has no sign-in shortcut in its output: Ava signs in through Claimwick's own sign-in use case only in a development run.

**Checked by:** `dotnet build` reports `LS5014` for a type carrying `<development-shortcut>` in a compilation without `DEBUG`; reviewers check the same exclusion on other stacks by reading the build configuration each stack page states. A project with no shortcuts has nothing to satisfy, and each stack page states how its builds exclude a shortcut.

## Conventions

### Authenticate as a Cast Person

**ID:** `litespec/quality/development#authenticate-as-a-cast-person`

**Strength:** default

**Applies to:** artifact: any

**Default:** A development sign-in replaces only the identity provider and completes sign-in through the product's own sign-in use case as a cast person, so sessions, tenancy, permissions and audit run as in production.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A sign-in that mints a session on its own skips the checks the sign-in use case runs. Going through that use case as a cast person exercises the same session, tenancy, permission and audit path as a production sign-in, so the shortcut hides no difference between local and released behavior.

**Reviewed for:** Does the development sign-in call the product's sign-in use case as a cast person, with no other way to gain a session? Look at the development sign-in and the sign-in use case.

### Seed from the Cast Through Public Use Cases

**ID:** `litespec/quality/development#seed-from-the-cast-through-public-use-cases`

**Strength:** default

**Applies to:** artifact: any

**Default:** Development seed data comes from the cast and goes through public use cases, never through direct writes to storage.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A direct write to storage bypasses the guards and side effects a command runs, so seeded state can be a state no user could create. Commands from cast values put the seed through the same path a production request takes.

**Reviewed for:** Does the seed call commands with values from the cast, with no direct writes to storage? Look at the seed code and its cast marks.

### Provide Seed and Reset from the Local Orchestrator

**ID:** `litespec/quality/development#provide-seed-and-reset-from-the-local-orchestrator`

**Strength:** default

**Applies to:** artifact: any

**Default:** The local orchestrator offers a seed command that fills the local state from the cast and a reset command that clears it; on .NET these are AppHost resource commands.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A seed that each developer runs from memory leaves a different local state on every machine. Putting seed and reset on the orchestrator gives one place that brings a clean stack to a known state.

**Reviewed for:** Does the orchestrator offer seed and reset commands, as AppHost resource commands where .NET is selected? Look at the orchestrator project and `AGENTS.md`.

### Keep the Real Identity Provider One Launch Profile Away

**ID:** `litespec/quality/development#keep-the-real-identity-provider-one-launch-profile-away`

**Strength:** default

**Applies to:** artifact: any

**Default:** A second launch profile runs the real identity provider, and the project runs it before each release.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A shortcut that replaces the identity provider proves nothing about the real sign-in. A second profile keeps the real provider one launch away, so a release can be checked against the sign-in users will actually run.

**Reviewed for:** Is there a launch profile that runs the real identity provider, and does the release evidence record running it before a release? Look at the launch profiles and the release evidence.

### Give Each External System a Stand-In That Answers Its Declared Operations

**ID:** `litespec/quality/development#give-each-external-system-a-stand-in-that-answers-its-declared-operations`

**Strength:** default

**Applies to:** artifact: any

**Default:** A development stand-in for an external system answers the operations its external-system record declares, as the [External Integrations extension](../ext/integrations.md) defines them.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A stand-in that answers operations the record does not declare teaches the local build a behavior the product never agreed to. Answering the declared operations keeps local runs honest to the external-system record.

**Reviewed for:** Does each stand-in answer every operation its external-system record declares, with no others? Compare each stand-in with its external-system record.

## Reference Example

This informative example demonstrates `litespec/quality/development#start-the-stack-with-one-local-command`, `litespec/quality/development#keep-development-shortcuts-out-of-release-builds`, `litespec/quality/development#authenticate-as-a-cast-person`, `litespec/quality/development#seed-from-the-cast-through-public-use-cases`, `litespec/quality/development#provide-seed-and-reset-from-the-local-orchestrator`, `litespec/quality/development#keep-the-real-identity-provider-one-launch-profile-away`, and `litespec/quality/development#give-each-external-system-a-stand-in-that-answers-its-declared-operations`.

Claimwick starts WebApi, PostgreSQL, its frontend and its Worker with one command named in its `AGENTS.md`, so Ava's expenses and Ben's approvals run on the same stack for every developer. Ava signs in through the development sign-in: it replaces only the identity provider, and Claimwick's own sign-in use case issues her session as `person/ava` with the employee actor, so the 25 EUR receipt threshold and the approval by Ben still run. The orchestrator's seed command reads `cast/claimwick` and creates the consultancies Harbor and Pine through the public add-client command, with the client-visit train fare of 48.90 EUR filed through the public submit-expense command; reset clears the state and seeds it again. The payment stand-in answers the operations Claimwick's external-system record declares. The development sign-in is compiled only in Debug, carries the `<development-shortcut>` mark and refuses to run outside development, while a second launch profile runs the real identity provider before each release.
