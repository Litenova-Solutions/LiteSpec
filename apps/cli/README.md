# Litespec CLI

## Audience

You maintain the `litespec` command line in `apps/cli/src/LiteSpec.Cli/`. You add a command, add a model finding, or change a validator message. After reading this page you know which file owns the behavior, how to change it, and how to cover the change with a fixture. The commands that maintain the standard itself live in the [maintainer tool](../maintain/README.md).

## What It Is

The CLI is a .NET 10 console tool named `litespec`. It scaffolds and validates consumer repositories, builds and checks the consumer project model, and generates the project book. The project file is `apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj`. It sets `OutputType` to `Exe`, `AssemblyName` to `litespec`, `PackAsTool` to `true`, and `ToolCommandName` to `litespec`. The package id is `LiteSpec.Cli`. The tool references `Microsoft.CodeAnalysis.CSharp` for the code readers, `apps/model/src/LiteSpec.CommandLine/` for option parsing and JSON output and `apps/model/src/LiteSpec.Standards/` for the page reader, the provision index, and the pattern page parser, and embeds `Book/Assets/book-shell.html` and the built book client as `LiteSpec.Book.Assets.*` for the generated book site.

The user-facing contract of each command lives in `docs/tools/`. Read those pages before you change behavior:

- [Initialize Consumer](../../docs/tools/init.md), [Scaffold Record](../../docs/tools/new.md), [Rename Record](../../docs/tools/rename.md), [Model Builder](../../docs/tools/model.md), [Book](../../docs/tools/book.md), and [Rules Query](../../docs/tools/rules.md) for the consumer commands.
- [CLI overview](../../docs/tools/README.md) for how to run the CLI from this repository and from a consumer, and for the `LITESPEC_HOME` environment variable.

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

Run the whole test suite. To run the CLI tests alone, pass their project: `dotnet test --project apps/cli/tests/LiteSpec.Cli.Tests`.

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

The entry file sits at the top of `apps/cli/src/LiteSpec.Cli/`. Each command lives in its own folder with its validator, its helpers, and its usage text. The code both command lines share lives in two libraries. `apps/model/src/LiteSpec.CommandLine/` holds how they talk on the command line: `CliArgs.cs` parses `--name value` and `--name=value` options, flags, and positionals and reports the first usage error; `CliJson.cs` serializes with two-space indentation, a `\n` line end on every operating system, and no escaping of quotes, apostrophes, or angle brackets. `apps/model/src/LiteSpec.Standards/` reads the standards material the release carries: `Release/LiteSpecHome.cs` locates the LiteSpec release the tool carries; `Pages/` holds the page reader, the stack selection, and the prose helpers; `Provisions/` holds the provision index builder and the scopes; `Patterns/` holds the pattern page and wireframe parsers; and `Schemas/` holds the JSON Schema evaluator every validator uses.

| Path | What it owns |
|:---|:---|
| `Program.cs` | Command dispatch and the top-level usage text. It sets UTF-8 console output, handles `--help` and `--version`, and routes `init`, `new`, `rename`, `model`, `book`, and `rules` to their `Run` methods. Unknown commands print usage to standard error and exit 2. |
| `RecordEditing/` | The `new` and `rename` commands, which edit record files. `NewCommand.cs` scaffolds a record page and reserves its identifier, allocating the next decision number for decisions, `AppendEntry.cs` appends result and scenario entries, `AppendListEntry.cs` appends list entries, `RenameCommand.cs` renames an identifier and rewrites every link, and `RecordPages.cs` owns the page homes and the shared identifier checks. |
| `Model/` | The `model check`, `format`, `render`, and `query` subcommands. `ModelCommand.cs` parses the subcommand and the shared options. `ModelBuilder.cs` builds `ProjectModel.cs`, `Checker.cs` runs every check, `GeneratedRegions.cs` rewrites generated regions, and `RecordQuery.cs` prints one record. The folders follow the build: `Configuration/` loads `.litespec/project.yaml` into record roots, code roots, frontends, and test roots; `Records/` reads and formats the record files; `Readers/` reads Dotnet, TypeScript, and JUnit inputs into the facts in `Code/`, `Tests/`, and `Screens/` (where `FrontendRecords.cs` writes the records file each web frontend imports); `Links/`, `Proof/`, and `CompletionStages/` compute the links, the proof statuses, and the completion stages; `Checks/` holds the checks, grouped by the record layer they judge (`Intent/`, `Behavior/`, `Implementation/`, `Proof/`, `Governance/`) with `RecordShape/` for the shape every record shares, and `Diff/` the contract and proof comparisons. `Readers/Dotnet/` splits by reader: `Backend/`, `Scenarios/`, and `Screens/`. |
| `Model/Consumer/` | The consumer pass inside `model check`. `ConsumerRun.cs` holds the run state with its text cache and findings, and runs the project record, manifest selection, frontend, page, glossary, and controlled UI checks in order. `FailFast.cs` carries an exit code and its message out of a malformed input. |
| `Model/Ui/` | The controlled UI pass the consumer pass calls. `UiValidator.Validate` runs `UiSession.cs`, which runs every UI check in one pass: the shared package configuration, the source lock, the design contract, the token and shared stylesheets, the class strings and imports, the dependency boundary, the pattern catalog, and the route suite coverage. |
| `Book/` | The `book` command. `BookCommand.cs` parses `--root`, `--out`, and `--editor`, builds the model in memory the way `model check` does, and writes one shell with sharded data through `WriteBook`. `Lexicon/` reads the book words from the carried release, `Outline/` places each record once under its exported parent, `Details/` adds proof, implementation, and completion data, and `Output/` writes the shards, the shell, and the Markdown. `Assets/book-shell.html` and the book client from `apps/cli/book/dist` (`book.js` and `book.css`) ship inside the binary. |
| `Init/` | The `init` command. `InitCommand.cs` reads the preset from `templates/presets/`, merges it over `templates/consumer/project.yaml`, stamps the release version, fills the project name, and writes `.litespec/project.yaml` without overwriting an existing file. |
| `Files/` | `SourceTree.cs` walks and reads a consumer's files and holds the one list of folders every walk skips. |
| `Rules/` | The `rules` command. `RulesCommand.cs` lists the provisions that govern a path or record id grouped by strength, `PathContext.cs` derives the target's stack, layer, and artifact, `ProvisionCatalog.cs` reads the provisions from the carried release, and `ProvisionRetriever.cs` matches them with the project's conventions and live departures. |

The solution file `LiteSpec.slnx` lists the CLI, the CLI tests, the maintainer tool, the shared library, the analyzers, and their tests. `Directory.Build.props` pins the target framework to `net10.0` with nullable reference types and implicit usings, and stops MSBuild files in parent folders from applying inside this repository. `Directory.Packages.props` pins `Microsoft.CodeAnalysis.CSharp`, `Markdig`, and `xunit.v3`. `global.json` selects `Microsoft.Testing.Platform` as the test runner and sets the lowest SDK, `10.0.114`, rolling forward to any later feature band.

### Dispatch and Option Parsing

`Program.cs` matches `args[0]` in a switch expression. `--help`, `-h`, and `help` print the top-level usage and exit 0. `--version` prints `LiteSpecHome.Version`, which is the `version` field of `litespec.manifest.yaml`, and exits 0. Each of `init`, `new`, `rename`, `model`, `book`, and `rules` calls that command's static `Run` with the remaining arguments. Anything else prints `Unknown command '<name>'.` plus the usage to standard error and exits 2.

Each command parses its own tail with the shared `CliArgs.Parse`, declaring the flag names and the value-option names it accepts. Names include their dashes, such as `--check` or `--root`. An option accepts `--name value` and `--name=value`. A flag stands alone. `CliArgs` treats `--help` and `-h` as help wherever they appear. Anything the command does not declare becomes a usage error such as `Unknown option '--foo'.`, and an option with no value becomes `Option --root needs a value.`. A positional beyond `maxPositionals` becomes `Unexpected argument '<value>'.`. Every `Run` prints its own `Usage` constant after a usage error and exits 2.

Two helpers apply across commands. `Json` is true when `--format json` or `--format=json` was passed, and `FormatError` reports any other `--format` value with `Unknown format '<value>'; use json or text.`. `Root` returns the full path of `--root` or the working directory. `model` parses its subcommand and `book` parses its `--root`, `--out`, and `--editor` options by hand and then delegate the shared flags to `CliArgs`.

The `--help` text of each command is the contract quoted in `docs/tools/`. Quote it when you document behavior, and change the `Usage` constant and the docs page together when you change options. Exit codes are stable: 0 for success, 1 for a finding or a failed check, and 2 for a usage error or an unreadable input. `init` exits 1 when the release's template, preset, or scaffold file is unreadable, and 2 when the project file exists or an option is invalid. `book` exits 1 for a model that does not build or an unwritable output, and 2 for a usage error, a root without a project file, a missing carried release, or a lexicon failure naming the carried file. `model query` exits 1 when no record holds the id.

### Findings and Diagnostic Codes

Model findings carry one reporting shape, shared with the authoring diagnostics the maintainer tool emits: a stable code, a one-sentence message, a file path, and a 1-based line. Human output prints one line per finding to standard error. JSON output prints one object to standard output so a caller never parses the human lines back apart.

`Checker.Codes` in `Model/Checker.cs` lists every model finding code, and the [Model Builder](../../docs/tools/model.md) page gives each code its meaning. `Finding` carries `Code`, `Message`, `Path` as an absolute path, `Line`, and `Subject` as the record id the finding is about, or null. `Check` sorts findings by path, line, code, and message so parallel checks print in the same order on every run. A departure covers a finding when its `covers` names the finding code, its page, or its provision and its `subject` or `paths` match the finding; a covered finding reports outcome `covered` and exits 0, until the departure passes its `expiresOn` date. `model check` prints `path:line [code] message` per open finding, then `N findings, M blocking, K covered.`. Its JSON output writes `findings` and `covered` arrays, and the model command page lists each field. An open blocking finding exits 1, and a departure past its date reports `departure-expired`, which blocks the run.

The consumer pass reports every finding with its code, message, path, and line, in the same shape as model findings, so `model check` prints one line per finding and writes one JSON object whatever the finding's pass.

### Release and Consumer Roots

Two roots meet in most commands: the LiteSpec release the tool carries, and the repository under check.

`LiteSpecHome` finds the release from the tool binary, not from the working directory, so the tool resolves the release it carries beside its binary at any working directory. `LITESPEC_HOME` overrides the search; when set, the folder must hold `litespec.manifest.yaml`, else `Root` is null. Without the variable, `Locate` walks up from `AppContext.BaseDirectory` for the manifest, which reaches the repository root in a repository checkout. `RequireRoot` throws `No LiteSpec release was found above '<dir>'. Set LITESPEC_HOME to the folder that holds litespec.manifest.yaml.` when no release resolves, and each command converts that into exit 2 with its usage text. `Version` reads the `version` field for `--version` and for the `reviewedLiteSpecVersion` stamp that `init` writes. `PathTo` joins a forward-slash relative path such as `docs/reference/provisions.md` onto the release root. The tests set `LITESPEC_HOME` to the repository root in `Support/TestCli.cs`, so every test runs against this repository.

`model` reads the consumer repository at `--root`, which defaults to the working directory, and refuses a root without `.litespec/project.yaml` with `Root '<root>' holds no .litespec/project.yaml.` and exit 2. It then loads `ConsumerConfig` from that file: record roots, application, domain, web API, and infrastructure roots, test roots, frontends, API client sources, test results directory, size, shape, and stacks. The builder reads shared provisions from the release docs the tool carries and the test run from TRX or JUnit XML files under `paths.testResults`. `book` takes `--root` and `--out <folder>`, builds the model in memory from the consumer root, and reads its words from the carried release.

### Test Harness and Fixtures

The tests live in `apps/cli/tests/LiteSpec.Cli.Tests/`. The test project targets the CLI binary as a project reference, uses `xunit.v3`, and excludes `Fixtures/**` from compilation so fixture files stay data. One folder covers each area: `Authoring/` for the `new` and `rename` tests, `Consumer/` and `Ui/` for the two passes inside `model check`, `Model/`, `Book/`, and `Init/`. `Support/TestCli.cs` runs a command through the CLI's own dispatch inside the test process, with its own output streams, working folder and variables (`CliHost`), and asserts the exit code with both streams. A command costs no process start, and the release files the CLI reads are parsed once per process. With `LITESPEC_TEST_EXECUTABLE` set to a native `litespec` executable, it runs that executable as its own process instead, so the same suite proves a native build; CI sets it on every platform. `LITESPEC_TEST_PROCESS=1` runs the built `litespec.dll` with `dotnet` as one process per command, and a command that sets its own `LITESPEC_HOME` always runs as a process, because the release root is read once per process. It sets `LITESPEC_HOME` to the repository root, finds the root by walking up for `litespec.manifest.yaml`, and offers `Fixture` for a path under `Fixtures/` and `Temp` for a fresh folder that is deleted on dispose unless `KEEP_FIXTURE` is set. `Support/InputErrorTests.cs` proves that ordinary user error reaches a message and a documented exit code, never an unhandled exception: a malformed project file, a missing project file, and an equals-form book option.

Model tests start from `Model/ModelFixture.cs` and `Fixtures/Model/`. `small/` is a library loans system at size small with one module, three business records, eight use cases, and one open gap recorded as a departure; seven use cases have handlers and the eighth is covered by that departure. `large/` holds the same records plus problems, features, journeys, one case per use case, constraints, research, limits, runbooks, and a frontend route that imports a generated client operation. Both fixtures exit 0 under `model check`. `Fixtures/Model/README.md` documents the contents, the finding codes, and the overlay mechanism. `broken/<code>/` holds one overlay per finding code with only the files that differ from `large/`; `Prepare` copies `large/` to a temp folder, copies the overlay over it, deletes the paths in `DELETES.txt` when present, and returns the folder. `ModelCheckTests.cs` runs one theory row per code and expects exit 1 with that code. `junit/` and `trx/` hold sample test results the proof checks read.

Book command tests run `book --root` against a temp copy of the `small` model fixture. Content tests write the book in-process from the frozen `Fixtures/Book/model.json` and `Fixtures/Book/review.json` through `BookTestBooks`, and assert the generated shell and shards.

To add a fixture repository for a new case, follow the harness the area already uses. For a new model finding, copy `large/` by hand into a new `broken/<code>/` overlay that holds only the differing files, add `DELETES.txt` with one relative path per line when the case deletes files, and add the code to the theory list in `ModelCheckTests.cs`. For a new authoring rule, follow the maintainer test harness in the [maintainer tool](../maintain/README.md). For a new book shape, extend the frozen review model only when the shape needs a new record type, and keep the existing files byte-identical otherwise. For new command behavior, extend the command tests against the `small` fixture.

### Common Changes

#### Add a Command

1. Add the usage text as a `Usage` constant on the new command class, with synopsis, options, and exit codes in the shape of the neighboring commands.
2. Add the dispatch arm in `Program.cs` and route unknown subcommands to exit 2 with the usage text.
3. Parse the tail with `CliArgs.Parse`, declaring every flag and value option. Print `Usage` on `--help` with exit 0 and on any parse error with exit 2.
4. Resolve roots through `LiteSpecHome` for the release and `CliArgs.Root` for the consumer. Convert a missing release into exit 2 with the usage text.
5. Write human lines to standard output or error and one JSON object to standard output under `--format json`, using `CliJson`. Build the object as a `JsonObject`, never as an anonymous object or a type the serializer reads by reflection: the native build has no reflection, and the build fails on a call that needs it.
6. Document the command on a new page under `docs/tools/` with the command page sections, and link it from `docs/tools/README.md`.

#### Add a Model Finding

1. Add the code to `Checker.Codes` in `Model/Checker.cs`.
2. Append the finding with its absolute path, 1-based line, message, and subject record id to the `findings` list in the matching `Check*` method. Keep the message to one sentence that names the record and the fix.
3. Keep departures honest: a finding with a null subject always stays open, and a departure covers a finding only by the `covers` rule the [departures page](../../docs/core/departures.md) states. Add a `departure-invalid` case when a departure names a code no check emits.
4. Add a `broken/<code>/` overlay under `Fixtures/Model/` with only the differing files, add the code to the theory list in `Model/ModelCheckTests.cs`, and document the code in the finding table on the [Model Builder](../../docs/tools/model.md) page.
5. Document the code and its meaning in the finding table on the [Model Builder](../../docs/tools/model.md) page.

#### Change a Message

1. Edit the message at its emission site, not in the test. Keep it to one sentence, name the file or record and the action the reader takes next.
2. Update every test that asserts the old text, including `Support/InputErrorTests.cs` when the message covers user error.
3. Check that JSON output needs no change: messages stay inside `message` fields, and codes, paths, and field names stay stable. Rename a code only as a new code with a new overlay, never by editing the old string in place.

### Native Build

The CLI compiles ahead of time with .NET Native AOT into one executable per platform that needs no .NET runtime. `LiteSpec.Cli.csproj` runs the trim, AOT, and single-file analyzers in every build, and `LiteSpec.CommandLine` and `LiteSpec.Standards` set `IsAotCompatible` for the same analyzers. `PublishAot` itself applies only to a pack or publish for a platform and to the top-level package, so a plain build restores no compiler. A call that needs runtime code, such as `JsonSerializer.Serialize` of an anonymous object, fails `dotnet build`. Every command therefore builds its JSON as a `JsonNode` tree and writes it with `CliJson`; `CliJson.Strings` and `CliJson.StringLists` build the common arrays. `JsonArray.Add` binds to its generic overload, which needs runtime code, for an argument typed as anything narrower than `JsonNode`, so cast the argument: `items.Add((JsonNode)new JsonObject { ... })`. `IL3000` is suppressed for one Roslyn method the CLI never calls, `CommonCompiler.GetAssemblyLocation`, and the CLI finds its own folder through `AppContext.BaseDirectory`. `InvariantGlobalization` leaves ICU out, because the CLI compares text ordinally and formats with the invariant culture. The CLI hashes with `Sha256`, its own SHA-256, because `System.Security.Cryptography` runs through OpenSSL on Linux, and a native executable would then start only where .NET loads an OpenSSL release;

Native AOT compiles only for the operating system it runs on, with the platform linker: the Visual Studio C++ build tools on Windows, Xcode on macOS, and `clang` with `zlib` on Linux. Build and test a native executable for this machine:

```bash
dotnet publish apps/cli/src/LiteSpec.Cli -c Release -r win-x64 -o artifacts/native
LITESPEC_TEST_EXECUTABLE=artifacts/native/litespec.exe dotnet test --project apps/cli/tests/LiteSpec.Cli.Tests
```

A pack or publish for a platform restores `Microsoft.DotNet.ILCompiler` at the runtime version the SDK bundles, so `global.json` sets the lowest SDK whose bundled runtime has no published advisory. That restore also adds the platform's packages to the lock file; run `dotnet restore LiteSpec.slnx --force-evaluate` before committing, which leaves only `Microsoft.NET.ILLink.Tasks`, the analyzers' package, at the bundled runtime version.

### Packing

`LiteSpec.Cli.csproj` sets `PackAsTool` to `true` with `ToolCommandName` `litespec`, so packing produces the `LiteSpec.Cli` tool packages described as `The LiteSpec command line: consumer and UI validation, the project model, and the project book.` `ToolPackageRuntimeIdentifiers` names the platforms. Three pack commands make the packages:

| Command | Package |
|:---|:---|
| `dotnet pack apps/cli/src/LiteSpec.Cli -c Release -p:TopLevelToolPackage=true` | `LiteSpec.Cli.<version>.nupkg`, which names one package per platform and holds no executable |
| `dotnet pack apps/cli/src/LiteSpec.Cli -c Release -r <rid>` | `LiteSpec.Cli.<rid>.<version>.nupkg`, the native executable with the release under `tools/any/<rid>/`, packed on a machine of that operating system |
| `dotnet pack apps/cli/src/LiteSpec.Cli -c Release -r any -p:PublishAot=false` | `LiteSpec.Cli.any.<version>.nupkg`, the framework-dependent build under `tools/net10.0/any/` for every other platform |

A consumer installs `LiteSpec.Cli`, and the SDK picks the package for the machine; `--add-source <folder>` installs from a local package folder holding all of them. Keep `PackageId`, `ToolCommandName`, and `Description` stable: install scripts and the consumer docs name them.

`.github/scripts/build-native.sh <rid> <folder>` packs one platform and writes its release archive, `litespec-<version>-<rid>.tar.gz`, or `.zip` on Windows. The archive holds one folder with the executable, the release, and the licence. A Linux build runs in a container through `.github/scripts/build-native-container.sh`: AlmaLinux 8 links against glibc 2.28, the oldest glibc the binary supports, and Alpine against musl. `apps/cli/install/install.sh` and `install.ps1` install an archive from a GitHub release after checking it against `SHA256SUMS`; `.github/scripts/install-native.sh` runs them against a local copy of the release, and `.github/scripts/smoke.sh` runs one consumer scenario against any install: `--tool`, `--binary`, or `--dll`. The CI workflow builds every platform, tests each installed binary, and runs the standalone binary on stock Linux distributions; the release workflow attests and publishes what CI built, pushing the platform packages before `LiteSpec.Cli`.

## Limits

The CLI reads text and JSON and writes text, JSON, HTML, and SVG. It runs no server, migrates no data, and publishes no package on your behalf. The consumer pass validates the declared contract and scans no route, environment, naming, secret, storage, or HTML sink beyond the controlled UI boundary. `model` builds from the declared roots and the latest TRX or JUnit files; it does not execute tests. `book` renders the model it builds in memory. Generated regions belong to their owning tool: `model render` rewrites only the text between `<!-- generated:<name> -->` fences for the region names the [model command page](../../docs/tools/model.md) lists, and authoring checks skip those regions.
