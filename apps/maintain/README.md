# LiteSpec Maintain

The `litespec-maintain` command line checks the LiteSpec repository against its own authoring contract and writes the generated provision index.

## Audience

You maintain the `litespec-maintain` command line in `apps/maintain/src/LiteSpec.Maintain/`. You add an authoring check, change a validator message, or change how the provision index builds. After reading this page, you know which file owns the behavior, how to change it, and how to cover the change with a fixture.

## What It Is

The maintainer tool is a .NET 10 console tool named `litespec-maintain`. It checks the LiteSpec repository against its own authoring contract and writes the generated provision index. It runs in this repository only and never ships to consumers.

The project file is `apps/maintain/src/LiteSpec.Maintain/LiteSpec.Maintain.csproj`. It sets `OutputType` to `Exe`, `AssemblyName` to `litespec-maintain`, and `IsPackable` to `false`. The tool references `apps/model/src/LiteSpec.CommandLine/` for option parsing and JSON, and `apps/model/src/LiteSpec.Standards/` for the release locator, the page reader, the provision index builder, the stack selection, the prose helpers, the schema evaluator, and the pattern page parser.

Each command's user-facing contract lives in `docs/maintain/`. Read those pages before you change behavior:

- [Validate Standards](../../docs/maintain/validate.md), [Generate Provisions](../../docs/maintain/provisions.md), and [Generate Provision Changelog](../../docs/maintain/changelog.md) for the three commands.
- [Maintain overview](../../docs/maintain/README.md) for running the tool.

## Quick Start

Run every command in this section from the repository root.

Build the solution:

```bash
dotnet build LiteSpec.slnx
```

Check this repository's authoring contract:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
```

Rebuild the provision index after changing a provision:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions
```

Write the provision changes since a git ref into the changelog, as the last step before tagging a release:

```bash
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- changelog --from v3.0.0
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

The entry file sits at the top of `apps/maintain/src/LiteSpec.Maintain/`. Each command owns a folder holding its validators, its helpers, and its usage text.

| Path | What it owns |
|:---|:---|
| `Program.cs` | Command dispatch and the top-level usage text. It sets UTF-8 console output, handles `--help` and `--version`, and routes `validate`, `provisions`, and `changelog` to their `Run` methods. Unknown commands print usage to standard error and exit 2. |
| `Validate/` | The `validate` command. `StandardsValidator.cs` holds the usage text and `Run`, and `MaintainerDiagnostics.cs` the full list of stable diagnostic codes. `Pages/` holds the page, provision, and value-style checks (`PageValidator.cs`, `ProvisionValidator.cs`, `ValueStyleValidator.cs`) and the parsed page they share. `Repository/` holds the repository-wide, manifest, marks, records, and field checks (`RepositoryValidator.cs`, `ManifestValidator.cs`, `MarksValidator.cs`, `RecordsValidator.cs`, `FieldValidator.cs`). `RepositoryWalk.cs` keeps the directory walk, which skips folders the checks never read, and `PageText.cs` the line, citation, and non-ASCII readers. File text and relative paths come from the `FileText` and `PathText` in `LiteSpec.Standards`; the manifest is read through `LiteSpecHome.ReadManifest`, and YAML files through `RecordText`. |
| `ProvisionIndex/` | The `provisions` command. `ProvisionsCommand.cs` writes or checks the index through the shared `Provisions` builder. |
| `Changelog/` | The `changelog` command. `ChangelogCommand.cs` writes or checks the changelog section through `ProvisionDiff`, which compares the index and the manifest at a git ref with the current ones. |

The solution file `LiteSpec.slnx` lists the maintainer tool and its tests beside the CLI, the analyzers, and the model library. `Directory.Build.props` pins the target framework to `net10.0` with nullable reference types and implicit usings, and stops MSBuild files in parent folders from applying inside this repository. `Directory.Packages.props` pins `Markdig` for the shared page reader and `xunit.v3` for the tests. `global.json` selects `Microsoft.Testing.Platform` as the test runner, and pins the SDK to `10.0.114`.

### Dispatch and Option Parsing

`Program.cs` matches `args[0]` in a switch expression. `--help`, `-h`, and `help` print the top-level usage and exit 0. `--version` prints `LiteSpecHome.Version`, which is the `version` field of `litespec.manifest.yaml`, and exits 0. Each command name hands the remaining arguments to that command's static `Run`. Anything else prints `Unknown command '<name>'.` plus the usage to standard error and exits 2.

Each command parses its own tail with the shared `CliArgs.Parse`, declaring the flag names and value-option names it accepts. Names carry their dashes, such as `--check` or `--root`. An option takes either `--name value` or `--name=value`, and a flag stands alone. `CliArgs` treats `--help` and `-h` as help wherever they appear. Anything the command did not declare becomes a usage error such as `Unknown option '--foo'.`, and an option without a value becomes `Option --root needs a value.`. A positional past `maxPositionals` becomes `Unexpected argument '<value>'.`. Every `Run` prints its own `Usage` constant after a usage error and exits 2.

Each command's `--help` text is the contract quoted in `docs/maintain/`. Quote it when you document behavior, and change the `Usage` constant together with the docs page when you change options. Exit codes are stable: 0 means success, 1 means an error finding or a stale index, and 2 means a usage error or an unreadable input.

### Findings and Diagnostic Codes

Authoring diagnostics share one reporting shape: a stable code, a one-sentence message, a file path, and a 1-based line. Human output prints one line per finding to standard error. JSON output prints one object to standard output, so no caller parses the human lines back apart.

`StandardsValidator.StableDiagnosticCodes` lists every code `validate` may emit, in declaration order, from `ANCHOR_BROKEN` to `STANDARD_STRENGTH_MODAL`. `Validate` throws `InvalidOperationException` when a check emits a code the list does not declare. An undeclared code then fails fast in tests instead of shipping as a typo. A diagnostic is an error or a warning: `MaintainerDiagnostics.IsWarning` reports the sentence, modal, casing, and wording codes as warnings, and every other code as an error. Human output writes `Relative:Line [CODE] Message` per error and appends ` (warning)` per warning. JSON output writes `tool`, `root`, `ok`, an `errors` array, and a `warnings` array of `file`, `line`, `code`, and `message` objects. A clean human run prints `LiteSpec authoring checks passed.` A run with only warnings prints `LiteSpec authoring checks passed with N warning(s).` and exits zero. A failing run appends `LiteSpec authoring checks failed with N error(s).` after the lines, with ` and M warning(s)` when warnings are present.

### Release and Repository Roots

`LiteSpecHome` locates the release from the tool binary rather than from the working directory. `LITESPEC_HOME` overrides the search. When set, the folder must hold `litespec.manifest.yaml`, else `Root` is null. Without the variable, `Locate` walks up from `AppContext.BaseDirectory` looking for the manifest, which reaches the repository root in a repository checkout. `RequireRoot` throws `No LiteSpec release was found above '<dir>'. Set LITESPEC_HOME to the folder that holds litespec.manifest.yaml.` when no release resolves, and each command turns that into exit 2 with its usage text. `Version` reads the `version` field for `--version`. `PathTo` joins a forward-slash relative path such as `docs/reference/provisions.md` onto the release root. The tests set `LITESPEC_HOME` to the repository root in `Support/Cli.cs`, so every test runs against this repository.

The `validate`, `provisions`, and `changelog` commands read the LiteSpec repository at `--root`, which defaults to the `LiteSpecHome` release root. `validate` rejects a root that is not a directory or is unreadable with `Repository root is not a directory: <path>` or `Repository root is unreadable: <path>: <reason>`. `provisions` and `changelog` reject a root without a `docs` folder with `Root '<path>' holds no docs folder.` The `changelog` command also needs `--from`, the git tag or commit to compare with, and reads the old index and manifest with `git show`.

### Test Harness and Fixtures

The tests live in `apps/maintain/tests/LiteSpec.Maintain.Tests/`. The test project references the maintainer binary as a project, uses `xunit.v3`, and keeps no fixtures folder: every case builds its repository in a temp folder. `Support/Cli.cs` runs the built `litespec-maintain.dll` with `dotnet` as its own process, the way a maintainer runs the tool, and asserts the exit code with both streams. It sets `LITESPEC_HOME` to the repository root, finds the root by walking up for `litespec.manifest.yaml`, and offers `Temp` for a fresh folder that is deleted on dispose unless `KEEP_FIXTURE` is set. `Support/InputErrorTests.cs` proves that ordinary user error produces a message with a documented exit code instead of an unhandled exception: a malformed percent escape in a link and a `provisions` root without docs.

Authoring tests build on `Authoring/StandardsFixture.cs`. `Base` writes a minimal repository into a temp folder: a standard page, an extension page, a stack page, a pattern page, a guide page, a tutorial page, a reference page, a command page, a glossary, a docs index with routing, and a rebuilt provision index. Each test mutates one file and runs `validate` or `provisions`. The fixture carries checkable evidence naming `litespec-maintain validate`, so the tree needs no tools stub.

To add a fixture repository for a new case, follow the harness its area already uses. For a new authoring rule, extend `StandardsFixture.Base` only when the rule needs a new page shape. Otherwise mutate one page from `Base` inside the test. `Authoring/ProvisionDiffTests.cs` covers the comparison, the rendering, and the section rewrite in process, plus the command end to end in a temporary git repository.

### Common Changes

#### Add an Authoring Diagnostic Code End to End

1. Add the code to `MaintainerDiagnostics.Codes` and `MaintainerDiagnostics.All` in `Validate/MaintainerDiagnostics.cs`, in declaration order. `StandardsValidator.StableDiagnosticCodes` exposes that list. A sentence, modal, casing, or wording finding also joins `MaintainerDiagnostics.Warnings`, so it prints as a warning instead of failing the run.
2. Emit it through the `Add` callback inside `Validate` with the repository-relative file, the 1-based line, the code, and a message naming the file and the fix. Never construct a `StandardsDiagnostic` with a code the list does not hold.
3. Add a test under the tests' `Authoring/` folder that starts from `StandardsFixture.Base`, mutates one file, and asserts the `file:line [CODE] message` line and the exit code. Add the mirror test showing the clean tree passes.
4. Quote the code in the affected behavior section of `docs/maintain/validate.md`, or on the relevant standard page when the rule restates visible behavior.

#### Change a Message

1. Edit the message where it is emitted, not in the test. Keep it to one sentence naming the file or record and the reader's next action.
2. Update every test asserting the old text, including `Support/InputErrorTests.cs` when the message covers user error.
3. Confirm the JSON output needs no change: messages stay inside `message` fields, while codes, paths, and field names stay stable. Rename a code only by adding a new code with a new overlay, never by editing the old string in place.

### Packing

The tool sets `IsPackable` to `false`, so packing the solution produces no maintainer package. The commands serve this repository only: they read the checkout's own pages, templates, and manifest, none of which a consumer holds.

## Limits

The tool reads text, YAML and JSON, and writes text and JSON. It runs no server, migrates no data, and publishes no package for you. `validate` reads the working tree only. It evaluates current standards material and skips `apps/`, `packages/`, `CHANGELOG.md`, and `LICENSE`. Generated regions belong to their owning tool: authoring checks skip the text between `<!-- generated:<name> -->` fences.
