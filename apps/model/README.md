# LiteSpec.Model

## Audience

You maintain the shared model library in `apps/model/`. You change how a record file parses, how an identifier or a finding code is formed, how a C# mark is read, or how a layer claim is kept. After reading this page you know which project owns each rule, and how to prove a change with its tests.

## What It Is

`apps/model` holds the rules that the CLI and the maintainer tool share: the record types and their folders, the identifier grammar, the finding codes, the YAML reader and writer, the layer claims, and the C# mark reader. It has no command line of its own. The [CLI](../cli/README.md) reads a consumer's records through it, and the [maintainer tool](../maintain/README.md) reads the LiteSpec pages through it.

The library is three projects. `LiteSpec.Model.Core` targets `netstandard2.0`, holds the record model, and reads YAML with YamlDotNet. `LiteSpec.Model.Roslyn` targets `netstandard2.0`, reads the marks in C# source, and references `Microsoft.CodeAnalysis.CSharp`. `LiteSpec.Shared` holds the code that both command lines use: option parsing, JSON output, the LiteSpec release locator, the page reader, the stack layout, the pattern parsers, and the JSON Schema evaluator.

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
| `src/LiteSpec.Model.Core/` | The record model: `RecordTypes.cs` names each record type and its page style, `RecordFolders.cs` and `RecordFiles.cs` place and read the record files, `Identifiers.cs` checks the identifier grammar, `FindingCodes.cs` and `FindingCode.cs` hold the finding codes, `LayerClaims.cs` and `RecordLayer.cs` hold the layer claims, and `YamlText.cs` and `YamlWriter.cs` read and write canonical YAML. `Marks/` and `Layers/` hold the mark and layer rules. |
| `src/LiteSpec.Model.Roslyn/` | The C# mark reader. `MarkReader.cs` reads the marks on a member from its syntax, `KindReader.cs` reads the record kind a type declares, and `RaisedEvents.cs` and `StateChanges.cs` read the events and state changes a method raises. The reader works on syntax only and never builds a compilation. |
| `src/LiteSpec.Shared/` | The code both command lines use. `CliArgs.cs` parses options, `CliJson.cs` writes JSON with two-space indentation, `LiteSpecHome.cs` locates the LiteSpec release, `Authoring/` reads the Markdown pages and the stack layout, `Patterns/` parses the pattern pages, and `Schemas/` holds the JSON Schema evaluator. |
| `tests/LiteSpec.Model.Tests/` | The model tests, one file per area, on `xunit.v3` and the Microsoft Testing Platform. `Layers/`, `Marks/` and `Roslyn/` hold the tests of those folders. |

A rule that changes a record type or a finding code belongs in `LiteSpec.Model.Core`, with a test in `tests/LiteSpec.Model.Tests/`. A change to a command's output belongs in the command, not here.

## Limits

The library reads and checks records. It runs no tests, reads no git history, and writes no file on its own: the CLI and the maintainer tool decide when to write. The Roslyn reader sees syntax only, so a mark that depends on a type declared in another file is read as written.
