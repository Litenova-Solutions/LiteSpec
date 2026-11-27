# .NET Stack

**Stack:** dotnet

**Concern:** records

This page states what the `dotnet` backend stack applies and how its reader links C# code with records. It is for developers who set up a .NET backend and need the model builder to see their code. A project naming `dotnet` in the `stacks` of its `.litespec/project.yaml` gets every page in this folder with its checks, and the reader follows each mark to its record.

## Concepts

### Marks and Their Lookup

A mark is the link between a code element and the record it implements, proves, or follows. A handler, an endpoint, an aggregate root and its methods, a domain event, a refusal type, and a test each name what they serve through a mark. The reader reads a mark in either declared style and reports the style the project did not declare.

Claimwick's reimburse-expense handler carries `/// <implements>use-case/spending.reimburse-expense</implements>`. Its approver-reminder event handler carries `/// <implements>reaction/spending.send-approver-reminder-on-expense-submitted</implements>`.

### Results the Reader Proves

Injected repositories prove `writes`: a repository serves the aggregate its implementing class names in an `implements` mark, or, with no such mark, the aggregate its interface names. Returned codes prove declared results and refusals, dispatch calls prove declared effects, and test marks with the latest test run prove each scenario. The builder computes verified status from TRX or JUnit XML written under `paths.testResults`: a JUnit `testcase` proves a Behavior scenario by carrying the scenario id in a `litespec.proves` property, one property per scenario or several ids separated by commas in one value.

### Hosts and Roots

A backend serves its use cases from more than one web host and runs more than one worker host. Each host holds endpoints or jobs absent from the other hosts, so the reader reads every project that `stackConfig.dotnet.webapi` and `stackConfig.dotnet.worker` name, whether each field holds one glob string or a list of glob strings. A host that is not a WebApi or worker project, such as a command-line host, is not a stack root.

A root matching no folder reads no source, so the builder reports `reader-missing` beside the unimplemented findings instead of letting them read as product gaps. A library project keeping its code under a product folder with no stack root configured reports `reader-missing` for the default modules root instead of reporting every aggregate and use case as unimplemented. The WebApi, worker, and shared kernel defaults name hosts and a kernel a valid project omits, so a missing default for those roots stays silent.

### Aggregate Roots Without the Base Type

A project claiming Implementation with its own layout and its own aggregate base keeps roots the LiteSpec base type does not describe. The reader still finds them: a type carrying an `aggregate` mark and a `State` property backed by an abstract state record is an aggregate root. A type with only the mark, or only the state record, does not state both facts, so it stays unrecognized. The library loan type carries `/// <implements>aggregate/lending.loan</implements>` with a `LoanState State` property backed by the abstract `LoanState` record, so the reader treats it as the loan root and reads its public methods as aggregate methods.

### Generated Values and Tables

The record states each identifier and refusal code once, and the analyzer package writes the value into the `partial` type from the mark and the record. The event `Classification` is the event identifier, and the exception `Code` is the `code` of the refusal result its rule names. Without the analyzer package, the value stays hand-written, and the check compares it with the record.

Each generated table has one source: `LiteSpecRegistry` answers a message type's use case, a marked type's use case, a message type's grant, and an operation name's use case. `LiteSpecPermissions` holds one constant and one entry per `permission/` record, and `LiteSpecRoles` holds each role's scope and the permissions its record allows. When the team adds a permission to Claimwick's `role/finance` record, `LiteSpecRoles.Of("role/finance").Permissions` carries it at the next build with no hand-written table to update.

### Test Runners and the Mutation Gate

Test projects reference xUnit v3 and run on Microsoft.Testing.Platform, with the root `global.json` naming that runner. Each test package resolves at the version `litespec.manifest.yaml` pins: the test SDK, the Testing.Platform extensions, the xUnit runner, the property-test and mock libraries, and the PostgreSQL container. The integration test project starts the pinned PostgreSQL in a Testcontainers container that starts and stops with the test run, so no shared server carries state from one run to the next. The mutation gate runs the Stryker.NET runner pinned in the root tool manifest, so a developer and CI run the same mutants against the same tool.

## Standards

### Read .NET Code Through the Stack Reader

**ID:** `litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader`

**Strength:** required

**Statement:** The .NET stack reader MUST read marks in both styles, injected repositories, returned codes, dispatch calls, aggregate method calls, raised events, and test results.

**Rationale:** Each check at level S reads one of those shapes. The mark links a code element to its record: a handler, an event handler, an endpoint, an aggregate root and its methods, a domain event, a refusal type, and a test each name what they serve. The reader reads a mark in either style and reports the style the project did not declare.

Injected repositories prove `writes`: a repository serves the aggregate its implementing class names in an `implements` mark, or, with no such mark, the aggregate its interface names. Returned codes prove declared results and refusals, and dispatch calls prove declared effects. The reader reports `behavior-unrecorded`, `use-case-unimplemented`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, and `refusal-unimplemented` from code.

Test marks with the latest test run prove each scenario, and the reader reports `proves-unreached` and `use-case-scenario-missing` from tests. The reader also reads the options classes binding keyed settings, through each class's `SectionName` constant and properties. It reads the template store `stackConfig.dotnet.templateStore` names: one C# file whose top-level dictionary initializer keys are the template keys and whose texts write each parameter as `{{PascalCaseName}}`. Checks at level S block where this reader is qualified and report elsewhere.

**Example:** Claimwick's reimburse-expense handler carries `/// <implements>use-case/spending.reimburse-expense</implements>`, and its approver-reminder event handler carries `/// <implements>reaction/spending.send-approver-reminder-on-expense-submitted</implements>`.

**Reviewed for:** Does the .NET reader read every shape the statement names? Check the stack reader source for marks, injected repositories, returned codes, dispatch calls, aggregate method calls, raised events, and test results.

### Read Every Declared Web and Worker Host

**ID:** `litespec/packs/dotnet/stack#read-every-declared-web-and-worker-host`

**Strength:** required

**Statement:** The .NET stack reader MUST read every project that `stackConfig.dotnet.webapi` and `stackConfig.dotnet.worker` name, whether each field holds one glob string or a list of glob strings.

**Rationale:** A backend serves its use cases from more than one web host and runs more than one worker host. Each host holds endpoints or jobs the others do not. Reading only the first host leaves the use cases that only the other hosts serve looking unimplemented.

Each named host is therefore a source root of the read. A host that is not a WebApi or worker project, such as a command-line host, is not a stack root.

**Example:** Claimwick serves member requests from one web host and partner requests from a second, so `webapi` lists both host projects and the reader reads the endpoints of each.

**Checked by:** `litespec model check` reports `reader-missing` (level S) for a named host folder that does not exist, and reads the handlers, endpoints and jobs of each named host that does.

### Report a Stack Root That Matches No Folder

**ID:** `litespec/packs/dotnet/stack#report-a-stack-root-that-matches-no-folder`

**Strength:** required

**Statement:** The model builder MUST report `reader-missing` for a configured .NET stack root that matches no folder, and for the default modules root, when that default matches no folder in a project that selects the .NET reader.

**Rationale:** A root matching no folder reads no source, so the records that source would implement are not gaps in the product. The tool read nothing where it expected code. Reporting the missing root beside the unimplemented findings lets a reader tell a reader gap from a product gap. The WebApi, worker, and shared kernel defaults name hosts and a kernel a valid project omits, so a missing default for those roots stays silent. Only a configured host or kernel path reports a finding.

**Example:** A library project keeping its code under a product folder with no stack root configured reports `reader-missing` for the default modules root instead of reporting every aggregate and use case as unimplemented.

**Checked by:** `litespec model check` reports `reader-missing` (level S) naming the field, the written pattern and the resolved folder that does not exist.

### Identify an Aggregate Root by Its Mark and State Record

**ID:** `litespec/packs/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record`

**Strength:** required

**Applies to:** trace: yes

**Statement:** The .NET stack reader MUST recognize a type that carries an `aggregate` mark and a `State` property backed by an abstract state record as an aggregate root.

**Rationale:** A project claiming Implementation with its own layout and its own aggregate base keeps roots the LiteSpec base type does not describe. The aggregate mark names the record the type implements, and the state record states its lifecycle. Together they identify the root without the base type.

A type with only the mark, or only the state record, does not state both facts, so it stays unrecognized. This provision states the `trace: yes` reading of the aggregate root beside the full contract the [domain page](domain.md) states.

**Example:** The library loan type carries `/// <implements>aggregate/lending.loan</implements>` with a `LoanState State` property backed by the abstract `LoanState` record, so the reader treats it as the loan aggregate root and reads its public methods as aggregate methods.

**Checked by:** `litespec model check` reports `aggregate-unimplemented` (level S) for an aggregate record that no type with its mark and its state record, and no type deriving the aggregate root base, implements.

### Write Each Mark in the Declared Style

**ID:** `litespec/packs/dotnet/stack#write-each-mark-in-the-declared-style`

**Strength:** required

**Statement:** A .NET mark MUST be an `implements`, `proves`, or `follows` documentation tag when `marks` is `xml`, and an `Implements`, `Proves`, or `Follows` attribute when `marks` is `attribute`.

**Rationale:** The `xml` style keeps business code free of attributes that are not of the business. The mark is a documentation tag read by the stack reader and the analyzers, which need the documentation file every LiteSpec project already generates. The `attribute` style carries the mark into metadata, so compilation keeps it and reflection reads it.

Its attribute types are generated into each project as internal types by the analyzer package, so no runtime package is referenced. Domain stays free of outer-layer dependencies. A code element takes one mark per record, and a code fix converts a mark between the styles.

**Example:** In the `xml` style, Claimwick's reimbursement handler carries its marks as documentation tags:

```csharp
/// <summary>Reimburses an approved expense.</summary>
/// <implements>use-case/spending.reimburse-expense</implements>
/// <follows>litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage</follows>
internal sealed class ReimburseExpenseCommandHandler
```

In the `attribute` style, the same handler carries them as attributes:

```csharp
[Implements("use-case/spending.reimburse-expense")]
[Follows("litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage")]
internal sealed class ReimburseExpenseCommandHandler
```

**Checked by:** `litespec model check` reports `mark-style-invalid` (level S), and `dotnet build` reports `LS5004`, for a mark written in the style the project did not declare.

### Generate Runtime Values from the Records

**ID:** `litespec/packs/dotnet/stack#generate-runtime-values-from-the-records`

**Strength:** required

**Statement:** A domain event `Classification` and a domain exception `Code` MUST equal the values their records state.

**Rationale:** The record states each identifier and refusal code once. A second hand-written copy in code is a second source that drifts. With the analyzer package installed, its source generator writes the value into the `partial` type from the mark and the record.

The event `Classification` is the event identifier, and the exception `Code` is the `code` of the refusal result its rule names. Without the analyzer package, the value stays hand-written, and the check compares it with the record.

**Example:** Claimwick's submitted-expense event carries its mark, and the generator writes its classification:

```csharp
/// <implements>domain-event/expense.submitted</implements>
public sealed partial record ExpenseSubmittedDomainEvent(ExpenseId ExpenseId, Money Amount) : IDomainEvent;
```

The generated part holds `public const string Classification = "domain-event/expense.submitted";`.

**Checked by:** `litespec model check` reports `runtime-value-drift` (level S) for a hand-written value that differs from its record, and `dotnet build` reports `LS5005` for the same value.

### Generate the Registry, Permissions, and Roles from the Records

**ID:** `litespec/packs/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records`

**Strength:** required

**Statement:** Code MUST read a use case's identity, its grant, a permission's facts, and a role's permission set from the classes the analyzer package generates from the marks and the records, never from a hand-written copy.

**Rationale:** Each of these facts is written once, on a record or in a mark. A hand-written table of them is a second source that drifts the first time a record changes. The generator writes them per project.

`LiteSpecRegistry` answers a message type's use case (`UseCaseOf`), a marked type's use case (`UseCaseOfType`), a message type's grant (`GrantOf`), and in an API project an operation name's use case (`UseCaseOfOperation`). `LiteSpecPermissions` holds one constant and one entry per `permission/` record, and `LiteSpecRoles` holds each role's scope and the permissions its record allows. A role listing a permission missing from the records, and two records generating one name, stop at build time.

**Example:** When the team adds a permission to Claimwick's `role/finance` record, `LiteSpecRoles.Of("role/finance").Permissions` carries it at the next build with no hand-written table to update, and `LiteSpecRegistry.GrantOf(typeof(ReimburseExpenseCommand))` answers the reimbursement grant.

**Checked by:** `dotnet build` reports `LS5009` for two records generating one constant name, `LS5011` for a message two marked handlers serve with different use cases, and `LS5012` for a role allowing a permission no record declares.

### Declare Marked Events and Exceptions as Partial Types

**ID:** `litespec/packs/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types`

**Strength:** required

**Statement:** A project that references the analyzer package MUST declare every marked domain event and rule exception as a `partial` type.

**Rationale:** The source generator adds the runtime value as a second part of the type. A type that is not `partial` takes no second part. The generator writes a value only where no member of that name is declared in source, so a hand-written value is compared with the record rather than duplicated. A code fix adds `partial` and deletes a hand-written value equaling the record.

**Example:** The declaration `public sealed partial record ExpenseSubmittedDomainEvent(...)` takes its generated `Classification`, while the same record without `partial` is reported.

**Checked by:** `dotnet build` reports `LS5006` for a marked domain event or rule exception that is not `partial`, in every project that references the analyzer package.

## Conventions

None.
