# Litespec CLI

## Audience

You maintain the `litespec` command line in `apps/cli/src/LiteSpec.Cli/`. You add a command, add a model finding, or change a validator message. After reading this page you know which file owns the behavior, how to change it, and how to cover the change with a fixture. The commands that maintain the standard itself live in the [maintainer tool](../maintain/README.md).

## What It Is

The CLI is a .NET 10 console tool named `litespec`. It scaffolds and validates consumer repositories, builds and checks the consumer project model, and generates the project book. The project file is `apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj`. It sets `OutputType` to `Exe`, `AssemblyName` to `litespec`, `PackAsTool` to `true`, and `ToolCommandName` to `litespec`. The package id is `LiteSpec.Cli`. The tool references `Microsoft.CodeAnalysis.CSharp` for the code readers, the shared library in `apps/model/src/LiteSpec.Shared/` for option parsing, the page reader, the provision index, and the pattern page parser, and embeds `Book/Assets/**` as `LiteSpec.Book.Assets.*` for the generated book site.

The user-facing contract of each command lives in `docs/tools/`. Read those pages before you change behavior:

- [Initialize Consumer](../../docs/tools/init.md), [Validate Consumer](../../docs/tools/consumer.md), [Validate Controlled UI](../../docs/tools/ui.md), [Model Builder](../../docs/tools/model.md), and [Book](../../docs/tools/book.md) for the consumer commands.
- [CLI overview](../../docs/tools/README.md) for how to run the CLI from this repository and from a consumer, and for the `LITESPEC_HOME` and `LITESPEC_TIMING` environment variables.

## Quick Start

Run every command in this section from the repository root.

Build the solution:

```bash
dotnet build LiteSpec.slnx
```

Check the authoring contract of this repository with the maintainer tool:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
```

Run the CLI test suite with the analyzer tests:

```bash
dotnet test --solution LiteSpec.slnx
```

Print the help of one command, for example the model command:

```bash
dotnet run --project apps/cli/src/LiteSpec.Cli -- model --help
```

Check the small model fixture directly, without copying it first:

```bash
dotnet run --project apps/cli/src/LiteSpec.Cli -- model check --root apps/cli/tests/LiteSpec.Cli.Tests/Fixtures/Model/small
```

## Layout

The entry file sits at the top of `apps/cli/src/LiteSpec.Cli/`. Each command lives in its own folder with its validator, its helpers, and its usage text. The code both command lines share lives in `apps/model/src/LiteSpec.Shared/`: `CliArgs.cs` parses `--name value` and `--name=value` options, flags, and positionals and reports the first usage error; `CliJson.cs` serializes with two-space indentation, a `\n` line end on every operating system, and no escaping of quotes, apostrophes, or angle brackets; `LiteSpecHome.cs` locates the LiteSpec release the tool carries; `Authoring/` holds the page reader, the provision index builder, the stack layout, and the prose helpers; `Patterns/` holds the pattern page and wireframe parsers; and `Schemas/` holds the JSON Schema evaluator every validator uses.

| Path | What it owns |
|:---|:---|
| `Program.cs` | Command dispatch and the top-level usage text. It sets UTF-8 console output, handles `--help` and `--version`, and routes `init`, `new`, `rename`, `consumer`, `ui`, `model`, and `book` to their `Run` methods. Unknown commands print usage to standard error and exit 2. |
| `Authoring/` | The `new` and `rename` commands. `NewCommand.cs` scaffolds a record page and reserves its identifier, `AppendEntry.cs` appends result and scenario entries, `RenameCommand.cs` renames an identifier and rewrites every link, and `RecordPages.cs` owns the page homes and the shared identifier checks. |
| `Consumer/` | The `consumer` command. `ConsumerValidator.cs` holds the usage text, the `Run` entry point, and the `RunState` carrier with its text cache, findings, and report writer. `ConsumerValidator.Project.cs` checks the project record and manifest version, `.Glob.cs` expands path globs, `.Pages.cs` checks record pages, and `.Language.cs` checks the language record. |
| `Ui/` | The `ui` command and the in-process check the consumer validator calls. `UiValidator.cs` holds the usage text, `Run`, and `Validate`. `UiValidator.Package.cs` checks the shared package configuration and source lock, `.Json.cs` evaluates the design contract and catalog JSON, `.Styles.cs` checks token and shared stylesheets, `.Classes.cs` checks class strings and imports, `.Catalog.cs` checks the pattern catalog, and `.Scan.cs` walks the consumer files. |
| `Model/` | The `model build`, `check`, `render`, and `query` subcommands. `ModelCommand.cs` parses the subcommand and the `--root`, `--out`, and `--format` options. `ConsumerConfig.cs` loads `litespec.project.json` into record roots, code roots, frontends, and test roots. `ModelBuilder.cs` builds `ProjectModel.cs`, `Checker.cs` runs every check, `Finding.cs` carries one finding, `Renderer.cs` rewrites generated regions, `Querier.cs` prints one record, and `Readers/` reads Dotnet, TypeScript, and JUnit inputs. |
| `Book/` | The `book` command. `BookCommand.cs` parses `--model` and `--out` and writes each page. `BookModel.cs` builds the index and the review data, `BookPages*.cs` render the page bodies, and `BookRender.cs` shells pages and writes `search.json`. `Assets/book.css` and `Assets/book.js` ship inside the binary. |
| `Init/` | The `init` command. `InitCommand.cs` reads the preset from `templates/presets/`, merges it over `templates/consumer/litespec.project.json`, stamps the release version, fills the project name, and writes `litespec.project.json` without overwriting an existing file. |

The solution file `LiteSpec.slnx` lists the CLI, the CLI tests, the maintainer tool, the shared library, the analyzers, and their tests. `Directory.Build.props` pins the target framework to `net10.0` with nullable reference types and implicit usings, and stops MSBuild files in parent folders from applying inside this repository. `Directory.Packages.props` pins `Microsoft.CodeAnalysis.CSharp`, `Markdig`, and `xunit.v3`. `global.json` selects `Microsoft.Testing.Platform` as the test runner and pins no SDK.

## Dispatch and Option Parsing

`Program.cs` matches `args[0]` in a switch expression. `--help`, `-h`, and `help` print the top-level usage and exit 0. `--version` prints `LiteSpecHome.Version`, which is the `version` field of `litespec.manifest.json`, and exits 0. Each command name calls that command's static `Run` with the remaining arguments. Anything else prints `Unknown command '<name>'.` plus the usage to standard error and exits 2.

Each command parses its own tail with the shared `CliArgs.Parse`, declaring the flag names and the value-option names it accepts. Names include their dashes, such as `--check` or `--root`. An option accepts `--name value` and `--name=value`. A flag stands alone. `CliArgs` treats `--help` and `-h` as help wherever they appear. Anything the command does not declare becomes a usage error such as `Unknown option '--foo'.`, and an option with no value becomes `Option --root needs a value.`. A positional beyond `maxPositionals` becomes `Unexpected argument '<value>'.`. Every `Run` prints its own `Usage` constant after a usage error and exits 2.

Two helpers apply across commands. `Json` is true when `--format json` or `--format=json` was passed, and `FormatError` reports any other `--format` value with `Unknown format '<value>'; use json or text.`. `Root` returns the full path of `--root` or the working directory. `model` and `book` parse their subcommand or their `--model` and `--out` options by hand and then delegate the shared flags to `CliArgs`.

The `--help` text of each command is the contract quoted in `docs/tools/`. Quote it when you document behavior, and change the `Usage` constant and the docs page together when you change options. Exit codes are stable: 0 for success, 1 for a finding or a failed check, and 2 for a usage error or an unreadable input. `init` exits 1 when the release's template, preset, or scaffold file is unreadable, and 2 when the project file exists or an option is invalid. `book` exits 1 for unreadable input or unwritable output. `model query` exits 1 when no record holds the id.

## Findings and Diagnostic Codes

Model findings carry one reporting shape, shared with the authoring diagnostics the maintainer tool emits: a stable code, a one-sentence message, a file path, and a 1-based line. Human output prints one line per finding to standard error. JSON output prints one object to standard output so a caller never parses the human lines back apart.

`Checker.Codes` in `Model/Checker.cs` lists every model finding code, from `record-shape` to `link-unresolved`. The full table of codes and meanings lives on the [Model Builder](../../docs/tools/model.md) page. `Finding` carries `Code`, `Message`, `Path` as an absolute path, `Line`, and `Subject` as the record id the finding is about, or null. `Check` sorts findings by path, line, code, and message so parallel checks print in the same order on every run, then `Suppress` partitions open findings from known ones. A `known-gap` record covers a finding when its `check` equals the finding code, its `subject` lists the finding subject, its status is not `retired`, and its end date has not passed. `model check` prints `path:line [code] message` per open finding and `path:line [known code] message` per covered one, then `N findings, M known.` JSON output writes `findings` and `known` arrays of `code`, `message`, `path`, `line`, and `subject` objects. Covered findings exit 0. Open findings and expired gaps exit 1.

`consumer` and `ui` report problems as plain strings rather than coded findings: `Consumer: <root>`, `Files scanned: N under <docs>, frontmatter pages: M`, the language summary, an `ADVISORY` section, and a `FAIL (N problem(s)):` section, or `PASS: project configuration, page frontmatter and glossary are valid.` JSON output writes `tool`, `consumer`, `ok`, a `scanned` object, a `problems` array, and an `advisories` array.

## Release and Consumer Roots

Two roots meet in most commands: the LiteSpec release the tool carries, and the repository under check.

`LiteSpecHome` finds the release from the tool binary, not from the working directory, so the tool resolves the release it carries beside its binary at any working directory. `LITESPEC_HOME` overrides the search; when set, the folder must hold `litespec.manifest.json`, else `Root` is null. Without the variable, `Locate` walks up from `AppContext.BaseDirectory` for the manifest, which reaches the repository root in a repository checkout. `RequireRoot` throws `No LiteSpec release was found above '<dir>'. Set LITESPEC_HOME to the folder that holds litespec.manifest.json.` when no release resolves, and each command converts that into exit 2 with its usage text. `Version` reads the `version` field for `--version` and for the `reviewedLiteSpecVersion` stamp that `init` writes. `PathTo` joins a forward-slash relative path such as `docs/reference/provisions.md` onto the release root. The tests set `LITESPEC_HOME` to the repository root in `Support/Cli.cs`, so every test runs against this repository.

`consumer`, `ui`, and `model` read the consumer repository at `--root`, which defaults to the working directory. Each one refuses a root without `litespec.project.json`: `consumer` writes `No litespec.project.json at <root>` and exits 2, `model` writes `Root '<root>' holds no litespec.project.json.` and exits 2, and `ui` writes `No litespec.project.json at <root>` and exits 2. `model` then loads `ConsumerConfig` from that file: record roots, language path, application, domain, web API, and infrastructure roots, test roots, frontends, API client sources, test results directory, size, shape, and stacks. The builder reads shared provisions from the release docs the tool carries and the test run from TRX or JUnit XML files under `paths.testResults`. `book` reads neither root: it takes `--model <model.json>` and `--out <folder>`, and reads the findings `model build` records in the model. Set `LITESPEC_TIMING` to any value to print `model` phase timings such as `[time] check-schema 0.1s` on standard error; normal runs print nothing besides their findings.

## Test Harness and Fixtures

The tests live in `apps/cli/tests/LiteSpec.Cli.Tests/`. The test project targets the CLI binary as a project reference, uses `xunit.v3`, and excludes `Fixtures/**` from compilation so fixture files stay data. One folder covers each command: `Authoring/` for the `new` and `rename` tests, `Consumer/`, `Ui/`, `Model/`, `Book/`, and `Init/`. `Support/Cli.cs` runs the built `litespec.dll` with `dotnet` as its own process, the way a consumer runs the tool, and asserts the exit code with both streams. It sets `LITESPEC_HOME` to the repository root, finds the root by walking up for `litespec.manifest.json`, and offers `Fixture` for a path under `Fixtures/` and `Temp` for a fresh folder that is deleted on dispose unless `KEEP_FIXTURE` is set. `Support/InputErrorTests.cs` proves that ordinary user error reaches a message and a documented exit code, never an unhandled exception: a malformed project file, a missing project file, and an equals-form book option.

Model tests start from `Model/ModelFixture.cs` and `Fixtures/Model/`. `small/` is a library loans system at size small with one module, three business records, eight use cases, and one open known gap; seven use cases have handlers and the eighth is covered by the gap. `large/` holds the same records plus problems, features, journeys, one case per use case, constraints, research, limits, runbooks, and a frontend route that imports a generated client operation. Both fixtures exit 0 under `model check`. `Fixtures/Model/README.md` documents the contents, the finding codes, and the overlay mechanism. `broken/<code>/` holds one overlay per finding code with only the files that differ from `large/`; `Prepare` copies `large/` to a temp folder, copies the overlay over it, deletes the paths in `DELETES.txt` when present, and returns the folder. `ModelCheckTests.cs` runs one theory row per code and expects exit 1 with that code. `WriteKnownGap` adds a covering gap for the suppression tests. `junit/` and `trx/` hold sample test results the proof checks read.

Book tests read the frozen `Fixtures/Book/model.json`, embed the findings of `Fixtures/Book/findings.json` where a test needs them, and assert the generated pages.

To add a fixture repository for a new case, follow the harness the area already uses. For a new model finding, copy `large/` by hand into a new `broken/<code>/` overlay that holds only the differing files, add `DELETES.txt` with one relative path per line when the case deletes files, and add the code to the theory list in `ModelCheckTests.cs`. For a new authoring rule, follow the maintainer test harness in the [maintainer tool](../maintain/README.md). For a new book shape, extend the frozen model only when the shape needs a new record type, and keep the existing file byte-identical otherwise.

## Common Changes

### Add a Command

1. Add the usage text as a `Usage` constant on the new command class, with synopsis, options, and exit codes in the shape of the neighboring commands.
2. Add the dispatch arm in `Program.cs` and route unknown subcommands to exit 2 with the usage text.
3. Parse the tail with `CliArgs.Parse`, declaring every flag and value option. Print `Usage` on `--help` with exit 0 and on any parse error with exit 2.
4. Resolve roots through `LiteSpecHome` for the release and `CliArgs.Root` for the consumer. Convert a missing release into exit 2 with the usage text.
5. Write human lines to standard output or error and one JSON object to standard output under `--format json`, using `CliJson`.
6. Document the command on a new page under `docs/tools/` with the command page sections, and link it from `docs/tools/README.md`.

### Add a Model Finding

1. Add the code to `Checker.Codes` in `Model/Checker.cs`.
2. Append the finding with its absolute path, 1-based line, message, and subject record id to the `findings` list in the matching `Check*` method. Keep the message to one sentence that names the record and the fix.
3. Honor known gaps: findings with a null subject always stay open; findings with a subject are suppressible by a `known-gap` record whose `check` equals the code and whose `subject` lists the id. Add `known-gap-expired` when a gap passes its end date with the check still red, and `gap-unknown-check` when a gap names a code no check uses.
4. Add a `broken/<code>/` overlay under `Fixtures/Model/` with only the differing files, and add the code to `FindingCodeList` in `Model/ModelCheckTests.cs`. Add the open-gap and expired-gap rows when the finding is suppressible.
5. Document the code and its meaning in the finding table on the [Model Builder](../../docs/tools/model.md) page.

### Change a Message

1. Edit the message at its emission site, not in the test. Keep it to one sentence, name the file or record and the action the reader takes next.
2. Update every test that asserts the old text, including `Support/InputErrorTests.cs` when the message covers user error.
3. Check that JSON output needs no change: messages stay inside `message` fields, and codes, paths, and field names stay stable. Rename a code only as a new code with a new overlay, never by editing the old string in place.

## Packing

`LiteSpec.Cli.csproj` sets `PackAsTool` to `true` with `ToolCommandName` `litespec`, so packing produces the `LiteSpec.Cli` tool package described as `The LiteSpec command line: consumer and UI validation, the project model, and the project book.` `dotnet pack apps/cli/src/LiteSpec.Cli -c Release -o artifacts/packages` produces `LiteSpec.Cli.<version>.nupkg` with the release under `tools/net10.0/any/`, as the [CLI overview](../../docs/tools/README.md) shows. A consumer installs it with `dotnet tool install LiteSpec.Cli --version <version>`, adding `--add-source <folder>` for a local package folder. Keep `PackageId`, `ToolCommandName`, and `Description` stable: install scripts and the consumer docs name them.

## Limits

The CLI reads text and JSON and writes text, JSON, HTML, and SVG. It runs no server, migrates no data, and publishes no package on your behalf. `consumer` and `ui` validate the declared contract and scan no route, environment, naming, secret, storage, or HTML sink beyond the controlled UI boundary the usage text lists. `model` builds from the declared roots and the latest TRX or JUnit files; it does not execute tests. `book` renders the model it is given; it does not rebuild the model. Generated regions belong to their owning tool: `model render` rewrites only the text between `<!-- generated:<name> -->` fences for the region names in `Renderer.KnownRegions`, and authoring checks skip those regions.
