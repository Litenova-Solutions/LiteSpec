using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the owner-team check: each record's owner belongs to the team owning its path.</summary>
public sealed class OwnerTeamCheckTests
{
    /// <summary>Owner On The Path Team Passes.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Owner_On_The_Path_Team_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "lending-team", "catalog-team");
        WriteCodeowners(temp.Path, "docs/behavior/** @acme/lending-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person"}"""));

        Assert.DoesNotContain("owner-unknown", TestModel.Codes(model));
    }

    /// <summary>Owner Outside The Path Team Reports Owner Unknown.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Owner_Outside_The_Path_Team_Reports_Owner_Unknown()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "lending-team", "catalog-team");
        WriteCodeowners(temp.Path, "docs/behavior/** @acme/lending-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "catalog-team"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "owner-unknown").Message;
        Assert.Contains("no team", message, StringComparison.Ordinal);
    }

    /// <summary>Missing Codeowners File Skips The Team Check.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Missing_Codeowners_File_Skips_The_Team_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "lending-team", "catalog-team");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "catalog-team"}"""));

        Assert.DoesNotContain("owner-unknown", TestModel.Codes(model));
    }

    /// <summary>Unmatched Path Skips The Team Check.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Unmatched_Path_Skips_The_Team_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "lending-team", "catalog-team");
        WriteCodeowners(temp.Path, "docs/product/** @acme/product-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "catalog-team"}"""));

        Assert.DoesNotContain("owner-unknown", TestModel.Codes(model));
    }

    /// <summary>Last Matching Pattern Wins.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Last_Matching_Pattern_Wins()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "team-a", "team-b");
        WriteCodeowners(temp.Path, "docs/** @acme/team-a\ndocs/behavior/** @acme/team-b\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "team-a"}"""));

        Assert.Contains("owner-unknown", TestModel.Codes(model));
    }

    /// <summary>Owner Outside The Owner List Still Reports The List Message.</summary>
    /// <proves>litespec/core/model#keep-one-owner-and-status-on-every-record</proves>
    [Fact]
    public void Owner_Outside_The_Owner_List_Still_Reports_The_List_Message()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "lending-team");
        WriteCodeowners(temp.Path, "docs/behavior/** @acme/lending-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "stranger"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "owner-unknown").Message;
        Assert.Contains("owner list", message, StringComparison.Ordinal);
    }

    /// <summary>Owner Object Mapping A Handle Passes.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Owner_Object_Mapping_A_Handle_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProjectJson(temp.Path, """{"owners": [{"name": "Lending engineering", "codeowners": ["@acme/lending-team"]}]}""");
        WriteCodeowners(temp.Path, "docs/behavior/** @acme/lending-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "Lending engineering"}"""));

        Assert.DoesNotContain("owner-unknown", TestModel.Codes(model));
    }

    /// <summary>Plain Owner Matching No Handle Reports Owner Unknown.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Plain_Owner_Matching_No_Handle_Reports_Owner_Unknown()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, "Lending engineering");
        WriteCodeowners(temp.Path, "* @a-shafie\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "Lending engineering"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "owner-unknown").Message;
        Assert.Contains("no team", message, StringComparison.Ordinal);
    }

    /// <summary>Owner Object Mapping Another Team Reports Owner Unknown.</summary>
    /// <proves>litespec/quality/ci#map-record-areas-to-owners</proves>
    [Fact]
    public void Owner_Object_Mapping_Another_Team_Reports_Owner_Unknown()
    {
        using var temp = Cli.Temp("checks");
        WriteProjectJson(temp.Path, """{"owners": [{"name": "Lending engineering", "codeowners": ["@acme/catalog-team"]}]}""");
        WriteCodeowners(temp.Path, "docs/behavior/** @acme/lending-team\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "docs", "behavior", "actors", "reader.md"), """{"kind": "person", "owner": "Lending engineering"}"""));

        Assert.Contains("owner-unknown", TestModel.Codes(model));
    }

    static void WriteProject(string root, params string[] owners) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            "{\"owners\": [" + string.Join(", ", owners.Select(owner => "\"" + owner + "\"")) + "]}");

    static void WriteProjectJson(string root, string json) =>
        File.WriteAllText(Path.Combine(root, "litespec.project.json"), json);

    static void WriteCodeowners(string root, string text)
    {
        Directory.CreateDirectory(Path.Combine(root, ".github"));
        File.WriteAllText(Path.Combine(root, ".github", "CODEOWNERS"), text);
    }
}
