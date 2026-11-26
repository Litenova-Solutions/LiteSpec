# Model Builder

## Name

`litespec model` checks the project model, reviews its prose, formats record files, renders generated outputs, answers record queries, and reports module edges.

## Synopsis

```bash
litespec model check [--root <consumer>] [--format json] [--release] [--since <ref>] [--module <module>] [--only-paths <paths...>] [--record-proof] [--verbose]
litespec model review [--root <consumer>] [--format json]
litespec model format [--root <consumer>] [--check]
litespec model render [--root <consumer>]
litespec model query [--root <consumer>] <id>
litespec model query [--root <consumer>] --search <text>
litespec model query [--root <consumer>] --list <type> [--module <module>]
litespec model edges [--root <consumer>] [--module <module>]
```

## Description

The model builder reads the written records, the code marks, the routes, the tests, and the latest test run into one model, and checks it. Every finding carries a code, a level, an outcome, a message, a path, a line, and a subject, as the field table below describes. [Underneath](#underneath) states how the model is read and how each finding's level, layer, and trace are decided.

The six subcommands are `check`, `review`, `format`, `render`, `query`, and `edges`.

`check` builds the model in memory and reports every finding with its level, code, message, path, and line. It runs only the findings of the layers the project claims in `layers`, in both the model checks and the consumer pass, as the finding table states per code. It runs the consumer pass over the project file, the record pages, the glossary, and the controlled UI, and reports those findings in the same shape, so one command and one CI step check the whole consumer. It reports `toolchain-drift` when the tool manifest, the analyzer reference, and the running CLI name different LiteSpec builds, as [Run One LiteSpec Build](../core/release.md#run-one-litespec-build) requires.

In text, `check` lists every finding in full, the open blocking findings first, then one line per finding code with its count and level, ordered by level (G, then S) and then by count, such as `       3  record-invalid (level G)`, and then the totals line. When the installed release differs from the release the project reviewed, the `release-drift` finding prints before every other finding: it names both releases and the command that installs the reviewed one, because every finding after it may not apply.

With `--format json`, it writes one JSON object: the `findings` array holds every finding the run shows, and the `covered` array holds every finding a live waiver covers.

With `--release`, `reader-missing` blocks for a claimed layer.

With `--since <ref>`, the command checks the tree at `<ref>` and the working tree, and blocks every finding present now and absent at `<ref>`. Findings match by code, subject identifier and message, never by line. When the base tree cannot be built or checked, every finding blocks and the output says why. The run also reports every client-breaking contract change since `<ref>` as a `contract-break` finding of level S, which always blocks at the level that runs it: a removed result, a removed refusal code, and a changed integration event.

With `--module <module>`, the command prints only the findings on the records and files of that module, and the exit code follows those findings alone. The command reads only that module's backend projects, so a concurrent edit that left another module unparsable does not fail the run.

With `--only-paths <paths...>`, the command reports only the findings on the named paths and their records: a finding belongs to the named paths when its file is one of them or sits under one of them, or when its subject record's file does. Paths resolve under the consumer root, and the exit code follows the kept findings alone, so parallel workers check only their owned slice. The two scopes combine: with both flags the command reports only the findings that belong to the module and to the named paths.

`check` writes nothing to the repository, except with `--record-proof`, which writes `.litespec/proof/proofs.json` from the run. Continuous integration runs the check with `--record-proof` on the pull request head, after the test run in the same job, so a merged pull request carries fresh proofs.

`review` builds the same model and reports the level R findings the check sets aside: prose quality only a person can weigh, such as a missing summary or a vague word. It prints in the same shape as `check`, with the same counts and totals, and it always exits zero: its findings never block, and agents are not asked to clear them.

`format` rewrites every record file under the record roots, and the glossary file, into canonical form. With `--check` it writes nothing and lists each file that would change.

`render` rewrites the fenced generated regions in explanation files and documents, touching no text outside the fences. It writes the `source-links` region at the end of each research and constraint explanation that declares sources, and it inserts or rewrites the `agent-protocol` region in `AGENTS.md` from the carried release and the project file.

It also rewrites each marked project-scope skill copy that differs from what the carried release generates, writes `litespec-records.ts` into each React and Svelte frontend frame, and writes the UI source lock beside the shared package. `render` writes nothing when a record file does not parse: it lists each invalid file and exits 2, so a rendered identifier list is never quietly short.

`query` prints one record with its links in both directions, showing its identifier and its folder path; agents use it to load a record's context. For a `step/<flow>.<act>` id it prints the step with its `title`, `statement`, `failure`, and branches, the screens and use cases serving it, the requirements it carries with the use cases fulfilling them, and for each serving or fulfilling record the code files whose marks name it and the tests proving its scenarios. Its completion block carries the stage with three counts, `inScope`, `parked` and `total`: `heldBy` lists only the in-scope records holding the stage back, and `parkedBy` lists each parked record with the waiver parking it and its `expiresOn`.

With `--search <text>`, it lists the written records and flow steps whose identifier, title, summary, or statement holds the text, case-insensitive, one line each with identifier, title, and path. An exact identifier match comes first, then other identifier matches, then title, summary, and statement matches, each group by identifier.

With `--list <type>`, it lists every written record of the type, such as `use-case`, one line each with identifier, title, and path, by identifier, or only those of one module with `--module <module>`. With `--search` or `--list`, no match prints nothing and exits 0.

`edges` prints the written record links between modules, per module: the outgoing and incoming counts with counts per link kind and per other module, then every pair of modules that link each other both ways. With `--module <module>` it prints only that module with the mutual pairs it belongs to. It only reads the built model; it writes nothing.

With `--format json`, `check` and `review` write one JSON object with each finding carrying these fields. A continuous integration job decides what blocked the run from `outcome`, because the level alone does not decide it.

| Field | Holds |
|:---|:---|
| `code` | The finding code, such as `record-invalid`. |
| `level` | The finding's level: G, S or R. |
| `outcome` | What the run does with the finding: `blocking` when it fails the run, `reported` when it prints and the run passes, and `covered` when a live waiver covers it. |
| `reason` | `unchanged` when `--since` reported a finding already present at `<ref>`, and `null` otherwise. |
| `message` | What is wrong, in one sentence. |
| `path` | The path of the finding, relative to the consumer root. |
| `line` | The line number of the finding, counting from one. |
| `subject` | The identifier of the record the finding is about, and `null` when the finding concerns no record. |

When a live waiver covers a finding, the finding is reported as covered and the run exits zero, as the [waivers page](../core/waivers.md) states. A waiver on a use case also covers the findings about the scenarios and results the use case holds. An expired waiver fails the check.

Every code is tabulated on the [finding codes](../reference/finding-codes.md) page: its check level, its record layer, its trace, the extension that adds it, its meaning, and its fix. That page is generated from the code table the tool reads, so no code is stated here.

The generated regions `render` writes, each fenced by `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`, are:

- `alert-inventory`, `runbook-index`, and `operations-log` in explanation files and documents.
- `source-links` at the end of each research and constraint explanation that declares sources, with one Markdown link definition per source with its date.
- `decision-index` in the README of a decisions folder, with the folder's decisions grouped by `kind`, then by the concern their followers compute.
- `waiver-index` in the README of the product `governance/waivers/` folder, with every waiver of the product including the module waivers and a Module column: gaps by `expiresOn` with their `closedBy`, then lasting waivers with their consequences.
- `agent-protocol` in `AGENTS.md` at the consumer root, carrying the release, size, shape, stacks, layers, extensions, selection key, and command from the project file. When `AGENTS.md` holds no region, `render` inserts it after the first heading, or at the end; when the file is missing, `render` writes it from the template.

Each project-scope skill copy carrying the generated marker is rewritten when it differs from what the carried release generates, holding exactly the carried files. A copy without the marker is left alone, a missing copy is never created, and global copies live outside the project and stay outside the render.

`render` also writes `litespec-records.ts` into every React and Svelte frontend frame: the use-case and screen ids of that frontend's own entry points with the `proves` helper browser tests annotate with.

The UI source lock is written at `ui.package.sourceLock` when the project declares a shared package and the lock file exists. Each component's `digest` covers its paths: the digest of its one path, or the SHA-256 of its paths' digests joined by newlines in recorded order. The `preset` is copied from the manifest baseline for the package's platform, and `cli` is the manifest pin of that platform's CLI. The team's entries, the `registry`, the `formatter`, and the `generator` stay as written. `render` never creates the lock, and a lock whose text is already current is left byte for byte as it was.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<id>` | for `query` without `--search` or `--list` | Prints the record with this identifier and its links both ways. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | Selects the consumer repository to read. |
| `--format <name>` | human lines | Only `check` and `review` read it. It accepts `json` and writes one JSON object holding the `findings` and `covered` arrays, each finding with the fields the Description lists. |
| `--release` | off | Only `check` reads it. Makes `reader-missing` block for a claimed layer. |
| `--since <ref>` | off | Only `check` reads it. Checks the tree at `<ref>` and the working tree, and blocks every finding present now and absent at `<ref>`; findings match by code, subject identifier and message, never by line. When the base cannot be built or checked, every finding blocks. Also reports every removed result, removed refusal code and changed integration event since `<ref>` as a `contract-break` finding. |
| `--check` | off | Only `format` reads it. Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--search <text>` | off | Only `query` reads it. Lists the written records and flow steps whose identifier, title, summary or statement holds the text, case-insensitive, one line each with identifier, title and path. An exact identifier match prints first, then other identifier matches, title, summary and statement matches; each group orders by identifier. Prints nothing and exits 0 when nothing matches. |
| `--list <type>` | off | Only `query` reads it. Lists every written record of the type, one line each with identifier, title and path, ordered by identifier. With `--module` it lists only the records of that module. An unknown type is a usage error. |
| `--module <module>` | off | Only `check`, `query --list` and `edges` read it. `check` prints only the findings on the records and files of the module, and the exit code follows those findings alone. `check` also reads only that module's backend projects. `edges` prints only that module with the mutual pairs it belongs to. An unknown module is a usage error. |
| `--only-paths <paths...>` | off | Only `check` reads it. Reports only the findings on the named paths and their records, and the exit code follows those findings alone. A finding belongs to the named paths when its file is one of them or sits under one of them, or when its subject record's file does. |
| `--record-proof` | off | Only `check` reads it. Writes `.litespec/proof/proofs.json` from the run, holding for each scenario the run commit and the outcome of the last passing run. Without it, `check` writes nothing. |
| `--verbose` | off | Only `check` reads it. Lists every rejected shape behind each schema error instead of the selected one. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The command finished with no open blocking finding and, for `check`, a complete inspection. `review` always exits 0. |
| `1` | `check` found an open blocking finding or an expired waiver, `query` found no record with the `id`, `format --check` found a file it would change, or the run failed. With `--search` or `--list`, no match prints nothing and exits 0. |
| `2` | The arguments are wrong, the root holds no project record, the `--since` revision names no commit, the `--list` type or `--module` name is unknown, or `format` or `render` found a file that does not parse. |
| `3` | `check` found no open blocking finding but the inspection was incomplete: a reader was missing, so some level S findings report without blocking. The run prints `inspection incomplete: reader for <frontend> missing, <N> findings not blocking` as its last line, one clause per frontend. |

## Examples

Check the model and read the findings from another program:

```bash
litespec model check --root ../consumer --format json
```

Review the prose quality a person weighs:

```bash
litespec model review --root ../consumer
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

Print one flow step with its trace:

```bash
litespec model query --root ../consumer step/submit-and-reimburse.submit-expense
```

Find the records that mention a word:

```bash
litespec model query --root ../consumer --search "receipt threshold"
```

List every use case of one module:

```bash
litespec model query --root ../consumer --list use-case --module spending
```

Check only one module:

```bash
litespec model check --root ../consumer --module spending
```

Check only the owned paths, such as one worker's slice:

```bash
litespec model check --root ../consumer --only-paths .litespec/behavior/modules/spending apps/api/src/Modules/Claimwick.Spending
```

Report the links between modules:

```bash
litespec model edges --root ../consumer
```

Check for release, with a missing reader for a claimed layer blocking:

```bash
litespec model check --root ../consumer --release
```

Block every finding the change introduced since the main branch, and report every breaking contract change since it:

```bash
litespec model check --root ../consumer --since main
```

Check the model and record the proof for the merge:

```bash
litespec model check --root ../consumer --record-proof
```

## Underneath

The model holds every record with its identifier, its folder path, and its resolved links, and names the code, screens, tests, and test runs it read. On top of the written records it adds the computed `implements`, `calls`, and `proves` links and each record's computed `coded`, `verified`, `stale`, and `reachable` state. Computed states are never written on a record; the book shows them. The model also lists each unmarked Domain part in `parts`, under the aggregate whose folder holds it, or under `kernel`.

Each screen lists its `operations`, its `useCases`, and its `calls`, with each call pairing one operation with the use case it reaches. The `calls` links carry one link per pair: a use case reached through two operations gets two links, and a use case whose operation is unknown carries an empty `operation`.

A proof is fresh when its run commit is HEAD, or when every path changed between the run commit and the working tree, committed and uncommitted, is a record or documentation path: under the project's LiteSpec folder (`paths.docs`, default `.litespec/`) or a Markdown file. Otherwise the proof is `stale` and reads `last passed at <short commit>, <N> commits ago`. The committed `.litespec/proof/proofs.json` holds, for each scenario, the run commit and the outcome of the last passing run. Only `check --record-proof` writes it; a check without the flag reads it. The command computes the changed paths once per run with git (`git diff --name-only <commit>` plus untracked files), cached per distinct run commit. When git is unavailable or the run commit is unknown to the repository, the proof is `stale` with the reason. A test that both passed and failed within the runs read reports `proof-invalid`. Run the model check with `--record-proof` after the test run, in the same job, so the run on the pull request head is recorded before the pull request merges.

The .NET stack reader links each code element to its record through its mark, in the style `marks` declares: a handler, an event handler, a job host, a port, an adapter, an endpoint, an aggregate root and its state-changing methods, a domain event, a refusal type, and a test. The link is always the mark, never a folder or a type name. A marked refusal type declares the `code` of the refusal result its rule names.

Every check has a level. G reads records only and always blocks. S reads code through a stack reader, the reader a stack selects to read its code, screens or tests: it blocks where the reader that produced the finding is qualified, and reports otherwise. R reads prose quality a person weighs: `litespec model review` reports it, and it never blocks.

Qualification is per reader: a reader is qualified when it ran and its handshake matched. The golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, including Blazor and Android screens, stays qualified, and a finding no reader produced, a record finding, is qualified.

A structural parent is any link or containment the Parents table and the Identifiers table on the [model page](../core/model.md) name as a record's parent; a missing structural parent is level G, and so is every other required link.

Every finding code also states the record layer it governs, in the `layer` column, and whether it ties that layer to the records, in the `trace` column. A code runs when its layer is `any` or a layer the project claims. A code in the Implementation or Proof layer also needs its trace to be `yes`, or the project to claim that layer with `litespec`.

So a check that reads records alone carries the layer `any` or `behavior` and runs for every project, while a check that reads code, screens, tests, test runs or measure results through a stack reader carries the layer `implementation` or `proof`, with a trace of `yes` when it ties the code or the tests to the records. The checks of LiteSpec's code layout, stacks, analyzers and operations rules carry the layer `implementation` with a trace of `no`, so they run only when the project claims that layer with `litespec`. The [scope page](../core/scope.md#layer-claims) states which claims select which rows. The `extension` column names the extension that adds a code, and the `delivery` extension adds the alert and runbook codes.

A web frontend is read by a TypeScript reader that ships with the tool as one bundled script. Once per command run that builds the model, the tool runs it with Node, sends one versioned JSON request on standard input, and reads one versioned JSON facts document back, so a consumer installs nothing beyond Node itself. The reader opens with a handshake naming the protocol version, the reader, and the TypeScript and Svelte versions. A version mismatch, an absent Node, or a reader failure produces the single `reader-missing` finding for that frontend instead of passing silently.

A finding records the reader that produced its facts, and its level takes that reader's qualification: a reader qualifies when its handshake matches, and the golden corpus is a test contract for the reader bundle that does not gate a consumer run. The .NET (Roslyn) reader, which reads Blazor and Android frontends, stays qualified, and a finding no reader produced, a record finding, is qualified.

The builder reads `paths`, `shape`, and `stacks` from the consumer's `.litespec/project.yaml`, shared provisions from the release the tool carries, and the test run from TRX or JUnit XML files under `paths.testResults`.

A JUnit `testcase` proves a Behavior scenario directly by carrying the scenario id in a `litespec.proves` property, one property per scenario or several ids separated by commas in one value: `<properties><property name="litespec.proves" value="scenario/borrow-copy.borrowing-an-available-copy-succeeds" /></properties>`. The case proves each id it names with the same rules as a `proves` mark on a test method, so a runner whose steps carry no mark, or a stack with no code reader, still proves its scenarios.

A check `--since` base is read through an archive (`git archive`) into a temporary folder, which is removed after the check. The command checks that tree and the working tree and compares the two finding sets. Nothing beneath it is separately runnable.

A search or list line holds the identifier, the title and the file path relative to the consumer root, as `<id> - <title> (<path>)`, for written records only.

A finding belongs to a module when its subject record sits under that module's folder, else when its file does. A finding on no record and no module file belongs to no module, so a module check omits it. A finding belongs to the named `--only-paths` paths when its file is one of them or sits under one of them, or when its subject record's file does. A finding on no record and no named file belongs to no named path, so a scoped check omits it.

Edges read the written record links the model resolves: a link from a record under one module folder to a record under another counts once under its field, and an effect entry counts under its verb. A link whose target names no record is skipped. Kind lines order by kind, module splits by module, modules by name, and mutual pairs by name.
