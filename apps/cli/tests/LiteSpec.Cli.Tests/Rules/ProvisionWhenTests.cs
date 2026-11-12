using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> evaluates applicability conditions against the marked record.</summary>
public sealed class ProvisionWhenTests
{
    /// <summary>A command handler omits the upload and document-number provisions.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesCommandHandlerOmitsUploadAndDocumentNumber()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ApproveExpense/ApproveExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("litespec/backend/api#bound-every-file-upload", result.Output);
        Assert.DoesNotContain("litespec/backend/api#store-every-upload-through-infrastructure", result.Output);
        Assert.DoesNotContain("litespec/backend/persistence#assign-document-numbers-from-a-sequence", result.Output);
    }

    /// <summary>A use case taking a receipt keeps the upload provisions.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesUploadUseCaseKeepsUploadProvisions()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/backend/api#store-every-upload-through-infrastructure:", result.Output);
    }

    /// <summary>A use case issuing a payout number keeps the document-number provision.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesPayoutUseCaseKeepsDocumentNumberProvision()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ReimburseExpense/ReimburseExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/backend/persistence#assign-document-numbers-from-a-sequence:", result.Output);
    }

    /// <summary>A query collapses more record provisions than a command, which leaves query-only out.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesQueryCollapsesMoreThanCommand()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var query = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ListExpenses/ListExpensesQueryHandler.cs");
        var command = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ApproveExpense/ApproveExpenseCommandHandler.cs");

        Assert.Equal(0, query.Status);
        Assert.Equal(0, command.Status);
        Assert.True(
            CollapsedCount(query.Output, "use-case/spending.list-expenses") > CollapsedCount(command.Output, "use-case/spending.approve-expense"),
            "The query handler collapses its query-only provisions while the command handler leaves them out.");
    }

    /// <summary>An erasure use case collapses more than a command without erasure.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesErasureCollapsesMoreThanPlainCommand()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var erasure = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ErasePersonalData/ErasePersonalDataCommandHandler.cs");
        var plain = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ApproveExpense/ApproveExpenseCommandHandler.cs");

        Assert.Equal(0, erasure.Status);
        Assert.Equal(0, plain.Status);
        Assert.True(
            CollapsedCount(erasure.Output, "use-case/spending.erase-personal-data") > CollapsedCount(plain.Output, "use-case/spending.approve-expense"),
            "The erasure handler collapses its erasure provisions while the plain command leaves them out.");
    }

    /// <summary>A record identifier prints conditional provisions with their condition shown.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordIdShowsWhenCondition()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "use-case/spending.approve-expense");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/backend/application#validate-query-input:", result.Output);
        Assert.Contains("when query", result.Output);
    }

    /// <summary>A path marking no record leaves conditional provisions out.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesUnmarkedPathOmitsConditionalProvisions()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/PlainCommandHandler.cs",
            "namespace Demo.Billing.Invoicing.SendInvoice;\ninternal sealed class PlainCommandHandler;\n");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/PlainCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("litespec/backend/api#store-every-upload-through-infrastructure", result.Output);
        Assert.DoesNotContain("litespec/backend/persistence#assign-document-numbers-from-a-sequence", result.Output);
    }

    /// <summary>How many provisions the collapsed line counts for the record.</summary>
    /// <param name="output">The rules output.</param>
    /// <param name="record">The record identifier.</param>
    /// <returns>The collapsed count, or -1 when the record has no collapsed line.</returns>
    static int CollapsedCount(string output, string record)
    {
        foreach (var line in output.Split('\n').Select(entry => entry.Trim()))
        {
            if (line.StartsWith(record + ":", StringComparison.Ordinal)
                && line.Contains(" provisions about the record;", StringComparison.Ordinal))
            {
                var rest = line[(record.Length + 1)..].Trim();
                var end = rest.IndexOf(" provisions about the record;", StringComparison.Ordinal);

                if (end > 0 && int.TryParse(rest[..end], out var count))
                {
                    return count;
                }
            }
        }

        return -1;
    }

    /// <summary>Copies the rules-basic fixture to a temporary root and adds one file.</summary>
    /// <param name="relative">The added file path, with forward-slash segments.</param>
    /// <param name="contents">The added file contents.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixtureWithFile(string relative, string contents)
    {
        var source = TestCli.Fixture("rules-basic");
        var temp = TestCli.Temp("rules-when");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var added = Path.Combine(temp.Path, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(added)!);
        File.WriteAllText(added, contents);
        return temp;
    }
}
