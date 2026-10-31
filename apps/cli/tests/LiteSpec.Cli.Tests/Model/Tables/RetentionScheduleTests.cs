using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Tables;

/// <summary>Proves the retention schedule renders from the aggregate data classes.</summary>
public sealed class RetentionScheduleTests
{
    /// <summary>Render writes every data class with its basis, period and enforcement.</summary>
    /// <proves>litespec/core/privacy#schedule-every-data-class-for-retention</proves>
    [Fact]
    public void Render_Writes_The_Retention_Schedule_From_Data_Classes()
    {
        using var temp = ModelFixture.Prepare("small");
        AddDataClasses(temp.Path);
        WriteSchedulePage(temp.Path);

        var render = Cli.Run("model", "render", "--root", temp.Path);
        Assert.Equal(0, render.Status);

        var page = Path.Combine(temp.Path, "docs", "behavior", "retention.md");

        Assert.Contains(
            """
            <!-- generated:retention-schedule -->
            | Data Class | Aggregate | Classification | Basis | Period | Enforced By |
            |:---|:---|:---|:---|:---|:---|
            | borrower-contact | `aggregate/lending.hold` | personal | statute | P7Y | record-expiry |
            | loan-ledger | `aggregate/lending.loan` | internal | business | none (on demand) | query-filter |
            <!-- /generated:retention-schedule -->
            """,
            File.ReadAllText(page));
    }

    static void AddDataClasses(string root)
    {
        var holds = Path.Combine(root, "docs", "behavior", "modules", "lending", "holds", "README.md");
        var holdsText = File.ReadAllText(holds);

        File.WriteAllText(holds, holdsText.Replace(
            "  \"records\": [",
            $"  \"dataClasses\": [{BorrowerContact}],\n  \"records\": ["));

        var loans = Path.Combine(root, "docs", "behavior", "modules", "lending", "loans", "README.md");
        var loansText = File.ReadAllText(loans);

        File.WriteAllText(loans, loansText.Replace(
            "  \"records\": [",
            $"  \"dataClasses\": [{LoanLedger}],\n  \"records\": ["));
    }

    static void WriteSchedulePage(string root)
    {
        var page = Path.Combine(root, "docs", "behavior", "retention.md");
        Directory.CreateDirectory(Path.GetDirectoryName(page)!);
        File.WriteAllText(page, """
            ---
            {
              "type": "retention-schedule",
              "title": "Retention",
              "owner": "lending-team",
              "status": "draft",
              "rows": []
            }
            ---
            # Retention

            <!-- generated:retention-schedule -->
            placeholder
            <!-- /generated:retention-schedule -->
            """);
    }

    /// <summary>The hold data class with a duration period.</summary>
    const string BorrowerContact = """{ "name": "borrower-contact", "classification": "personal", "fields": ["borrower-email"], "onErasure": "erase", "basis": "statute", "period": "P7Y", "enforcedBy": "record-expiry" }""";

    /// <summary>The loan data class with an on-demand period.</summary>
    const string LoanLedger = """{ "name": "loan-ledger", "classification": "internal", "fields": ["shelf"], "onErasure": "retain", "basis": "business", "period": { "mode": "none", "reason": "on demand" }, "enforcedBy": "query-filter" }""";
}
