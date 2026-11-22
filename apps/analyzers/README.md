# LiteSpec.Analyzers

Use this page when you maintain the Roslyn analyzers in `apps/analyzers/`. It shows which file owns each behavior, how to add a rule from start to finish, how the tests prove a rule, and how to verify the package against a real project.

## Audience

You maintain the Roslyn analyzers when you add a rule, change what a rule reports, change how layers resolve, or ship a release.

## What It Is

`LiteSpec.Analyzers` is a Roslyn analyzer package that checks a .NET application against the LiteSpec standard while it compiles. It covers layer boundaries, Domain modeling, naming, and the traceability from code to records.

The package reports 55 diagnostics in five categories:

- `LiteSpec.Domain` (`LS1001` to `LS1016`) covers Domain only, except `LS1014`, which covers every production layer.
- `LiteSpec.Layers` (`LS2001` and `LS2003` to `LS2028`) covers Application, Ports, Infrastructure, WebApi, and Worker as each rule states.
- `LiteSpec.Style` (`LS3001` to `LS3005`) covers production files, and `LiteSpec.Tests` (`LS4001`) covers test files only.
- `LiteSpec.Traceability` (`LS5003` to `LS5006`, `LS5009`, and `LS5011` to `LS5014`) covers every layer as each rule states.

Each diagnostic is an error by default, except `LS3005` at info, names the provision it enforces, and links to that provision on `litespec.dev` through its help link.

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
| `src/LiteSpec.Analyzers/AnalyzerReleases.Unshipped.md` | The rules the release being prepared adds or removes: `LS5014` is added, and `LS2017`, `LS5001`, `LS5002`, `LS5007`, and `LS5008` are removed. Every other rule the package reports ships in a release. |
| `src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md` | Shipped rules: the 45 rules of Release 4.0.0 and the 15 rules of Release 7.0.0. |
| `src/LiteSpec.Analyzers/build/LiteSpec.Analyzers.props` | Passes the project file's `layers` as `LiteSpecImplementation` and `LiteSpecProof` (`litespec`, `own`, or empty when the layer is not claimed) through `CompilerVisibleProperty`; a file with no `layers` reads as `litespec` for both. A project sets either property itself to override the file. |
| `src/LiteSpec.Analyzers/LayerMap/FileScope.cs` | Decides whether a rule applies to one file, from the rule's layer predicate over the file's layer. |
| `src/LiteSpec.Analyzers/LayerMap/CodeMap.cs` | Maps every source file of one compilation to its layer, module, aggregate, and use case. |
| `src/LiteSpec.Analyzers/LayerMap/References.cs` | Resolves the type one written name refers to, skipping names an enclosing construct covers. |
| `../model/src/LiteSpec.Model/Layers/` | The shared layer surface the analyzers, the CLI, and the checks read: the `ArchitectureLayer` enum and `ArchitectureLayerResolver`, which reads a file's layer from the nearest layer folder, the file's role suffix, or the assembly name. |
| `src/LiteSpec.Analyzers/Rules/LayerAnalyzer.cs` | Base class of the layer-gated rules: skips compilations whose claims exclude the rule, and registers the rule for per-file gating. |
| `src/LiteSpec.Analyzers/Rules/Descriptors.cs` | All 52 analyzer descriptors with IDs, categories, messages, provisions, and help links. The other 3 of the 55 diagnostics, `LS5009`, `LS5012`, and `LS5013`, live in `Generation/GeneratorDiagnostics.cs`. |
| `src/LiteSpec.Analyzers/Generation/GeneratorDiagnostics.cs` | The generator diagnostics for `LS5009`, `LS5012`, and `LS5013` (four descriptors: the permission and role forms of `LS5009`, `LS5012`, and the non-virtual base `Code` of `LS5013`) with IDs, messages, provisions, and help links. |
| `src/LiteSpec.Analyzers/Rules/Provision.cs` | Provision IDs and help links, pinned to release `7.0.0`. |
| `src/LiteSpec.Analyzers/Rules/Domain/` | 14 analyzers for `LS1001` to `LS1016`. |
| `src/LiteSpec.Analyzers/Rules/Layers/` | 25 analyzers for `LS2001` and `LS2003` to `LS2028`, by the page each cites: `Application/` (handlers, the command pipeline, repositories, the message and event mediators, message definitions, infrastructure work), `Architecture/` (layer direction, module and use case boundaries, controllers, visibility, platform ownership, adapters, duplicate models), and `Naming/` (the naming rules). |
| `src/LiteSpec.Analyzers/Rules/Style/` | 4 analyzers for `LS3001` to `LS3005`. |
| `src/LiteSpec.Analyzers/Rules/Tests/` | 1 analyzer for `LS4001`. |
| `src/LiteSpec.Analyzers/Rules/Traceability/` | 5 analyzers for `LS5003` to `LS5006` and `LS5014`. |
| `src/LiteSpec.Analyzers/Generation/` | The source generator, which emits `LS5009`, `LS5011`, `LS5012`, and `LS5013` beside the code it writes. |
| `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` | Test harness that runs one analyzer over marked sources. |
| `tests/LiteSpec.Analyzers.Tests/Domain/` | Tests for the Domain rules, one file per analyzer. |
| `tests/LiteSpec.Analyzers.Tests/Layers/` | Tests for the layer rules, each new boundary rule on a module-project fixture with onion folder paths. |
| `tests/LiteSpec.Analyzers.Tests/Style/` | Tests for the style rules. |
| `tests/LiteSpec.Analyzers.Tests/Tests/` | Tests for the test rules. |

Three files at the repository root shape how the analyzers build. `Directory.Build.props` targets `net10.0` by default with nullable reference types and implicit usings, and blocks MSBuild files in parent folders from applying inside this repository. The analyzer project overrides the framework back to `netstandard2.0` and switches implicit usings off. `Directory.Packages.props` pins the central package versions. `.github/workflows/ci.yml` builds `LiteSpec.slnx`, runs its tests, packs the analyzers into `artifacts`, checks the provision index, and validates the repository.

### How a Compilation Flows Through the Package

Every layer-gated rule inherits `LayerAnalyzer`, and the traceability rules go through `TraceabilityAnalyzer`, so each rule follows the same path through a compilation. `Initialize` turns off analysis of generated code with `GeneratedCodeAnalysisFlags.None`, switches concurrent execution on, and registers a single compilation-start action. That action skips the compilation when the claimed record layers exclude the rule, and otherwise registers the rule for per-file gating, where each action builds a `FileScope` in `Register` and tests it against the file under analysis.

A rule outside its layer registers nothing and costs nothing.

The generator lives outside this flow. `LiteSpecGenerator` implements `IIncrementalGenerator` and writes its sources through `GeneratedSourceWriter.Execute`, which stays silent in the shared kernel and writes the full surface for a module project. A host sees the registry, the permissions, and the roles through its module project, so it generates none of its own.

The resolver reads each file: the nearest layer folder (`Domain/`, `Application/`, `Ports/`, `Infrastructure/`, `WebApi/`, with `Contracts/` counting as `Application`), a sibling `{Product}.{Module}.Contracts` project counting as `Application` for its owner module, or the file's role suffix. A file's layer is its top folder in the module. A file under `Application/` or `WebApi/` takes its use case from the next folder; `Ports/` and `Infrastructure/` hold shared folders only. The assembly name suffix places only hosts, the kernel, the platform, and test projects: `Shop.WebApi` and `Shop.Api` mean WebApi, `Shop.Worker` means Worker, `Shop.Kernel` means Domain, `Shop.Platform` means Infrastructure, and `Shop.Domain.Tests` means Test. A module project names no layer, so each of its files takes its layer from its folder. The suffix check matches a full name or a dot segment, which leaves `Shop.DomainTests` resolving to `None` with no rule running there. A host file with neither signal takes its layer from the assembly, and a file the rules cannot place stays silent.

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
| `LS1014` | `AggregateAccessAnalyzer` | Every layer |
| `LS1015` | `AggregateMutationAnalyzer` | Domain |
| `LS1016` | `AggregateReferenceAnalyzer` | Domain |
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
| `LS2018` | `LayerDirectionAnalyzer` | Application, Ports, WebApi |
| `LS2019` | `UseCaseIsolationAnalyzer` | Every layer |
| `LS2020` | `ModuleBoundaryAnalyzer` | Every layer |
| `LS2021` | `MessageSentOutsideEntryPointAnalyzer` | Domain, Application, Infrastructure |
| `LS2022` | `EventPublishedByUserCodeAnalyzer` | Every layer |
| `LS2023` | `MessageDefinitionAnalyzer` | Every layer |
| `LS2024` | `PlatformOwnershipAnalyzer` | Every layer |
| `LS2025` | `UseCaseInfrastructureAnalyzer` | Infrastructure |
| `LS2026` | `DuplicateModelAnalyzer` | Every layer |
| `LS2027` | `InfrastructureWorkAnalyzer` | Application |
| `LS2028` | `ModuleVisibilityAnalyzer` | Every layer |
| `LS3001`, `LS3002` | `AsyncMethodAnalyzer` | Every production layer |
| `LS3003` | `FileTypeNameAnalyzer` | Every production layer |
| `LS3004` | `SealedClassAnalyzer` | Every production layer |
| `LS3005` | `PrimaryConstructorAnalyzer` | Every production layer |
| `LS4001` | `TestAssertionAnalyzer` | Test |
| `LS5003` | `MarkCountAnalyzer` | Every layer |
| `LS5004` | `MarkStyleAnalyzer` | Every layer |
| `LS5005` | `RuntimeValueAnalyzer` | Domain |
| `LS5006` | `PartialTypeAnalyzer` | Domain |
| `LS5009` | `LiteSpecGenerator` | Domain, Application, Infrastructure, WebApi |
| `LS5011` | `LiteSpecGenerator` | Every layer |
| `LS5012` | `LiteSpecGenerator` | Domain, Application, Infrastructure, WebApi |
| `LS5013` | `LiteSpecGenerator` | Domain |
| `LS5014` | `DevelopmentShortcutAnalyzer` | Every layer |

Each analyzer registers the narrowest action its rule can use. Declarations such as enums and exceptions get a symbol action, using directives and repository interfaces get a syntax-node action, throws and invocations get an operation action, and file names get a syntax-tree action.

Rules that need the whole compilation wait until compilation end: `LS1003` and `LS1004` locate aggregates and their state bases across all types, `LS2026` compares records across use case folders, and `LS3004` gathers every base type before flagging a leaf class with no derived type. Those four descriptors carry the `CompilationEnd` tag, while all other rules report as the compiler reads each declaration.

`Descriptors.Create` builds every descriptor identically: severity `Error` (`LS3005` passes `Info`), enabled by default, the provision ID in the description as `Provision: litespec/<page>#<slug>` and in the custom tags, and the help link as `https://litespec.dev/7.0.0/<page>/#<slug>` from `Provision.HelpLink`. When you cite a rule, quote its descriptor line together with its `Provision.Of` line, since the pair names the category, the message, and the provision.

Release tracking follows the Roslyn convention. `AnalyzerReleases.Shipped.md` lists the 45 rules of Release 4.0.0 and the 15 rules of Release 7.0.0, each with its category (`LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability`) and severity `Error`, except `LS3005` at `Info`. `AnalyzerReleases.Unshipped.md` collects the rules the release being prepared adds or removes, up to the moment that release is tagged, and currently holds the one added rule and the five removed rules. To ship a rule, move its row from the unshipped file to the shipped file under a release heading, as the release-tracking help at the top of each file describes.

The project turns `EnforceExtendedAnalyzerRules` on, so the Roslyn meta-analyzers check the rules themselves. It suppresses `RS1012` and `RS1013` in `NoWarn` because `LayerAnalyzer` registers each rule from its compilation-start action and passes that context into `Register`, a path the meta-analyzer cannot see. It also suppresses `NU5128`, since the package ships an analyzer with no `lib` folder. `TreatWarningsAsErrors` and `GenerateDocumentationFile` remain on, so analyzer code carries XML documentation and any warning fails the build.

### Test Harness

`Verify.AnalyzerAsync` in `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` runs a single analyzer over C# sources, then compares the diagnostics it produces against the spans marked in those sources. A marked span looks like `{|LS1002:OrderStatus|}`: the diagnostic ID followed by the exact text the diagnostic should cover. The harness strips the markup, builds a compilation from the clean text, runs the analyzer, and asserts an exact match between actual and marked diagnostics, with nothing missing and nothing extra. Locations render as `ID Path(line,column)-(line,column)` with 1-based lines, so each failure pinpoints the span that moved.

The single-file overload accepts one source string. The multi-file overload accepts a list of `(Path, Source)` tuples for rules that read across files. `LS3004` uses the multi-file form to prove that a base class with a derived type in another file stays quiet while the unsealed leaf still reports:

```csharp
public Task AcceptsABaseWithADerivedClassInAnotherFile() =>
    Verify.AnalyzerAsync<SealedClassAnalyzer>(
        [
            ("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public class Order { }
                """),
            ("Shop.Spending/Domain/PriorityOrder.cs", """
                namespace Shop.Orders;

                public class {|LS3004:PriorityOrder|} : Order { }
                """),
        ]);
```

Three optional parameters shape the test compilation:

- `assemblyName` defaults to `Shop.Spending` and drives the layer only through the host, kernel, platform, and test suffixes above, so a test proves layer gating by passing a different name such as `Shop.WebApi`.
- `path` defaults to `Source.cs` and names the single file's place in the module tree, so a single-file test states its layer with `path: "Shop.Spending/Domain/Source.cs"`. The nearest layer folder on the path decides the layer, and a path with no layer folder reads by role suffix or assembly name.
- `documentationFile` defaults to `true` and parses with `DocumentationMode.Parse`; passing `false` parses the way a build without a documentation file does, where `///` lines count as ordinary comments. That flag exists for `LS1011`: the documentation analyzer accepts `/// <summary>` text both as plain comments and as structured XML, and the test `ReadsCommentsWithoutADocumentationFile` proves both paths.

References come from the running .NET install through `TRUSTED_PLATFORM_ASSEMBLIES`, narrowed to the base class library: names starting with `System.`, plus `System.dll`, `netstandard.dll`, and `mscorlib.dll`. Test sources compile with nullable context enabled, and the harness fails fast with `The test source does not compile` when a source carries an error, so a red test means the rule is wrong, not the sample.

Since only the base library is referenced, tests declare the framework types they need as inline stand-ins: an `AggregateRoot<TId>` base for aggregate tests, an `ICommandMediator` interface for pipeline tests, and an `IDocumentSession` interface inside a `Marten` namespace for session tests. When you cover a rule that names a framework type, copy the stand-in from the neighboring test.

A new rule typically needs the three tests `DomainEnumAnalyzerTests` shows: one test where the breach reports with a marked span, one where the compliant shape passes with no markup, and one where the rule stays quiet outside its layer.

```csharp
[Fact(DisplayName = "LS1002 reports an enum declared in Domain")]
public Task ReportsAnEnumInDomain() =>
    Verify.AnalyzerAsync<DomainEnumAnalyzer>("""
        namespace Shop.Orders;

        public enum {|LS1002:OrderStatus|} { Draft, Placed }
        """, path: "Shop.Spending/Domain/Source.cs");
```

### Add a Rule

Work through these steps in order. Each step names the file it changes.

1. Pick the next free ID in the range its layer defines: `LS1xxx` for a Domain rule, `LS2xxx` for an outer-layer rule, `LS3xxx` for a style rule in production code, `LS4xxx` for a test rule, `LS5xxx` for a traceability rule. Search both release files, `Descriptors.cs`, and `GeneratorDiagnostics.cs` to confirm the ID is free.
2. Add the `Provision.Of` line and the descriptor in `Rules/Descriptors.cs`, beside the rules of the same category. Set the category to `LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability`, derive the title, message, and description from the provision, and pass `compilationEnd: true` only when the rule needs the whole compilation, as `LS1003`, `LS1004`, and `LS3004` do.
3. Add one line to `AnalyzerReleases.Unshipped.md` under New Rules with the ID, the category, `Error`, and `Descriptors` in the Notes column, matching the existing rows.
4. Write the analyzer as a `sealed` class extending `LayerAnalyzer`, placed in the `Domain`, `Layers`, `Style`, `Tests`, or `Traceability` folder. Declare the `[DiagnosticAnalyzer(LanguageNames.CSharp)]` attribute, expose the descriptor through `SupportedDiagnostics`, accept the file's layer in `AppliesTo`, and register the narrowest action in `Register`. Build a `FileScope` in `Register` and gate every action through it. Resolve well-known types once from `context.Compilation` inside `Register` and close over them, as `AsyncMethodAnalyzer` and `NondeterministicReadAnalyzer` do. When a rule reads module or use case boundaries, build a `CodeMap` in `Register` next to the scope.
5. Add a test file under `tests/LiteSpec.Analyzers.Tests/` next to the neighboring tests. Cover the breach with a marked span, the compliant shape with no markup, and the neighboring layer where the rule stays silent. For a boundary rule, prove the onion layout with a module-project fixture with folder paths. Declare every framework type the rule names as a stand-in, following the neighboring test.
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
2. Create a scratch folder outside the repository holding a console project, for example `Scratch.Orders`.
3. Register the `artifacts` folder as a local package source and reference version `7.0.0` with `PrivateAssets="all"`, as the package README shows.
4. Add a breaching file under a `Domain/` folder, such as an enum, and a compliant file, such as the matching record hierarchy from the analyzer tests.
5. Build the scratch project. The build reports an `LS1002` error on the enum name and stays silent on the record hierarchy. When a rule stays silent everywhere, or reports outside its layer, revisit `AppliesTo` and the test harness rather than the packaging.
6. Remove the scratch folder once the check passes, leaving no stray project on disk.

## Limits

The analyzers judge only the files in the layers the rule table names, plus test files such as `Shop.Domain.Tests` for the test rules. They stay silent everywhere else, including `Shop.DomainTests` and files that no layer folder or role suffix places. They skip generated code.

They report the syntax and symbols the compiler sees: each rule recognizes handlers, repositories, aggregates, child entities, and controllers by the names and base types its code reads, so a type concealed behind an unfamiliar name or an indirect base can slip through, and each rule's reference page under the [Analyzer rules overview](../../docs/analyzers/rules/README.md) records what it ignores.

Diagnostics default to errors; a project lowers severity per rule in `.editorconfig` with `dotnet_diagnostic.LS1011.severity`, and a project that sets a Standard aside records a waiver citing the provision. The package supports the Roslyn version it pins and newer compilers only.

### Why netstandard2.0 and Roslyn 4.14

The analyzer project targets `netstandard2.0` because analyzers load into every compiler host that builds a consumer project, and `netstandard2.0` loads everywhere. The project sets `LangVersion` to `latest` and switches implicit usings off, so its code compiles the same way on any SDK.

The project references `Microsoft.CodeAnalysis.CSharp` at version `4.14.0` through `VersionOverride`, while `Directory.Packages.props` pins the central version at `5.9.0` for the repository tools. Version `4.14.0` is the oldest compiler the package supports, which means the .NET 9.0.300 SDK and Visual Studio 17.14. Write analyzer code against the `4.14.0` API surface only, so the package keeps working for consumers on an older SDK. The reference carries `PrivateAssets="all"`, so Roslyn never leaks into the package. The test project pins the same `4.14.0` version, so the tests prove the rules against the oldest supported compiler.
