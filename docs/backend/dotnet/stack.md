# .NET Stack

## Intent

A backend built on .NET declares the `dotnet` stack in `litespec.project.json`. The declaration applies every page in this folder, and it tells the model builder to read C# code and test results in TRX or JUnit XML. This page states the declaration, the form each mark takes in C#, the runtime values generated from the records, what the stack reader reads from C# code, and the result formats the builder reads.

## Standards

### Declare `dotnet` as the Backend Stack

**ID:** `litespec/backend/dotnet/stack#declare-dotnet-as-the-backend-stack`

**Statement:** A project whose backend runs on .NET MUST name `dotnet` in the `stacks` of `litespec.project.json`.

**Rationale:** The `stacks` entry applies every page in this folder with its checks and analyzers, so an undeclared backend follows no .NET rule. It also selects the readers the model builder uses for C# handlers, events, refusal codes, and scenarios.

**Reviewed for:** The `stacks` array in `litespec.project.json`: `dotnet` listed when the backend runs on .NET.

### Read .NET Code Through the Stack Reader

**ID:** `litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader`

**Statement:** The .NET stack reader MUST read marks in both styles, injected repositories, returned codes, dispatch calls, aggregate method calls, raised events, and test results.

**Rationale:** Each S check reads one of those shapes. The mark links a code element to its record, so a handler, an event handler, an endpoint, an aggregate root and its methods, a domain event, a refusal type, and a test each name what they serve. The reader reads a mark in either style and reports the style the project did not declare. Injected repositories prove `changes`: a repository serves the aggregate its implementing class names in an `implements` mark, or, with no such mark, the aggregate its interface names. Returned codes prove declared results and refusals, and dispatch calls prove declared effects, so the reader reports `behavior-unrecorded`, `use-case-unimplemented`, `write-undeclared`, `result-undeclared`, `effect-undeclared`, and `refusal-unimplemented` from code. Test marks with the latest test run prove each scenario, so the reader reports `proves-unreached` and `use-case-scenario-missing` from tests. The reader also reads the options classes that bind keyed settings, through each class's `SectionName` constant and properties, and the template store `stackConfig.dotnet.templateStore` names: one C# file whose top-level dictionary initializer keys are the template keys and whose texts write each parameter as `{{PascalCaseName}}`. S checks block where this reader is qualified and report elsewhere.

**Example:** The reimburse-expense handler carries `/// <implements>use-case/spending.reimburse-expense</implements>`, and the reaction class carries `/// <implements>reaction/spending.send-approver-reminders-on-expense-submitted</implements>`.

**Checked by:** `litespec model check` reports its tier beside each S finding read from C# code and test results.

### Write Each Mark in the Declared Style

**ID:** `litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style`

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

**Checked by:** `litespec model check` reports `mark-style-invalid` (tier S), and `dotnet build` reports `LS5004`, for a mark written in the style the project did not declare.

### Generate Runtime Values from the Records

**ID:** `litespec/backend/dotnet/stack#generate-runtime-values-from-the-records`

**Statement:** A domain event `Classification` and a domain exception `Code` MUST equal the values their records state.

**Rationale:** The record states each identifier and refusal code once, so a second hand-written copy in code is a second source that drifts. With the analyzer package installed, its source generator writes the value into the `partial` type from the mark and the record: the event `Classification` is the event identifier, and the exception `Code` is the `code` of the refusal result its rule names. Without the analyzer package, the value stays hand-written and the check compares it with the record.

**Example:** The event carries its mark and the generator writes its classification:

```csharp
/// <implements>event/expense.submitted</implements>
public sealed partial record ExpenseSubmittedEvent(ExpenseId ExpenseId, Money Amount) : IDomainEvent;
```

The generated part holds `public const string Classification = "event/expense.submitted";`.

**Checked by:** `litespec model check` reports `runtime-value-drift` (tier S) for a hand-written value that differs from its record, and `dotnet build` reports `LS5005` for the same value.

### Generate the Registry, Permissions, and Roles from the Records

**ID:** `litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records`

**Statement:** Code MUST read a use case's identity, its grant, a permission's facts, and a role's permission set from the classes the analyzer package generates from the marks and the records, never from a hand-written copy.

**Rationale:** Each of these facts is written once, on a record or in a mark, and a hand-written table of them is a second source that drifts the first time a record changes. The generator writes them per project: `LiteSpecRegistry` answers a message type's use case (`UseCaseOf`), a marked type's use case (`UseCaseOfType`), a message type's grant (`GrantOf`), and in an API project an operation name's use case (`UseCaseOfOperation`); `LiteSpecPermissions` holds one constant and one entry per `permission/` record; and `LiteSpecRoles` holds each role's scope and the permissions its record allows. A role that names a permission no record declares, and two records that generate one name, stop at build time.

**Example:** When Claimwick's `role/finance` record gains a permission, `LiteSpecRoles.Of("role/finance").Allows` carries it at the next build, and `LiteSpecRegistry.GrantOf(typeof(ReimburseExpenseCommand))` answers the reimbursement grant, with no hand-written table to update.

**Checked by:** `dotnet build` reports `LS5009` for two records generating one constant name, `LS5010` for a permission whose first word is no known verb, `LS5011` for a message two marked handlers serve with different use cases, and `LS5012` for a role allowing a permission no record declares.

### Declare Marked Events and Exceptions as Partial Types

**ID:** `litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types`

**Statement:** A project that references the analyzer package MUST declare every marked domain event and rule exception as a `partial` type.

**Rationale:** The source generator adds the runtime value as a second part of the type, and a type that is not `partial` cannot take one. The generator writes a value only where no member of that name is declared in source, so a hand-written value is compared with the record rather than duplicated. A code fix adds `partial` and deletes a hand-written value that equals the record.

**Example:** `public sealed partial record ExpenseSubmittedEvent(...)` takes its generated `Classification`; the same record without `partial` is reported.

**Checked by:** `litespec model check` reports `partial-missing` (tier S), and `dotnet build` reports `LS5006`, for a marked domain event or rule exception that is not `partial`, in every project that references the analyzer package.

### Write Test Results the Model Builder Reads

**ID:** `litespec/backend/dotnet/stack#write-test-results-the-model-builder-reads`

**Statement:** A .NET test run MUST write its results as TRX or JUnit XML under `paths.testResults`.

**Rationale:** The model builder computes verified status from TRX and JUnit XML only. A run written in another format, or outside `paths.testResults`, verifies no scenario even when every test passes.

**Checked by:** `litespec model check` reports no scenario verified when no result file exists.

## Conventions

None.
