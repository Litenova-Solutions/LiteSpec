using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves shared contract rows render the declared type with no local row, nested objects hold one more list, and nested row types naming library types render as is.</summary>
public sealed class GenerateSharedRowsTests
{
    /// <summary>Two commands taking the same declared row reuse its type, write no local row, and the rich command matches its golden.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void SharedMessageRowReusesBackendType()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteSharedLoanLine(temp.Path);
        AddLinesInput(temp.Path, "borrow-copy");
        AddLinesInput(temp.Path, "extend-loan");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              commandInterface: Library.Messaging.ICommand
              queryInterface: Library.Messaging.IQuery
              usings:
              - Library.Lending.Contracts
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("LoanLine.generated.cs", StringComparison.Ordinal));
        Assert.Contains("IReadOnlyList<LoanLine> Lines", Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<LoanLine> Lines", Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ExtendLoanCommand.generated.cs", StringComparison.Ordinal)).Content, StringComparison.Ordinal);
        GenerateGoldens.AssertEqual(
            "messages-shared-row-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content);
    }

    /// <summary>A result list taking a declared row reuses its type, writes no local row, and matches its golden.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void SharedResultRowReusesBackendType()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteSharedLoanLine(temp.Path);
        WriteReadLines(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
              usings:
              - Library.Lending.Contracts
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("ReadLines/LoanLine.generated.cs", StringComparison.Ordinal));
        GenerateGoldens.AssertEqual(
            "results-shared-row-dotnet.txt",
            Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("ReadLinesQueryResult.generated.cs", StringComparison.Ordinal)).Content);
    }

    /// <summary>A hand-written sibling beside a generated row keeps the row local.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SharedRowWithHandSiblingStaysLocal()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "ListLoans");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "LoansRow.generated.cs"), "internal sealed partial record LoansRow();\n");
        File.WriteAllText(Path.Combine(folder, "LoansRow.cs"), "internal sealed partial record LoansRow;\n");
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Contains(plan.Files, file => file.AbsolutePath.EndsWith("LoansRow.generated.cs", StringComparison.Ordinal));
    }

    /// <summary>A nested object holding a list renders its named type with the list inside its local row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedObjectHoldingListRenders()
    {
        using var temp = ModelFixture.Prepare("small");
        AddTotalsInput(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var command = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal)).Content;
        var row = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("FineTotals.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("FineTotals Totals", command, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<FineLine> Fines", row, StringComparison.Ordinal);
        Assert.Contains("Money Total", row, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("FineLine.generated.cs", StringComparison.Ordinal));
    }

    /// <summary>A nested list holding a list renders no message.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedListHoldingListSkipsMessage()
    {
        using var temp = ModelFixture.Prepare("small");
        AddMatrixInput(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal));
    }

    /// <summary>A nested list naming a library type renders the type as is with no local row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedLibraryRowTypeRendersAsIs()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteVerifyCopy(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var row = Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith("BrokenSegment.generated.cs", StringComparison.Ordinal)).Content;

        Assert.Contains("IReadOnlyList<BreakValue> Breaks", row, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.EndsWith("BreakValue.generated.cs", StringComparison.Ordinal));
    }

    /// <summary>Writes the shared copy-line row the lending commands reuse.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteSharedLoanLine(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Contracts");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "LoanLine.cs"), "namespace Library.Lending.Contracts;\n\npublic sealed record LoanLine(string CopyCode, int LoanDays);\n");
    }

    /// <summary>Adds the shared copy-line list input to one loan command.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="useCase">The use-case folder holding the record.</param>
    static void AddLinesInput(string root, string useCase)
    {
        var path = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", useCase, useCase + ".yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "idempotency:",
                """
                - name: lines
                  meaning: The copy lines the loan covers.
                  type: list
                  rowShape:
                    fields:
                    - name: copyCode
                      meaning: The shelf code of the copy.
                      type: text
                      required: true
                    - name: loanDays
                      meaning: The days the copy is lent.
                      type: integer
                      required: true
                  rowType: LoanLine
                  required: true
                  origin: caller
                  binding: body
                  classification: internal
                idempotency:
                """,
                StringComparison.Ordinal));
    }

    /// <summary>Adds a fine-totals object holding a per-day list to the borrow command.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void AddTotalsInput(string root)
    {
        var path = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "idempotency:",
                """
                - name: totals
                  meaning: The fine totals of the loan.
                  type: object
                  rowShape:
                    fields:
                    - name: fines
                      meaning: The per-day fine lines.
                      type: list
                      required: true
                      rowType: FineLine
                      rowShape:
                        fields:
                        - name: day
                          meaning: The fined day.
                          type: date
                          required: true
                        - name: amount
                          meaning: The fined amount.
                          type: money
                          currency: EUR
                          required: true
                    - name: total
                      meaning: The grand total.
                      type: money
                      currency: EUR
                      required: true
                  rowType: FineTotals
                  required: true
                  origin: caller
                  binding: body
                  classification: internal
                idempotency:
                """,
                StringComparison.Ordinal));
    }

    /// <summary>Adds a matrix list holding a list to the borrow command, which the vocabulary cannot render.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void AddMatrixInput(string root)
    {
        var path = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "idempotency:",
                """
                - name: matrix
                  meaning: The shelf matrix the loan covers.
                  type: list
                  rowShape:
                    fields:
                    - name: rows
                      meaning: The matrix rows.
                      type: list
                      required: true
                      rowShape:
                        fields:
                        - name: cells
                          meaning: The matrix cells.
                          type: list
                          required: true
                          rowShape:
                            fields:
                            - name: cell
                              meaning: One cell.
                              type: text
                              required: true
                  rowType: ShelfMatrix
                  required: true
                  origin: caller
                  binding: body
                  classification: internal
                idempotency:
                """,
                StringComparison.Ordinal));
    }

    /// <summary>Writes a query listing loan lines through the shared copy-line row.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteReadLines(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-lines");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-lines.yaml"),
            """
            type: use-case
            id: use-case/lending.read-lines
            title: Read lines
            summary: Lending returns the copy lines of one loan.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            reachedThrough:
            - entry-point/library-app
            input:
            - name: loanId
              meaning: The loan to read.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              classification: internal
            output:
              include:
                rows:
                  fields:
                  - name: lines
                    meaning: The copy lines of the loan.
                    type: list
                    rowShape:
                      fields:
                      - name: copyCode
                        meaning: The shelf code of the copy.
                        type: text
                        required: true
                      - name: loanDays
                        meaning: The days the copy is lent.
                        type: integer
                        required: true
                    rowType: LoanLine
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: one loan answers the query
            steps:
            - statement: The reader reads the lines of one loan.
            """);
    }

    /// <summary>Writes a query verifying a copy, whose broken segments hold library-typed break values.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteVerifyCopy(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "verify-copy");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "verify-copy.yaml"),
            """
            type: use-case
            id: use-case/lending.verify-copy
            title: Verify copy
            summary: Lending verifies a copy against the catalog and lists what breaks the match.
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            reachedThrough:
            - entry-point/library-app
            input:
            - name: copyId
              meaning: The copy to verify.
              type: id
              identifies: aggregate/lending.copy
              required: true
              origin: caller
              classification: internal
            output:
              include:
                rows:
                  fields:
                  - name: segments
                    meaning: The broken segments of the copy record.
                    type: list
                    rowShape:
                      fields:
                      - name: segment
                        meaning: The segment name.
                        type: text
                        required: true
                      - name: breaks
                        meaning: The break values of the segment.
                        type: list
                        required: true
                        rowType: BreakValue
                        rowShape:
                          fields:
                          - name: value
                            meaning: One break value.
                            type: text
                            required: true
                    rowType: BrokenSegment
                  classification: internal
                  reads:
                  - aggregate/lending.copy
                  freshness:
                    mode: immediate
            paging:
              mode: none
              reason: one copy answers the query
            steps:
            - statement: The catalog verifies the copy record.
            """);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
