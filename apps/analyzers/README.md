# LiteSpec.Analyzers

## Who This Is For

You maintain the Roslyn analyzers in `apps/analyzers/`. You add a rule, change what a rule reports, change layer resolution, or release the package. After reading this page you know which file owns each behavior, how to add a rule end to end, how the tests prove it, and how to check the package against a real project.

## What It Is

`LiteSpec.Analyzers` is a Roslyn analyzer package that checks a .NET application against the LiteSpec standard: layer boundaries, Domain modeling, naming, and traceability from code to records. It reports 45 diagnostics in five categories: `LiteSpec.Domain` (`LS1001` to `LS1013`, Domain only), `LiteSpec.Layers` (`LS2001` and `LS2003` to `LS2020`, Application, Infrastructure, WebApi, and Worker as stated per rule), `LiteSpec.Style` (`LS3001` and `LS3004`, production files), `LiteSpec.Tests` (`LS4001`, test files only), and `LiteSpec.Traceability` (`LS5001` to `LS5009`, `LS5011`, and `LS5012`, every layer as stated per rule). Every diagnostic is an error by default, names the provision it enforces, and links to that provision on `litespec.dev` through its help link. The package ships the analyzer assembly beside the assemblies it loads at analysis time: the project sets `DevelopmentDependency` to `true`, `IncludeBuildOutput` to `false`, and packs `LiteSpec.Analyzers.dll`, `LiteSpec.Model.Core.dll`, `LiteSpec.Model.Roslyn.dll`, and `LiteSpec.Analyzers.CodeFixes.dll` under `analyzers/dotnet/cs`, with `build/LiteSpec.Analyzers.props` under `build`, so consumers reference it with `PrivateAssets="all"` and it never becomes a runtime dependency. The user-facing rule list lives in the package README at `apps/analyzers/src/LiteSpec.Analyzers/README.md`, and the full per-rule reference with breach and compliant examples lives in one reference page per rule under the [Analyzer rules overview](../../docs/analyzers/rules/README.md). Read both before you change what a rule reports.

## Quick Start

Run every command in this section from the repository root.

Build the solution, which includes the analyzers and their tests:

```bash
dotnet build LiteSpec.slnx
```

Run the analyzer tests:

```bash
dotnet test apps/analyzers/tests/LiteSpec.Analyzers.Tests/LiteSpec.Analyzers.Tests.csproj
```

Pack the analyzer package exactly as the release and the end-to-end check use it:

```bash
dotnet pack apps/analyzers/src/LiteSpec.Analyzers -c Release -o artifacts
```

Install the package in a consumer project, or once in `Directory.Build.props`, as the package README shows:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="4.1.0" PrivateAssets="all" />
</ItemGroup>
```

The package needs the .NET 9.0.300 SDK, Visual Studio 17.14, or later. That floor comes from the Roslyn version below, not from the target framework.

## Why netstandard2.0 and Roslyn 4.14

The analyzer project targets `netstandard2.0` because analyzers load into every compiler host that builds a consumer project, and `netstandard2.0` loads everywhere. The project sets `LangVersion` to `latest` and disables implicit usings, so the code compiles the same way on any SDK.

The project references `Microsoft.CodeAnalysis.CSharp` at version `4.14.0` through `VersionOverride`, while `Directory.Packages.props` pins the central version at `5.9.0` for the repository tools. Version `4.14.0` is the oldest compiler the package supports: the .NET 9.0.300 SDK and Visual Studio 17.14. You write analyzer code against the `4.14.0` API surface only, so the package keeps working for consumers who have not moved to a newer SDK. The reference uses `PrivateAssets="all"`, so Roslyn never leaks into the package. The test project pins the same `4.14.0` version, so the tests prove the rules against the oldest supported compiler.

## Layout

| Path | What it owns |
|:---|:---|
| `src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj` | Target framework, Roslyn version, analyzer packaging, release-tracking files, and warning suppressions. |
| `src/LiteSpec.Analyzers/README.md` | Package README that ships on NuGet. Its Rules table is the user-facing rule list. |
| `src/LiteSpec.Analyzers/AnalyzerReleases.Unshipped.md` | No pending rules: every rule the package reports ships in the current release. |
| `src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md` | Shipped rules: the 45 rules of Release 4.0.0. |
| `src/LiteSpec.Analyzers/build/LiteSpec.Analyzers.props` | Makes the `LiteSpecLayer` and `LiteSpecLayout` MSBuild properties visible to the analyzers through `CompilerVisibleProperty`, and sets the layout from the selected extensions. It passes the project file's `layers` as `LiteSpecImplementation` and `LiteSpecProof` (`litespec`, `own`, or empty when the layer is not claimed); a file with no `layers` reads as `litespec` for both. A project sets either property itself to override the file. |
| `src/LiteSpec.Analyzers/Layers/FileScope.cs` | Decides whether a rule applies to one file, from the rule's layer predicate over the file's layer. |
| `src/LiteSpec.Analyzers/Layers/CodeMap.cs` | Maps every source file of one compilation to its layer, module, and use case, reaction, or job folder. |
| `src/LiteSpec.Analyzers/Layers/References.cs` | Resolves the type one written name refers to, skipping names an enclosing construct covers. |
| `../model/src/LiteSpec.Model.Core/Layers/` | The shared layer surface the analyzers, the CLI, and the checks read: the `Layer` and `Layout` enums and `LayerResolver`, which reads a file's layer from `LiteSpecLayer`, the assembly name, the nearest layer folder, or the file's role suffix. |
| `src/LiteSpec.Analyzers/Rules/LayerAnalyzer.cs` | Base class of the layer-gated rules: gates a declared or layer-projects compilation once, and registers the rule for per-file gating under the module layout. `LayerNameAnalyzer` (`LS2017`) inherits `DiagnosticAnalyzer` directly. |
| `src/LiteSpec.Analyzers/Rules/Descriptors.cs` | All 43 analyzer descriptors with IDs, categories, messages, provisions, and help links. The other 2 of the 45 diagnostics, `LS5009` and `LS5012`, live in `Generation/GeneratorDiagnostics.cs`. |
| `src/LiteSpec.Analyzers/Generation/GeneratorDiagnostics.cs` | The generator diagnostics for `LS5009` and `LS5012` (three descriptors: the permission and role forms of `LS5009`, and `LS5012`) with IDs, messages, provisions, and help links. |
| `src/LiteSpec.Analyzers/Rules/Provision.cs` | Provision IDs and help links, pinned to release `4.1.0`. |
| `src/LiteSpec.Analyzers/Rules/Domain/` | 11 analyzers for `LS1001` to `LS1013`. |
| `src/LiteSpec.Analyzers/Rules/Layers/` | 18 analyzers for `LS2001` and `LS2003` to `LS2020`. |
| `src/LiteSpec.Analyzers/Rules/Style/` | 2 analyzers for `LS3001` and `LS3004`. |
| `src/LiteSpec.Analyzers/Rules/Tests/` | 1 analyzer for `LS4001`. |
| `src/LiteSpec.Analyzers/Rules/Traceability/` | 5 analyzers for `LS5001` to `LS5008`. |
| `src/LiteSpec.Analyzers/Generation/` | The source generator, which emits `LS5009`, `LS5011`, and `LS5012` beside the code it writes. |
| `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` | Test harness that runs one analyzer over marked sources. |
| `tests/LiteSpec.Analyzers.Tests/Domain/` | Tests for the Domain rules, one file per analyzer. |
| `tests/LiteSpec.Analyzers.Tests/Layers/` | Tests for the layer rules, each new boundary rule on a module-layout and a five-project fixture. |
| `tests/LiteSpec.Analyzers.Tests/Style/` | Tests for the style rules. |
| `tests/LiteSpec.Analyzers.Tests/Tests/` | Tests for the test rules. |

The repository root adds three files the analyzers depend on. `Directory.Build.props` sets the default target to `net10.0` with nullable reference types and implicit usings, and stops MSBuild files in parent folders from applying inside this repository. The analyzer project overrides the framework back to `netstandard2.0` and disables implicit usings. `Directory.Packages.props` pins the central package versions. `.github/workflows/ci.yml` builds `LiteSpec.slnx`, tests it, packs the analyzers to `artifacts`, checks the provision index, and validates the repository.

## How a Compilation Flows Through the Package

The layer-gated rules inherit `LayerAnalyzer` (the traceability rules through `TraceabilityAnalyzer`), so they follow the same path through a compilation. `Initialize` disables analysis of generated code with `GeneratedCodeAnalysisFlags.None`, enables concurrent execution, and registers one compilation-start action. That action skips the compilation when the declared `LiteSpecLayer` value is `None` or unknown, gates a declared or layer-projects compilation once through `AppliesTo`, and otherwise registers the rule for per-file gating: each action builds a `FileScope` in `Register` and checks it for the file under analysis. A rule in the wrong layer registers nothing and costs nothing. `LS2017` is the exception: `LayerNameAnalyzer` inherits `DiagnosticAnalyzer` directly, reads the declared value itself, and reports an unknown one, whatever the layer. The generator is separate: `LiteSpecGenerator` implements `IIncrementalGenerator` and writes its sources through `Emitter.Execute`, which writes nothing and reports nothing when the layer is `None`, and writes the full surface for the module-layout application project.

`LayerResolver` prefers the declared layer: when the project sets `<LiteSpecLayer>Domain</LiteSpecLayer>`, the build props expose it as `build_property.LiteSpecLayer` and the resolver parses it, ignoring dots and case, so `Application.Abstractions` and `applicationabstractions` both work, and `Test` and `Tests` both name the test layer. A declared value applies to every file in the project. An unknown value reports `LS2017` and opts the project out as `None`. Without the property, the resolver follows the layout the build props set from the selected extensions. Under `layer-projects` it reads the assembly name suffix: `Shop.Domain` is Domain, `Shop.Application.Abstractions` is ApplicationAbstractions, and so on down to `Shop.Worker`, while `Shop.Domain.Tests` is Test. The suffix check matches a full name or a dot segment, so `Shop.DomainTests` resolves to `None` and no rule runs there. Under `modules` it reads each file: the nearest layer folder (`Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, with `Contracts/` reading as `Application`), or the file's role suffix in a use case, reaction, or job folder. A host file with neither reads its layer from the assembly, and a file the rules cannot place stays quiet. `None` also turns the whole package off for one project on purpose: no analyzer rule runs, and the generator writes no source and reports no diagnostic.

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
| `LS3001` | `AsyncMethodAnalyzer` | All production projects |
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

Each analyzer registers the narrowest action its rule allows: a symbol action for declarations such as enums and exceptions, a syntax-node action for using directives and repository interfaces, an operation action for throws and invocations, and a syntax-tree action for file names. Rules that need the whole compilation decide at the end of compilation: `LS1003` and `LS1004` find aggregates and their state bases across all types, and `LS3004` collects every base type before it reports a leaf class nothing derives from. Those three descriptors carry the `CompilationEnd` tag, and every other rule reports as the compiler reads each declaration.

`Descriptors.Create` builds every descriptor the same way: severity `Error`, enabled by default, the provision ID in the description as `Provision: litespec/<page>#<slug>` and in the custom tags, and the help link as `https://litespec.dev/4.1.0/<page>/#<slug>` from `Provision.HelpLink`. To cite a rule you quote its descriptor line and its `Provision.Of` line, which together name the category, the message, and the provision.

Release tracking follows the Roslyn convention. `AnalyzerReleases.Shipped.md` lists the 45 rules of Release 4.0.0, each with category `LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability` and severity `Error`. `AnalyzerReleases.Unshipped.md` holds no pending rules. When you ship a rule, you move its row from the unshipped file to the shipped file under a release heading, as the release-tracking help linked at the top of each file describes.

The project sets `EnforceExtendedAnalyzerRules` to `true`, so the Roslyn meta-analyzers check the rules themselves. It suppresses `RS1012` and `RS1013` in `NoWarn` because `LayerAnalyzer` registers each rule from its compilation-start action and hands that context to `Register`, which the meta-analyzer cannot see. It also suppresses `NU5128` because the package ships an analyzer with no `lib` folder. `TreatWarningsAsErrors` and `GenerateDocumentationFile` stay on, so analyzer code ships with XML documentation and a warning fails the build.

## Test Harness

`Verify.AnalyzerAsync` in `tests/LiteSpec.Analyzers.Tests/Support/Verify.cs` runs one analyzer over C# sources and compares its diagnostics with the spans the sources mark. A marked span reads `{|LS1002:OrderStatus|}`: the diagnostic ID, then the exact text the diagnostic covers. The harness strips the markup, builds a compilation from the clean text, runs the analyzer, and asserts that the actual diagnostics match the marked spans exactly, with nothing missing and nothing extra. Locations print as `ID Path(line,column)-(line,column)` with 1-based lines, so a failure tells you which span moved.

The single-file overload takes one source string, and the multi-file overload takes a list of `(Path, Source)` tuples for rules that read across files. `LS3004` uses the multi-file form to prove that a base class with a derived type in another file stays quiet while the unsealed leaf still reports:

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

Four optional parameters shape the compilation. `assemblyName` defaults to `Shop.Domain` and decides the layer through the suffix rules above, so a test proves layer gating by passing another name such as `Shop.WebApi`. `layer` feeds `build_property.LiteSpecLayer` through a test options provider and wins over the assembly name, so a test proves the declared property with an assembly such as `Shop.Core` plus `layer: "Domain"`. `layout` feeds `build_property.LiteSpecLayout` and defaults to `modules`, so a test proves five-project behavior with `layout: "layer-projects"`. `documentationFile` defaults to `true` and parses with `DocumentationMode.Parse`; pass `false` to parse as a build without a documentation file does, where `///` lines are ordinary comments. That flag exists for `LS1011`: the documentation analyzer accepts `/// <summary>` text read as plain comments as well as structured XML, and the test `ReadsCommentsWithoutADocumentationFile` proves both paths.

References come from the running .NET install through `TRUSTED_PLATFORM_ASSEMBLIES`, filtered to the base class library: names starting with `System.`, plus `System.dll`, `netstandard.dll`, and `mscorlib.dll`. Test sources compile with nullable context enabled, and the harness fails fast with `The test source does not compile` when a source has an error, so a red test means the rule is wrong, not the sample. Because only the base library is referenced, tests declare stand-in framework types inline: an `AggregateRoot<TId>` base for aggregate tests, an `ICommandMediator` interface for pipeline tests, and an `IDocumentSession` interface inside a `Marten` namespace for session tests. Copy the stand-in from the neighboring test when you cover a rule that names a framework type.

A typical test for a new rule follows `DomainEnumAnalyzerTests`: one test reports the breach with a marked span, one test accepts the compliant shape with no markup, one test proves the rule stays quiet outside its layer, and one test proves the declared `LiteSpecLayer` property wins over the assembly name.

```csharp
[Fact(DisplayName = "LS1002 reports an enum declared in Domain")]
public Task ReportsAnEnumInDomain() =>
    Verify.AnalyzerAsync<DomainEnumAnalyzer>("""
        namespace Shop.Orders;

        public enum {|LS1002:OrderStatus|} { Draft, Placed }
        """);
```

## Add a Rule

Follow these steps in order. Each step names the file you change.

1. Pick the next free ID in the range the layers define: `LS1xxx` for a Domain rule, `LS2xxx` for an outer-layer rule, `LS3xxx` for a style rule in production code, `LS4xxx` for a test rule, `LS5xxx` for a traceability rule. Check both release files, `Descriptors.cs`, and `GeneratorDiagnostics.cs` so the ID is unused.
2. Add the `Provision.Of` line and the descriptor in `Rules/Descriptors.cs`, next to the rules of the same category. Name the category `LiteSpec.Domain`, `LiteSpec.Layers`, `LiteSpec.Style`, `LiteSpec.Tests`, or `LiteSpec.Traceability`, write the title, message, and description from the provision, and pass `compilationEnd: true` only when the rule needs the whole compilation, as `LS1003`, `LS1004`, and `LS3004` do.
3. Add one line to `AnalyzerReleases.Unshipped.md` under New Rules with the ID, the category, `Error`, and `Descriptors` in the Notes column, matching the existing rows.
4. Write the analyzer as a `sealed` class that extends `LayerAnalyzer`, in the `Domain`, `Layers`, `Style`, `Tests`, or `Traceability` folder. Declare the `[DiagnosticAnalyzer(LanguageNames.CSharp)]` attribute, expose the descriptor through `SupportedDiagnostics`, accept the file's layer in `AppliesTo`, and register the narrowest action in `Register`. Build a `FileScope` in `Register` and gate every action through it. Resolve well-known types once from `context.Compilation` inside `Register` and close over them, as `AsyncMethodAnalyzer` and `NondeterministicReadAnalyzer` do. A rule that reads module or slice boundaries builds a `CodeMap` in `Register` beside the scope.
5. Add a test file under `tests/LiteSpec.Analyzers.Tests/` beside the neighboring tests. Cover the breach with a marked span, the compliant shape with no markup, the neighboring layer where the rule stays quiet, and the declared `layer` property winning over the assembly name. A boundary rule proves both layouts: a module-layout fixture with folder paths, and a five-project fixture with `layout: "layer-projects"`. Declare any framework type the rule names as a stand-in, following the neighboring test.
6. Add the rule row to the Rules table in `src/LiteSpec.Analyzers/README.md` with its ID, layer, short report text, and provision link.
7. Document the rule with one reference page under the [Analyzer rules overview](../../docs/analyzers/rules/README.md): the provision, the layers, where the diagnostic sits, what the rule ignores, and a breach example beside a compliant example taken from your tests.

## Performance Rules

The analyzers run on every keystroke in an IDE, so you keep per-compilation work small and shared. Build the `FileScope` and the `CodeMap` once in the compilation-start action and close over them; never rebuild them per symbol. Resolve well-known types once with `GetTypeByMetadataName` inside `Register` and close over the results; never look them up per node. In compilation-end actions, walk `context.Compilation.Assembly.GlobalNamespace` only, which is the source assembly under analysis, and skip symbols with no `DeclaringSyntaxReferences`, so referenced packages cost nothing. Never call `GetSemanticModel` yourself: use the symbol, operation, and syntax-node contexts the compiler hands you, which already carry the right model. Pick the narrowest registration the rule allows, and keep `EnableConcurrentExecution` with `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` from the base class. When you add a rule, check its registration against this list before you write the test.

## End-To-End Check Against a Scratch Project

The unit tests prove each rule in isolation. This check proves the packed package reports in a real build. Pack the package, create a scratch project outside the repository, point it at a local feed, and build.

1. Pack the release binary from the repository root with `dotnet pack apps/analyzers/src/LiteSpec.Analyzers -c Release -o artifacts`.
2. Create a scratch folder outside the repository with a console project whose assembly name ends in `.Domain`, for example `Scratch.Domain`.
3. Add the `artifacts` folder as a local package source and reference version `4.1.0` with `PrivateAssets="all"`, as the package README shows.
4. Add a breaching file, such as an enum in Domain, and a compliant file, such as the matching record hierarchy from the analyzer tests.
5. Build the scratch project. The build lists an `LS1002` error on the enum name and stays quiet on the record hierarchy. A rule that stays quiet everywhere, or that reports outside its layer, sends you back to `AppliesTo` and the test harness, not to the packaging.
6. Delete the scratch folder when the check passes, so no stray project stays on disk.

## Limits

The analyzers check the files of the layers the rule table names, plus test files such as `Shop.Domain.Tests` for the test rules, and stay quiet everywhere else, including `Shop.DomainTests` and files no layer folder or role suffix places. They skip generated code. They report syntax and symbols the compiler sees: a rule recognizes handlers, repositories, aggregates, child entities, and controllers by the names and base types its code reads, so a type that hides behind an unfamiliar name or an indirect base can slip past, and the rule's reference page under the [Analyzer rules overview](../../docs/analyzers/rules/README.md) records what each rule ignores. Diagnostics are errors by default; a project changes severity per rule in `.editorconfig` with `dotnet_diagnostic.LS1011.severity`, and a project that departs from a Standard records a departure that cites the provision. The package supports the Roslyn version it pins and newer compilers only.
