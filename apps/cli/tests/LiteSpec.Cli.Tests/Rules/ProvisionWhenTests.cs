using System.Text.Json;
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

    /// <summary>A plain command prints no provision conditioned on another kind and keeps its judgment list short.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesPlainCommandOmitsEveryOtherKindCondition()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/ApproveExpense/ApproveExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);

        var conditional = ConditionalIds("query", "upload", "document-number", "erasure");

        Assert.NotEmpty(conditional);

        var returned = result.Output.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..(line.IndexOf(':') > 2 ? line.IndexOf(':') : line.Length)].Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in conditional)
        {
            Assert.DoesNotContain(id, returned);
        }

        Assert.True(
            JudgmentLines(result.Output) <= 30,
            "The judgment groups hold more than 30 lines.");
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

    /// <summary>A code path marking a decision never returns the project provision.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesProjectProvisionNeverReturnsOnACodePath()
    {
        using var withStage = CopyDecisionsFixture(StageBlock);
        using var withoutStage = CopyDecisionsFixture(null);
        var marked = "apps/api/src/Demo/Billing/Invoicing/Audit/InvoiceRenderer.cs";
        var withResult = TestCli.Run("rules", "--root", withStage.Path, marked);
        var withoutResult = TestCli.Run("rules", "--root", withoutStage.Path, marked);

        Assert.Equal(0, withResult.Status);
        Assert.Equal(0, withoutResult.Status);
        Assert.Equal(
            CollapsedCount(withoutResult.Output, "decision/0008-render-invoices-as-pdf"),
            CollapsedCount(withResult.Output, "decision/0008-render-invoices-as-pdf"));
    }

    /// <summary>A decision record prints the project provision with its condition shown.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesDecisionRecordShowsProjectCondition()
    {
        using var root = CopyDecisionsFixture(StageBlock);
        var result = TestCli.Run("rules", "--root", root.Path, "decision/0008-render-invoices-as-pdf");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/ext/stage#promote-through-a-recorded-decision:", result.Output);
        Assert.Contains("when project", result.Output);
    }

    /// <summary>A code path marking the named adoption decision keeps the audit-adoption provision.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesNamedDecisionKeepsAuditAdoption()
    {
        using var root = CopyDecisionsFixture(AuditBlock("decision/0007-adopt-audit-trail"));
        var named = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Audit/AuditTrailRenderer.cs");
        var other = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Audit/InvoiceRenderer.cs");

        Assert.Equal(0, named.Status);
        Assert.Equal(0, other.Status);
        Assert.Equal(
            CollapsedCount(other.Output, "decision/0008-render-invoices-as-pdf") + 1,
            CollapsedCount(named.Output, "decision/0007-adopt-audit-trail"));
    }

    /// <summary>The audit-adoption provision follows the decision the audit selection names.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesAuditAdoptionFollowsTheNamedDecision()
    {
        using var named = CopyDecisionsFixture(AuditBlock("decision/0008-render-invoices-as-pdf"));
        using var other = CopyDecisionsFixture(AuditBlock("decision/0007-adopt-audit-trail"));
        var marked = "apps/api/src/Demo/Billing/Invoicing/Audit/InvoiceRenderer.cs";
        var namedResult = TestCli.Run("rules", "--root", named.Path, marked);
        var otherResult = TestCli.Run("rules", "--root", other.Path, marked);

        Assert.Equal(0, namedResult.Status);
        Assert.Equal(0, otherResult.Status);
        Assert.Equal(
            CollapsedCount(otherResult.Output, "decision/0008-render-invoices-as-pdf") + 1,
            CollapsedCount(namedResult.Output, "decision/0008-render-invoices-as-pdf"));
    }

    /// <summary>A decision record prints the audit-adoption provision with its condition shown, even unnamed.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesDecisionRecordShowsAuditAdoptionCondition()
    {
        using var root = CopyDecisionsFixture(AuditBlock("decision/0007-adopt-audit-trail"));
        var result = TestCli.Run("rules", "--root", root.Path, "decision/0008-render-invoices-as-pdf");

        Assert.Equal(0, result.Status);
        Assert.Contains("- litespec/ext/audit#record-the-audit-adoption-decision:", result.Output);
        Assert.Contains("when audit-adoption", result.Output);
    }

    /// <summary>The stage extension selection, as YAML under selectedExtensions.</summary>
    const string StageBlock = """
        - id: stage
          criterion: no deployment holds customer data
          releaseStage: pre-production
        """;

    /// <summary>The audit extension selection naming the adoption decision, as YAML under selectedExtensions.</summary>
    /// <param name="decision">The adoption decision identifier.</param>
    /// <returns>The selection block.</returns>
    static string AuditBlock(string decision) => $"""
        - id: audit
          criterion: customer data needs an audit trail
          decision: {decision}
        """;

    /// <summary>Copies the rules-basic fixture with two decisions and two files marking them.</summary>
    /// <param name="extensions">The selectedExtensions block, or null to leave the selection empty.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyDecisionsFixture(string? extensions)
    {
        var source = TestCli.Fixture("rules-basic");
        var temp = TestCli.Temp("rules-decided-when");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        if (extensions is not null)
        {
            var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
            File.WriteAllText(project, File.ReadAllText(project).Replace(
                "selectedExtensions: []",
                "selectedExtensions:\n" + extensions,
                StringComparison.Ordinal));
        }

        AddFixtureFile(
            temp,
            ".litespec/implementation/decisions/0007-adopt-audit-trail.yaml",
            """
            type: decision
            id: decision/0007-adopt-audit-trail
            title: Adopt Audit Trail
            choice: Pipeline producer
            reopenedWhen: The trail outgrows one store.
            """);
        AddFixtureFile(
            temp,
            ".litespec/implementation/decisions/0008-render-invoices-as-pdf.yaml",
            """
            type: decision
            id: decision/0008-render-invoices-as-pdf
            title: Render Invoices as PDF
            choice: PDF library
            reopenedWhen: Invoices stop needing a downloadable file.
            """);
        AddFixtureFile(
            temp,
            "apps/api/src/Demo/Billing/Invoicing/Audit/AuditTrailRenderer.cs",
            """
            namespace Demo.Billing.Invoicing.Audit;

            /// <summary>Renders the audit choice.</summary>
            /// <follows>decision/0007-adopt-audit-trail</follows>
            internal sealed class AuditTrailRenderer;
            """);
        AddFixtureFile(
            temp,
            "apps/api/src/Demo/Billing/Invoicing/Audit/InvoiceRenderer.cs",
            """
            namespace Demo.Billing.Invoicing.Audit;

            /// <summary>Renders the invoice choice.</summary>
            /// <follows>decision/0008-render-invoices-as-pdf</follows>
            internal sealed class InvoiceRenderer;
            """);

        return temp;
    }

    /// <summary>Adds one file to a temporary consumer root.</summary>
    /// <param name="root">The temporary consumer root.</param>
    /// <param name="relative">The file path, with forward-slash segments.</param>
    /// <param name="contents">The file contents.</param>
    static void AddFixtureFile(TempFolder root, string relative, string contents)
    {
        var file = Path.Combine(root.Path, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, contents);
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

    /// <summary>How many statement lines the judgment groups hold.</summary>
    /// <param name="output">The rules output.</param>
    /// <returns>The count of provision lines under Required, Defaults, Advisory and Patterns.</returns>
    static int JudgmentLines(string output)
    {
        var count = 0;
        var judging = false;

        foreach (var line in output.Split('\n').Select(entry => entry.Trim()))
        {
            if (line is "Required:" or "Defaults:" or "Advisory:" or "Patterns:")
            {
                judging = true;
                continue;
            }

            if (line is "Checked by a tool:" or "Concern-wide:" or "Decided:")
            {
                judging = false;
                continue;
            }

            if (judging && line.StartsWith("- ", StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The identifiers of every provision carrying one of the conditions.</summary>
    /// <param name="conditions">The applicability conditions.</param>
    /// <returns>The provision identifiers.</returns>
    static IReadOnlyList<string> ConditionalIds(params string[] conditions)
    {
        var wanted = conditions.ToHashSet(StringComparer.Ordinal);
        var ids = new List<string>();
        var file = Path.Combine(TestCli.Repository, "docs", "reference", "provisions.json");

        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("provisions", out var list)
            && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray()
                : Enumerable.Empty<JsonElement>();

        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = Field(item, "id");
            var when = Field(item, "when");

            if (id.Length > 0 && wanted.Contains(when))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Field(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

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
