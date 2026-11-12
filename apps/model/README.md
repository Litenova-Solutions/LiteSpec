# LiteSpec.Model

## Audience

You maintain the shared model library in `apps/model/`. You change how a record file parses, how an identifier or a finding code is formed, how a C# mark is read, or how a layer claim is kept. After reading this page you know which project owns each rule, and how to prove a change with its tests.

## What It Is

`apps/model` holds the rules that the CLI and the maintainer tool share: the record types and their folders, the identifier grammar, the finding codes, the YAML reader and writer, the layer claims, and the C# mark reader. It has no command line of its own. The [CLI](../cli/README.md) reads a consumer's records through it, and the [maintainer tool](../maintain/README.md) reads the LiteSpec pages through it.

The library is four projects. `LiteSpec.Model` targets `netstandard2.0`, holds the project model's vocabulary, and reads YAML with YamlDotNet. `LiteSpec.Model.Roslyn` targets `netstandard2.0`, reads the marks in C# source, and references `Microsoft.CodeAnalysis.CSharp`. `LiteSpec.CommandLine` holds how both command lines talk: option parsing and JSON output. `LiteSpec.Standards` reads the standards material the release carries: the LiteSpec release locator, the page reader, the stack selection, the provisions, the pattern parsers, and the JSON Schema evaluator.

## Quick Start

Run every command in this section from the repository root.

Run the model tests:

```bash
dotnet test --project apps/model/tests/LiteSpec.Model.Tests
```

Build the solution, which includes the model projects:

```bash
dotnet build LiteSpec.slnx
```

## Layout

| Path | What it owns |
|:---|:---|
| `src/LiteSpec.Model/` | The project model's vocabulary, one folder per concept. `Records/` names each record type, field and folder and checks the identifier grammar (`RecordTypes.cs`, `RecordFields.cs`, `RecordFolders.cs`, `RecordFiles.cs`, `Identifiers.cs`). `Findings/` holds the finding codes, severities and suppressions. `Project/` reads the project file's paths and layer claims. `Events/` holds the act-event check both hosts run. `Text/` reads and writes JSON and canonical YAML and converts name casing (`YamlText.cs`, `YamlWriter.cs`, `Naming.cs`). `Marks/` and `Layers/` hold the mark and architecture layer rules. |
| `src/LiteSpec.Model.Roslyn/` | The C# mark reader. `MarkReader.cs` reads the marks on a member from its syntax, `KindReader.cs` reads the record kind a type declares, and `RaisedEvents.cs` and `StateChanges.cs` read the events and state changes a method raises. The reader works on syntax only and never builds a compilation. |
| `src/LiteSpec.CommandLine/` | How both command lines talk: `CliArgs.cs` parses options and `CliJson.cs` writes JSON with two-space indentation. |
| `src/LiteSpec.Standards/` | The standards material the release carries. `Release/LiteSpecHome.cs` locates the LiteSpec release, `Pages/` reads the Markdown pages and the stack selection, `Provisions/` builds the provision index and reads scopes, `Patterns/` parses the pattern pages, `Schemas/` holds the JSON Schema evaluator, and `Files/` reads record and file text. |
| `tests/LiteSpec.Model.Tests/` | The model tests, one file per area, on `xunit.v3` and the Microsoft Testing Platform. `Layers/`, `Marks/` and `Roslyn/` hold the tests of those folders. |

A rule that changes a record type or a finding code belongs in `LiteSpec.Model`, with a test in `tests/LiteSpec.Model.Tests/`. A change to a command's output belongs in the command, not here.

## Limits

The library reads and checks records. It runs no tests, reads no git history, and writes no file on its own: the CLI and the maintainer tool decide when to write. The Roslyn reader sees syntax only, so a mark that depends on a type declared in another file is read as written.
