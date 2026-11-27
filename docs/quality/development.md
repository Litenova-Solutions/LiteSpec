# Local Development

**Stack:** any
**Concern:** workspace

Local development needs a running stack, representative data, and a way to sign in without the real identity provider. The shortcuts that make this fast stay out of each release: a development sign-in that reaches a release is a sign-in bypass. This page states how to start the stack, which defaults keep local work close to production, and the rule that keeps each shortcut out of release builds.

## Concepts

### Development Shortcuts

A development shortcut is code that exists only to make local work or demonstration faster and that replaces or weakens a production behavior: a sign-in shortcut, a stand-in for an outside system, a seed or reset command, a relaxed guard. A shortcut changes how the product runs locally, not what the product does. The release build holds none of it, and the team exercises the real behavior before a release.

### The Cast in Development

Seed data and personas come from the cast record, whose identifier is `cast/<product-name>` and whose home is `.litespec/cast.yaml` beside `product.yaml` and `glossary.yaml`. A person in the cast carries an actor, so a development sign-in becomes that actor through the product own sign-in use case. A party, occasion or figure gives a seed command the value to write through a public use case.

The cast `today` is the date each example shares as now, so the development clock reads it and the stack runs on the cast day. Each occasion the seed brings about is a named scenario that skips when it ran before, with a baseline scenario setting up the parties. Code that uses a cast entry carries the entry id, so you see which code uses which entry.

### Seed Shape and Review

Seed data comes from the cast and passes through public use cases, with direct writes to storage left out. The local orchestrator offers a seed command that fills local state from the cast and a reset command that clears it; on .NET these are AppHost resource commands. The seed splits into named scenarios after a baseline that sets up the parties: each names the cast occasion it brings about, the scenarios it depends on, and how it tells it ran before, skipping when it did.

The orchestrator sets a development clock to the cast `today`, each host reads it through the scoped clock, and the frontend marks the displayed date as a development date; with no `today` in the cast, the real clock runs. A seed step that needs a value the cast lacks adds the fact to the cast first and reads it from there, so the seed invents no name, amount, or date. Data for paging, search, and speed comes from a fixed random seed, lives in its own scenario, and carries the generated mark, so it reads as load data instead of as an example the specification states.

The screen review and the rendered-page audit run on a stack seeded with each scenario, and each screen in the model gets reached with data by at least one scenario. A stand-in for an outside system answers the operations its external-system record declares, as the External Integrations extension defines them. A second launch profile runs the real identity provider, and the team runs it before each release.

## Standards

### Start the Stack with One Local Command

**ID:** `litespec/quality/development#start-the-stack-with-one-local-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST start WebApi, PostgreSQL, configured frontends, and its Worker where one exists, with one local command named in the consumer `AGENTS.md`.

**Rationale:** A stack that needs several terminals and a wiki page starts differently for each developer, and the differences turn into bug reports nobody reproduces. One named command brings WebApi, PostgreSQL, the frontends, and the background Worker up together, so each developer and each agent starts from the same running system. The baseline command is the Aspire `AppHost`, with `ServiceDefaults` supplying the shared discovery, health, resilience, and telemetry defaults its resources rely on.

**Reviewed for:** Does one command named in the consumer `AGENTS.md` start WebApi, PostgreSQL, the frontends, and the Worker? Look at `AGENTS.md` and the AppHost project.

### Keep Development Shortcuts Out of Release Builds

**ID:** `litespec/quality/development#keep-development-shortcuts-out-of-release-builds`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A development shortcut MUST be excluded from release builds at compile time, refuse to run outside the development environment, and carry the development-shortcut mark.

**Rationale:** A runtime setting leaves the choice to whatever the deployment passes. A missing setting fails open, so the shortcut that makes local sign-in fast turns into the sign-in bypass in a release: a sign-in shortcut that issues a session without the real identity provider lets anyone in. Compile-time exclusion removes the shortcut from the release artifact, so no deployment setting brings it back. The development-shortcut mark gives a tool one way to find each shortcut and report the one that reached a release compilation. Settled by decision/0113-shortcuts-stay-out-of-release-builds.

**Example:** Claimwick development sign-in lives in a project the solution references only when `$(Configuration)` is `Debug`, its registration sits behind `#if DEBUG`, and its type carries `/// <development-shortcut>Sign in as Ava locally</development-shortcut>`. A `Release` build has no sign-in shortcut in its output: Ava signs in through Claimwick own sign-in use case only in a development run.

**Checked by:** `dotnet build` reports `LS5014` for a type carrying `<development-shortcut>` in a compilation without `DEBUG`; reviewers check the same exclusion on other stacks by reading the build configuration each stack page states. A project with no shortcuts has nothing to satisfy, and each stack page states how its builds exclude a shortcut.

### Seed Only Development and Test Environments

**ID:** `litespec/quality/development#seed-only-development-and-test-environments`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Seed and reset commands MUST refuse to run in production, with production reference data (charts, rates, country data) arriving only through migrations or startup data that is part of the release.

**Rationale:** A seed that runs in production overwrites real data with example values, and a reset that runs there deletes it. Refusing outside development and test keeps example data where it belongs, while migrations and release startup data carry the reference rows production needs in a form the release controls and reviews.

**Example:** Claimwick seed and reset commands check the environment first and stop with an error in production. The country rows and rates production needs arrive in a migration that ships with the release, so the cast values the development seed writes stay out of production.

**Reviewed for:** Do the seed and reset commands refuse to run in production, and does production reference data arrive only through migrations or startup data in the release? Look at the seed and reset commands and the migration and startup data.

### Run the API Walker After API Boundary Changes

**ID:** `litespec/quality/development#run-the-api-walker-after-api-boundary-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** When a change touches an API boundary, a workspace MUST run the [`litespec api explore` command](../tools/api-explore.md) from a fixed seed against the seeded local stack.

**Rationale:** A walker reaches endpoints and payloads that hand-picked calls miss. A change to an API boundary adds an endpoint, alters a payload or leaves an event without a handler, and a build or one test walks only part of the surface. A fixed seed repeats the same route on each run, and running against the seeded local stack puts each call on real data, so a handler that needs an id no read returns shows up before release.

**Example:** After Claimwick changes its submit-expense endpoint, the team runs `litespec api explore` from the fixed seed against the seeded local stack. The walker reaches the endpoint with Ava cast data and reports the response, so a payload the approval screen reads wrong shows up in that run.

**Reviewed for:** Does the change report show `litespec api explore` run from a fixed seed against the seeded local stack after the last API boundary change? Look at the completion report and the walker output.

## Conventions

### Check Seeded Figures Against the Cast

**ID:** `litespec/quality/development#check-seeded-figures-against-the-cast`

**Strength:** default

**Applies to:** artifact: any

**Default:** A seed scenario reads back the state it seeded and compares the totals, counts and states with those the cast states, reporting each difference; a project whose proofs do not run yet checks its calculated results this way.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A seed that writes commands and stops shows only that the commands succeeded. A calculated total or an approval state goes wrong while each command succeeds, and a project with no running proofs learns that only at runtime. Reading the seeded state back and comparing it with the cast holds the results to the decided examples, and reporting each difference says which figure is wrong.

**Example:** Claimwick approval scenario reads the seeded expense list back and checks Ava 48.90 EUR train fare for the approved state and the count of expenses in the open month against the cast. When a calculation writes 48.91 EUR, the seed reports the total difference instead of passing silently.

**Reviewed for:** Does each seed scenario read back what it seeded, compare totals, counts and states with the cast figures, and report each difference? Look at the seed scenarios and the cast figures they cite.

### Show Failure Causes in Development

**ID:** `litespec/quality/development#show-failure-causes-in-development`

**Strength:** default

**Applies to:** artifact: any

**Default:** In development, a server failure names its cause in the problem detail with the exception type and message, and every host's logs are readable from a terminal without a browser dashboard.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A generic problem detail hides which handler or rule failed, so you read a missing record as a bad payload. Naming the exception type and message in the development problem detail shows the cause, and terminal-readable logs keep failures in the WebApi, the Worker and the frontends diagnosable without a browser dashboard.

**Example:** Claimwick development WebApi returns a problem detail naming the exception type and message when an expense rule refuses a submit, and the WebApi, Worker and frontend logs print to the terminal that runs the AppHost, so Ben sees the cause without a browser dashboard.

**Reviewed for:** Does a development server failure name its exception type and message in the problem detail, and can every host's logs be read from a terminal without a browser dashboard? Look at the development problem-detail writer and the host log output.

### Add a Missing Example Fact to the Cast, Not to the Seed

**ID:** `litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed`

**Strength:** default

**Applies to:** artifact: any

**Default:** A seed step that needs a value the cast lacks adds the fact to the cast and reads it from there; the seed invents no name, amount, or date.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A value invented in the seed answers no example the specification states, so the seeded product shows a figure no reader traces. Adding the fact to the cast keeps the seed on the one source the [Writing](../core/writing.md) page requires of examples.

**Example:** Claimwick seed needs a hotel night for the client visit that the cast lacks, so the night joins the cast as a figure and the seed reads it. The seed holds no amount of its own.

**Reviewed for:** Does every name, amount, and date in the seed resolve to a cast entry, with no value invented in the seed? Look at the seed code against `.litespec/cast.yaml`.

## Reference Example

This informative example demonstrates `litespec/quality/development#start-the-stack-with-one-local-command`, `litespec/quality/development#keep-development-shortcuts-out-of-release-builds`, `litespec/quality/development#seed-only-development-and-test-environments`, and `litespec/quality/development#run-the-api-walker-after-api-boundary-changes`.

Claimwick starts WebApi, PostgreSQL, its frontend and its Worker with one command named in its `AGENTS.md`, so Ava expenses and Ben approvals run on the same stack for each developer. Ava signs in through the development sign-in in a development run only: it replaces the identity provider, and Claimwick own sign-in use case issues her session as `person/ava`. Seed and reset refuse to run in production, where reference rows arrive through a migration in the release. After a change to the submit-expense endpoint, the team runs `litespec api explore` from the fixed seed against the seeded local stack.
