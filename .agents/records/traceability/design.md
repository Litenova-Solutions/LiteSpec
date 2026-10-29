# Traceability: Tool Design

The tool contract every worker builds to. The provisions in `docs/` are the authority; this page fixes the names, codes and types the tools share so the CLI, the analyzer and the source generator agree.

## Marks

A mark is the one written link from a code element to the record it serves. A project declares one mark style in `litespec.project.json`:

```json
"marks": "xml"
```

| Style | Code mark | Test mark |
|:---|:---|:---|
| `xml` (default) | `/// <implements>use-case/spending.submit-expense</implements>` | `/// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>` |
| `attribute` | `[Implements("use-case/spending.submit-expense")]` | `[Proves("scenario/submit-expense.submitting-with-a-receipt-succeeds")]` |

A frontend use-case module carries the same typed constant in both styles: `export const litespec = { implements: "use-case/spending.submit-expense" } as const;`.

### What Carries a Mark

| Layer | Code element | Kind | Mark names | Count |
|:---|:---|:---|:---|:---|
| Domain | Aggregate root class | `AggregateRoot` | `aggregate/` | one |
| Domain | Public aggregate root method that changes state | `AggregateMethod` | `use-case/` | one or more |
| Domain | Domain event | `DomainEvent` | `event/` | one |
| Domain | Domain exception for one rejected rule | `RuleException` | `rule/` | one |
| Domain | Domain service holding one rule calculation | `DomainService` | `rule/` | one |
| Application | Command or query handler | `Handler` | `use-case/` | one |
| Application | Event handler `{Action}On{Fact}Handler` | `EventHandler` | `reaction/` | one |
| Application, Infrastructure | Job host | `Job` | `job/` | one |
| Application | Port for an external system | `Port` | `external-system/` | one or more |
| Infrastructure | Adapter implementing that port | `Adapter` | `external-system/` | one or more |
| WebApi | Endpoint | `Endpoint` | `use-case/` | one |
| Frontend | Use-case module folder | `FrontendModule` | `use-case/` | one |
| Tests | Test method | `Test` | `scenario/` or `rule/` | one or more |

Kinds are recognized from the code, never from the mark: a handler implements `ICommandHandler<,>` or `IQueryHandler<,>` (any arity, by interface name); an event handler implements `IEventHandler<>` or `IDomainEventReaction<>`; a job derives from `BackgroundService` or implements `IHostedService` and is named `*Job` or `*Worker`; an aggregate root derives from `AggregateRoot<>`; an aggregate method is a public instance or static method on an aggregate root that raises a domain event (calls `RaiseDomainEvent`, directly or through a private method of the same type); a domain event implements `IDomainEvent`; a rule exception derives from `DomainException` and is not abstract; an endpoint implements `IEndpoint` or is named `*Endpoint` in WebApi; a port is an interface in Application or Application.Abstractions that carries an `external-system/` mark; an adapter is a class implementing a marked port; a test method carries `[Fact]`, `[Theory]`, `[Scenario]`, `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[TestMethod]` or `[DataTestMethod]`.

A port and a domain service are recognized only by their mark, so an unmarked port is caught only by `external-system-unimplemented`. An adapter is recognized by implementing a marked port, so an unmarked adapter carries `mark-missing`. An aggregate method is recognized by raising an event, so a state-changing method that raises nothing is recognized only once it carries a mark. Types in test projects are never domain events, jobs or endpoints. A minimal-API route with no endpoint type is not a kind; `use-case-unexposed` catches the use case from the top.

The same record id twice on one element counts as two marks for `mark-count`.

## Findings

Every finding has one code, one tier, and at most one analyzer rule id. The core library owns this table.

| Code | Tier | Rule | Meaning |
|:---|:---|:---|:---|
| `mark-missing` | S | LS5001 | A code element the marks table names carries no mark. |
| `mark-unresolved` | S | LS5002 | A mark names no record, or a record of a type its code element does not take. |
| `mark-count` | S | LS5003 | A code element carries more or fewer marks than its kind takes. |
| `mark-style-mixed` | S | LS5004 | A mark is written in the style the project did not declare. |
| `runtime-value-drift` | S | LS5005 | A hand-written event `Classification` or exception `Code` differs from what its record states. |
| `partial-missing` | S | LS5006 | A marked domain event or rule exception is not `partial`, so the generator cannot write its runtime value. Reported only where the analyzer package generates values. |
| `act-event-undeclared` | S | LS5007 | An aggregate method marked with a use case raises an event the use case does not list in `raises`. |
| `act-event-unraised` | S | LS5008 | A use case lists an event in `raises` that no method marked with it raises. |
| `use-case-act-uncalled` | S | | A command handler calls no aggregate method marked with its use case. |
| `aggregate-unimplemented` | S | | No aggregate root carries the aggregate's mark. |
| `reaction-unimplemented` | S | | No event handler carries the reaction's mark. |
| `job-unimplemented` | S | | No job host carries the job's mark. |
| `external-system-unimplemented` | S | | No port carries the external system's mark. |
| `use-case-unexposed` | S | | A use case reached through a `browser` or `api` entry point has no endpoint carrying its mark. |
| `effect-unimplemented` | A | | A declared effect edge no code takes. |
| `use-case-unreached` | A | | A use case reached through a browser entry point has no screen calling it. |

`behavior-unrecorded` stays for a refusal code literal in a declaring position that no record declares, and for a screen call that resolves to no endpoint. A mark is the link: the CLI no longer derives a handler's use case from its folder, an event's record from its class name, or a reaction's record from a regular expression.

A rule exception's refusal code is its rule's `refusal` result's `code`. The CLI counts a marked rule exception as declaring that code, so `refusal-not-thrown` reads the mark when no literal exists.

## Provisions and Analyzer Rules

Every LS5xxx rule has category `LiteSpec.Traceability`, a reference page `docs/analyzers/rules/ls50NN.md`, an entry in `AnalyzerReleases.Unshipped.md`, and the help link of the provision it enforces. A rule reports only on symbols declared in the compilation it runs in.

| Rule | Finding | Provision | Runs in layers |
|:---|:---|:---|:---|
| LS5001 | `mark-missing` | `litespec/core/system#point-code-and-tests-up-to-records` | every layer, for the kinds that layer holds |
| LS5002 | `mark-unresolved` | `litespec/core/system#point-code-and-tests-up-to-records` | every layer |
| LS5003 | `mark-count` | `litespec/core/system#point-code-and-tests-up-to-records` | every layer |
| LS5004 | `mark-style-mixed` | `litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style` | every layer |
| LS5005 | `runtime-value-drift` | `litespec/backend/dotnet/stack#generate-runtime-values-from-the-records` | Domain |
| LS5006 | `partial-missing` | `litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types` | Domain |
| LS5007 | `act-event-undeclared` | `litespec/backend/domain#raise-the-declared-events-from-the-marked-act` | Domain |
| LS5008 | `act-event-unraised` | `litespec/backend/domain#raise-the-declared-events-from-the-marked-act` | Domain |
| LS5009 | (generator) | `litespec/backend/dotnet/stack#generate-runtime-values-from-the-records` | Application, Infrastructure, WebApi |
| LS5010 | (generator) | `litespec/core/access#state-the-scope-of-every-permission` | Application, Infrastructure, WebApi |
| LS5011 | (generator) | `litespec/core/system#point-code-and-tests-up-to-records` | every layer |

CLI-only findings and their provisions: `use-case-act-uncalled` (`litespec/backend/domain#call-the-marked-act-from-each-command-handler`); `aggregate-unimplemented`, `reaction-unimplemented`, `job-unimplemented`, `external-system-unimplemented`, `use-case-unexposed`, `use-case-unreached` (`litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark`); `effect-unimplemented` (`litespec/core/system#declare-every-edge-between-records`); `mark-style-mixed` (`litespec/core/model#declare-one-mark-style`).

The CLI computes cross-project findings over one syntax forest of every backend project, resolving calls by name through marks read from source. Accepted limits: a call through target-typed `new()` or an interface the root does not declare, an event raised in a constructor of a root created by another method, and an event raised through a base-class helper.

## Severity

`Severity.Of(finding, effectiveStatus, phase, readerQualified)` in the core decides one of `blocking`, `reported`, `known`:

1. Covered by an open known gap or an in-date approved deviation: `known` (unchanged rule, `Suppression.Partition`).
2. A finding about a retired record: not reported.
3. Tier A: `reported`.
4. Tier S and the finding's subject has effective status `draft`: `reported`.
5. Tier S and `check.phase` is `frozen` and the finding is a proof finding (`proves-unconfirmed`, `use-case-without-scenario`, or a `mark-*` finding on a test, or LS4001): `reported`.
6. Tier S and the reader is not qualified: `reported`.
7. Otherwise: `blocking`. Tier G blocks unless known.

The CLI prints `draft` or `frozen` beside a reported S finding so a reader sees why it does not block. The analyzer maps `blocking` to error, `reported` to warning, `known` to no diagnostic. Precedence: suppression by known gap or deviation first (a deviation covers an analyzer finding when it lists the file in `code` or the subject in `subject`, as the CLI does), then `Severity.Of` sets the effective severity through `Diagnostic.Create(descriptor, location, effectiveSeverity, ...)`, and an `.editorconfig` entry for the rule id overrides both. Every LS5xxx descriptor defaults to `Error`. Tests cover draft and approved subjects with and without an `.editorconfig` lowering.

## Project File

```json
"marks": "xml",
"check": { "phase": "working" }
```

`marks`: `xml` (default) or `attribute`. `check.phase`: `working` (default) or `frozen`.

## Libraries and Hosts

| Assembly | Target | Owns |
|:---|:---|:---|
| `apps/model/src/LiteSpec.Model.Core` | netstandard2.0 | `Identifiers`, `RecordTypes`, `Finding` and its table, `IRecordFacts`, `Authority`, `Suppression`, `Severity`, `JsonText`, `RecordFiles`, `RecordIndex`, `ProjectSettings` (`marks`, `check.phase`), `Marks` (kinds, styles, expected record types and counts) |
| `apps/model/src/LiteSpec.Model.Roslyn` | netstandard2.0, Roslyn 4.14 | `MarkReader` (both styles, from syntax: `<implements>` and `<proves>` doc tags and `Implements`/`Proves` attributes), `KindReader` (the kind of a symbol), `CodeFacts` for one compilation |
| `apps/cli` | net10.0 | Reads records and code into the whole model; reads marks through `MarkReader`; runs every finding |
| `apps/analyzers/src/LiteSpec.Analyzers` | netstandard2.0 | LS5001 to LS5008 per compilation, reading records as `AdditionalFiles`; the source generator |
| `apps/analyzers/src/LiteSpec.Analyzers.CodeFixes` | netstandard2.0 | Code fixes: add the missing mark, one action per candidate record titled with the record title (candidates: records of the expected type whose name the folder and type name derive, for handlers, endpoints, event handlers and tests; no Fix All); correct a mistyped id, offering the three nearest records of the expected type by edit distance, one action each, no Fix All; convert a mark to the declared style (Fix All allowed); add `partial` (Fix All allowed); delete a hand-written value equal to the record (Fix All allowed) |

The analyzer package props find the project file with `$([MSBuild]::GetPathOfFileAbove(litespec.project.json, $(MSBuildProjectDirectory)))`, add it as an `AdditionalFile`, and add every `*.md` under its folder except `node_modules`, `bin` and `obj` (`LiteSpecRecords` overrides the glob). The analyzer reads `paths.docs` and `paths.behaviorDocs` from the project file and indexes only the Markdown files under them. With no project file in reach, or a docs root it cannot resolve, every LS5xxx rule and the generator stay silent; the CLI owns that configuration error. The index is cached per compilation, keyed by the content checksums of the files.

## Generated Code

The generator in the analyzer package writes, per project, into namespace `LiteSpec`, all `internal`:

- attribute style only: `ImplementsAttribute` (`AttributeTargets.Class | Interface | Struct | Method`, `AllowMultiple = true`) and `ProvesAttribute` (`AttributeTargets.Method`, `AllowMultiple = true`); `ProvesAttribute` implements xUnit v3 `ITraitAttribute` with trait `Scenario` when the compilation references xUnit v3, and is a plain attribute otherwise (trait filtering is xUnit v3 only);
- a value is written only where no member of that name is declared in source;
- for each `partial` domain event with an `event/` mark: `public const string Classification = "<event id>";`;
- for each `partial` rule exception with a `rule/` mark whose rule names a `refusal` result with a `code`: `public override string Code => "<code>";` (or a plain property when the base declares no virtual `Code`);
- `LiteSpecRegistry`: per assembly, a read-only map from each marked type in that assembly to its record ids, from each use-case id to its `access.grant`, and from each marked handler's message type to its use case; `UseCaseOf` answers a message type's use case and `GrantOf` its grant, both null when the type is no marked handler's message; a message two marked handlers serve with different use cases reports LS5011 and takes no entry;
- `LiteSpecPermissions`: in Application, Infrastructure and WebApi projects only, one `public const string` per `permission/` record, named by PascalCasing each kebab word of the permission name; two records producing one name stop generation with LS5009 naming both; one `LiteSpecPermission` entry per permission with its id, scope, group, whether it only reads, and its risks, every entry on `All` and one from `Of`, which throws for an unknown id; a permission whose first word is no known verb reads `false` and reports LS5010.

## Rename

`litespec rename` rewrites the identifier inside marks in both .NET styles and in frontend `litespec` constants, beside the record citations.

## Frontend Marks (phase 3)

`litespec model build` writes `litespec-records.ts` per frontend at `<frontend root>/src/lib/litespec-records.ts`, holding the union of use-case ids and the `LiteSpecMark` type. The TypeScript reader reads each module's `litespec` constant; an unmarked use-case folder is `mark-missing`.
