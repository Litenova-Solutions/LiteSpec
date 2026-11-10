# .NET Stack

**Stack:** dotnet
**Concern:** records

## Intent

A backend built on .NET declares the `dotnet` stack in `.litespec/project.yaml`. The declaration applies every page in this folder, and it tells the model builder to read C# code and test results in TRX or JUnit XML. This page states the declaration, the form each mark takes in C#, the runtime values generated from the records, what the stack reader reads from C# code, the result formats the builder reads, and the test projects, packages, and runners.

## Concepts

### Marks and Their Lookup

The [marks table](../../core/model.md#marks) owns which mark each code element carries. [Write Code for a Use Case](../../guide/write-code-for-a-use-case.md) maps each backend element to its folder, its mark, and its check.

## Standards

### Declare `dotnet` as the Backend Stack

**ID:** `litespec/backend/dotnet/stack#declare-dotnet-as-the-backend-stack`

**Strength:** required

**Statement:** A project whose backend runs on .NET MUST name `dotnet` in the `stacks` of `.litespec/project.yaml`.

**Rationale:** The `stacks` entry applies every page in this folder with its checks and analyzers, so an undeclared backend follows no .NET rule. It also selects the readers the model builder uses for C# handlers, events, refusal codes, and scenarios.

**Reviewed for:** Does the `stacks` array in `.litespec/project.yaml` list `dotnet` when the backend runs on .NET? Look at `.litespec/project.yaml`.

### Read .NET Code Through the Stack Reader

**ID:** `litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader`

**Strength:** required

**Statement:** The .NET stack reader MUST read marks in both styles, injected repositories, returned codes, dispatch calls, aggregate method calls, raised events, and test results.

**Rationale:** Each level S check reads one of those shapes. The mark links a code element to its record, so a handler, an event handler, an endpoint, an aggregate root and its methods, a domain event, a refusal type, and a test each name what they serve. The reader reads a mark in either style and reports the style the project did not declare. Injected repositories prove `changes`: a repository serves the aggregate its implementing class names in an `implements` mark, or, with no such mark, the aggregate its interface names. Returned codes prove declared results and refusals, and dispatch calls prove declared effects, so the reader reports `behavior-unrecorded`, `use-case-unimplemented`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, and `refusal-unimplemented` from code. Test marks with the latest test run prove each scenario, so the reader reports `proves-unreached` and `use-case-scenario-missing` from tests. The reader also reads the options classes that bind keyed settings, through each class's `SectionName` constant and properties, and the template store `stackConfig.dotnet.templateStore` names: one C# file whose top-level dictionary initializer keys are the template keys and whose texts write each parameter as `{{PascalCaseName}}`. Level S checks block where this reader is qualified and report elsewhere.

**Example:** The reimburse-expense handler carries `/// <implements>use-case/spending.reimburse-expense</implements>`, and the event handler class carries `/// <implements>reaction/spending.send-approver-reminder-on-expense-submitted</implements>`.

**Reviewed for:** Does the .NET reader read each shape the statement names? Look at the stack reader source for marks, injected repositories, returned codes, dispatch calls, aggregate method calls, raised events, and test results.

### Read Every Declared Web and Worker Host

**ID:** `litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host`

**Strength:** required

**Statement:** The .NET stack reader MUST read every project that `stackConfig.dotnet.webapi` and `stackConfig.dotnet.worker` name, whether each field holds one glob string or a list of glob strings.

**Rationale:** A backend can serve its use cases from more than one web host and run more than one worker host, and each host holds endpoints or jobs the other does not. Reading only the first host leaves the use cases that only the other hosts serve looking unimplemented, so each named host is a source root of the read. A host that is not a WebApi or worker project, such as a command-line host, is not a stack root.

**Example:** Claimwick serves member requests from one web host and partner requests from a second web host, so `webapi` lists both host projects and the reader reads the endpoints of each.

**Checked by:** `litespec model check` reports `reader-missing` (level S) for a named host folder that does not exist, and reads the handlers, endpoints and jobs of each named host that does.

### Report a Stack Root That Matches No Folder

**ID:** `litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder`

**Strength:** required

**Statement:** The model builder MUST report `reader-missing` for a configured .NET stack root that matches no folder, and for the default modules root, and the default Domain and Infrastructure roots under the layer-projects layout, when that default matches no folder in a project that selects the .NET reader.

**Rationale:** A root that matches no folder reads no source, so the records that source would implement are not gaps in the product: the tool read nothing where it expected code. Reporting the missing root beside the unimplemented findings lets a reader tell a reader gap from a product gap. The WebApi, worker and shared kernel defaults name hosts and a kernel that a valid project may omit, so a missing default for those roots stays silent and only a configured host or kernel path reports.

**Example:** A library project that keeps its code under a product folder and names no stack root reports `reader-missing` for the default modules root, instead of reporting every aggregate and use case as unimplemented.

**Checked by:** `litespec model check` reports `reader-missing` (level S) naming the field, the written pattern and the resolved folder that does not exist.

### Identify an Aggregate Root by Its Mark and State Record

**ID:** `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record`

**Strength:** required

**Applies to:** trace: yes

**Statement:** The .NET stack reader MUST recognize a type that carries an `aggregate` mark and a `State` property backed by an abstract state record as an aggregate root.

**Rationale:** A project that claims Implementation with `own` keeps its own code layout and its own aggregate base, so a root that follows the LiteSpec base type is not the only root the code holds. The aggregate mark names the record the type implements and the state record states its lifecycle, and together they identify the root without the base type. A type with only the mark, or only the state record, does not state both facts, so it stays unrecognized. This provision states the traced reading of the aggregate root beside `litespec/backend/dotnet/domain#use-the-project-aggregate-root-contract`, which governs the full contract.

**Example:** The library loan type carries `/// <implements>aggregate/lending.loan</implements>` and a `LoanState State` property backed by the abstract `LoanState` record, so the reader reads it as the loan aggregate root and reads its public methods as aggregate methods.

**Checked by:** `litespec model check` reports `aggregate-unimplemented` (level S) for an aggregate record that no type with its mark and its state record, and no type deriving the aggregate root base, implements.

### Write Each Mark in the Declared Style

**ID:** `litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style`

**Strength:** required

**Statement:** A .NET mark MUST be an `implements`, `proves`, or `follows` documentation tag when `marks` is `xml`, and an `Implements`, `Proves`, or `Follows` attribute when `marks` is `attribute`.

**Rationale:** The `xml` style keeps business code free of attributes that are not of the business: the mark is a documentation tag, read by the stack reader and by the analyzers, which need the documentation file every LiteSpec project already generates. The `attribute` style carries the mark into metadata, so it survives compilation and reflection can read it. Its attribute types are generated into each project as internal types by the analyzer package, so no runtime package is referenced, and Domain stays free of outer-layer dependencies. A code element takes one mark per record; a code fix converts a mark between the styles. This provision states the .NET form of `litespec/core/model#declare-one-mark-style`.

**Example:** In the `xml` style:

```csharp
/// <summary>Reimburses an approved expense.</summary>
/// <implements>use-case/spending.reimburse-expense</implements>
/// <follows>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</follows>
internal sealed class ReimburseExpenseCommandHandler(IExpenseRepository expenseRepository)
```

In the `attribute` style:

```csharp
[Implements("use-case/spending.reimburse-expense")]
[Follows("litespec/backend/application#keep-command-handlers-to-load-call-and-stage")]
internal sealed class ReimburseExpenseCommandHandler(IExpenseRepository expenseRepository)
```

**Checked by:** `litespec model check` reports `mark-style-invalid` (level S), and `dotnet build` reports `LS5004`, for a mark written in the style the project did not declare.

### Generate Runtime Values from the Records

**ID:** `litespec/backend/dotnet/stack#generate-runtime-values-from-the-records`

**Strength:** required

**Statement:** A domain event `Classification` and a domain exception `Code` MUST equal the values their records state.

**Rationale:** The record states each identifier and refusal code once, so a second hand-written copy in code is a second source that drifts. With the analyzer package installed, its source generator writes the value into the `partial` type from the mark and the record: the event `Classification` is the event identifier, and the exception `Code` is the `code` of the refusal result its rule names. Without the analyzer package, the value stays hand-written and the check compares it with the record.

**Example:** The event carries its mark and the generator writes its classification:

```csharp
/// <implements>event/expense.submitted</implements>
public sealed partial record ExpenseSubmittedEvent(ExpenseId ExpenseId, Money Amount) : IDomainEvent;
```

The generated part holds `public const string Classification = "event/expense.submitted";`.

**Checked by:** `litespec model check` reports `runtime-value-drift` (level S) for a hand-written value that differs from its record, and `dotnet build` reports `LS5005` for the same value.

### Generate the Registry, Permissions, and Roles from the Records

**ID:** `litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records`

**Strength:** required

**Statement:** Code MUST read a use case's identity, its grant, a permission's facts, and a role's permission set from the classes the analyzer package generates from the marks and the records, never from a hand-written copy.

**Rationale:** Each of these facts is written once, on a record or in a mark, and a hand-written table of them is a second source that drifts the first time a record changes. The generator writes them per project: `LiteSpecRegistry` answers a message type's use case (`UseCaseOf`), a marked type's use case (`UseCaseOfType`), a message type's grant (`GrantOf`), and in an API project an operation name's use case (`UseCaseOfOperation`); `LiteSpecPermissions` holds one constant and one entry per `permission/` record; and `LiteSpecRoles` holds each role's scope and the permissions its record allows. In the module layout the modules project generates all three, and the host beside it reads them from there; the shared kernel project generates none, and the host generates only `LiteSpecHostOperations`, which answers `UseCaseOfOperation` for the endpoints the host itself holds. A role that names a permission no record declares, and two records that generate one name, stop at build time.

**Example:** When Claimwick's `role/finance` record gains a permission, `LiteSpecRoles.Of("role/finance").Allows` carries it at the next build, and `LiteSpecRegistry.GrantOf(typeof(ReimburseExpenseCommand))` answers the reimbursement grant, with no hand-written table to update.

**Checked by:** `dotnet build` reports `LS5009` for two records generating one constant name, `LS5011` for a message two marked handlers serve with different use cases, and `LS5012` for a role allowing a permission no record declares.

### Declare Marked Events and Exceptions as Partial Types

**ID:** `litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types`

**Strength:** required

**Statement:** A project that references the analyzer package MUST declare every marked domain event and rule exception as a `partial` type.

**Rationale:** The source generator adds the runtime value as a second part of the type, and a type that is not `partial` cannot take one. The generator writes a value only where no member of that name is declared in source, so a hand-written value is compared with the record rather than duplicated. A code fix adds `partial` and deletes a hand-written value that equals the record.

**Example:** `public sealed partial record ExpenseSubmittedEvent(...)` takes its generated `Classification`; the same record without `partial` is reported.

**Checked by:** `dotnet build` reports `LS5006` for a marked domain event or rule exception that is not `partial`, in every project that references the analyzer package.

### Write Test Results the Model Builder Reads

**ID:** `litespec/backend/dotnet/stack#write-test-results-the-model-builder-reads`

**Strength:** required

**Statement:** A .NET test run MUST write its results as TRX or JUnit XML under `paths.testResults`.

**Rationale:** The model builder computes verified status from TRX and JUnit XML only. A run written in another format, or outside `paths.testResults`, proves no scenario even when every test passes. A JUnit `testcase` proves a Behavior scenario without a `proves` mark by carrying the scenario id in a `litespec.proves` property, one property per scenario or several ids separated by commas in one value; the case proves each id it names with the same rules as a `proves` mark on a test method.

**Reviewed for:** Which test runs write a result outside TRX or JUnit XML, or outside `paths.testResults`? Look at the test run output beside `paths.testResults` in `.litespec/project.yaml`.

### Test with xUnit v3 on Microsoft.Testing.Platform

**ID:** `litespec/backend/dotnet/stack#test-with-xunit-v3-on-microsoft-testing-platform`

**Strength:** required

**Applies to:** layer: proof

**Statement:** A .NET test project MUST reference xUnit v3 and run on Microsoft.Testing.Platform, with `global.json` naming that runner.

**Rationale:** The .NET 10 `dotnet test` command refuses the VSTest mode for xUnit v3 projects, so each test project runs on the platform runner, and the root `global.json` states that choice once for every project beneath it. A project that runs on another runner reports its results in a shape the model builder does not read.

**Reviewed for:** Which test project references no xUnit v3 package, or runs on a runner other than Microsoft.Testing.Platform? Look at each test project file and at `global.json`.

### Pin the Test Packages in the Manifest

**ID:** `litespec/backend/dotnet/stack#pin-the-test-packages-in-the-manifest`

**Strength:** required

**Applies to:** layer: proof

**Statement:** A .NET test project MUST reference each test package at the version `litespec.manifest.yaml` pins.

**Rationale:** A test package that moves to a newer version changes what the tests prove without any change to the code under test. The manifest is the one place the release names its pins, so the test SDK, the Microsoft.Testing.Platform extensions, the xUnit runner, the property-test and mock libraries, and the PostgreSQL container each resolve to the version the release reviewed.

**Reviewed for:** Which test project references a test package at a version the manifest does not pin? Look at each test project file beside the package pins in `litespec.manifest.yaml`.

### Start the Integration Database in a Container

**ID:** `litespec/backend/dotnet/stack#start-the-integration-database-in-a-container`

**Strength:** required

**Applies to:** layer: proof

**Statement:** The .NET integration test project MUST start the pinned PostgreSQL in a Testcontainers container.

**Rationale:** A container from the pinned image gives every test run the database version the release runs, and it starts and stops with the test run, so no shared server carries state from one run to the next. The fixture that owns the container is the one `litespec/backend/testing#share-one-production-faithful-integration-harness` names.

**Reviewed for:** Which integration test reaches a database that no container started for the run? Look at the integration fixture and the Testcontainers package reference in the integration test project.

## Conventions

### Run Mutation Testing with Stryker.NET

**ID:** `litespec/backend/dotnet/stack#run-mutation-testing-with-stryker-net`

**Strength:** default

**Applies to:** layer: proof

**Default:** Run the mutation gate with the Stryker.NET runner that the root tool manifest pins.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Stryker.NET is the .NET runner for the mutation gate that `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines` names, and the root tool manifest pins its version, so a developer and CI run the same mutants against the same tool.

**Reviewed for:** Does the mutation gate run Stryker.NET at the version the root tool manifest pins? Look at `.config/dotnet-tools.json` and the mutation job's command.
