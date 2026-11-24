# Continuous Integration

**Stack:** any
**Concern:** operations

Every pull request runs a fixed set of CI jobs, and each job checks one surface: compilation, tests, measured checks, the project model of records joined to code, documentation, and generated contracts. This page names those jobs and states which file changes select each one. No change merges with its surface unchecked. Mutation testing, the supply-chain gate, artifact promotion, branch protection, and the release job are stated in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Concepts

### Jobs and Gates

Each row of the jobs table is one CI job, and every job is a gate: when the job fails, the pull request fails with it. The `Selected by` column names the file changes that switch a job on, so a pull request runs the jobs its changed areas need and skips the rest. The `Small` and `Large` columns state what each job runs for each project size, and the `Command` column names what the job runs. The [command index](../tools/README.md) states how each `litespec` command runs in a consumer. A pull request that changes only backend code skips `test-frontend`.

| Job | Checks | Selected by | Command | Small | Large |
|:---|:---|:---|:---|:---|:---|
| build | Solution and frontends compile. | Any source change. | `dotnet build <solution>` and `pnpm build` per frontend. | Runs. | Runs. |
| test-backend | Architecture, integration, and acceptance suites pass. | Backend or contract change. | `dotnet test <solution>`. | Architecture and integration only. | Runs all suites. |
| test-frontend | Lint, type-check, and route suite pass. | Frontend change. | `pnpm lint`, `pnpm type-check`, and the route suite. | Runs. | Runs. |
| measure | Each measured check the change names passes. | Backend, frontend, or measured-requirement change. | k6 thresholds, Lighthouse CI assert, and the ZAP baseline; results feed the model. | Runs. | Runs. |
| model | Model check and every check reading the in-memory model. | Record, code, or test change. | `litespec model check --record-proof`. | Runs. | Runs. |
| docs | Page contract, configuration, prose. | Documentation change. | `litespec model check`. | Runs. | Runs. |
| contract | Published document and clients unchanged. | API or contract change. | Regenerate, then `git diff --exit-code`. | Runs. | Runs. |

Projects that select the [Delivery extension](../ext/delivery.md) add its mutation, supply-chain, and release jobs, selected and promoted the same way.

The model job runs `litespec model check --record-proof`. That command blocks on the planned-Intent findings and every link finding in every run, on a developer machine and in the pull-request check alike, as [Check Levels](../core/model.md#check-levels) states.

Only `litespec model check --record-proof` writes `.litespec/proof/proofs.json`, from the run on the pull request head, so a merged pull request carries fresh proofs: the check runs after the test run in the same job.

The model job treats exit 3 as failure unless the job allows incomplete inspection. Exit 3 means no finding blocks but a reader was missing, so some findings report without blocking.

### Measured Checks by Kind

Each measured requirement names its check, and each kind of measure has one tool that runs it. The measure job runs the check for each kind the change touches.

The tools write their result files under `TestResults/`, and the builder reads those files the way it reads test runs, so a measured requirement shows verified once its check passes. For example, a change that affects API latency runs the k6 thresholds, and the threshold results under `TestResults/` mark the requirement verified.

Availability and latency objectives show verified from production only; a project that selects the [Delivery extension](../ext/delivery.md) states them as OpenSLO files there.

| Kind | Tool | Evidence |
|:---|:---|:---|
| API latency and errors | k6 thresholds | Threshold results under `TestResults/`. |
| Frontend performance and weight | Lighthouse CI assert on largest contentful paint, cumulative layout shift, total blocking time, and resource weight | Assert results under `TestResults/`. |
| Accessibility | axe in the route suite | The route suite report with zero violations. |
| Availability and latency objectives | OpenTelemetry signals | Production dashboards linked in the book, with OpenSLO files where the Delivery extension applies. |
| Security | An ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks | ZAP results under `TestResults/` with scan reports showing no unaccepted finding. |

The [Security](security.md) page states the ASVS target and the security scans, the [Operations](operations.md) page states the availability objectives and dashboards, and `litespec/frontend/ui#pass-every-route-suite-check` states the route suite.

### Supply-Chain Gate

The [Delivery extension](../ext/delivery.md) gives projects that select it a supply-chain gate: scan scope, pin rules, inventory, and license review.

## Standards

### Run Applicable Gates on Every Pull Request

**ID:** `litespec/quality/ci#run-applicable-gates-on-every-pull-request`

**Strength:** required

**Statement:** Every pull request MUST run the jobs the jobs table selects for its changed areas.

**Rationale:** Each job guards one surface. A pull request that skips the job for a surface it changed merges work nobody checked. A skipped job is recorded on the pull request with the reason its surface did not change, so the reviewer sees which surfaces stayed unchecked and why. A project that selects the Delivery extension adds its supply-chain gate as one of these jobs under `litespec/ext/delivery#enforce-the-supply-chain-gate`.

**Reviewed for:** Does the pull request show every selected job passing, with each skipped job recorded with its reason? Look at the pull request checks list.

### Check Documentation on Every Pull Request

**ID:** `litespec/quality/ci#check-documentation-on-every-pull-request`

**Strength:** required

**Statement:** The docs job MUST run on every pull request and fail on a page that breaks the page contract or configuration.

**Rationale:** Documentation breaks come in small forms: a record with a missing field, a rejected term, a misconfigured path, a page outside its [page contract](../core/authoring.md). Each one is trivial to fix in the pull request that introduced it and hard to trace once later pages build on it. Running the docs job on every pull request catches each break where it started, and `litespec model review` reports the prose rules beside it.

**Checked by:** `litespec model check` reports `project-invalid` (level G) for a page or configuration that breaks its contract, and `litespec model review` reports `page-section-missing`, `page-heading-invalid`, and `prose-missing` (level R) for a page missing a section or heading, or a record that lacks its prose field.

### Keep Generated Contracts Fresh

**ID:** `litespec/quality/ci#keep-generated-contracts-fresh`

**Strength:** required

**Statement:** CI MUST regenerate committed OpenAPI and API types from source deterministically and fail on any difference.

**Rationale:** A committed contract that no longer matches its source sends consumers a stale API, and a frontend builds against operations the backend no longer serves. Regenerating from source in CI and failing on any difference proves the committed files match the code. Generation stays deterministic, with no timestamps, machine paths, or unstable ordering, so identical source always yields identical files.

**Reviewed for:** Does regenerating OpenAPI and API types from source produce no diff? Regenerate them on the pull request and read the diff.

### Refuse Backend Calls Outside the Generated Client

**ID:** `litespec/quality/ci#refuse-backend-calls-outside-the-generated-client`

**Strength:** required

**Statement:** The contract job MUST fail a pull request whose frontend code calls the backend outside the generated client.

**Rationale:** Regenerating the contract proves the committed client matches the API, but a hand-written fetch beside it still compiles and drifts on the next contract change. Failing the pull request keeps every backend call inside the generated client that `litespec/frontend/data#use-one-configured-client-per-application` names. A contract change then breaks the build at the call sites that need to move.

**Reviewed for:** Does the contract job show a clean regeneration with no backend call outside the generated client? Read the contract job report and its lint report.

### Run Measured Checks on Every Pull Request

**ID:** `litespec/quality/ci#run-measured-checks-on-every-pull-request`

**Strength:** required

**Statement:** The measure job MUST run the check each measured requirement names for its kind.

**Rationale:** Each kind of measure has one tool, as the measured-checks table above maps: k6 thresholds for API latency and errors, Lighthouse CI assert for frontend performance and weight, axe in the route suite for accessibility, OpenTelemetry signals for availability and latency objectives, and an ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks for security. The tools write their result files under `TestResults/`, the builder reads them the way it reads test runs, and `litespec model check` reports `requirement-measure-missing` for a measured requirement with no check or no passing run. Every tool the table names is free and maintained; CodeQL runs only on public repositories, as `litespec/quality/security#scan-every-pull-request-with-a-free-static-analyzer` states.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a measured requirement with no check or no passing run.

## Conventions

### Assert Loading, Stability, and Blocking Time per Route

**ID:** `litespec/quality/ci#assert-loading-stability-and-blocking-time-per-route`

**Strength:** default

**Default:** Assert, for each route the Lighthouse CI run visits, a largest contentful paint of at most 2.5 seconds, a cumulative layout shift of at most 0.1, and a total blocking time of at most 200 milliseconds, as the median of three runs.

**Replacement:** A consumer can replace this default by naming its own thresholds and their reason in its Lighthouse CI configuration.

**Rationale:** A Lighthouse CI assert that names no metric passes every build. Largest contentful paint says when the page shows its main content, cumulative layout shift says whether the page jumps while the staff member reads it, and total blocking time stands in for interaction to next paint, which a lab run cannot measure because nobody clicks. The defaults are the values the web community treats as good, and the median of three runs keeps one slow run from failing a build. Field interaction to next paint is measured in production, where the [Operations](operations.md) page places its signals.

**Reviewed for:** Does every route in the Lighthouse CI run assert largest contentful paint, cumulative layout shift, and total blocking time? Look in the Lighthouse CI configuration.

## Reference Example

This informative example demonstrates `litespec/quality/ci#run-applicable-gates-on-every-pull-request` and `litespec/quality/ci#keep-generated-contracts-fresh`.

A backend-only pull request runs `build`, `test-backend`, `measure`, `model`, `docs`, and `contract`. The docs job runs even with no documentation change, because it checks every pull request. A pull request that touches a frontend also runs `test-frontend`.
