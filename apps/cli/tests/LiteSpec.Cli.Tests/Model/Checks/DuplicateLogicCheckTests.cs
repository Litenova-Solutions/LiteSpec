using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves one rule, calculation or projection keeps one home instead of living as a copy in two slices.</summary>
public sealed class DuplicateLogicCheckTests
{
    /// <summary>Same Body In Two Use Case Folders Reports Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SameBodyInTwoUseCaseFoldersReportsDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.ApproveExpense", "ApproveExpenseTotals"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("SubmitExpenseTotals.cs", finding.Message);
        Assert.Contains("ApproveExpenseTotals.cs", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Renamed Locals In Two Slices Still Report Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void RenamedLocalsInTwoSlicesStillReportDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs", """
            namespace Library.Spending.Application.Expenses.ApproveExpense;

            public sealed class ApproveExpenseTotals
            {
                public decimal TotalFor(IReadOnlyList<ExpenseLine> rows, decimal factor, int length)
                {
                    var sum = 0m;
                    var number = 0;
                    foreach (var row in rows)
                    {
                        sum += row.Amount;
                        number++;
                    }
                    var mean = number == 0 ? 0m : sum / number;
                    var grant = mean * factor;
                    var limit = Math.Min(grant, 500m);
                    return limit * length;
                }
            }
            """);

        var finding = Assert.Single(Run(model));
        Assert.Contains("SubmitExpenseTotals.cs", finding.Message);
        Assert.Contains("ApproveExpenseTotals.cs", finding.Message);
    }

    /// <summary>Same Body Across Modules Reports Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SameBodyAcrossModulesReportsDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Reporting", "Application", "Reimbursements"), "ReimbursementTotals.cs", TotalsClass("Library.Reporting.Application.Reimbursements", "ReimbursementTotals"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("SubmitExpenseTotals.cs", finding.Message);
        Assert.Contains("ReimbursementTotals.cs", finding.Message);
    }

    /// <summary>Same Body In Same Folder Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SameBodyInSameFolderPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        var folder = Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense");
        WriteSource(temp.Path, folder, "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, folder, "SubmitExpenseAverages.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseAverages"));

        Assert.Empty(Run(model));
    }

    /// <summary>Same Body In Same File Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SameBodyInSameFilePasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", """
            namespace Library.Spending.Application.Expenses.SubmitExpense;

            public sealed class SubmitExpenseTotals
            {
                public decimal TotalFor(IReadOnlyList<ExpenseLine> lines, decimal rate, int days)
                {
                    var total = 0m;
                    var count = 0;
                    foreach (var line in lines)
                    {
                        total += line.Amount;
                        count++;
                    }
                    var average = count == 0 ? 0m : total / count;
                    var allowance = average * rate;
                    var capped = Math.Min(allowance, 500m);
                    return capped * days;
                }

                public decimal TotalAgain(IReadOnlyList<ExpenseLine> lines, decimal rate, int days)
                {
                    var total = 0m;
                    var count = 0;
                    foreach (var line in lines)
                    {
                        total += line.Amount;
                        count++;
                    }
                    var average = count == 0 ? 0m : total / count;
                    var allowance = average * rate;
                    var capped = Math.Min(allowance, 500m);
                    return capped * days;
                }
            }
            """);

        Assert.Empty(Run(model));
    }

    /// <summary>Different Member Call Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void DifferentMemberCallPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs",
            TotalsClass("Library.Spending.Application.Expenses.ApproveExpense", "ApproveExpenseTotals").Replace("line.Amount", "line.Quantity"));

        Assert.Empty(Run(model));
    }

    /// <summary>Different Literal Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void DifferentLiteralPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs",
            TotalsClass("Library.Spending.Application.Expenses.ApproveExpense", "ApproveExpenseTotals").Replace("500m", "400m"));

        Assert.Empty(Run(model));
    }

    /// <summary>Small Body Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SmallBodyPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        const string body = """
            namespace Library.Spending.Application.Expenses.SUBMIT;

            public sealed class CLASS
            {
                public decimal Doubled(decimal value)
                {
                    var doubled = value * 2;
                    var rounded = Math.Round(doubled);
                    return rounded;
                }
            }
            """;
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs",
            body.Replace("SUBMIT", "SubmitExpense").Replace("CLASS", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs",
            body.Replace("SUBMIT", "ApproveExpense").Replace("CLASS", "ApproveExpenseTotals"));

        Assert.Empty(Run(model));
    }

    /// <summary>Six Statement Body Reports Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SixStatementBodyReportsDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseSums.cs", SumClass("SubmitExpense", "SubmitExpenseSums", six: true));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseSums.cs", SumClass("ApproveExpense", "ApproveExpenseSums", six: true));

        Assert.Single(Run(model));
    }

    /// <summary>Five Statement Body Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void FiveStatementBodyPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseSums.cs", SumClass("SubmitExpense", "SubmitExpenseSums", six: false));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseSums.cs", SumClass("ApproveExpense", "ApproveExpenseSums", six: false));

        Assert.Empty(Run(model));
    }

    /// <summary>Long Single Statement Reports Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void LongSingleStatementReportsDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        var sum = "return " + string.Join(" + ", Enumerable.Repeat("a", 40)) + ";";
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseSums.cs", LongSumClass("SubmitExpense", "SubmitExpenseSums", sum));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseSums.cs", LongSumClass("ApproveExpense", "ApproveExpenseSums", sum));

        Assert.Single(Run(model));
    }

    /// <summary>Same Local Function In Two Slices Reports Duplicate.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void SameLocalFunctionInTwoSlicesReportsDuplicate()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", LocalSumClass("SubmitExpense", "SubmitExpenseTotals", "submit"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs", LocalSumClass("ApproveExpense", "ApproveExpenseTotals", "approve"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("SubmitExpenseTotals.cs", finding.Message);
        Assert.Contains("ApproveExpenseTotals.cs", finding.Message);
    }

    /// <summary>Generated File Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void GeneratedFilePasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.g.cs", TotalsClass("Library.Spending.Application.Expenses.ApproveExpense", "ApproveExpenseTotals"));

        Assert.Empty(Run(model));
    }

    /// <summary>Auto Generated Marker Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void AutoGeneratedMarkerPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Application", "Expenses", "ApproveExpense"), "ApproveExpenseTotals.cs",
            "// <auto-generated/>\n" + TotalsClass("Library.Spending.Application.Expenses.ApproveExpense", "ApproveExpenseTotals"));

        Assert.Empty(Run(model));
    }

    /// <summary>Test Project Copy Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</proves>
    [Fact]
    public void TestProjectCopyPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProjectWithTestRoots(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteFile(temp.Path, Path.Combine("src", "Library.Spending", "Application", "Expenses", "SubmitExpense"), "SubmitExpenseTotals.cs", TotalsClass("Library.Spending.Application.Expenses.SubmitExpense", "SubmitExpenseTotals"));
        WriteFile(temp.Path, Path.Combine("src", "Library.Tests"), "ApproveExpenseTotals.cs", TotalsClass("Library.Tests", "ApproveExpenseTotals"));

        Assert.Empty(Run(model));
    }

    /// <summary>Identical TryParse On Two Identifiers Passes.</summary>
    /// <proves>litespec/backend/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact]
    public void IdenticalTryParseOnTwoIdentifiersPasses()
    {
        using var temp = TestCli.Temp("duplicates");
        WriteProject(temp.Path);
        var model = ModelWithoutRecords(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Expenses"), "ExpenseId.cs", IdentifierClass("Library.Spending.Domain.Expenses", "ExpenseId"));
        WriteSource(temp.Path, Path.Combine("Library.Reporting", "Domain", "Loans"), "LoanId.cs", IdentifierClass("Library.Reporting.Domain.Loans", "LoanId"));

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the duplicate logic check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The logic duplicate findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        DuplicateLogicChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "logic-duplicate").ToList();
    }

    /// <summary>A model with no records: the check reads backend source only.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the loaded configuration.</returns>
    static ProjectModel ModelWithoutRecords(string root) => new() { Config = ConsumerConfig.Load(root) };

    /// <summary>One totals class with a nine-statement body.</summary>
    /// <param name="ns">The namespace of the class.</param>
    /// <param name="name">The class name.</param>
    /// <returns>The file text.</returns>
    static string TotalsClass(string ns, string name) =>
        """
        namespace NS;

        public sealed class NAME
        {
            public decimal TotalFor(IReadOnlyList<ExpenseLine> lines, decimal rate, int days)
            {
                var total = 0m;
                var count = 0;
                foreach (var line in lines)
                {
                    total += line.Amount;
                    count++;
                }
                var average = count == 0 ? 0m : total / count;
                var allowance = average * rate;
                var capped = Math.Min(allowance, 500m);
                return capped * days;
            }
        }
        """.Replace("NS", ns).Replace("NAME", name);

    /// <summary>One sums class with a five- or six-statement body.</summary>
    /// <param name="useCase">The use case folder name.</param>
    /// <param name="name">The class name.</param>
    /// <param name="six">Whether the body holds six statements rather than five.</param>
    /// <returns>The file text.</returns>
    static string SumClass(string useCase, string name, bool six)
    {
        var fifth = six ? "        var fifth = fourth / 2;\n        return fifth + b;" : "        return fourth + b;";

        return """
            namespace Library.Spending.Application.Expenses.USECASE;

            public sealed class NAME
            {
                public decimal Sum(decimal a, decimal b, decimal c)
                {
                    var first = a + b;
                    var second = first + c;
                    var third = second * 2;
                    var fourth = third - a;
            FIFTH
                }
            }
            """.Replace("USECASE", useCase).Replace("NAME", name).Replace("FIFTH", fifth);
    }

    /// <summary>One sums class with a single long statement.</summary>
    /// <param name="useCase">The use case folder name.</param>
    /// <param name="name">The class name.</param>
    /// <param name="sum">The return statement text.</param>
    /// <returns>The file text.</returns>
    static string LongSumClass(string useCase, string name, string sum) =>
        """
        namespace Library.Spending.Application.Expenses.USECASE;

        public sealed class NAME
        {
            public decimal Total(decimal a)
            {
                SUM
            }
        }
        """.Replace("USECASE", useCase).Replace("NAME", name).Replace("SUM", sum);

    /// <summary>One totals class whose outer method differs by one literal while its local function repeats exactly.</summary>
    /// <param name="useCase">The use case folder name.</param>
    /// <param name="name">The class name.</param>
    /// <param name="source">The source literal naming this slice.</param>
    /// <returns>The file text.</returns>
    static string LocalSumClass(string useCase, string name, string source) =>
        """
        namespace Library.Spending.Application.Expenses.USECASE;

        public sealed class NAME
        {
            public decimal Total(IReadOnlyList<ExpenseLine> lines)
            {
                var origin = "SOURCE";
                return Sum(lines) + origin.Length;

                decimal Sum(IReadOnlyList<ExpenseLine> rows)
                {
                    var total = 0m;
                    var count = 0;
                    foreach (var row in rows)
                    {
                        total += row.Amount;
                        count++;
                    }
                    var average = count == 0 ? 0m : total / count;
                    var capped = Math.Min(average, 500m);
                    return capped;
                }
            }
        }
        """.Replace("USECASE", useCase).Replace("NAME", name).Replace("SOURCE", source);

    /// <summary>One typed identifier with the TryParse each identifier carries.</summary>
    /// <param name="ns">The namespace of the identifier.</param>
    /// <param name="name">The identifier type name.</param>
    /// <returns>The file text.</returns>
    static string IdentifierClass(string ns, string name) =>
        """
        namespace NS;

        public readonly record struct NAME(Guid Value) : IParsable<NAME>
        {
            public static NAME From(Guid value) => new NAME(value);

            public static NAME Parse(string value, IFormatProvider? provider) => From(Guid.Parse(value));

            public static bool TryParse(string? value, IFormatProvider? provider, out NAME result)
            {
                if (Guid.TryParse(value, out var parsed))
                {
                    result = From(parsed);
                    return true;
                }

                result = default;
                return false;
            }
        }
        """.Replace("NS", ns).Replace("NAME", name);

    /// <summary>Writes the fixture project with two modules, the Kernel and the Platform.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": ["src/Library.Spending", "src/Library.Reporting"],
              "kernel": "src/Library.Kernel", "platform": "src/Library.Platform"}}}
            """);
    }

    /// <summary>Writes the fixture project with the whole source tree as one modules root and the tests declared as test roots.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProjectWithTestRoots(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": ["src"]}},
             "paths": {"testRoots": ["src/Library.Tests"]}}
            """);
    }

    /// <summary>Writes one C# source file under a module project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The project folder path under the source root.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string root, string project, string file, string text) =>
        WriteFile(root, Path.Combine("src", project), file, text);

    /// <summary>Writes one C# source file under a folder relative to the fixture root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The folder path relative to the fixture root.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteFile(string root, string folder, string file, string text)
    {
        var absolute = Path.Combine(root, folder);
        Directory.CreateDirectory(absolute);
        File.WriteAllText(Path.Combine(absolute, file), text);
    }
}
