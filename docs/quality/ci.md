# Continuous Integration

**Stack:** any
**Concern:** operations

This page states the checks each pull request runs and which file changes switch each job on. It is for developers and reviewers who need to know what runs, what the model records, and where results land. A change with no check for its surface stays unverified.

## Concepts

### Jobs and Gates

Each row of the jobs table is one CI job, and each job gates the pull request: when the job fails, the pull request fails with it. The `Selected by` column names the file changes that switch a job on, so a pull request runs the jobs its changed areas need and skips the rest. The `Small` and `Large` columns state what each job runs for each project size, and the `Command` column names what the job runs. A pull request that changes only backend code skips `test-frontend`.

| Job | Checks | Selected by | Command | Small | Large |
|:---|:---|:---|:---|:---|:---|
| build | Solution and frontends compile. | Any source change. | `dotnet build <solution>` and `pnpm build` per frontend. | Runs. | Runs. |
| test-backend | Architecture, integration, and acceptance suites pass. | Backend or contract change. | `dotnet test <solution>`. | Architecture and integration only. | Runs all suites. |
| test-frontend | Lint, type-check, and route suite pass. | Frontend change. | `pnpm lint`, `pnpm type-check`, and the route suite. | Runs. | Runs. |
| measure | Each measured check the change names passes. | Backend, frontend, or measured-requirement change. | k6 thresholds, Lighthouse CI assert, and the ZAP baseline; results feed the model. | Runs. | Runs. |
| model | Model check and each check reading the in-memory model. | Record, code, or test change. | `litespec model check --record-proof`. | Runs. | Runs. |
| docs | Page contract, configuration, prose. | Documentation change. | `litespec model check`. | Runs. | Runs. |
| contract | Published document and clients unchanged. | API or contract change. | Regenerate, then `git diff --exit-code`. | Runs. | Runs. |

Projects that select the [Delivery extension](../ext/delivery.md) add its mutation, supply-chain, and release jobs, selected and promoted the same way.

The model job runs `litespec model check --record-proof` after the test run in the same job. That command blocks on the planned-Intent findings and each link finding in each run, on a developer machine and in the pull-request check alike. Only that command writes `.litespec/proof/proofs.json`, from the run on the pull request head, so a merged pull request carries fresh proofs. Exit 3 means a reader was missing, so some findings report without blocking.

### Measured Checks by Kind

Each measured requirement names its check, and each kind of measure has one tool that runs it. The measure job runs the check for each kind the change touches. The tools write their result files under `TestResults/`, and the builder reads those files the way it reads test runs, so a measured requirement shows verified once its check passes. A change that affects API latency runs the k6 thresholds, and the threshold results under `TestResults/` mark the requirement verified.

Availability and latency objectives show verified from production only; a project that selects the [Delivery extension](../ext/delivery.md) states them as OpenSLO files there. The book links the production dashboards that show them.

| Kind | Tool | Evidence |
|:---|:---|:---|
| API latency and errors | k6 thresholds | Threshold results under `TestResults/`. |
| Frontend performance and weight | Lighthouse CI assert on largest contentful paint, cumulative layout shift, total blocking time, and resource weight | Assert results under `TestResults/`. |
| Accessibility | axe in the route suite | The route suite report with zero violations. |
| Availability and latency objectives | OpenTelemetry signals | Production dashboards linked in the book, with OpenSLO files where the Delivery extension applies. |
| Security | An ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks | ZAP results under `TestResults/` with scan reports showing no unaccepted finding. |

The [Security](security.md) page states the ASVS target and the security scans, the [Operations](operations.md) page states the availability objectives and dashboards, and the route suite runs the geometry sensors.

### Contracts and Loading Detail

The committed OpenAPI document and the API types mirror the source: regeneration from source yields identical files, with no timestamps, machine paths, or unstable ordering. Frontend code reaches the backend through the generated client, so a contract change breaks the build at the call sites that need to move. For each route the Lighthouse CI run visits, the lab medians read largest contentful paint 2.5 seconds, cumulative layout shift 0.1, and total blocking time 200 milliseconds across three runs. Field interaction to next paint comes from production, where the [Operations](operations.md) page places its signals.

## Standards

### Run Applicable Gates on Every Pull Request

**ID:** `litespec/quality/ci#run-applicable-gates-on-every-pull-request`

**Strength:** required

**Statement:** Every pull request MUST run the jobs the jobs table selects for its changed areas.

**Rationale:** Each job guards one surface. A pull request that skips the job for a surface it changed merges work with no check on that surface. A skipped job carries the reason its surface stayed out, so the reviewer sees which surfaces stayed unchecked and why.

**Reviewed for:** Does the pull request show every selected job passing, with each skipped job recorded with its reason? Look at the pull request checks list.

### Check Documentation on Every Pull Request

**ID:** `litespec/quality/ci#check-documentation-on-every-pull-request`

**Strength:** required

**Statement:** The docs job MUST run on every pull request and fail on a page that breaks the page contract or configuration.

**Rationale:** Documentation breaks come in small forms: a record with a missing field, a rejected term, a misconfigured path, a page outside its page contract. Each one is trivial to fix in the pull request that introduced it and hard to trace once later pages build on it. Running the docs job on each pull request catches each break where it started.

**Checked by:** `litespec model check` reports `project-invalid` (level G) for a page or configuration that breaks its contract, and `litespec model review` reports `page-section-missing`, `page-heading-invalid`, and `prose-missing` (level R) for a page missing a section or heading, or a record that lacks its prose field.

### Run Measured Checks on Every Pull Request

**ID:** `litespec/quality/ci#run-measured-checks-on-every-pull-request`

**Strength:** required

**Statement:** The measure job MUST run the check each measured requirement names for its kind.

**Rationale:** Each kind of measure has one tool, and the measured-checks table maps them: k6 for API latency and errors, Lighthouse CI assert for frontend performance and weight, axe in the route suite for accessibility, OpenTelemetry signals for availability and latency objectives, and an ASVS 5.0 level with the ZAP baseline, free static analysis, OSV-Scanner, and Gitleaks for security. The tools write their result files under `TestResults/`, the builder reads them the way it reads test runs, and the check reports each measured requirement with no check or no passing run.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a measured requirement with no check or no passing run.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/quality/ci#run-applicable-gates-on-every-pull-request` and `litespec/quality/ci#run-measured-checks-on-every-pull-request`.

A backend-only pull request runs `build`, `test-backend`, `measure`, `model`, `docs`, and `contract`. The docs job runs with no documentation change, because it checks each pull request. A pull request that touches a frontend also runs `test-frontend`.
