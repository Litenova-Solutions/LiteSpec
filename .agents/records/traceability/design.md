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
| Domain | Public aggregate root method that changes state | `AggregateMethod` | `use-case/` | one |
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
| Tests | Test method | `Test` | `scenario/`, `rule/`, `requirement/` (quality), `decision/` (technical), `external-system/`, `integration-event/`, `litespec/` | one or more |

Kinds are recognized from the code, never from the mark: a handler implements `ICommandHandler<,>` or `IQueryHandler<,>` (any arity, by interface name); an event handler implements `IEventHandler<>` or `IDomainEventReaction<>`; a job derives from `BackgroundService` or implements `IHostedService` and is named `*Job` or `*Worker`, or is a class in Application or Infrastructure carrying a `job/` mark; an aggregate root derives from `AggregateRoot<>`; an aggregate method is a public instance or static method on an aggregate root that raises a domain event (calls `RaiseDomainEvent`, directly or through a private method of the same type); a domain event implements `IDomainEvent`; a rule exception derives from `DomainException` and is not abstract; an endpoint implements `IEndpoint` or is named `*Endpoint` in WebApi; a port is an interface in Application or Application.Abstractions that carries an `external-system/` mark; an adapter is a class implementing a marked port; a test method carries `[Fact]`, `[Theory]`, `[Scenario]`, `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[TestMethod]` or `[DataTestMethod]`.

A port and a domain service are recognized only by their mark, so an unmarked port is caught only by `external-system-unimplemented`. A job host is recognized by its hosted-service shape or by its mark, so a plain class carrying a `job/` mark is its job's host. An adapter is recognized by implementing a marked port, so an unmarked adapter carries `mark-missing`. An aggregate method is recognized by raising an event, so a state-changing method that raises nothing is recognized only once it carries a mark. Types in test projects are never domain events, jobs or endpoints. A minimal-API route with no endpoint type is not a kind; `use-case-unexposed` catches the use case from the top.

The same record id twice on one element counts as two marks for `mark-count`.

### Test Proof Targets

A test proves the lowest record it verifies, from the seven kinds the marks table lists for it. A `requirement/` target proves only a quality requirement; a `decision/` target proves only a technical decision, a decision under the project's implementation decisions folder; a `litespec/` target resolves as a `follows` target does. `mark-unresolved` reports a target of a kind a test does not take, a functional or business requirement, a product decision, or an unknown provision. Only scenario and rule proofs confirm through the test body and feed `proves-unconfirmed` and verified status; a passing test proving a quality requirement, a technical decision, an external system, or an integration event marks that record `tested`, written per id in the model output beside the other computed facts. Tested never verifies a use case.

## Findings

Every finding has one code, one tier, and at most one analyzer rule id. The core library owns this table.

| Code | Tier | Rule | Meaning |
|:---|:---|:---|:---|
| `mark-missing` | S | LS5001 | A code element the marks table names carries no mark. |
| `mark-unresolved` | S | LS5002 | A mark names no record, or a record of a type its code element does not take, or sits on a type or method that takes no mark. |
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
| LS5012 | (generator) | `litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records` | Domain, Application, Infrastructure, WebApi |

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
| `apps/analyzers/src/LiteSpec.Analyzers.CodeFixes` | netstandard2.0 | Code fixes: add the missing mark, one action per candidate record titled with the record title (candidates: records of the expected type whose name the folder and type name derive, for handlers, endpoints, event handlers and tests, with scenarios only for a test; no Fix All); correct a mistyped id, offering the three nearest records of the expected type by edit distance, one action each, no Fix All; convert a mark to the declared style (Fix All allowed); add `partial` (Fix All allowed); delete a hand-written value equal to the record (Fix All allowed) |

The analyzer package props find the project file with `$([MSBuild]::GetPathOfFileAbove(litespec.project.json, $(MSBuildProjectDirectory)))`, add it as an `AdditionalFile`, and add every `*.md` under its folder except `node_modules`, `bin` and `obj` (`LiteSpecRecords` overrides the glob). The analyzer reads `paths.docs` and `paths.behaviorDocs` from the project file and indexes only the Markdown files under them. With no project file in reach, or a docs root it cannot resolve, every LS5xxx rule and the generator stay silent; the CLI owns that configuration error. The index is cached per compilation, keyed by the content checksums of the files.

## Generated Code

The generator in the analyzer package writes, per project, into namespace `LiteSpec`, all `internal`:

- attribute style only: `ImplementsAttribute` (`AttributeTargets.Class | Interface | Struct | Method`, `AllowMultiple = true`) and `ProvesAttribute` (`AttributeTargets.Method`, `AllowMultiple = true`); `ProvesAttribute` implements xUnit v3 `ITraitAttribute` with trait `Scenario` when the compilation references xUnit v3, and is a plain attribute otherwise (trait filtering is xUnit v3 only);
- a value is written only where no member of that name is declared in source;
- for each `partial` domain event with an `event/` mark: `public const string Classification = "<event id>";`;
- for each `partial` rule exception with a `rule/` mark whose rule names a `refusal` result with a `code`: `public override string Code => "<code>";` (or a plain property when the base declares no virtual `Code`);
- `LiteSpecRegistry`: per assembly, a read-only map from each marked type in that assembly to its record ids, from each use-case id to its `access.grant`, and from each marked handler's message type to its use case; `UseCaseOf` answers a message type's use case and `GrantOf` its grant, both null when the type is no marked handler's message; a message two marked handlers serve with different use cases reports LS5011 and takes no entry;
- `LiteSpecPermissions`: in Domain, Application, Infrastructure and WebApi projects, one `public const string` per `permission/` record, named by PascalCasing each kebab word of the permission name; two records producing one name stop generation with LS5009 naming both; one `LiteSpecPermission` entry per permission with its id, scope, group, whether it only reads, and its risks, every entry on `All` and one from `Of`, which throws for an unknown id; a permission whose first word is no known verb reads `false` and reports LS5010.
- `LiteSpecRoles`: wherever `LiteSpecPermissions` is generated, one `public const string` per `role/` record named as permission constants are (LS5009 covers a collision), one `LiteSpecRole` entry per role with its id, scope and the permission ids its record allows in record order, `All` sorted by constant name, and `Of`, which throws `ArgumentException` for an unknown id; a role allowing a permission no record declares reports LS5012 (Error) and is still emitted;
- `LiteSpecRegistry.UseCaseOfType(Type)`: the `use-case/` id the type itself carries in an `implements` mark, from `MarkedTypes`, or null; in WebApi projects only, `UseCaseOfOperation(string)`: the use case each marked endpoint implements, keyed by the PascalCased use-case name (use-case names are unique project-wide, so each operation name maps to one use case);

## Rename

`litespec rename` rewrites the identifier inside marks in both .NET styles and in frontend `litespec` constants, beside the record citations.

## Frontend

The provisions are `litespec/core/behavior#give-every-page-its-screen-record` and its neighbours, `litespec/frontend/structure#mark-each-route-with-its-screen`, `litespec/frontend/web/structure#generate-the-records-file-into-the-frame`, `litespec/frontend/testing#mark-the-scenario-a-browser-test-proves`, `#reach-a-screen-that-offers-the-scenarios-use-case`, `#assert-the-result-a-scenario-ends-in`, `#prove-each-scenario-at-one-boundary`, `#name-the-journey-a-journey-test-walks`, and `litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove`.

### Records

A `screen` record (Behavior) per page, at `docs/behavior/screens/<entry-point>.<name>.md`, id `screen/<entry-point>.<name>`. Fields: `entryPoint` (required, one `browser` or `device` entry point), exactly one of `address` (route template) and `host` (a screen, for an overlay), `offers` (use cases, optional), `serves` (journey steps, optional), `floorplan` (optional, a floorplan in the design contract). The `<name>` follows the one-to-six-word rule other names follow. Body is `## Summary` only; a navigation screen states there that it offers no use case. Results shown, outgoing links and copy are derived, never written.

### Marks

| Element | Mark | Names |
|:---|:---|:---|
| Web use-case module folder | `litespec.ts`: `export const litespec = { implements: "use-case/..." } as const satisfies LiteSpecMark;` | one use case |
| Web route entry (Svelte `+page.ts` beside `+page.svelte`, React route file) | the same constant, `implements: "screen/..."` | one screen |
| Blazor `@page` component, Android destination activity or fragment class | the .NET mark style the project declares | one screen |
| Browser test | `test(title, proves("scenario/..."), fn)`; `proves` returns `{ annotation: [{ type: "proves", description: id }] }` | the lowest record it verifies |
| Journey test | also `{ type: "journey", description: "journey/..." }`, never a proof | |

Layouts, error pages, and route groups are not route entries and carry no mark, as each stack page states. For a module folder at `{module}/reads/{use-case}/`, the expected mark names `use-case/<module>.<use-case>` from the module and use-case segments alone.

`litespec model build` writes `<frame>/litespec-records.ts` per frontend (`src/app/` React, `src/lib/app/` Svelte) with `UseCaseId` and `ScreenId` unions of that frontend's own entry points only, the `LiteSpecMark` type, and the `proves` helper typed to the proof-target unions.

### Reader

A TypeScript reader in `apps/cli/fe-reader/`, bundled at pack time to one `.mjs` with `typescript` (below 7) and `svelte`, run as `node <reader>` once per build or check. The facts protocol is one JSON document each way, versioned. The request carries the protocol version, the per-frontend roots (routes, modules, frame, and test roots), the path aliases, the generated-client location, and the record-id sets (`UseCaseId`, `ScreenId`, and the proof targets). The response carries the protocol version, the reader, and the TypeScript and Svelte versions from the handshake, then facts only: module marks, route marks with addresses, per-route calls, and tests with their annotations, `goto` targets, and asserted code literals, plus diagnostics. A version mismatch produces the single `reader-unavailable` finding for that frontend. Parse only: `ts.createSourceFile` per file, `svelte/compiler` `parse` with `modernAst: true`. Per-route `calls` follow imports only within the route's own folder and the use-case module folders it imports directly, through their `litespec.ts` marks, collecting generated-client operation calls and mapping each operation through the endpoint's mark to its use case; the frame and shared packages are not followed, and an operation resolving to two use cases is a reader diagnostic. Facts are cached under `.model/` by file content hash, reader version, and record-id set hash. The regex reader (`TypeScriptSource.cs`, `FrontendMarks.cs` and siblings) is replaced; `ScreenModel`, `CodeModel` and `ScenarioModel` stay.

A finding records the reader that produced its facts, and `Severity.Of` takes that reader's qualification. Qualification is per reader: the TypeScript reader qualifies once its golden corpus (Svelte, React and Playwright files with expected facts) passes and its handshake matches, and is unqualified when Node is absent, the handshake mismatches or the reader fails, which reports one `reader-unavailable` finding. The Roslyn reader, including Blazor and Android screens, stays qualified.

### Findings

| Code | Tier | Meaning |
|:---|:---|:---|
| `mark-missing`, `mark-unresolved`, `mark-count` | S | extended to module folders, route modules and browser tests |
| `screen-unimplemented` | S | no route carries the screen's mark |
| `behavior-unrecorded` | S | (route case) a route with no mark or a mark naming no screen |
| `screen-calls-mismatch` | S | `offers` differs from the computed calls |
| `screen-serves-mismatch` | G | a served step no offered use case serves |
| `step-unscreened` | G | a browser or device step no screen serves; its pair `step-unserved` names a step no use case serves |
| `use-case-unreached` | A | per frontend, through each screen's entry point |
| `result-unshown` | A | a reachable refusal with no sentence and no asserting test |
| `proves-duplicated` | A | a scenario with `proves` marks on more than one test |
| `proves-unconfirmed` | S | extended to browser tests: no `goto` reaches a screen offering the use case, or no assertion in the same test holds the refusal code or the success result's declared text or code |
| `reader-unavailable` | S | the TypeScript reader could not run |

Journey coverage is computed: a step is exercised when a passing test proved a scenario of a use case that serves it.

## Technical Marks

A technical class names the Behavior record it serves in an `implements` mark, or why it is built as it is in a `follows` mark (`litespec/core/system#point-every-technical-class-up-to-its-record-or-choice`, `litespec/core/model#record-each-technical-choice-as-a-decision`).

| Style | Form |
|:---|:---|
| `xml` | `/// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>` |
| `attribute` | `[Follows("decision/0002-store-expenses-as-documents")]` |

| Layer | Code element | Kind | Mark names | Count |
|:---|:---|:---|:---|:---|
| Application, Application.Abstractions, Infrastructure, WebApi | Technical class | `Technical` | `implements`: any Behavior record type (`aggregate/`, `use-case/`, `reaction/`, `job/`, `external-system/`, `event/`, `rule/`, `notification/`, `setting/`, `integration-event/`); `follows`: `litespec/` provision, `decision/`, `deviation/` | one or more marks in total, of either verb |

A technical class is a top-level class (not a record, struct, interface, enum or delegate) in a project whose layer is Application, Application.Abstractions, Infrastructure or WebApi, that takes no other kind and declares at least one ordinary method (not only constructors, properties, fields or operators), or carries an `implements` mark naming a `setting/` record (an options class that binds a setting with properties alone). Excluded: generated code (a file the compiler treats as generated, or a class carrying `[GeneratedCode]`), classes deriving from a type named `Migration` or `ModelSnapshot`, test projects, nested types. Partial parts count as one class; a mark on any part counts.

`follows` is a third mark verb beside `implements` and `proves`. Any type or method of any kind may carry `follows` marks, and a `follows` mark is never stray. A technical class needs at least one mark of either verb; its `implements` marks may name any Behavior record type. A `follows` mark never counts toward `mark-count` of another kind.

A `follows` target resolves when it is:

- a `litespec/<page>#<slug>` provision in the release the tool carries, on a page the project's stacks and extensions apply (CLI); the analyzer checks existence only, from the provision ids the package embeds at pack time;
- a `decision/` or `deviation/` record that is not retired.

Otherwise the mark reports `mark-unresolved` (LS5002), subject the element.

| Code | Tier | Rule | Meaning |
|:---|:---|:---|:---|
| `decision-unfollowed` | S | | A decision under `docs/implementation/decisions/` that no code element follows. CLI only. |
| `operation-use-case-mismatch` | S | | An operation in the committed contract carries no `x-litespec-use-case`, or names another use case than its endpoint's mark. CLI only. |
| `constituent-orphaned` | A | | An unmarked Domain type sits under no aggregate folder and outside the shared kernel. CLI only. |

`decision-decides-nothing` covers product decisions only (decisions outside `docs/implementation/decisions/`). The generator writes `FollowsAttribute` (`AttributeTargets.Class | Interface | Struct | Method`, `AllowMultiple = true`) in the attribute style. `LiteSpecRegistry` does not list `follows` targets. `litespec rename` rewrites `follows` ids as it rewrites `implements` ids.
