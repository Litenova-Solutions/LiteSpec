# LiteSpec.Analyzers

Use this page when you maintain the Roslyn analyzers in `apps/analyzers/`. It shows which file owns each behavior, how to add a rule from start to finish, how the tests prove a rule, and how to verify the package against a real project.

## Audience

You maintain the Roslyn analyzers when you add a rule, change what a rule reports, change how layers resolve, or ship a release.

## What It Is

`LiteSpec.Analyzers` is a Roslyn analyzer package that checks a .NET application against the LiteSpec standard while it compiles. It covers layer boundaries, Domain modeling, naming, and the traceability from code to records.

The package reports 49 diagnostics in five categories:

- `LiteSpec.Domain` (`LS1001` to `LS1013`) covers Domain only.
- `LiteSpec.Layers` (`LS2001` and `LS2003` to `LS2022`) covers Application, Infrastructure, WebApi, and Worker as each rule states.
- `LiteSpec.Style` (`LS3001` to `LS3004`) covers production files, and `LiteSpec.Tests` (`LS4001`) covers test files only.
- `LiteSpec.Traceability` (`LS5001` to `LS5009`, `LS5011`, and `LS5012`) covers every layer as each rule states.

Each diagnostic is an error by default, names the provision it enforces, and links to that provision on `litespec.dev` through its help link.

The package ships the analyzer assembly beside the assemblies it loads at analysis time. The project sets `DevelopmentDependency` to `true` and `IncludeBuildOutput` to `false`, then packs `LiteSpec.Analyzers.dll`, `LiteSpec.Model.dll`, `LiteSpec.Model.Roslyn.dll`, and `LiteSpec.Analyzers.CodeFixes.dll` under `analyzers/dotnet/cs` plus `build/LiteSpec.Analyzers.props` under `build`. Consumers reference the package with `PrivateAssets="all"`, so it never turns into a runtime dependency.

Users read the rule list in the package README at `apps/analyzers/src/LiteSpec.Analyzers/README.md`, and the full per-rule reference, with breach and compliant examples, on one reference page per rule under the [Analyzer rules overview](../../docs/analyzers/rules/README.md). Read both before you change what any rule reports.

## Quick Start

Run every command in this section from the repository root. The package requires the .NET 9.0.300 SDK, Visual Studio 17.14, or later. That minimum comes from the Roslyn version below, not from the target framework.

Build the solution, analyzers and tests included:

```bash
dotnet build LiteSpec.slnx
```

Run the analyzer tests:

```bash
dotnet test apps/analyzers/tests/LiteSpec.Analyzers.Tests/LiteSpec.Analyzers.Tests.csproj
```

Pack the analyzer package exactly as the release and the end-to-end check consume it:

```bash
dotnet pack apps/analyzers/src/LiteSpec.Analyzers -c Release -o artifacts
```

Add the package to a consumer project, or once in `Directory.Build.props`, following the package README:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="7.0.0" PrivateAssets="all" />
</ItemGroup>
```

## Layout

| Path | What it owns |
|:---|:---|
| `src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj` | Target framework, Roslyn version, analyzer packaging, release-tracking files, and warning suppressions. |
| `src/LiteSpec.Analyzers/README.md` | Package README that ships on NuGet. Its Rules table is the user-facing rule list. |
| `src/LiteSpec.Analyzers/AnalyzerReleases.Unshipped.md` | The rules the release being prepared adds: none. Every rule the package reports ships in a release. |
| `src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md` | Shipped rules: the 45 rules of Release 4.0.0 and the 4 rules of Release 7.0.0. |
| `src/LiteSpec.Analyzers/build/LiteSpec.Analyzers.props` | Makes the `LiteSpecLayer` and `LiteSpecLayout` MSBuild properties visible to the analyzers through `CompilerVisibleProperty`, and sets the layout from the selected extensions. It passes the project file's `layers` as `LiteSpecImplementation` and `LiteSpecProof` (`litespec`, `own`, or empty when the layer is not claimed); a file with no `layers` reads as `litespec` for both. A project sets either property itself to override the file. |
| `src/LiteSpec.Analyzers/LayerMap/FileScope.cs` | Decides whether a rule applies to one file, from the rule's layer predicate over the file's layer. |
| `src/LiteSpec.Analyzers/LayerMap/CodeMap.cs` | Maps every source file of one compilation to its layer, module, and use case, reaction, or job folder. |
| `src/LiteSpec.Analyzers/LayerMap/References.cs` | Resolves the type one written name refers to, skipping names an enclosing construct covers. |
| `../model/src/LiteSpec.Model/Layers/` | The shared layer surface the analyzers, the CLI, and the checks read: the `ArchitectureLayer` and `BackendLayout` enums and `ArchitectureLayerResolver`, which reads a file's layer from `LiteSpecLayer`, the assembly name, the nearest layer folder, or the file's role suffix. |
| `src/LiteSpec.Analyzers/Rules/LayerAnalyzer.cs` | Base class of the layer-gated rules: gates a declared or layer-projects compilation once, and registers the rule for per-file gating under the module layout. `LayerNameAnalyzer` (`LS2017`) inherits `DiagnosticAnalyzer` directly. |
| `src/LiteSpec.Analyzers/Rules/Descriptors.cs` | All 47 analyzer descriptors with IDs, categories, messages, provisions, and help links. The other 2 of the 49 diagnostics, `LS5009` and `LS5012`, live in `Generation/GeneratorDiagnostics.cs`. |
| `src/LiteSpec.Analyzers/Generation/GeneratorDiagnostics.cs` | The generator diagnostics for `LS5009` and `LS5012` (three descriptors: the permission and role forms of `LS5009`, and `LS5012`) with IDs, messages, provisions, and help links. |
| `src/LiteSpec.Analyzers/Rules/Provision.cs` | Provision IDs and help links, pinned to release `7.0.0`. |
| `src/LiteSpec.Analyzers/Rules/Domain/` | 11 analyzers for `LS1001` to `LS1013`. |
| `src/LiteSpec.Analyzers/Rules/Layers/` | 20 analyzers for `LS2001` and `LS2003` to `LS2022`, by the page each cites: `Application/` (handlers, the command pipeline, repositories, the message and event mediators), `Architecture/` (layer direction, module and slice boundaries, controllers, visibility), and `Naming/` (the naming rules). |
| `src/LiteSpec.Analyzers/Rules/Style/` | 3 analyzers for `LS3001` to `LS3004`. |
| `src/LiteSpec.Analyzers/Rules/Tests/` | 1 analyzer for `LS4001`. |
| `src/LiteSpec.Analyzers/Rules/Traceability/` | 5 analyzers for `LS5001` to `LS5008`. |
| `src/LiteSpec.Analyzers/Generation/` | The source generator, which emits `LS5009`, `LS5011`, and `LS5012` beside the code it writes. |
| `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` | Test harness that runs one analyzer over marked sources. |
| `tests/LiteSpec.Analyzers.Tests/Domain/` | Tests for the Domain rules, one file per analyzer. |
| `tests/LiteSpec.Analyzers.Tests/Layers/` | Tests for the layer rules, each new boundary rule on a module-layout and a five-project fixture. |
| `tests/LiteSpec.Analyzers.Tests/Style/` | Tests for the style rules. |
| `tests/LiteSpec.Analyzers.Tests/Tests/` | Tests for the test rules. |

Three files at the repository root shape how the analyzers build. `Directory.Build.props` targets `net10.0` by default with nullable reference types and implicit usings, and blocks MSBuild files in parent folders from applying inside this repository. The analyzer project overrides the framework back to `netstandard2.0` and switches implicit usings off. `Directory.Packages.props` pins the central package versions. `.github/workflows/ci.yml` builds `LiteSpec.slnx`, runs its tests, packs the analyzers into `artifacts`, checks the provision index, and validates the repository.

### How a Compilation Flows Through the Package

Every layer-gated rule inherits `LayerAnalyzer`, and the traceability rules go through `TraceabilityAnalyzer`, so each rule follows the same path through a compilation. `Initialize` turns off analysis of generated code with `GeneratedCodeAnalysisFlags.None`, switches concurrent execution on, and registers a single compilation-start action. That action ignores the compilation when the declared `LiteSpecLayer` value is `None` or unknown, gates a declared or layer-projects compilation once through `AppliesTo`, and otherwise registers the rule for per-file gating, where each action builds a `FileScope` in `Register` and tests it against the file under analysis.

A rule outside its layer registers nothing and costs nothing. `LS2017` breaks this pattern: `LayerNameAnalyzer` inherits `DiagnosticAnalyzer` directly, reads the declared value on its own, and reports an unknown value in any layer.

The generator lives outside this flow. `LiteSpecGenerator` implements `IIncrementalGenerator` and writes its sources through `GeneratedSourceWriter.Execute`, which stays silent when the layer is `None` and writes the full surface for the module-layout application project otherwise.

The declared layer wins in `ArchitectureLayerResolver`. When a project sets `<LiteSpecLayer>Domain</LiteSpecLayer>`, the build props expose the value as `build_property.LiteSpecLayer` and the resolver parses it while ignoring dots and case, so `Application.Abstractions` and `applicationabstractions` both match, and `Test` and `Tests` both name the test layer. One declared value covers every file in the project. An unknown value reports `LS2017` and opts the project out as `None`.

With no declared property, the resolver uses the layout the build props derive from the selected extensions. Under `layer-projects` it reads the assembly name suffix, so `Shop.Domain` means Domain, `Shop.Application.Abstractions` means ApplicationAbstractions, and the chain continues down to `Shop.Worker`, while `Shop.Domain.Tests` means Test. The suffix check matches a full name or a dot segment, which leaves `Shop.DomainTests` resolving to `None` with no rule running there. Under `modules` the resolver reads each file instead: the nearest layer folder (`Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, with `Contracts/` counting as `Application`), or the file's role suffix inside a use case, reaction, or job folder. A host file with neither signal takes its layer from the assembly, and a file the rules cannot place stays silent.

Setting `None` deliberately switches the whole package off for one project: no analyzer rule runs, and the generator emits neither source nor diagnostics.

| Rule | Analyzer class | Layer |
|:---|:---|:---|
| `LS1001` | `DomainOuterLayerAnalyzer` | Domain |
| `LS1002` | `DomainEnumAnalyzer` | Domain |
| `LS1003`, `LS1004` | `AggregateStateAnalyzer` | Domain |
| `LS1005`, `LS1006` | `DomainExceptionAnalyzer` | Domain |
| `LS1007` | `DomainEventAnalyzer` | Domain |
| `LS1008` | `FloatingPointAnalyzer` | Domain |
| `LS1009` | `NondeterministicReadAnalyzer` | Domain |
| `LS1010` | `RepositoryContractAnalyzer` | Domain |
| `LS1011` | `DomainDocumentationAnalyzer` | Domain |
| `LS1012` | `TypedIdentifierAnalyzer` | Domain |
| `LS1013` | `ExhaustiveSwitchAnalyzer` | Domain |
| `LS2001` | `ImplementationVisibilityAnalyzer` | Application, Infrastructure, WebApi, Worker |
| `LS2004` | `CommandPipelineAnalyzer` | Application, Infrastructure, Worker |
| `LS2003` | `ControllerAnalyzer` | WebApi |
| `LS2005` | `HandlerNamingAnalyzer` | Application, Infrastructure, Worker |
| `LS2006` | `EndpointNamingAnalyzer` | WebApi |
| `LS2007` | `AutomationNamingAnalyzer` | Application, Infrastructure, Worker |
| `LS2009` | `ResultPartNamingAnalyzer` | Application, Infrastructure, Worker |
| `LS2010` | `RepositoryVariableNamingAnalyzer` | Application, Infrastructure, Worker |
| `LS2011` | `QueryRepositoryAnalyzer` | Application, Infrastructure, Worker |
| `LS2012` | `HandlerAuthorizationAnalyzer` | Application, Infrastructure, Worker |
| `LS2013` | `HandlerAuditValueAnalyzer` | Application, Infrastructure, Worker |
| `LS2014` | `FindOrCreateAnalyzer` | Application, Infrastructure, Worker |
| `LS2015` | `HandlerLoopWriteAnalyzer` | Application, Infrastructure, Worker |
| `LS2016` | `PipelineOwnedDependencyAnalyzer` | Application, Infrastructure, Worker |
| `LS2017` | `LayerNameAnalyzer` | Every layer |
| `LS2018` | `LayerDirectionAnalyzer` | Application, WebApi |
| `LS2019` | `SliceIsolationAnalyzer` | Every layer |
| `LS2020` | `ModuleBoundaryAnalyzer` | Every layer |
| `LS2021` | `MessageSentOutsideEntryPointAnalyzer` | Domain, Application, Infrastructure |
| `LS2022` | `EventPublishedByUserCodeAnalyzer` | Every layer |
| `LS3001`, `LS3002` | `AsyncMethodAnalyzer` | All production projects |
| `LS3003` | `FileTypeNameAnalyzer` | All production projects |
| `LS3004` | `SealedClassAnalyzer` | All production projects |
| `LS4001` | `TestAssertionAnalyzer` | Test |
| `LS5001` | `MarkPresenceAnalyzer` | Every layer |
| `LS5002`, `LS5003`, `LS5004` | `MarkLinkAnalyzer` | Every layer |
| `LS5005` | `RuntimeValueAnalyzer` | Domain |
| `LS5006` | `PartialTypeAnalyzer` | Domain |
| `LS5007`, `LS5008` | `ActEventAnalyzer` | Domain |
| `LS5009` | `LiteSpecGenerator` | Domain, Application, Infrastructure, WebApi |
| `LS5011` | `LiteSpecGenerator` | Every layer |
| `LS5012` | `LiteSpecGenerator` | Domain, Application, Infrastructure, WebApi |

Each analyzer registers the narrowest action its rule can use. Declarations such as enums and exceptions get a symbol action, using directives and repository interfaces get a syntax-node action, throws and invocations get an operation action, and file names get a syntax-tree action.

Rules that need the whole compilation wait until compilation end: `LS1003` and `LS1004` locate aggregates and their state bases across all types, and `LS3004` gathers every base type before flagging a leaf class with no derived type. Those three descriptors carry the `CompilationEnd` tag, while all other rules report as the compiler reads each declaration.

`Descriptors.Create` builds every descriptor identically: severity `Error`, enabled by default, the provision ID in the description as `Provision: litespec/<page>#<slug>` and in the custom tags, and the help link as `https://litespec.dev/7.0.0/<page>/#<slug>` from `Provision.HelpLink`. When you cite a rule, quote its descriptor line together with its `Provision.Of` line, since the pair names the category, the message, and the provision.

Release tracking follows the Roslyn convention. `AnalyzerReleases.Shipped.md` lists the 45 rules of Release 4.0.0 and the 4 rules of Release 7.0.0, each with its category (`LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability`) and severity `Error`. `AnalyzerReleases.Unshipped.md` collects the rules the release being prepared adds, up to the moment that release is tagged, and currently holds none. To ship a rule, move its row from the unshipped file to the shipped file under a release heading, as the release-tracking help at the top of each file describes.

The project turns `EnforceExtendedAnalyzerRules` on, so the Roslyn meta-analyzers check the rules themselves. It suppresses `RS1012` and `RS1013` in `NoWarn` because `LayerAnalyzer` registers each rule from its compilation-start action and passes that context into `Register`, a path the meta-analyzer cannot see. It also suppresses `NU5128`, since the package ships an analyzer with no `lib` folder. `TreatWarningsAsErrors` and `GenerateDocumentationFile` remain on, so analyzer code carries XML documentation and any warning fails the build.

### Test Harness

`Verify.AnalyzerAsync` in `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` runs a single analyzer over C# sources, then compares the diagnostics it produces against the spans marked in those sources. A marked span looks like `{|LS1002:OrderStatus|}`: the diagnostic ID followed by the exact text the diagnostic should cover. The harness strips the markup, builds a compilation from the clean text, runs the analyzer, and asserts an exact match between actual and marked diagnostics, with nothing missing and nothing extra. Locations render as `ID Path(line,column)-(line,column)` with 1-based lines, so each failure pinpoints the span that moved.

The single-file overload accepts one source string. The multi-file overload accepts a list of `(Path, Source)` tuples for rules that read across files. `LS3004` uses the multi-file form to prove that a base class with a derived type in another file stays quiet while the unsealed leaf still reports:

```csharp
public Task AcceptsABaseWithADerivedClassInAnotherFile() =>
    Verify.AnalyzerAsync<SealedClassAnalyzer>(
        [
            ("Order.cs", """
                namespace Shop.Orders;

                public class Order { }
                """),
            ("PriorityOrder.cs", """
                namespace Shop.Orders;

                public class {|LS3004:PriorityOrder|} : Order { }
                """),
        ]);
```

Four optional parameters shape the test compilation:

- `assemblyName` defaults to `Shop.Domain` and drives the layer through the suffix rules above, so a test proves layer gating by passing a different name such as `Shop.WebApi`.
- `layer` feeds `build_property.LiteSpecLayer` through a test options provider and wins over the assembly name, so a test proves the declared property with an assembly like `Shop.Core` plus `layer: "Domain"`.
- `layout` feeds `build_property.LiteSpecLayout` and defaults to `modules`, so a test proves five-project behavior with `layout: "layer-projects"`.
- `documentationFile` defaults to `true` and parses with `DocumentationMode.Parse`; passing `false` parses the way a build without a documentation file does, where `///` lines count as ordinary comments. That flag exists for `LS1011`: the documentation analyzer accepts `/// <summary>` text both as plain comments and as structured XML, and the test `ReadsCommentsWithoutADocumentationFile` proves both paths.

References come from the running .NET install through `TRUSTED_PLATFORM_ASSEMBLIES`, narrowed to the base class library: names starting with `System.`, plus `System.dll`, `netstandard.dll`, and `mscorlib.dll`. Test sources compile with nullable context enabled, and the harness fails fast with `The test source does not compile` when a source carries an error, so a red test means the rule is wrong, not the sample.

Since only the base library is referenced, tests declare the framework types they need as inline stand-ins: an `AggregateRoot<TId>` base for aggregate tests, an `ICommandMediator` interface for pipeline tests, and an `IDocumentSession` interface inside a `Marten` namespace for session tests. When you cover a rule that names a framework type, copy the stand-in from the neighboring test.

A new rule typically needs the four tests `DomainEnumAnalyzerTests` shows: one test where the breach reports with a marked span, one where the compliant shape passes with no markup, one where the rule stays quiet outside its layer, and one where the declared `LiteSpecLayer` property wins over the assembly name.

```csharp
[Fact(DisplayName = "LS1002 reports an enum declared in Domain")]
public Task ReportsAnEnumInDomain() =>
    Verify.AnalyzerAsync<DomainEnumAnalyzer>("""
        namespace Shop.Orders;

        public enum {|LS1002:OrderStatus|} { Draft, Placed }
        """);
```

### Add a Rule

Work through these steps in order. Each step names the file it changes.

1. Pick the next free ID in the range its layer defines: `LS1xxx` for a Domain rule, `LS2xxx` for an outer-layer rule, `LS3xxx` for a style rule in production code, `LS4xxx` for a test rule, `LS5xxx` for a traceability rule. Search both release files, `Descriptors.cs`, and `GeneratorDiagnostics.cs` to confirm the ID is free.
2. Add the `Provision.Of` line and the descriptor in `Rules/Descriptors.cs`, beside the rules of the same category. Set the category to `LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability`, derive the title, message, and description from the provision, and pass `compilationEnd: true` only when the rule needs the whole compilation, as `LS1003`, `LS1004`, and `LS3004` do.
3. Add one line to `AnalyzerReleases.Unshipped.md` under New Rules with the ID, the category, `Error`, and `Descriptors` in the Notes column, matching the existing rows.
4. Write the analyzer as a `sealed` class extending `LayerAnalyzer`, placed in the `Domain`, `Layers`, `Style`, `Tests`, or `Traceability` folder. Declare the `[DiagnosticAnalyzer(LanguageNames.CSharp)]` attribute, expose the descriptor through `SupportedDiagnostics`, accept the file's layer in `AppliesTo`, and register the narrowest action in `Register`. Build a `FileScope` in `Register` and gate every action through it. Resolve well-known types once from `context.Compilation` inside `Register` and close over them, as `AsyncMethodAnalyzer` and `NondeterministicReadAnalyzer` do. When a rule reads module or slice boundaries, build a `CodeMap` in `Register` next to the scope.
5. Add a test file under `tests/LiteSpec.Analyzers.Tests/` next to the neighboring tests. Cover the breach with a marked span, the compliant shape with no markup, the neighboring layer where the rule stays silent, and the declared `layer` property winning over the assembly name. For a boundary rule, prove both layouts: a module-layout fixture with folder paths and a five-project fixture with `layout: "layer-projects"`. Declare every framework type the rule names as a stand-in, following the neighboring test.
6. Add the rule's row to the Rules table in `src/LiteSpec.Analyzers/README.md` with its ID, layer, short report text, and provision link.
7. Document the rule on its own reference page under the [Analyzer rules overview](../../docs/analyzers/rules/README.md): the provision, the layers, where the diagnostic lands, what the rule ignores, and a breach example next to a compliant example drawn from your tests.

### Performance Rules

The analyzers run on every keystroke in an IDE, so per-compilation work must stay small and shared:

- Build the `FileScope` and the `CodeMap` once in the compilation-start action and close over them instead of rebuilding them per symbol.
- Resolve well-known types once with `GetTypeByMetadataName` inside `Register` and close over the results instead of looking them up per node.
- In compilation-end actions, walk only `context.Compilation.Assembly.GlobalNamespace`, which is the source assembly under analysis, and skip symbols with no `DeclaringSyntaxReferences`, so referenced packages cost nothing.
- Never call `GetSemanticModel` yourself; use the symbol, operation, and syntax-node contexts the compiler hands you, since they already carry the right model.
- Register the narrowest action the rule allows, and keep `EnableConcurrentExecution` with `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` from the base class.

Before you write the test for a new rule, check its registration against this list.

### End-to-End Check Against a Scratch Project

The unit tests prove each rule alone. This check proves the packed package reports inside a genuine build. Pack the package, create a scratch project outside the repository, aim it at a local feed, and build.

1. Pack the release binary from the repository root with `dotnet pack apps/analyzers/src/LiteSpec.Analyzers -c Release -o artifacts`.
2. Create a scratch folder outside the repository holding a console project whose assembly name ends in `.Domain`, for example `Scratch.Domain`.
3. Register the `artifacts` folder as a local package source and reference version `7.0.0` with `PrivateAssets="all"`, as the package README shows.
4. Add a breaching file, such as an enum in Domain, and a compliant file, such as the matching record hierarchy from the analyzer tests.
5. Build the scratch project. The build reports an `LS1002` error on the enum name and stays silent on the record hierarchy. When a rule stays silent everywhere, or reports outside its layer, revisit `AppliesTo` and the test harness rather than the packaging.
6. Remove the scratch folder once the check passes, leaving no stray project on disk.

## Limits

The analyzers judge only the files in the layers the rule table names, plus test files such as `Shop.Domain.Tests` for the test rules. They stay silent everywhere else, including `Shop.DomainTests` and files that no layer folder or role suffix places. They skip generated code.

They report the syntax and symbols the compiler sees: each rule recognizes handlers, repositories, aggregates, child entities, and controllers by the names and base types its code reads, so a type concealed behind an unfamiliar name or an indirect base can slip through, and each rule's reference page under the [Analyzer rules overview](../../docs/analyzers/rules/README.md) records what it ignores.

Diagnostics default to errors; a project lowers severity per rule in `.editorconfig` with `dotnet_diagnostic.LS1011.severity`, and a project that sets a Standard aside records a waiver citing the provision. The package supports the Roslyn version it pins and newer compilers only.

### Why netstandard2.0 and Roslyn 4.14

The analyzer project targets `netstandard2.0` because analyzers load into every compiler host that builds a consumer project, and `netstandard2.0` loads everywhere. The project sets `LangVersion` to `latest` and switches implicit usings off, so its code compiles the same way on any SDK.

The project references `Microsoft.CodeAnalysis.CSharp` at version `4.14.0` through `VersionOverride`, while `Directory.Packages.props` pins the central version at `5.9.0` for the repository tools. Version `4.14.0` is the oldest compiler the package supports, which means the .NET 9.0.300 SDK and Visual Studio 17.14. Write analyzer code against the `4.14.0` API surface only, so the package keeps working for consumers on an older SDK. The reference carries `PrivateAssets="all"`, so Roslyn never leaks into the package. The test project pins the same `4.14.0` version, so the tests prove the rules against the oldest supported compiler.
