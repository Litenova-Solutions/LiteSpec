# Litespec Maintain

## Audience

You maintain the `litespec-maintain` command line in `apps/maintain/src/LiteSpec.Maintain/`. You add an authoring check, change a validator message, or change how the provision index builds. After reading this page you know which file owns the behavior, how to change it, and how to cover the change with a fixture.

## What It Is

The maintainer tool is a .NET 10 console tool named `litespec-maintain`. It checks the LiteSpec repository against its own authoring contract and writes the generated provision index. It runs in this repository only and is never packed for consumers. The project file is `apps/maintain/src/LiteSpec.Maintain/LiteSpec.Maintain.csproj`. It sets `OutputType` to `Exe`, `AssemblyName` to `litespec-maintain`, and `IsPackable` to `false`. The tool references the shared library in `apps/model/src/LiteSpec.Shared/` for option parsing, JSON, the release locator, the page reader, the provision index builder, the stack layout, the prose helpers, the schema evaluator, and the pattern page parser.

The user-facing contract of each command lives in `docs/maintain/`. Read those pages before you change behavior:

- [Validate Standards](../../docs/maintain/validate.md) and [Generate Provisions](../../docs/maintain/provisions.md) for the two commands.
- [Maintain overview](../../docs/maintain/README.md) for how to run the tool.

## Quick Start

Run every command in this section from the repository root.

Build the solution:

```bash
dotnet build LiteSpec.slnx
```

Check the authoring contract of this repository:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
```

Rebuild the provision index after a provision change:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions
```

Run the maintainer test suite with the other suites:

```bash
dotnet test --solution LiteSpec.slnx
```

Print the help of one command, for example validate:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate --help
```

## Layout

The entry file sits at the top of `apps/maintain/src/LiteSpec.Maintain/`. Each command lives in `Authoring/` with its validator, its helpers, and its usage text.

| Path | What it owns |
|:---|:---|
| `Program.cs` | Command dispatch and the top-level usage text. It sets UTF-8 console output, handles `--help` and `--version`, and routes `validate` and `provisions` to their `Run` methods. Unknown commands print usage to standard error and exit 2. |
| `Authoring/` | The `validate` and `provisions` commands. `StandardsValidator.cs` holds the usage text, the full `StableDiagnosticCodes` list, and `Run`. `StandardsValidator.Pages.cs`, `.Provisions.cs`, `.Manifest.cs`, and `.Repository.cs` hold the page, provision, manifest, and repository checks. `ProvisionsCommand.cs` writes or checks the index through the shared `Provisions` builder. |

The solution file `LiteSpec.slnx` lists the maintainer tool and its tests beside the CLI, the analyzers, and the model library. `Directory.Build.props` pins the target framework to `net10.0` with nullable reference types and implicit usings, and stops MSBuild files in parent folders from applying inside this repository. `Directory.Packages.props` pins `Markdig` for the shared page reader and `xunit.v3` for the tests. `global.json` selects `Microsoft.Testing.Platform` as the test runner and pins no SDK.

## Dispatch and Option Parsing

`Program.cs` matches `args[0]` in a switch expression. `--help`, `-h`, and `help` print the top-level usage and exit 0. `--version` prints `LiteSpecHome.Version`, which is the `version` field of `litespec.manifest.json`, and exits 0. Each command name calls that command's static `Run` with the remaining arguments. Anything else prints `Unknown command '<name>'.` plus the usage to standard error and exits 2.

Each command parses its own tail with the shared `CliArgs.Parse`, declaring the flag names and the value-option names it accepts. Names include their dashes, such as `--check` or `--root`. An option accepts `--name value` and `--name=value`. A flag stands alone. `CliArgs` treats `--help` and `-h` as help wherever they appear. Anything the command does not declare becomes a usage error such as `Unknown option '--foo'.`, and an option with no value becomes `Option --root needs a value.`. A positional beyond `maxPositionals` becomes `Unexpected argument '<value>'.`. Every `Run` prints its own `Usage` constant after a usage error and exits 2.

The `--help` text of each command is the contract quoted in `docs/maintain/`. Quote it when you document behavior, and change the `Usage` constant and the docs page together when you change options. Exit codes are stable: 0 for success, 1 for a finding or a stale index, and 2 for a usage error or an unreadable input.

## Findings and Diagnostic Codes

Authoring diagnostics carry one reporting shape: a stable code, a one-sentence message, a file path, and a 1-based line. Human output prints one line per finding to standard error. JSON output prints one object to standard output so a caller never parses the human lines back apart.

`StandardsValidator.StableDiagnosticCodes` lists every code `validate` may emit, in declaration order, from `ANCHOR_BROKEN` to `LAYER_UNLISTED`. `Validate` throws `InvalidOperationException` when a check emits a code the list does not declare, so an undeclared code fails fast in tests instead of shipping as a typo. `WarningDiagnosticCodes` is the burn-down list for rules that warn without failing the build; it is empty today, and `Run` splits diagnostics into errors and warnings against it. Human output writes `Relative:Line [CODE] Message` per error, then a warning count section with `Run with --warnings to list every occurrence.` JSON output writes `tool`, `root`, `ok`, an `errors` array of `file`, `line`, `code`, and `message` objects, and a `warnings` array of the same shape. A passing human run prints `LiteSpec authoring checks passed.` A failing run appends `LiteSpec authoring checks failed with N error(s).` after the lines.

## Release and Repository Roots

`LiteSpecHome` finds the release from the tool binary, not from the working directory. `LITESPEC_HOME` overrides the search; when set, the folder must hold `litespec.manifest.json`, else `Root` is null. Without the variable, `Locate` walks up from `AppContext.BaseDirectory` for the manifest, which reaches the repository root in a repository checkout. `RequireRoot` throws `No LiteSpec release was found above '<dir>'. Set LITESPEC_HOME to the folder that holds litespec.manifest.json.` when no release resolves, and each command converts that into exit 2 with its usage text. `Version` reads the `version` field for `--version`. `PathTo` joins a forward-slash relative path such as `docs/reference/provisions.md` onto the release root. The tests set `LITESPEC_HOME` to the repository root in `Support/Cli.cs`, so every test runs against this repository.

`validate` and `provisions` read the LiteSpec repository at `--root`, which defaults to the `LiteSpecHome` release root. `validate` refuses a root that is not a directory or is unreadable with `Repository root is not a directory: <path>` or `Repository root is unreadable: <path>: <reason>`. `provisions --check` refuses a root without a `docs` folder with `Root '<path>' holds no docs folder.`

## Test Harness and Fixtures

The tests live in `apps/maintain/tests/LiteSpec.Maintain.Tests/`. The test project targets the maintainer binary as a project reference, uses `xunit.v3`, and holds no fixtures folder: every case builds its repository in a temp folder. `Support/Cli.cs` runs the built `litespec-maintain.dll` with `dotnet` as its own process, the way a maintainer runs the tool, and asserts the exit code with both streams. It sets `LITESPEC_HOME` to the repository root, finds the root by walking up for `litespec.manifest.json`, and offers `Temp` for a fresh folder that is deleted on dispose unless `KEEP_FIXTURE` is set. `Support/InputErrorTests.cs` proves that ordinary user error reaches a message and a documented exit code, never an unhandled exception: a malformed percent escape in a link and a `provisions` root without docs.

Authoring tests start from `Authoring/StandardsFixture.cs`. `Base` writes a minimal repository into a temp folder: a topic page, an extension page, a stack page, a pattern page, a guide page, a tutorial page, a reference page, a command page, a glossary, a docs index with routing, and a rebuilt provision index. Each test mutates one file and runs `validate` or `provisions`. The fixture carries checkable evidence that names `litespec-maintain validate`, so the tree needs no tools stub.

To add a fixture repository for a new case, follow the harness the area already uses. For a new authoring rule, extend `StandardsFixture.Base` only when the rule needs a new page shape; otherwise mutate one page from `Base` inside the test.

## Common Changes

### Add an Authoring Diagnostic Code End to End

1. Add the code to `StableDiagnosticCodes` in `Authoring/StandardsValidator.cs`, in declaration order.
2. Emit it through the `Add` callback inside `Validate` with the repository-relative file, the 1-based line, the code, and a message that names the file and the fix. Never construct a `StandardsDiagnostic` with a code the list does not hold.
3. Decide the severity: codes in `WarningDiagnosticCodes` print under a warning count and exit 0; every other code exits 1. The list is empty today, so a new rule fails the build unless you add it there deliberately.
4. Add a test under `Authoring/` that starts from `StandardsFixture.Base`, mutates one file, and asserts the `file:line [CODE] message` line and the exit code. Add the mirror test that the clean tree passes.
5. Quote the code on the affected `docs/maintain/validate.md` behavior or the relevant standard page when the rule restates visible behavior.

### Change a Message

1. Edit the message at its emission site, not in the test. Keep it to one sentence, name the file or record and the action the reader takes next.
2. Update every test that asserts the old text, including `Support/InputErrorTests.cs` when the message covers user error.
3. Check that JSON output needs no change: messages stay inside `message` fields, and codes, paths, and field names stay stable. Rename a code only as a new code with a new overlay, never by editing the old string in place.

## Packing

The tool sets `IsPackable` to `false`, so packing the solution produces no maintainer package. The commands serve this repository only: they read the checkout's own pages, templates, and manifest, which a consumer never holds.

## Limits

The tool reads text and JSON and writes text and JSON. It runs no server, migrates no data, and publishes no package on your behalf. `validate` reads the working tree only; it evaluates current standards material and skips `apps/`, `packages/`, `CHANGELOG.md`, and `LICENSE.md`. Generated regions belong to their owning tool: authoring checks skip the text between `<!-- generated:<name> -->` fences.
