# Continuous Integration

**Stack:** any
**Concern:** operations

## Intent

Every pull request runs a fixed set of CI jobs, and each job checks one surface: compilation, tests, measured checks, the project model of records joined to code, documentation, and generated contracts. This page names those jobs and states which file changes select each one. No change merges with its surface unchecked; mutation testing, the supply-chain gate, artifact promotion, branch protection and the release job live in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Concepts

### Jobs and Gates

Each row of the jobs table is one CI job, and every job is a gate: when the job fails, the pull request fails. The `Selected by` column names the file changes that switch a job on, so a pull request runs the jobs its changed areas need and skips the rest. The `Small` and `Large` columns name what each job runs for a small project and for a large project. The `Command` column names what the job runs, and the [command index](../tools/README.md) states how each `litespec` command runs in a consumer.

| Job | Checks | Selected by | Command | Small | Large |
|:---|:---|:---|:---|:---|:---|
| build | Solution and frontends compile. | Any source change. | `dotnet build <solution>` and `pnpm build` per frontend. | Runs. | Runs. |
| test-backend | Architecture, integration, and acceptance suites pass. | Backend or contract change. | `dotnet test <solution>`. | Architecture and integration only. | Runs all suites. |
| test-frontend | Lint, type-check, and route suite pass. | Frontend change. | `pnpm lint`, `pnpm type-check`, and the route suite. | Runs. | Runs. |
| measure | Each measured check the change names passes. | Backend, frontend, or measured-requirement change. | k6 thresholds, Lighthouse CI assert, and the ZAP baseline; results feed the model. | Runs. | Runs. |
| model | Model check and every check reading the in-memory model. | Record, code, or test change. | `litespec model check`. | Runs. | Runs. |
| docs | Page contract, configuration, prose. | Documentation change. | `litespec model check`. | Runs. | Runs. |
| contract | Published document and clients unchanged. | API or contract change. | Regenerate, then `git diff --exit-code`. | Runs. | Runs. |

A project that selects the [Delivery extension](../ext/delivery.md) adds the mutation, supply-chain and release jobs that extension states, with the same selection and promotion rules.

### Measured Checks by Kind

Each measured requirement names its check, and each kind of measure has one tool that runs it. The measure job runs the check for the change's kinds, the tools write their result files under `TestResults/`, and the builder reads them the way it reads test runs, so a measured requirement shows verified when its check passes. Availability and latency objectives are evidenced in production only; a project that selects the [Delivery extension](../ext/delivery.md) states them as OpenSLO files there.

| Kind | Tool | Evidence |
|:---|:---|:---|
| API latency and errors | k6 thresholds | Threshold results under `TestResults/`. |
| Frontend performance and weight | Lighthouse CI assert | Assert results under `TestResults/`. |
| Accessibility | axe in the route suite | The route suite report with zero violations. |
| Availability and latency objectives | OpenTelemetry signals | Production dashboards linked in the book, with OpenSLO files where the Delivery extension applies. |
| Security | An ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks | ZAP results under `TestResults/` with scan reports showing no unaccepted finding. |

The [security page](security.md) states the ASVS target and the security scans, the [operations page](operations.md) states the availability objectives and dashboards, and `litespec/frontend/ui#check-every-route` states the route suite.

### The Supply-Chain Gate

The supply-chain gate, its scan scope, pin rules, inventory and license review live in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Standards

### Run Applicable Gates on Every Pull Request

**ID:** `litespec/quality/ci#run-applicable-gates-on-every-pull-request`

**Strength:** required

**Statement:** Every pull request MUST run the jobs the jobs table selects for its changed areas.

**Rationale:** Each job guards one surface, so a pull request that skips the job for its changed area merges unchecked work. A job skipped because its surface did not change is recorded with that reason on the pull request, which keeps the skipped surface visible to the reviewer. A project that selects the Delivery extension adds its supply-chain gate as one of these jobs under `litespec/ext/delivery#enforce-the-supply-chain-gate`.

**Reviewed for:** The pull request checks list shows each selected job passing, with skipped jobs recorded with their reason.

### Check Documentation on Every Pull Request

**ID:** `litespec/quality/ci#check-documentation-on-every-pull-request`

**Strength:** required

**Statement:** The docs job MUST run on every pull request and fail on a page that breaks the page contract, configuration, or prose rules.

**Rationale:** Documentation breaks take many small forms: a record with a missing field, a rejected term, a misconfigured path, a page outside its [page contract](../core/authoring.md). Each one is trivial to fix at the pull request that introduced it and hard to trace once later pages build on it. Running the docs job on every pull request catches each break where it started.

**Checked by:** `litespec model check` reports `page-section-missing`, `page-heading-invalid`, or `project-invalid` (level G) for a page or configuration that breaks its contract, and `prose-missing` (level A) for a record that lacks its prose field.

### Keep Generated Contracts Fresh

**ID:** `litespec/quality/ci#keep-generated-contracts-fresh`

**Strength:** required

**Statement:** CI MUST regenerate committed OpenAPI and API types from source deterministically and fail on any difference.

**Rationale:** A committed contract that no longer matches its source sends consumers a stale API: a frontend builds against operations the backend no longer serves. Regenerating from source on CI and failing on any difference proves the committed files match the code. Generation stays deterministic, with no timestamps, machine paths, or unstable ordering, so an identical source always yields identical files.

**Reviewed for:** Regenerating OpenAPI and API types from source on the pull request produces no diff.

### Refuse Backend Calls Outside the Generated Client

**ID:** `litespec/quality/ci#refuse-backend-calls-outside-the-generated-client`

**Strength:** required

**Statement:** The contract job MUST fail a pull request whose frontend code calls the backend outside the generated client.

**Rationale:** Regenerating the contract proves the committed client matches the API, but a hand-written fetch beside it still compiles and drifts on the next contract change. Failing the pull request keeps every backend call inside the generated client that `litespec/frontend/data#use-one-configured-client-per-application` names, so a contract change breaks the build at the call sites that must move.

**Reviewed for:** The contract job report shows a clean regeneration and a lint report with no backend call outside the generated client.

### Run Measured Checks on Every Pull Request

**ID:** `litespec/quality/ci#run-measured-checks-on-every-pull-request`

**Strength:** required

**Statement:** The measure job MUST run the check each measured requirement names for its kind.

**Rationale:** Each kind has one tool, as the measured-checks table on this page maps: k6 thresholds for API latency and errors, Lighthouse CI assert for frontend performance and weight, axe in the route suite for accessibility, OpenTelemetry signals for availability and latency objectives, and an ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks for security. The tools write their result files under `TestResults/`, the builder reads them the way it reads test runs, and `litespec model check` reports `requirement-measure-missing` for a measured requirement with no check or no passing run. Every tool the table names is free and maintained; CodeQL is used only on public repositories, as `litespec/quality/security#scan-every-pull-request-with-a-free-static-analyzer` states.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a measured requirement with no check or no passing run.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/quality/ci#run-applicable-gates-on-every-pull-request` and `litespec/quality/ci#keep-generated-contracts-fresh`.

A backend-only pull request runs `build`, `test-backend`, `measure`, `model`, `docs`, and `contract`: the docs job runs even without a documentation change because it checks every pull request. A pull request that changes a frontend also runs `test-frontend`.
