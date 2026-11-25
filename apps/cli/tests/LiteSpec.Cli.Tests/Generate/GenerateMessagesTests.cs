using System.Text.Json;
using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

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

        Assert.Contains("Guid ReaderId, string? Cursor = null, int Limit = 25", query, StringComparison.Ordinal);
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

        Assert.Contains("Guid ReaderId, int Offset = 0, int Limit = 25", query, StringComparison.Ordinal);
        Assert.Contains("The row number the page starts at.", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Cursor", query, StringComparison.Ordinal);
    }

    /// <summary>A use case under tenant scope carries the tenant key when the tenancy selection names its term.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantUseCaseCarriesTenantKey()
    {
        using var temp = ModelFixture.Prepare("small");
        var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
        File.WriteAllText(
            project,
            File.ReadAllText(project).Replace(
                "selectedExtensions: []",
                "selectedExtensions:\n- id: tenancy\n  criterion: Independent customers share one deployment.\n  tenant: term/workspace",
                StringComparison.Ordinal));
        var glossary = Path.Combine(temp.Path, ".litespec", "glossary.yaml");
        File.WriteAllText(
            glossary,
            File.ReadAllText(glossary).Replace(
                "- id: term/loan",
                "- id: term/workspace\n  term: workspace\n  meaning: One customer organization sharing the deployment.\n  layer: behavior\n- id: term/loan",
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

        Assert.Contains("Guid ReaderId, Guid WorkspaceId", command, StringComparison.Ordinal);
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
        Assert.Contains("public sealed partial record ReadLoanQuery(LoanId LoanId);", query.Content, StringComparison.Ordinal);
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
                    || file.AbsolutePath.EndsWith("ReimbursedExpenseRow.generated.cs", StringComparison.Ordinal))
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

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
