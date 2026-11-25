using System.Text.Json;
using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the message and result generators write one partial record per use case, with the inputs, implied members, message interface, outputs, rows, and next cursor the records state.</summary>
public sealed class GenerateMessagesTests
{
    /// <summary>Each command and query renders its message file in its use case folder, and the rich command matches its golden.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void MessagesDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRichBorrowCopy(temp.Path);
        WriteLoanKind(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              commandInterface: Library.Messaging.ICommand
              queryInterface: Library.Messaging.IQuery
              usings:
              - Library.Lending.Contracts.Loans
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Equal(
            [
                "apps/api/src/Modules/Library.Lending/Application/Copies/RegisterCopy/RegisterCopyCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Copies/RetireCopy/RetireCopyCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Holds/CancelHold/CancelHoldCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Holds/PlaceHold/PlaceHoldCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy/BorrowCopyCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/ExtendLoan/ExtendLoanCommand.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/ListLoans/ListLoansQuery.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/ReturnCopy/ReturnCopyCommand.generated.cs",
            ],
            plan.Files
                .Select(file => Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));

        GenerateGoldens.AssertEqual(
            "messages-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content);
    }

    /// <summary>Each use case with an output renders its result file in its use case folder, and the multi-group result matches its golden.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ResultsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              usings:
              - Library.Lending.Contracts.Loans
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Equal(
            [
                "apps/api/src/Modules/Library.Lending/Application/Loans/ListLoans/HoldsRow.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/ListLoans/ListLoansQueryResult.generated.cs",
                "apps/api/src/Modules/Library.Lending/Application/Loans/ListLoans/LoansRow.generated.cs",
            ],
            plan.Files
                .Select(file => Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));

        GenerateGoldens.AssertEqual(
            "results-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQueryResult.generated.cs", StringComparison.Ordinal)).Content);
        GenerateGoldens.AssertEqual(
            "results-row-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("LoansRow.generated.cs", StringComparison.Ordinal)).Content);
    }

    /// <summary>A paged query carries the cursor and limit with their defaults, and its result the next cursor.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PagedQueryCarriesCursorLimitAndNextCursor()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              queryInterface: Library.Messaging.IQuery
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var query = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQuery.generated.cs", StringComparison.Ordinal)).Content;
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQueryResult.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("Guid? ReaderId, string? Cursor = null, int Limit = 25", query, StringComparison.Ordinal);
        Assert.Contains("The due date and loan identity of the last row.", query, StringComparison.Ordinal);
        Assert.Contains("string? NextCursor = null", result, StringComparison.Ordinal);
    }

    /// <summary>An offset query carries the offset and limit instead of the cursor.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OffsetQueryCarriesOffsetAndLimit()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "cursor: The due date and loan identity of the last row.",
                "offset: The row number the page starts at.",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var query = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQuery.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("Guid? ReaderId, int Offset = 0, int Limit = 25", query, StringComparison.Ordinal);
        Assert.Contains("The row number the page starts at.", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Cursor", query, StringComparison.Ordinal);
    }

    /// <summary>A use case under tenant scope carries the tenant key first, as a Guid when no aggregate carries the term name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantUseCaseCarriesTenantKey()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteTenancy(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("BorrowCopyCommand(Guid WorkspaceId, CopyId CopyId, Guid? ReaderId)", command, StringComparison.Ordinal);
        Assert.Contains("/// <param name=\"WorkspaceId\">The tenant the call acts in.</param>", command, StringComparison.Ordinal);
    }

    /// <summary>The tenant key rides the tenant aggregate's identifier type when an aggregate carries the term name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantKeyRidesTenantAggregateIdentifier()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteTenancy(temp.Path);
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "workspaces");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "aggregate.yaml"),
            """
            type: aggregate
            id: aggregate/lending.workspace
            title: Workspace
            states:
            - name: Active
              meaning: The workspace lends.
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("BorrowCopyCommand(WorkspaceId WorkspaceId, CopyId CopyId, Guid? ReaderId)", command, StringComparison.Ordinal);
    }

    /// <summary>An input identifying the tenant aggregate carries the tenant key itself, so no key is implied.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantIdentifyingInputSuppressesImpliedKey()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteTenancy(temp.Path);
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "workspaces");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "aggregate.yaml"),
            """
            type: aggregate
            id: aggregate/lending.workspace
            title: Workspace
            states:
            - name: Active
              meaning: The workspace lends.
            """);
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "- name: copyId",
                "- name: workspaceId\n  meaning: The workspace the loan belongs to.\n  type: id\n  identifies: aggregate/lending.workspace\n  required: true\n  origin: caller\n  classification: internal\n- name: copyId",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("internal sealed partial record BorrowCopyCommand(WorkspaceId WorkspaceId, CopyId CopyId, Guid? ReaderId);", command, StringComparison.Ordinal);
    }

    /// <summary>An integer input stating size 64 rides a long in the message.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SizedIntegerRendersLong()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan", "extend-loan.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "- name: days\n  meaning: The number of days the loan is extended.\n  type: integer\n",
                "- name: days\n  meaning: The number of days the loan is extended.\n  type: integer\n  size: 64\n",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ExtendLoanCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("long Days", command, StringComparison.Ordinal);
    }

    /// <summary>A map input rides a read-only dictionary of its value type by its key type in the message.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapInputRendersDictionary()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(path, "\n" + """
            input:
            - name: countsByState
              meaning: The retired copies counted by lifecycle state.
              type: map
              keyType: text
              valueType: integer
              required: true
              origin: caller
              binding: body
              classification: internal
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("RetireCopyCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyDictionary<string, int> CountsByState", command, StringComparison.Ordinal);
    }

    /// <summary>A map output field rides a read-only dictionary of its value type by its key type in the result.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapOutputRendersDictionary()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "        rowType: LoansRow\n      classification: internal",
                "        rowType: LoansRow\n      - name: countsByState\n        type: map\n        meaning: The loans counted by lifecycle state.\n        keyType: text\n        valueType: integer\n        required: true\n      classification: internal",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQueryResult.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyDictionary<string, int> CountsByState", result, StringComparison.Ordinal);
    }

    /// <summary>A map input with decimal values rides a read-only dictionary of decimals carrying its scale fact.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapDecimalValuesRenderDictionary()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(path, "\n" + """
            input:
            - name: priceByState
              meaning: The retired copy price by lifecycle state.
              type: map
              keyType: text
              valueType: decimal
              scale: 2
              required: true
              origin: caller
              binding: body
              classification: internal
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("RetireCopyCommand.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyDictionary<string, decimal> PriceByState", command, StringComparison.Ordinal);
    }

    /// <summary>Selects tenancy with the workspace term and declares the term, so messages imply the tenant key.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteTenancy(string root)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        File.WriteAllText(
            project,
            File.ReadAllText(project).Replace(
                "selectedExtensions: []",
                "selectedExtensions:\n- id: tenancy\n  criterion: Independent customers share one deployment.\n  tenant: term/workspace",
                StringComparison.Ordinal));
        var glossary = Path.Combine(root, ".litespec", "glossary.yaml");
        File.WriteAllText(
            glossary,
            File.ReadAllText(glossary).Replace(
                "- id: term/loan",
                "- id: term/workspace\n  term: workspace\n  meaning: One customer organization sharing the deployment.\n  layer: behavior\n- id: term/loan",
                StringComparison.Ordinal));
    }

    /// <summary>A public use case renders its message and result into the Contracts target with public visibility.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PublicUseCaseRendersIntoContractsTarget()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteReadLoan(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Contracts.{Aggregate}.{UseCase}
              visibility: public
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Contracts.{Aggregate}.{UseCase}
              visibility: public
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);

        var query = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadLoanQuery.generated.cs", StringComparison.Ordinal));
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadLoanResult.generated.cs", StringComparison.Ordinal));

        Assert.Equal(
            "apps/api/src/Modules/Library.Lending.Contracts/Loans/ReadLoan/ReadLoanQuery.generated.cs",
            Path.GetRelativePath(temp.Path, query.AbsolutePath).Replace('\\', '/'));
        Assert.Contains("namespace Library.Lending.Contracts.Loans.ReadLoan;", query.Content, StringComparison.Ordinal);
        Assert.Contains("public sealed partial record ReadLoanQuery(LoanId Loan);", query.Content, StringComparison.Ordinal);
        Assert.Contains("public sealed partial record ReadLoanResult(LoanId LoanId, DateOnly DueDate);", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.Contains("Application", StringComparison.Ordinal) && file.AbsolutePath.Contains("ReadLoan", StringComparison.Ordinal));
    }

    /// <summary>Message and result type names follow the use case, its kind, and its visibility.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MessageAndResultNamesFollowUseCaseKindAndVisibility()
    {
        Assert.Equal("BorrowCopyCommand", MessagesRenderer.MessageTypeName("borrow-copy", "command"));
        Assert.Equal("ListLoansQuery", MessagesRenderer.MessageTypeName("list-loans", "query"));
        Assert.Equal("BorrowCopyCommandResult", MessagesRenderer.ResultTypeName("borrow-copy", "command", isPublic: false));
        Assert.Equal("ListLoansQueryResult", MessagesRenderer.ResultTypeName("list-loans", "query", isPublic: false));
        Assert.Equal("ReadLoanResult", MessagesRenderer.ResultTypeName("read-loan", "query", isPublic: true));
        Assert.Equal("CloseLoansResult", MessagesRenderer.ResultTypeName("close-loans", "command", isPublic: true));
    }

    /// <summary>A messages target without a use-case placeholder in its path reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MessagesTargetWithoutUseCasePathReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{UseCase}", StringComparison.Ordinal));
    }

    /// <summary>A results target without an aggregate placeholder in its namespace reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ResultsTargetWithoutAggregateNamespaceReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{Aggregate}", StringComparison.Ordinal));
    }

    /// <summary>A messages target for a TypeScript stack reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MessagesTargetForTypeScriptReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: svelte
              path: apps/web/src/generated
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains(".NET", StringComparison.Ordinal));
    }

    /// <summary>A command keying on an input the message does not carry renders no file, while the other messages still render theirs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CommandWithUncarriedKeyPlaceholderRendersNoFile()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "  mode: none\n  reason: Each call lends its own copy, and lending an unavailable copy is refused.",
                "  mode: key\n  key: \"{nope}\"\n  keyScope: account\n  replay: returns-first-result",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Equal(7, plan.Files.Count);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.Contains("BorrowCopy", StringComparison.Ordinal));
    }

    /// <summary>A command keying on a dotted placeholder renders its key through the member path.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CommandWithDottedKeyPlaceholderRendersMemberPath()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "  mode: none\n  reason: Each call lends its own copy, and lending an unavailable copy is refused.",
                "  mode: key\n  key: \"{copyId.branch}\"\n  keyScope: account\n  replay: returns-first-result",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var content = Assert.Single(plan.Files, file => file.AbsolutePath.Contains("BorrowCopy", StringComparison.Ordinal)).Content;

        Assert.Contains("public string IdempotencyKey => $\"{CopyId.Branch}\";", content, StringComparison.Ordinal);
    }

    /// <summary>A message row field holding a list maps to a read-only list of its inner row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedMessageRowListMaps()
    {
        var row = JsonDocument.Parse("""{"name": "remindedAt", "type": "list", "required": true, "rowShape": {"fields": [{"name": "sentAt", "type": "instant", "required": true}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapRow(row, out var mapped));
        Assert.Equal("IReadOnlyList<DateTimeOffset>", mapped!.Type);
    }

    /// <summary>An optional output field maps nullable, and a required one stays non-nullable.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptionalOutputFieldMapsNullable()
    {
        var missing = JsonDocument.Parse("""{"name": "source", "meaning": "The shelf the loan came from.", "type": "text", "required": false}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapOutput(missing, out var optional));
        Assert.Equal("Source", optional!.Property);
        Assert.Equal("string?", optional.Type);

        var stated = JsonDocument.Parse("""{"name": "source", "meaning": "The shelf the loan came from.", "type": "text"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapOutput(stated, out var required));
        Assert.Equal("string", required!.Type);
    }

    /// <summary>A member takes the PascalCase field name: no suffix is added to an id.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MemberTakesFieldNameWithoutSuffix()
    {
        Assert.Equal("Copy", MessageFieldTypes.PropertyName("copy"));
        Assert.Equal("CopyId", MessageFieldTypes.PropertyName("copyId"));

        var input = JsonDocument.Parse("""{"name": "copy", "meaning": "The copy borrowed.", "type": "id", "identifies": "aggregate/lending.copy", "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(input, out var mapped));
        Assert.Equal("Copy", mapped!.Property);
        Assert.Equal("CopyId", mapped.Type);
    }

    /// <summary>A money field maps to the shared money object, and a file field to the carried bytes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyAndFileFieldsMap()
    {
        var money = JsonDocument.Parse("""{"name": "deposit", "meaning": "The deposit held.", "type": "money", "currencyField": "depositCurrency", "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(money, out var amount));
        Assert.Equal("Money", amount!.Type);

        var file = JsonDocument.Parse("""{"name": "scan", "meaning": "The scanned slip.", "type": "file", "contentTypes": ["application/pdf"], "maxSize": "5 MB", "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(file, out var bytes));
        Assert.Equal("byte[]", bytes!.Type);

        var missing = JsonDocument.Parse("""{"name": "scan", "meaning": "The scanned slip.", "type": "file", "contentTypes": ["application/pdf"], "maxSize": "5 MB", "required": false, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(missing, out var absent));
        Assert.Equal("byte[]?", absent!.Type);
    }

    /// <summary>A money input taking its currency from an aggregate rides a decimal, while an output keeps the shared object.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyInputWithCurrencyHomeRidesDecimal()
    {
        var money = JsonDocument.Parse("""{"name": "fee", "meaning": "The processing fee.", "type": "money", "currencyHome": "aggregate/lending.copy", "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(money, out var amount));
        Assert.Equal("decimal", amount!.Type);

        var missing = JsonDocument.Parse("""{"name": "fee", "meaning": "The processing fee.", "type": "money", "currencyHome": "aggregate/lending.copy", "required": false, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(missing, out var absent));
        Assert.Equal("decimal?", absent!.Type);

        var output = JsonDocument.Parse("""{"name": "fee", "meaning": "The processing fee.", "type": "money", "currencyHome": "aggregate/lending.copy", "required": true}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapOutput(output, out var answered));
        Assert.Equal("Money", answered!.Type);
    }

    /// <summary>A money row of an input list rides a decimal with a currencyHome, and the shared object on an output list.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyRowWithCurrencyHomeFollowsItsList()
    {
        var row = JsonDocument.Parse("""{"name": "share", "meaning": "The share of the split.", "type": "money", "currencyHome": "aggregate/lending.copy", "required": true}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapRow(row, out var input, isInput: true));
        Assert.Equal("decimal", input!.Type);

        Assert.True(MessageFieldTypes.TryMapRow(row, out var output));
        Assert.Equal("Money", output!.Type);
    }

    /// <summary>A list of one field keeps its named row type when rowType names one, else it lists the element.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleFieldListWithRowTypeKeepsNamedRow()
    {
        var named = JsonDocument.Parse("""{"name": "tags", "meaning": "The tags on the loan.", "type": "list", "rowType": "LoanTagRow", "rowShape": {"fields": [{"name": "tag", "type": "text", "required": true, "meaning": "One tag."}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapOutput(named, out var kept));
        Assert.Equal("IReadOnlyList<LoanTagRow>", kept!.Type);
        Assert.Equal("LoanTagRow", kept.RowType);
        Assert.Single(kept.Row);

        var plain = JsonDocument.Parse("""{"name": "tags", "meaning": "The tags on the loan.", "type": "list", "rowShape": {"fields": [{"name": "tag", "type": "text", "required": true, "meaning": "One tag."}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapOutput(plain, out var collapsed));
        Assert.Equal("IReadOnlyList<string>", collapsed!.Type);
        Assert.Null(collapsed.RowType);
    }

    /// <summary>A single optional group stays nested and nullable, with its optional fields nullable beside it.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleOptionalGroupStaysNestedAndNullable()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLoanSummary(temp.Path);
        AppendGenerate(temp.Path, ShelfResultsTarget);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadLoanSummaryQueryResult.generated.cs", StringComparison.Ordinal)).Content;
        var row = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("LoanTagRow.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Contains("internal sealed partial record ReadLoanSummaryQueryResult(ReadLoanSummaryQueryResult.LoanGroup? Loan)", result, StringComparison.Ordinal);
        Assert.Contains("public sealed partial record LoanGroup(DateOnly DueDate, string? Note, IReadOnlyList<LoanTagRow> Tags);", result, StringComparison.Ordinal);
        Assert.Contains("internal sealed partial record LoanTagRow(string Tag);", row, StringComparison.Ordinal);
    }

    /// <summary>An optional input rides nullable whatever its origin: derived, claim, or verified claim, including an optional list.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptionalDerivedAndClaimInputsMapNullable()
    {
        var derived = JsonDocument.Parse("""{"name": "handledAt", "meaning": "The time the decision runs.", "type": "instant", "required": false, "origin": "derived"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(derived, out var at));
        Assert.Equal("DateTimeOffset?", at!.Type);

        var claim = JsonDocument.Parse("""{"name": "readerId", "meaning": "The signed-in reader.", "type": "id", "identifies": "actor/reader", "required": false, "origin": "claim"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(claim, out var reader));
        Assert.Equal("Guid?", reader!.Type);

        var verified = JsonDocument.Parse("""{"name": "readerId", "meaning": "The signed-in reader.", "type": "id", "identifies": "actor/reader", "required": false, "origin": "verified-claim", "verifiedBy": "entry-point/library-app"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(verified, out var confirmed));
        Assert.Equal("Guid?", confirmed!.Type);

        var list = JsonDocument.Parse("""{"name": "tags", "meaning": "The tags on the loan.", "type": "list", "required": false, "origin": "derived", "rowShape": {"fields": [{"name": "tag", "type": "text", "required": true, "meaning": "One tag."}], "minItems": 0}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(list, out var tags));
        Assert.Equal("IReadOnlyList<string>?", tags!.Type);

        var required = JsonDocument.Parse("""{"name": "handledAt", "meaning": "The time the decision runs.", "type": "instant", "required": true, "origin": "derived"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(required, out var always));
        Assert.Equal("DateTimeOffset", always!.Type);
    }

    /// <summary>A grouped result compiles: each nested group type is qualified by the result type in the record header.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GroupedResultCompiles()
    {
        var text = ResultsRenderer.RenderDotnet(new ResultsRenderer.ResultData(
            "use-case/lending.list-loans",
            "List loans",
            "ListLoansQueryResult",
            [],
            [
                new ResultsRenderer.ResultGroup(
                    "rows",
                    "Rows",
                    [new MessageFieldTypes.MappedField("Loans", "IReadOnlyList<string>", null, [], "One row per loan.")],
                    true),
                new ResultsRenderer.ResultGroup(
                    "holds",
                    "Holds",
                    [new MessageFieldTypes.MappedField("Count", "int", null, [], "The waiting holds.")],
                    false),
            ],
            true,
            "Library.Lending.Application.Loans.ListLoans",
            ["System.Collections.Generic"],
            false));

        Assert.Contains("ListLoansQueryResult.RowsGroup Rows", text, StringComparison.Ordinal);
        Assert.Contains("ListLoansQueryResult.HoldsGroup? Holds", text, StringComparison.Ordinal);

        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: TestContext.Current.CancellationToken);
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GroupedResult",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics(cancellationToken: TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors.Select(error => error.ToString())));
    }

    /// <summary>An offset query result carries no next cursor: only a cursor-paged query answers one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OffsetQueryResultCarriesNoNextCursor()
    {
        using var temp = ModelFixture.Prepare("small");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "cursor: The due date and loan identity of the last row.",
                "offset: The row number the page starts at.",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ListLoansQueryResult.generated.cs", StringComparison.Ordinal)).Content;

        Assert.DoesNotContain("NextCursor", result, StringComparison.Ordinal);
    }

    /// <summary>Generate writes the message and result files, and a second check finds them current.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateWritesMessageFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              commandInterface: Library.Messaging.ICommand
              queryInterface: Library.Messaging.IQuery
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var message = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommand.generated.cs");
        var query = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "ListLoans", "ListLoansQuery.generated.cs");
        var row = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "ListLoans", "LoansRow.generated.cs");
        Assert.True(File.Exists(message), "command file");
        Assert.True(File.Exists(query), "query file");
        Assert.True(File.Exists(row), "row file");
        Assert.Contains("internal sealed partial record BorrowCopyCommand", File.ReadAllText(message), StringComparison.Ordinal);
        Assert.Contains(": IQuery<ListLoansQueryResult>", File.ReadAllText(query), StringComparison.Ordinal);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Every Claimwick use case renders its message and result files at their placed paths.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClaimwickMessagesAndResultsRenderAtPlacedPaths()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(root)));

        Assert.DoesNotContain(plan.Errors, error => error.Contains("'messages'", StringComparison.Ordinal) || error.Contains("'results'", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.ConfigErrors, error => error.Contains("'messages'", StringComparison.Ordinal) || error.Contains("'results'", StringComparison.Ordinal));
        Assert.Equal(
            [
                "apps/api/src/Modules/Claimwick.Reporting/Application/ListReimbursedExpenses/ListReimbursedExpensesQuery.generated.cs",
                "apps/api/src/Modules/Claimwick.Reporting/Application/ListReimbursedExpenses/ListReimbursedExpensesQueryResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Reporting/Application/ListReimbursedExpenses/ReimbursedExpenseRow.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending.Contracts/Expenses/ReadExpense/ReadExpenseQuery.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending.Contracts/Expenses/ReadExpense/ReadExpenseResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ApproveExpense/ApproveExpenseCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ApproveExpense/ApproveExpenseCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ErasePersonalData/ErasePersonalDataCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ErasePersonalData/ErasePersonalDataCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ExportExpenses/ExportExpensesQuery.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ExportExpenses/ExportExpensesQueryResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ExportExpenses/ExportedExpenseRow.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ListExpenses/ExpenseSummary.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ListExpenses/ListExpensesQuery.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ListExpenses/ListExpensesQueryResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/RefuseExpense/RefuseExpenseCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/RefuseExpense/RefuseExpenseCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ReimburseExpense/ReimburseExpenseCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ReimburseExpense/ReimburseExpenseCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Policies/ChangeReceiptThreshold/ChangeReceiptThresholdCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Policies/ChangeReceiptThreshold/ChangeReceiptThresholdCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Receipts/AttachReceipt/AttachReceiptCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/Receipts/AttachReceipt/AttachReceiptCommandResult.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/ReminderPreferences/ChangeReminderPreference/ChangeReminderPreferenceCommand.generated.cs",
                "apps/api/src/Modules/Claimwick.Spending/Application/ReminderPreferences/ChangeReminderPreference/ChangeReminderPreferenceCommandResult.generated.cs",
            ],
            plan.Files
                .Where(file =>
                    file.AbsolutePath.EndsWith("Command.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("Query.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("Result.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("ExpenseSummary.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("ExportedExpenseRow.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("ReimbursedExpenseRow.generated.cs", StringComparison.Ordinal)
                    || file.AbsolutePath.EndsWith("SubmittedLine.generated.cs", StringComparison.Ordinal))
                .Select(file => Path.GetRelativePath(root, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));
    }

    /// <summary>Rewrites the small fixture's borrow-copy command with one input per mapped shape, a money pair, a choice, many values, and a key.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRichBorrowCopy(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
            """
            type: use-case
            id: use-case/lending.borrow-copy
            title: Borrow copy
            summary: A reader borrows an available copy from the shelf.
            exposure: A signed-in reader reaches this command through the library app under the borrow-copy permission. A wrong lending hands a copy to a reader already past the loan limit.
            kind: command
            access:
              grant: permission/borrow-copy
              tenant: own
            reachedThrough:
            - entry-point/library-app
            input:
            - name: copyId
              meaning: The copy the reader borrows.
              type: id
              identifies: aggregate/lending.copy
              required: true
              origin: caller
              classification: internal
            - name: readerId
              meaning: The signed-in reader borrowing the copy, read from verified claims.
              type: id
              identifies: actor/reader
              required: false
              origin: verified-claim
              verifiedBy: entry-point/library-app
              classification: personal
            - name: shelf
              meaning: The shelf the copy is taken from.
              type: text
              required: true
              origin: caller
              classification: internal
            - name: loanDays
              meaning: The days the loan runs.
              type: integer
              required: true
              origin: caller
              classification: internal
            - name: fine
              meaning: The daily fine for a late return.
              type: decimal
              scale: 2
              required: true
              origin: caller
              classification: internal
            - name: deposit
              meaning: The deposit held for the loan.
              type: money
              currencyField: depositCurrency
              required: true
              origin: caller
              classification: internal
            - name: depositCurrency
              meaning: The ISO 4217 currency code of the deposit.
              type: text
              maxLength: 3
              required: true
              origin: caller
              classification: internal
            - name: notified
              meaning: Whether the reader wants a due notice.
              type: boolean
              required: false
              origin: caller
              classification: internal
            - name: dueDate
              meaning: The day the loan is due.
              type: date
              required: true
              origin: caller
              classification: internal
            - name: borrowedAt
              meaning: The time the loan starts.
              type: instant
              required: true
              origin: caller
              classification: internal
            - name: loanPeriod
              meaning: How long the loan runs.
              type: duration
              required: false
              origin: caller
              classification: internal
            - name: receiptEmail
              meaning: The address the receipt goes to.
              type: email
              required: false
              origin: caller
              classification: personal
            - name: kind
              meaning: The kind of loan the reader takes.
              type: choice
              set: reference-data/lending.loan-kind
              required: true
              origin: caller
              classification: internal
            - name: tags
              meaning: The tags the reader puts on the loan.
              type: text
              many: true
              required: true
              origin: caller
              classification: internal
            idempotency:
              mode: key
              key: "{copyId}"
              keyScope: account
              replay: returns-first-result
            steps:
            - statement: The reader borrows the copy from the shelf.
            """);

    /// <summary>Writes the loan-kind closed set the rich command's choice field names.</summary>
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

    /// <summary>Writes a public query another module reaches, with one output group and no paging.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteReadLoan(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-loan");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-loan.yaml"),
            """
            type: use-case
            id: use-case/lending.read-loan
            title: Read loan
            summary: Lending returns one loan with its due date so a calling module can confirm the loan before it uses its own copy.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            reachedThrough:
            - module/catalog
            input:
            - name: loan
              meaning: The loan to read.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              classification: internal
            output:
              include:
                loan:
                  fields:
                  - name: loanId
                    meaning: The read loan.
                    type: id
                    identifies: aggregate/lending.loan
                  - name: dueDate
                    meaning: The day the loan is due.
                    type: date
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: one loan answers the query
            steps:
            - statement: The catalog names one loan.
            """);
    }

    /// <summary>Two lists of one result naming the same row type with the same members render one row file naming both lists.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListsSharingOneRowTypeRenderOneRowFile()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteShelfReport(temp.Path, secondRowAmount: "integer");
        AppendGenerate(temp.Path, ShelfResultsTarget);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var row = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadShelvesRow.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Empty(plan.ConfigErrors);
        Assert.Contains("/// <summary>One row of Open and Closed.</summary>", row, StringComparison.Ordinal);
        Assert.Contains("internal sealed partial record ReadShelvesRow(string Shelf, int Count);", row, StringComparison.Ordinal);
    }

    /// <summary>Two lists naming the same row type with different members keep apart, and the duplicate-file check reports them.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListsSharingOneRowTypeWithDifferentMembersReport()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteShelfReport(temp.Path, secondRowAmount: "text");
        AppendGenerate(temp.Path, ShelfResultsTarget);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Contains(plan.ConfigErrors, error => error.Contains("ReadShelvesRow.generated.cs' twice", StringComparison.Ordinal));
    }

    /// <summary>The results target the shared-row tests render through.</summary>
    const string ShelfResultsTarget = """
        generate:
        - kind: results
          stack: dotnet
          path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
          namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
        """;

    /// <summary>Writes a query whose output holds two lists naming one row type.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="secondRowAmount">The type of the count field in the second list's row.</param>
    static void WriteShelfReport(string root, string secondRowAmount)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-shelves");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-shelves.yaml"),
            $$"""
            type: use-case
            id: use-case/lending.read-shelves
            title: Read shelves
            summary: Lending lists the open and the closed shelves with their loan counts so the desk sees where loans gather.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            output:
              include:
                shelves:
                  fields:
                  - name: open
                    meaning: The open shelves with their loan counts.
                    type: list
                    rowType: ReadShelvesRow
                    rowShape:
                      fields:
                      - name: shelf
                        type: text
                        required: true
                        meaning: The shelf.
                      - name: count
                        type: integer
                        required: true
                        meaning: The loans on the shelf.
                      minItems: 0
                  - name: closed
                    meaning: The closed shelves with their loan counts.
                    type: list
                    rowType: ReadShelvesRow
                    rowShape:
                      fields:
                      - name: shelf
                        type: text
                        required: true
                        meaning: The shelf.
                      - name: count
                        type: {{secondRowAmount}}
                        required: true
                        meaning: The loans on the shelf.
                      minItems: 0
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: the shelves of one branch fit one answer
            steps:
            - statement: The desk asks for the shelves.
            """);
    }

    /// <summary>Writes a query whose single output group is optional, with an optional field and a named single-field row.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteLoanSummary(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-loan-summary");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-loan-summary.yaml"),
            """
            type: use-case
            id: use-case/lending.read-loan-summary
            title: Read loan summary
            summary: Lending reads one loan with its due date, note, and tags so the desk sees the loan at a glance.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            output:
              include:
                loan:
                  required: false
                  fields:
                  - name: dueDate
                    meaning: The day the loan is due.
                    type: date
                  - name: note
                    meaning: The desk note on the loan.
                    type: text
                    maxLength: 200
                    required: false
                  - name: tags
                    meaning: The tags on the loan.
                    type: list
                    rowType: LoanTagRow
                    rowShape:
                      fields:
                      - name: tag
                        type: text
                        required: true
                        meaning: One tag.
                      minItems: 0
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: one loan answers the query
            steps:
            - statement: The desk asks for one loan.
            """);
    }

    /// <summary>A single-object input maps to its named row type with its row fields, nullable when optional.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectInputRendersNamedRowType()
    {
        var input = JsonDocument.Parse("""{"name": "borrower", "meaning": "The borrower the loan is read for.", "type": "object", "rowType": "BorrowerForm", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true, "meaning": "The borrower name."}, {"name": "card", "type": "text", "required": false, "meaning": "The library card number."}]}, "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(input, out var mapped));
        Assert.Equal("Borrower", mapped!.Property);
        Assert.Equal("BorrowerForm", mapped.Type);
        Assert.Equal("BorrowerForm", mapped.RowType);
        Assert.Equal(["Name", "Card"], mapped.Row.Select(field => field.Property));
        Assert.Equal(["string", "string?"], mapped.Row.Select(field => field.Type));

        var missing = JsonDocument.Parse("""{"name": "borrower", "meaning": "The borrower the loan is read for.", "type": "object", "rowType": "BorrowerForm", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true, "meaning": "The borrower name."}]}, "required": false, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(missing, out var absent));
        Assert.Equal("BorrowerForm?", absent!.Type);
    }

    /// <summary>A single-object input without a row type maps to nothing, since a single object never collapses onto its field.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectInputWithoutRowTypeRendersNoMapping()
    {
        var input = JsonDocument.Parse("""{"name": "borrower", "meaning": "The borrower the loan is read for.", "type": "object", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true, "meaning": "The borrower name."}]}, "required": true, "origin": "caller"}""").RootElement;

        Assert.False(MessageFieldTypes.TryMapInput(input, out _));
    }

    /// <summary>A list naming an aggregate part in rowType reuses the part type and writes no local row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListWithPartRowTypeReusesPartType()
    {
        var input = JsonDocument.Parse("""{"name": "renewals", "meaning": "The renewals of the loan.", "type": "list", "rowType": "aggregate/lending.loan#renewal", "rowShape": {"fields": [{"name": "renewedAt", "type": "instant", "required": true, "meaning": "The renewal time."}, {"name": "note", "type": "text", "required": false, "meaning": "The renewal note."}]}, "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(input, out var mapped));
        Assert.Equal("IReadOnlyList<Renewal>", mapped!.Type);
        Assert.Null(mapped.RowType);
        Assert.Empty(mapped.Row);
    }

    /// <summary>A part row type renders qualified when the qualifier resolves it, else the short part name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PartRowTypeQualifiesThroughResolver()
    {
        var input = JsonDocument.Parse("""{"name": "renewals", "meaning": "The renewals of the loan.", "type": "list", "rowType": "aggregate/lending.loan#renewal", "rowShape": {"fields": [{"name": "renewedAt", "type": "instant", "required": true, "meaning": "The renewal time."}]}, "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(input, out var qualified, qualify: reference => "Library.Lending.Domain.Loans.Parts.Renewal"));
        Assert.Equal("IReadOnlyList<Library.Lending.Domain.Loans.Parts.Renewal>", qualified!.Type);

        Assert.True(MessageFieldTypes.TryMapInput(input, out var brief, qualify: _ => null));
        Assert.Equal("IReadOnlyList<Renewal>", brief!.Type);
    }

    /// <summary>A row member of type object maps to its named row type, and to nothing without one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedObjectRowRendersNamedType()
    {
        var row = JsonDocument.Parse("""{"name": "borrower", "type": "object", "required": true, "rowType": "BorrowerForm", "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}]}, "meaning": "The borrower."}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapRow(row, out var mapped));
        Assert.Equal("Borrower", mapped!.Property);
        Assert.Equal("BorrowerForm", mapped.Type);

        var missing = JsonDocument.Parse("""{"name": "borrower", "type": "object", "required": true, "rowShape": {"fields": [{"name": "name", "type": "text", "required": true}]}, "meaning": "The borrower."}""").RootElement;

        Assert.False(MessageFieldTypes.TryMapRow(missing, out _));
    }

    /// <summary>A row choice naming a closed set rides the closed-set type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowChoiceWithSetRendersClosedSet()
    {
        var row = JsonDocument.Parse("""{"name": "kind", "type": "choice", "set": "reference-data/lending.loan-kind", "required": true, "meaning": "The kind of loan."}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapRow(row, out var mapped));
        Assert.Equal("LoanKind", mapped!.Type);
    }

    /// <summary>An id identifying an entity inside an aggregate rides the part identifier.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EntityIdentifiesRendersPartIdentifier()
    {
        var input = JsonDocument.Parse("""{"name": "renewalId", "meaning": "The renewal to read.", "type": "id", "identifies": "aggregate/lending.loan#renewal", "required": true, "origin": "caller"}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(input, out var mapped));
        Assert.Equal("RenewalId", mapped!.Type);

        var row = JsonDocument.Parse("""{"name": "renewalId", "type": "id", "identifies": "aggregate/lending.loan#renewal", "required": true, "meaning": "The renewal."}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapRow(row, out var member));
        Assert.Equal("RenewalId", member!.Type);
    }

    /// <summary>A use case taking a single object renders its message member with a row file beside it.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectInputRendersMessageMemberAndRowFile()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteReadRenewals(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var query = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadRenewalsQuery.generated.cs", StringComparison.Ordinal)).Content;
        var row = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("RenewalFilter.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Contains("internal sealed partial record ReadRenewalsQuery(RenewalFilter Filter);", query, StringComparison.Ordinal);
        Assert.Contains("internal sealed partial record RenewalFilter(DateOnly From, DateOnly? To);", row, StringComparison.Ordinal);
    }

    /// <summary>Two same-named parts in different aggregates render fully qualified, each from its own declaring namespace.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SameNamedPartRowsRenderQualified()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRenewalParts(temp.Path);
        WriteReadRenewals(temp.Path);
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-renewals", "read-renewals.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "rowType: RenewalFilter",
                "rowType: aggregate/lending.loan#renewal",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var query = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadRenewalsQuery.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Contains("ReadRenewalsQuery(Library.Lending.Domain.Loans.Parts.Renewal Filter)", query, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("RenewalFilter.generated.cs", StringComparison.Ordinal));
    }

    /// <summary>Writes a query taking one filter object with a from and an optional to date.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteReadRenewals(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-renewals");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-renewals.yaml"),
            """
            type: use-case
            id: use-case/lending.read-renewals
            title: Read renewals
            summary: Lending reads the renewals of a loan within a date span so the desk sees the renewal history.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            input:
            - name: filter
              meaning: The date span to read renewals within.
              type: object
              rowType: RenewalFilter
              rowShape:
                fields:
                - name: from
                  meaning: The first day to read renewals from.
                  type: date
                  required: true
                - name: to
                  meaning: The last day to read renewals to.
                  type: date
                  required: false
              required: true
              origin: caller
              classification: internal
            output:
              include:
                renewals:
                  fields:
                  - name: renewedAt
                    meaning: The renewal time.
                    type: instant
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: the renewals of one loan fit one answer
            steps:
            - statement: The desk asks for the renewals.
            """);
    }

    /// <summary>Declares the renewal part on the loan and hold aggregates and writes each declaring type, so two parts share one name.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRenewalParts(string root)
    {
        File.AppendAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"),
            "\nparts:\n- name: renewal\n  identity: false\n");
        File.AppendAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "aggregate.yaml"),
            "\nparts:\n- name: renewal\n  identity: false\n");
        var loans = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Domain", "Loans", "Parts");
        var holds = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Domain", "Holds", "Parts");
        Directory.CreateDirectory(loans);
        Directory.CreateDirectory(holds);
        File.WriteAllText(
            Path.Combine(loans, "Renewal.cs"),
            """
            namespace Library.Lending.Domain.Loans.Parts;

            internal sealed record Renewal(DateTimeOffset RenewedAt);
            """);
        File.WriteAllText(
            Path.Combine(holds, "Renewal.cs"),
            """
            namespace Library.Lending.Domain.Holds.Parts;

            internal sealed record Renewal(DateTimeOffset RenewedAt);
            """);
    }

    /// <summary>A file row field maps to the carried bytes, as a top-level file does.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void FileRowMapsToBytes()
    {
        var single = JsonDocument.Parse("""{"name": "scans", "meaning": "The scanned pages.", "type": "list", "required": true, "origin": "caller", "rowShape": {"fields": [{"name": "scan", "type": "file", "contentTypes": ["image/png"], "maxSize": "10 MB", "required": true, "meaning": "One scanned page."}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(single, out var files));
        Assert.Equal("IReadOnlyList<byte[]>", files!.Type);

        var mixed = JsonDocument.Parse("""{"name": "pages", "meaning": "The scanned pages with titles.", "type": "list", "required": true, "origin": "caller", "rowType": "ScanPage", "rowShape": {"fields": [{"name": "title", "type": "text", "required": true, "meaning": "The page title."}, {"name": "scan", "type": "file", "contentTypes": ["image/png"], "maxSize": "10 MB", "required": false, "meaning": "One scanned page."}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(mixed, out var rows));
        Assert.Equal("IReadOnlyList<ScanPage>", rows!.Type);
        Assert.Equal("byte[]?", rows.Row.Single(row => row.Property == "Scan").Type);

        var nested = JsonDocument.Parse("""{"name": "bundles", "meaning": "The scan bundles.", "type": "list", "required": true, "origin": "caller", "rowShape": {"fields": [{"name": "pages", "meaning": "The pages of one bundle.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "scan", "type": "file", "contentTypes": ["image/png"], "maxSize": "10 MB", "required": true, "meaning": "One scanned page."}]}}]}}""").RootElement;

        Assert.True(MessageFieldTypes.TryMapInput(nested, out var bundle));
        Assert.Equal("IReadOnlyList<IReadOnlyList<byte[]>>", bundle!.Type);
    }

    /// <summary>One group holding several lists renders each list as a sibling property on a flat result.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SeveralListsInOneGroupRenderFlat()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "use-cases", "export-holds");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "export-holds.yaml"),
            """
            type: use-case
            id: use-case/lending.export-holds
            title: Export holds
            kind: query
            output:
              include:
                file:
                  fields:
                  - name: holds
                    meaning: One row per waiting hold.
                    type: list
                    rowShape:
                      fields:
                      - name: hold
                        meaning: The hold identity.
                        type: id
                        identifies: aggregate/lending.hold
                        required: true
                    rowType: ExportedHoldRow
                  - name: copies
                    meaning: One row per held copy.
                    type: list
                    rowShape:
                      fields:
                      - name: copy
                        meaning: The copy on hold.
                        type: id
                        identifies: aggregate/lending.copy
                        required: true
                    rowType: ExportedCopyRow
                  classification: internal
                  reads:
                  - aggregate/lending.hold
                  freshness:
                    mode: immediate
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var result = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ExportHoldsQueryResult.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyList<ExportedHoldRow> Holds, IReadOnlyList<ExportedCopyRow> Copies", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Group", result, StringComparison.Ordinal);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
