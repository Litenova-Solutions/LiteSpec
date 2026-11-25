using System.Text.Json;
using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the event generators write one payload record per domain and module event.</summary>
public sealed class GenerateEventsTests
{
    /// <summary>Each domain event renders its golden payload file in its aggregate folder.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void DomainEventsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRichLoanBorrowed(temp.Path);
        WriteLoanKind(temp.Path);
        WriteLoanRenewalPart(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
              usings:
              - Library.Lending.Contracts.Loans
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Equal(
            [
                "apps/api/src/Modules/Library.Lending/Domain/Copies/Events/CopyCopyRegisteredDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Copies/Events/CopyCopyRetiredDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Holds/Events/HoldHoldCanceledDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Holds/Events/HoldHoldPlacedDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Events/LoanLoanBorrowedDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Events/LoanLoanExtendedDomainEvent.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Events/LoanLoanReturnedDomainEvent.generated.cs",
            ],
            plan.Files
                .Select(file => Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));

        GenerateGoldens.AssertEqual(
            "domain-events-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("LoanLoanBorrowedDomainEvent.generated.cs", StringComparison.Ordinal)).Content);
    }

    /// <summary>Each module event renders its golden payload file in its fact folder, with a field stating its own untyped identifier where the contracts project cannot see the domain type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ModuleEventsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRichLoanBorrowed(temp.Path);
        WriteLoanKind(temp.Path);
        WriteLoanRenewalPart(temp.Path);
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "module-events", "loan-receipt");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-receipt.yaml"),
            """
            type: module-event
            id: module-event/lending.loan-receipt
            title: Loan Receipt
            summary: A loan receipt announces a lent copy and its due date.
            fields:
            - loanId
            - name: copyId
              type: id
            - dueDate
            - note
            translates: domain-event/loan.loan-borrowed
            compatibility: The selection may gain a new field the domain event carries. No field may be removed, and no meaning may change.
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: module-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}
              namespace: Library.{Module}.Contracts.{Aggregate}
              usings:
              - Library.Lending.Contracts.Loans
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Equal(
            "apps/api/src/Modules/Library.Lending.Contracts/Loans/OnLoanReceipt/LoanReceiptModuleEvent.generated.cs",
            Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'));
        GenerateGoldens.AssertEqual("module-events-dotnet.txt", file.Content);
    }

    /// <summary>A domain-events target without a marker type reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DomainEventsTargetWithoutMarkerTypeReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("markerType", StringComparison.Ordinal));
    }

    /// <summary>An event target without an aggregate placeholder in its path reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EventsTargetWithoutAggregatePathReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{Aggregate}", StringComparison.Ordinal));
    }

    /// <summary>An event target without an aggregate placeholder in its namespace reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EventsTargetWithoutAggregateNamespaceReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: module-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}
              namespace: Library.{Module}.Contracts
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{Aggregate}", StringComparison.Ordinal));
    }

    /// <summary>An event target for a TypeScript stack reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EventsTargetForTypeScriptReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: module-events
              stack: svelte
              path: apps/web/src/generated
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains(".NET", StringComparison.Ordinal));
    }

    /// <summary>A module event publishing a field its domain event does not carry renders no file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ModuleEventWithUncarriedFieldRendersNoFile()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "module-events", "loan-receipt");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-receipt.yaml"),
            """
            type: module-event
            id: module-event/lending.loan-receipt
            title: Loan Receipt
            summary: A loan receipt announces a lent copy and its due date.
            fields:
            - loanId
            - no-such-field
            translates: domain-event/loan.loan-borrowed
            compatibility: The selection may gain a new field the domain event carries. No field may be removed, and no meaning may change.
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: module-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}
              namespace: Library.{Module}.Contracts.{Aggregate}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Files);
    }

    /// <summary>A domain event carrying a field of no known shape renders no file, while the other events still render theirs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DomainEventWithUnknownTypeRendersNoFile()
    {
        using var temp = ModelFixture.Prepare("small");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml"),
            """
            type: domain-event
            id: domain-event/loan.loan-borrowed
            title: Loan borrowed
            meaning: A copy was lent.
            carries:
            - name: loanId
              type: text
            - name: shelf
              type: wat
            ignored: true
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Equal(6, plan.Files.Count);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.Contains("LoanBorrowed", StringComparison.Ordinal));
    }

    /// <summary>An untyped identifier maps to a Guid on the event and message mappers alike.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UntypedIdentifierMapsToGuid()
    {
        Assert.True(EventFieldTypes.TryMap(Carry("""{"name": "sessionId", "type": "id"}"""), out var carried));
        Assert.Equal("Guid", carried!.Type);

        Assert.True(MessageFieldTypes.TryMapInput(Carry("""{"name": "sessionId", "meaning": "The desk session.", "type": "id", "required": true, "origin": "caller"}"""), out var input));
        Assert.Equal("Guid", input!.Type);
    }

    /// <summary>An optional list rides nullable, and a row type naming a code type renders no nested row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptionalListAndCodeRowTypeMap()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "tags", "type": "list", "required": false, "rowShape": {"fields": [{"name": "tag", "type": "text", "required": true}]}}"""),
            out var tags));
        Assert.Equal("IReadOnlyList<string>?", tags!.Type);
        Assert.Empty(tags.Row);

        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "fines", "type": "list", "rowShape": {"fields": [{"name": "amount", "type": "decimal", "scale": 2, "required": true}, {"name": "note", "type": "text", "required": false}]}, "rowType": "FineRow"}"""),
            out var fines));
        Assert.Equal("IReadOnlyList<FineRow>", fines!.Type);
        Assert.Empty(fines.Row);
    }

    /// <summary>A list of one field keeps its named row type when rowType names one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleFieldListWithRowTypeKeepsNamedRow()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "tags", "type": "list", "rowType": "LoanTag", "rowShape": {"fields": [{"name": "tag", "type": "text", "required": true}]}}"""),
            out var tags));
        Assert.Equal("IReadOnlyList<LoanTag>", tags!.Type);
        Assert.Empty(tags.Row);
    }

    /// <summary>A single-object carry maps to its named row type, or to a nested row record without one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectCarryMapsToRowType()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "borrower", "type": "object", "rowType": "BorrowerForm", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}]}}"""),
            out var named));
        Assert.Equal("BorrowerForm", named!.Type);
        Assert.Empty(named.Row);

        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "borrower", "type": "object", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}, {"name": "card", "type": "text", "required": false}]}}"""),
            out var nested));
        Assert.Equal("BorrowerRow", nested!.Type);
        Assert.Equal(["Name", "Card"], nested.Row.Select(field => field.Property));
    }

    /// <summary>A part row type renders qualified when the qualifier resolves it, else the short part name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PartRowTypeQualifiesThroughResolver()
    {
        var carry = Carry("""{"name": "renewals", "type": "list", "rowType": "aggregate/lending.loan#renewal", "rowShape": {"fields": [{"name": "renewedAt", "type": "instant", "required": true}]}}""");

        Assert.True(EventFieldTypes.TryMap(carry, out var qualified, qualify: reference => "Library.Lending.Domain.Loans.Parts.Renewal"));
        Assert.Equal("IReadOnlyList<Library.Lending.Domain.Loans.Parts.Renewal>", qualified!.Type);

        Assert.True(EventFieldTypes.TryMap(carry, out var brief));
        Assert.Equal("IReadOnlyList<Renewal>", brief!.Type);
    }

    /// <summary>A row member of type object maps to its named row type, and to nothing without one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedObjectRowMapsToNamedType()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "lines", "type": "list", "rowShape": {"fields": [{"name": "copyId", "type": "id", "identifies": "aggregate/lending.copy", "required": true}, {"name": "borrower", "type": "object", "required": true, "rowType": "BorrowerForm", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}]}}]}}"""),
            out var lines));
        Assert.Equal("BorrowerForm", lines!.Row.Single(row => row.Property == "Borrower").Type);

        Assert.False(EventFieldTypes.TryMap(
            Carry("""{"name": "lines", "type": "list", "rowShape": {"fields": [{"name": "copyId", "type": "id", "identifies": "aggregate/lending.copy", "required": true}, {"name": "borrower", "type": "object", "required": true, "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}]}}]}}"""),
            out _));
    }

    /// <summary>An untyped row identifier maps to a Guid, and an existing type naming a part maps to the part name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UntypedRowIdentifierAndPartTypeMap()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "batches", "type": "list", "rowShape": {"fields": [{"name": "batch", "type": "id", "required": true}]}}"""),
            out var batches));
        Assert.Equal("IReadOnlyList<Guid>", batches!.Type);

        Assert.True(EventFieldTypes.TryMap(Carry("""{"name": "line", "type": "aggregate/spending.expense#line-item"}"""), out var line));
        Assert.Equal("LineItem", line!.Type);
        Assert.Empty(line.Row);
    }

    /// <summary>A carry of no known shape maps to nothing: an unknown type, a type naming no part, and an unreadable row type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownCarryShapesMapToNothing()
    {
        Assert.False(EventFieldTypes.TryMap(Carry("""{"name": "shelf", "type": "wat"}"""), out _));
        Assert.False(EventFieldTypes.TryMap(Carry("""{"name": "loan", "type": "aggregate/lending.loan"}"""), out _));
        Assert.False(EventFieldTypes.TryMap(
            Carry("""{"name": "fines", "type": "list", "rowShape": {"fields": [{"name": "amount", "type": "decimal", "scale": 2, "required": true}, {"name": "note", "type": "text", "required": false}]}, "rowType": "wat"}"""),
            out _));
    }

    /// <summary>A carried file maps to the bytes, nullable when the event may arrive without it.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CarriedFileMapsToBytes()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "slipScan", "type": "file", "contentTypes": ["application/pdf"], "maxSize": "5 MB"}"""),
            out var stored));
        Assert.Equal("byte[]", stored!.Type);

        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "slipScan", "type": "file", "contentTypes": ["application/pdf"], "maxSize": "5 MB", "required": false}"""),
            out var missing));
        Assert.Equal("byte[]?", missing!.Type);
    }

    /// <summary>A row field holding a list maps to a read-only list of its inner row, and a multi-field inner row maps to its named row type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedRowListsMap()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "lines", "type": "list", "rowShape": {"fields": [{"name": "copyId", "type": "id", "identifies": "aggregate/lending.copy", "required": true}, {"name": "remindedAt", "type": "list", "required": true, "rowShape": {"fields": [{"name": "sentAt", "type": "instant", "required": true}]}}]}}"""),
            out var lines));
        Assert.Equal("IReadOnlyList<LinesRow>", lines!.Type);
        Assert.Equal("IReadOnlyList<DateTimeOffset>", lines.Row.Single(row => row.Property == "RemindedAt").Type);

        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "boxes", "type": "list", "rowShape": {"fields": [{"name": "code", "type": "text", "required": true}, {"name": "periods", "type": "list", "required": true, "rowShape": {"fields": [{"name": "start", "type": "date", "required": true}, {"name": "end", "type": "date", "required": true}]}, "rowType": "aggregate/lending.loan#renewal"}]}}"""),
            out var boxes));
        Assert.Equal("IReadOnlyList<BoxesRow>", boxes!.Type);
        Assert.Equal("IReadOnlyList<Renewal>", boxes.Row.Single(row => row.Property == "Periods").Type);
    }

    /// <summary>A nested row shape maps to nothing when its multi-field rows name no row type, or when rows nest deeper than one level.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void InvalidNestedRowShapesMapToNothing()
    {
        Assert.False(EventFieldTypes.TryMap(
            Carry("""{"name": "boxes", "type": "list", "rowShape": {"fields": [{"name": "code", "type": "text", "required": true}, {"name": "periods", "type": "list", "required": true, "rowShape": {"fields": [{"name": "start", "type": "date", "required": true}, {"name": "end", "type": "date", "required": true}]}}]}}"""),
            out _));
        Assert.False(EventFieldTypes.TryMap(
            Carry("""{"name": "boxes", "type": "list", "rowShape": {"fields": [{"name": "periods", "type": "list", "required": true, "rowShape": {"fields": [{"name": "spans", "type": "list", "required": true, "rowShape": {"fields": [{"name": "start", "type": "date", "required": true}]}}]}}]}}"""),
            out _));
    }

    /// <summary>A project with attribute marks renders the event mark as an attribute.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AttributeMarksRenderAsAttributes()
    {
        var text = DomainEventsRenderer.RenderDotnet(new DomainEventsRenderer.EventData(
            "domain-event/loan.loan-borrowed",
            "loan",
            "loan-borrowed",
            "A copy was lent.",
            [new EventFieldTypes.MappedField("LoanId", "Guid", [], new Dictionary<string, string>(StringComparer.Ordinal))],
            "Library.Lending.Domain.Loans.Events",
            "Library.Kernel.Domain.IDomainEvent",
            [],
            MarkStyle.Attribute));

        Assert.Contains("[Implements(\"domain-event/loan.loan-borrowed\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<implements>", text, StringComparison.Ordinal);
    }

    /// <summary>A module event with a marker implements it, and a using of the file's own enclosing namespace reads as none.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ModuleMarkerAndUsingsRender()
    {
        var marked = ModuleEventsRenderer.RenderDotnet(new ModuleEventsRenderer.EventData(
            "module-event/lending.loan-receipt",
            "loan-receipt",
            "A loan receipt announces a lent copy and its due date.",
            [new EventFieldTypes.MappedField("LoanId", "Guid", [], new Dictionary<string, string>(StringComparer.Ordinal))],
            "Library.Lending.Contracts.Loans.OnLoanReceipt",
            "Library.Platform.Messaging.IModuleEvent",
            ["Library.Kernel.Values"],
            MarkStyle.Xml));

        Assert.Contains(") : IModuleEvent;", marked, StringComparison.Ordinal);
        Assert.Contains("using Library.Platform.Messaging;", marked, StringComparison.Ordinal);
        Assert.Contains("using Library.Kernel.Values;", marked, StringComparison.Ordinal);

        var bare = ModuleEventsRenderer.RenderDotnet(new ModuleEventsRenderer.EventData(
            "module-event/lending.loan-receipt",
            "loan-receipt",
            "A loan receipt announces a lent copy and its due date.",
            [new EventFieldTypes.MappedField("LoanId", "Guid", [], new Dictionary<string, string>(StringComparer.Ordinal))],
            "Library.Lending.Contracts.Loans.OnLoanReceipt",
            "",
            ["Library.Lending.Contracts.Loans"],
            MarkStyle.Xml));

        Assert.Contains("public sealed partial record LoanReceiptModuleEvent(Guid LoanId);", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("using ", bare, StringComparison.Ordinal);
    }

    /// <summary>Event type names follow the aggregate, the fact, and the kind suffix, and the fact folder opens with On.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EventNamesFollowAggregateFactAndKind()
    {
        Assert.Equal("ExpenseSubmittedDomainEvent", DomainEventsRenderer.TypeName("expense", "submitted"));
        Assert.Equal("LoanLoanBorrowedDomainEvent", DomainEventsRenderer.TypeName("loan", "loan-borrowed"));
        Assert.Equal("ExpenseReimbursedModuleEvent", ModuleEventsRenderer.TypeName("expense-reimbursed"));
        Assert.Equal("OnExpenseReimbursed", ModuleEventsRenderer.OnFolder("expense-reimbursed"));
    }

    /// <summary>Generate writes the event files, and a second check finds them current.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateWritesEventFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Domain", "Loans", "Events", "LoanLoanBorrowedDomainEvent.generated.cs");
        Assert.True(File.Exists(file), "domain event file");
        Assert.Contains("internal sealed partial record LoanLoanBorrowedDomainEvent", File.ReadAllText(file), StringComparison.Ordinal);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Every Claimwick domain and module event renders its payload file at its placed path.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClaimwickEventsRenderAtPlacedPaths()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(root)));

        Assert.Equal(
            [
                "apps/api/src/Modules/Claimwick.Spending.Contracts/Expenses/OnExpensePersonalDataErased/ExpensePersonalDataErasedModuleEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending.Contracts/Expenses/OnExpenseReimbursed/ExpenseReimbursedModuleEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Expenses/Events/ExpenseApprovedDomainEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Expenses/Events/ExpensePersonalDataErasedDomainEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Expenses/Events/ExpenseRefusedDomainEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Expenses/Events/ExpenseReimbursedDomainEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Expenses/Events/ExpenseSubmittedDomainEvent.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Domain/Receipts/Events/ReceiptAttachedDomainEvent.generated.cs",
            ],
            plan.Files
                .Where(file => file.AbsolutePath.EndsWith("DomainEvent.generated.cs", StringComparison.Ordinal) || file.AbsolutePath.EndsWith("ModuleEvent.generated.cs", StringComparison.Ordinal))
                .Select(file => Path.GetRelativePath(root, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));
    }

    /// <summary>An integer carry stating size 64 rides a long in the domain event.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SizedIntegerCarryRendersLong()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "- name: dueDate\n  type: text\n",
                "- name: dueDate\n  type: text\n- name: renewals\n  type: integer\n  size: 64\n",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var @event = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("LoanLoanBorrowedDomainEvent.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("long Renewals", @event, StringComparison.Ordinal);
    }

    /// <summary>A map carry rides a read-only dictionary of its value type by its key type in the domain event.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapCarryRendersDictionary()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "- name: dueDate\n  type: text\n",
                "- name: dueDate\n  type: text\n- name: countsByState\n  type: map\n  keyType: text\n  valueType: integer\n",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: domain-events
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Events
              namespace: Library.{Module}.Domain.{Aggregate}.Events
              markerType: Library.Kernel.Domain.IDomainEvent
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var @event = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("LoanLoanBorrowedDomainEvent.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyDictionary<string, int> CountsByState", @event, StringComparison.Ordinal);
    }

    /// <summary>Rewrites the small fixture's loan-borrowed event with one carry per mapped shape.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRichLoanBorrowed(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml"),
            """
            type: domain-event
            id: domain-event/loan.loan-borrowed
            title: Loan borrowed
            meaning: A copy was lent to a reader.
            carries:
            - name: loanId
              type: id
              identifies: aggregate/lending.loan
            - name: copyId
              type: id
              identifies: aggregate/lending.copy
            - name: readerId
              type: id
              identifies: actor/reader
            - name: shelf
              type: text
            - name: renewals
              type: integer
            - name: fine
              type: decimal
              scale: 2
            - name: deposit
              type: money
              currency: EUR
            - name: notified
              type: boolean
            - name: dueDate
              type: date
            - name: borrowedAt
              type: instant
            - name: loanPeriod
              type: duration
            - name: receiptEmail
              type: email
            - name: kind
              type: choice
              set: reference-data/lending.loan-kind
            - name: tags
              type: list
              rowShape:
                fields:
                - name: tag
                  meaning: One tag of the borrowed copy.
                  type: text
                  required: true
            - name: lines
              type: list
              rowShape:
                fields:
                - name: copyId
                  meaning: The copy the line lends.
                  type: id
                  identifies: aggregate/lending.copy
                  required: true
                - name: note
                  meaning: The line note, when one is kept.
                  type: text
                  required: false
                - name: remindedAt
                  meaning: The times a reminder went out for the line.
                  type: list
                  required: true
                  rowShape:
                    fields:
                    - name: sentAt
                      meaning: When the reminder went out.
                      type: instant
                      required: true
            - name: note
              type: text
              required: false
            - name: sessionId
              type: id
            - name: latestRenewal
              type: aggregate/lending.loan#renewal
            - name: slip
              type: DueSlip
              required: false
            - name: renewals
              type: list
              rowShape:
                fields:
                - name: renewedAt
                  meaning: The renewal time.
                  type: instant
                  required: true
                - name: note
                  meaning: The renewal note, when one is kept.
                  type: text
                  required: false
              rowType: aggregate/lending.loan#renewal
            - name: slipScan
              type: file
              contentTypes:
              - application/pdf
              maxSize: 5 MB
              required: false
            ignored: true
            """);

    /// <summary>Writes the loan-kind closed set the rich event's choice field names.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteLoanKind(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "reference-data", "loan-kind");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-kind.yaml"),
            """
            type: reference-data
            id: reference-data/lending.loan-kind
            title: Loan Kind
            summary: The kinds of loan the library lends.
            choices:
            - standard
            - reference-only
            rows:
            - value: standard
              key: standard-2026
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");

    /// <summary>Declares the renewal part on the small fixture's loan aggregate, with no identity of its own.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteLoanRenewalPart(string root) =>
        File.AppendAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"),
            "\nparts:\n- name: renewal\n  identity: false\n");

    /// <summary>A file row field on an event carry maps to the carried bytes, as a message file row does.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void FileRowCarryMapsToBytes()
    {
        Assert.True(EventFieldTypes.TryMap(
            Carry("""{"name": "scans", "type": "list", "rowShape": {"fields": [{"name": "scan", "type": "file", "contentTypes": ["image/png"], "maxSize": "10 MB", "required": true}]}}"""),
            out var files));
        Assert.Equal("IReadOnlyList<byte[]>", files!.Type);
    }

    /// <summary>Parses one carries entry.</summary>
    /// <param name="json">The entry as JSON.</param>
    /// <returns>The parsed entry.</returns>
    static JsonElement Carry(string json) => JsonDocument.Parse(json).RootElement;
}
