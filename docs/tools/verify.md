# Verify

## Name

`litespec verify` runs every scenario probe against the running product and reports each failed expectation.

## Synopsis

```bash
litespec verify [--root <consumer>] [--base-url <url>] [--use-case <id>]
litespec verify --adversarial [--root <consumer>] [--base-url <url>] [--use-case <id>]
```

## Description

The command loads the project model as `litespec model check` does and selects every scenario carrying `probe`. With `--use-case` it runs only the probes of that use case, such as `use-case/spending.submit-expense`.

Each probe names the caller in `as`, the request in `request`, and the expected response in `expect`. An omitted `as` means anonymous. The `request` names the HTTP `method`, the `path` with `{name}` placeholders resolved from `verify.values`, and the optional `body` with the same placeholders inside strings. The `expect` names the `status`, the optional `refusal` code, and the optional `fields` checks.

The command follows no redirect. A sign-in step that answers a redirect has set its session cookie and counts as signed in, and a probe compares the answer the host gave, not the page a redirect leads to. Against a loopback address the command sends a secure session cookie back over plain http, as a browser does for localhost, so a host that marks its session cookie secure still signs in. Every sign-in and probe request carries the `verify.headers`, such as the marker header a cookie-authenticated write needs.

A `{name}` placeholder reads `verify.values`. When `verify.valuesFrom` names a command, the command runs it once from the consumer root before the first probe, and the JSON object it prints overlays the fixed values, so a probe can name the expense a fresh seed created. The command reads the base address the probes run against in the `LITESPEC_BASE_URL` environment variable, so it can create that expense through the same host. A values command that fails or prints no JSON object is a `probe-unreachable` finding for every probe, and no request is sent.

The command signs each cast person in once through the `verify.signIn` requests, in order, with one cookie container per person. A sign-in path names the person as `{person}` without the `person/` prefix, such as `ava` for `person/ava`. An anonymous probe sends with no session. The request goes to `--base-url` when it is passed, else to `verify.baseUrl` of the project file.

The command compares the answered status with the expected status, the refusal with the problem the host answered, and each field with the response body. A refusal matches when the problem `code` equals the named code or the problem `type` ends with `/<code>`. Each field names a JSON Pointer `path` with either `equals` for the exact value or `present` for whether the path exists.

A failed status, refusal, or field is a `probe-failed` finding. A host or sign-in the command cannot reach is a `probe-unreachable` finding. A probe `as` naming no cast person is a usage error the command reports before it sends anything.

In text, each finding prints as `path:line [S] [code] message`, the same shape `litespec model check` uses, with the scenario file, its line, and the scenario id as the subject. The command writes `.litespec/verify/report.json` with the base address, the probes run, and the findings. The report lists each probe with its scenario, use case, caller, method, path, expected status, and result (`passed`, `failed`, or `unreachable`), each finding with its code, message, path, line, and subject, and the run `date`.

The model builder reads the report as a test run: a passed probe proves its scenario, a failed probe fails it, and an unreachable probe is no run. A passing probe records its run in the proof ledger like a passing test run, so the proof goes stale when code outside the record paths changes.

In Claimwick, the submit-expense scenario that files a train fare carries a probe as `person/ava` posting to `/api/expenses` and expecting `201` with `/state` equal to `submitted`. The project file signs `ava` in through `GET /auth/development/{person}` at `http://localhost:5000` and fixes the expense id in `verify.values`.

`litespec verify --adversarial` replays every scenario probe the command above collects through three hostile matrices and reports each weakness the replay exposes. With `--use-case` it replays only the probes of that use case, such as `use-case/spending.submit-expense`.

Access replays every probe with `as` that expects 2xx with no session. A 2xx reply is an `access-limit-failed` finding: the use case answers a caller it should refuse. Race sends every write probe (any method but GET, HEAD and OPTIONS) of a command use case whose `idempotency.mode` is `key` or `state` five times at once; a read probe on such a scenario reads back what the command left, so it is not raced. Any 5xx is an `idempotency-failed` finding, and so are two 2xx replies with different bodies: the repeat ran twice instead of returning one result.

Input replays every probe with a JSON object body once per top-level property with that value's JSON type swapped, and once with a 10,000-character string. Text swaps to `12345`, a number swaps to `"x"`, a true or false value swaps to `"x"`, and an object or array swaps to `"x"`. A 5xx is an `input-handling-failed` finding; a 2xx to a swapped type is an `input-validation-failed` finding.

The command prints each finding as `path:line [S] [code] message`, the same shape `litespec verify` uses, writes `.litespec/verify/adversarial.json` with the base address, the probes replayed, and the findings, and exits 0 when no replay exposed a weakness, 1 when one did or the host could not be reached, and 2 on the same usage errors as `litespec verify`.

In Claimwick, the submit-expense probe files a train fare as `person/ava` and expects `201`. The adversarial run replays it with no session: a `201` reply is `access-limit-failed`, because an anonymous caller files the fare. The reimburse-expense command carries `key` idempotency that returns the first result, so five parallel replays must answer one shared body, and two different success bodies are `idempotency-failed`. An `amount` of `42.5` replayed as `"x"` must be refused: a `500` answer is `input-handling-failed` and a `200` answer is `input-validation-failed`.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--base-url <url>` | `verify.baseUrl` | The API host address the probes run against. |
| `--use-case <id>` | every probe | Run only the probes of this use case, such as `use-case/spending.submit-expense`. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | Every probe passed. |
| `1` | A probe failed or the host could not be reached. |
| `2` | The arguments are wrong, the root holds no project file, a record file does not parse, a probe names no cast person, or no base address is set. |

## Examples

Run every probe against the address the project file names:

```bash
litespec verify --root ../consumer
```

Run only the submit-expense probes:

```bash
litespec verify --root ../consumer --use-case use-case/spending.submit-expense
```

Run every probe against another host:

```bash
litespec verify --root ../consumer --base-url http://localhost:5001
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses and builds the project model. It collects the scenario records holding `probe`, checks each `as` against the cast `people` ids before it sends anything, signs each person in once with one cookie container, resolves each request through the shared resolver, sends it with the person's client, and evaluates the response with the shared evaluator. The resolver, the signed-in client, and the evaluator live in `Verify/Probes` as shared types, so the adversarial run replays probes through them. The model builder reads the written report through the probe run reader beside the TRX and JUnit readers, merged by the combined test-run reader.

With `--adversarial` the command skips the status, refusal, and field checks and replays each probe through the access, race, and input matrices through the same sessions and the same resolver. The race matrix reads the `idempotency.mode` of the use case each probe belongs to. Nothing beneath this command is separately runnable.
