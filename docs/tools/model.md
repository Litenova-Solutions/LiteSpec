# Model Builder

## Name

`litespec model` checks the project model, formats record files, renders generated outputs, and answers record queries.

## Synopsis

```bash
litespec model check [--root <consumer>] [--format json] [--release] [--since <ref>] [--all]
litespec model format [--root <consumer>] [--check]
litespec model render [--root <consumer>]
litespec model query [--root <consumer>] <id>
```

## Description

The model builder reads the written records, the code marks, the routes, the tests, and the latest test run into one model, and checks it. Every finding carries the code, level, outcome, message, path, line and subject described below. The [Underneath](#underneath) section states how the model is read and how each level, layer and trace is decided.

The four subcommands:

- `check` builds the model in memory and reports every finding with its level, code, message, path, and line. It runs only the findings of the layers the project claims in `layers`, in both the model checks and the consumer pass, as the finding table below states per code. It runs the consumer pass over the project file, the record pages, the glossary, and the controlled UI, and reports those findings in the same shape, so one command and one CI step hold the whole consumer. In text it lists every open blocking finding in full, then one line per finding code with its level and count ordered by level (G, then S, then A) and then by count, such as `record-invalid G 3`, and the totals line. By default the level A findings are summarized in those counts and not listed one by one; `--all` lists them. With `--format json` it writes one JSON object: the `findings` array holds every finding the run shows, and the `covered` array holds every finding a live departure covers. With `--release` the release-readiness findings block: `use-case-unverified`. With `--since <ref>` only the findings about records and code changed since `<ref>` block, and the rest report; it also reports every client-breaking contract change since `<ref>`: a removed result, a removed refusal code, and a changed integration event: each as a `contract-break` finding of level S, which always blocks at the level that runs it. It also reports each scenario whose proving test method changed since `<ref>` while the scenario did not, as a `proof-hash-drift` finding of level A, which never blocks.
- `format` rewrites every record file under the record roots, and the glossary file, into canonical form. With `--check` it writes nothing and lists each file that would change.
- `render` rewrites the fenced generated regions in explanation files and documents, touching no text outside the fences, and writes `litespec-records.ts` into each React and Svelte frontend frame.
- `query` prints one record with its links in both directions, showing its identifier and its folder path. Agents use it to load a record's context.

With `--format json`, `check` writes one JSON object, and each finding in it carries these fields. A continuous integration job uses `outcome` to decide what blocked the run, because the level alone does not decide it.

| Field | Holds |
|:---|:---|
| `code` | The finding code, such as `record-invalid`. |
| `level` | The finding's level: G, S or A. |
| `outcome` | What the run does with the finding: `blocking` when it fails the run, `reported` when it prints and the run passes, and `covered` when a live departure covers it. |
| `reason` | `unchanged` when `--since` demoted a blocking finding outside the changed scope, and `null` otherwise. |
| `message` | What is wrong, in one sentence. |
| `path` | The path of the finding, relative to the consumer root. |
| `line` | The line number of the finding, counting from one. |
| `subject` | The identifier of the record the finding is about, and `null` when the finding concerns no record. |

A finding that a live departure covers is reported as covered and exits zero, as the [departures page](../core/departures.md) states. A departure on a use case also covers the findings about the scenarios and results it holds. An expired departure fails the check.

The [finding codes](../reference/finding-codes.md) page holds the table of every code, with its check level, its record layer, its trace, the extension that adds it, its meaning and its fix. The page is generated from the code table the tool reads, so this page states no code.

The `render` subcommand writes these generated regions, each fenced by `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`: `alert-inventory`, `runbook-index`, and `operations-log`. It also writes `litespec-records.ts` into every React and Svelte frontend frame: the use-case and screen ids of that frontend's own entry points with the `proves` helper browser tests annotate with.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<id>` | for `query` | Prints the record with this identifier and its links both ways. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | Selects the consumer repository to read. |
| `--format <name>` | human lines | Only `check` reads it. It accepts `json` and writes one JSON object holding the `findings` and `covered` arrays, each finding with the fields the Description lists. |
| `--release` | off | Only `check` reads it. Makes the release-readiness findings block: `use-case-unverified`. |
| `--since <ref>` | off | Only `check` reads it. Blocks only on findings about records and code changed since `<ref>`; older findings report. Also reports every removed result, removed refusal code and changed integration event since `<ref>` as a `contract-break` finding. Also reports each scenario whose proving test method changed since `<ref>` while the scenario did not, as an advisory `proof-hash-drift` finding. |
| `--all` | off | Only `check` reads it. In text, lists every level A finding one by one. Without it, the level A findings are summarized by code in the counts. In JSON every finding is always listed. |
| `--check` | off | Only `format` reads it. Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The command finished with no open blocking finding. |
| `1` | `check` found an open blocking finding or an expired departure, `query` found no record with the `id`, `format --check` found a file it would change, or the run failed. |
| `2` | The arguments are wrong, the root holds no project record, the `--since` revision names no commit, or `format` found a file that does not parse. |

## Examples

Check the model and read the findings from another program:

```bash
litespec model check --root ../consumer --format json
```

Render the generated outputs after a record change:

```bash
litespec model render --root ../consumer
```

Rewrite every record file into canonical form:

```bash
litespec model format --root ../consumer
```

List the files that would change, without writing:

```bash
litespec model format --root ../consumer --check
```

Print one record with its links:

```bash
litespec model query --root ../consumer use-case/orders.cancel-order
```

Check for release, with the release-readiness findings blocking:

```bash
litespec model check --root ../consumer --release
```

Block only on the findings changed since the main branch, and report every breaking contract change since it:

```bash
litespec model check --root ../consumer --since main
```

## Underneath

The model holds every record, each with its identifier, its folder path, and its resolved links. It names the code, screens, tests, and test runs it read. It adds the computed `implements`, `calls`, and `proves` links and the computed `coded`, `verified`, `stale`, and `reachable` state of each record. Computed states are never written on a record; the book shows them. The model also lists each unmarked Domain constituent type under the aggregate whose folder holds it, or under `kernel`, in `constituents`.

Each screen lists its `operations`, its `useCases`, and its `calls`, where each call pairs one operation with the use case it reaches. The `calls` links carry one link per such pair, so a use case reached through two operations gets two links, and a use case whose operation is unknown carries an empty `operation`.

A proof goes stale when its scenario changes after the proof passed. `.litespec/proof/proofs.json` is committed: for each scenario it holds the scenario hash, its test body's hash and the hash of its proving test methods at the last passing run. On every later build, a scenario whose hash or test body differs from the recorded one has a stale proof, and a stale proof verifies nothing until a newer passing run is read. A test that both passed and failed within the runs read reports `proof-invalid`. `model check --since <ref>` compares each proving test method with the hash the base's ledger recorded: a scenario whose own hash held still while its proving test method moved reports `proof-hash-drift`, an advisory finding. Run the model check after the test run, in the same job, so the hash is recorded before a record changes.

The .NET stack reader links each code element to its record through its mark, in the style `marks` declares: a handler, an event handler, a job host, a port, an adapter, an endpoint, an aggregate root and its state-changing methods, a domain event, a refusal type, and a test. A folder or a type name is never the link. A marked refusal type declares the `code` of the refusal result its rule names.

Every check has a level. G reads records only and always blocks. S reads code through a stack reader: it blocks where the reader that produced the finding is qualified, and reports otherwise. A stack reader is the reader a stack selects to read its code, screens or tests. Qualification is per reader: a reader is qualified when it ran and its handshake matched; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, including Blazor and Android screens, stays qualified. A finding no reader produced, a record finding, is qualified. A is advisory: it reports and blocks only when `litespec model check --release` makes a release-readiness finding block. A structural parent is any link or containment the Parents table and the Identifiers table on the [model page](../core/model.md) name as a record's parent; a missing structural parent is level G. Every other required link is level G.

Every finding code also states the record layer it governs, in the `layer` column, and whether it ties that layer to the records, in the `trace` column. A code runs when its layer is `any` or a layer the project claims. A code in the Implementation or Proof layer also needs its trace to be `yes`, or the project to claim that layer with `litespec`. So a check that reads records alone has the layer `any` or `behavior` and runs for every project. A check that reads code, screens, tests, test runs or measure results through a stack reader has the layer `implementation` or `proof`, with a trace of `yes` when it ties the code or the tests to the records. The checks of LiteSpec's code layout, stacks, analyzers and operations rules have the layer `implementation` with a trace of `no`, so they run only when the project claims that layer with `litespec`. The [scope page](../core/scope.md#layer-claims) states which claims select which rows. The `extension` column names the extension that adds a code, and the `delivery` extension adds the alert and runbook codes.

A web frontend is read by a TypeScript reader that ships with the tool as one bundled script: the tool runs it with Node once per command run that builds the model, sends one versioned JSON request on standard input, and reads one versioned JSON facts document back, so a consumer installs nothing beyond Node itself. The reader opens with a handshake naming the protocol version, the reader, and the TypeScript and Svelte versions; a version mismatch, an absent Node, or a reader failure produces the single `reader-missing` finding for that frontend instead of passing silently. A finding records the reader that produced its facts, and its level takes that reader's qualification: a reader qualifies when its handshake matches; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, which reads Blazor and Android frontends, stays qualified. A finding no reader produced, a record finding, is qualified.

The builder reads `paths`, `shape`, and `stacks` from the consumer's `.litespec/project.yaml`. It reads shared provisions from the release the tool carries and the test run from TRX or JUnit XML files under `paths.testResults`. A JUnit `testcase` proves a Behavior scenario directly by carrying the scenario id in a `litespec.proves` property, one property per scenario or several ids separated by commas in one value: `<properties><property name="litespec.proves" value="scenario/borrow-copy.borrowing-an-available-copy-succeeds" /></properties>`. The case proves each id it names with the same rules as a `proves` mark on a test method, so a runner whose steps carry no mark, or a stack with no code reader, still proves its scenarios. A check `--since` base is read through an archive into a temporary folder, which is removed after the check. Nothing beneath it is separately runnable.
