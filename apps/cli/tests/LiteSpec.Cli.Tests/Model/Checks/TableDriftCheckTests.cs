using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the table-drift check against hand-built models.</summary>
public sealed class TableDriftCheckTests
{
    [Fact]
    public void Transitions_Table_Matching_Frontmatter_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "loan.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"states": [{"name": "Active", "meaning": "The loan is open."}], "transitions": [{"from": "-", "to": "Active"}]}"""));
        model.Bodies.Add(TestModel.Body(file, "# Loan\n\n## Transitions\n\n| From | To |\n| --- | --- |\n| - | Active |\n"));

        Assert.DoesNotContain("table-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Transitions_Table_Differing_From_Frontmatter_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "loan.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"states": [{"name": "Active", "meaning": "The loan is open."}], "transitions": [{"from": "-", "to": "Active"}]}"""));
        model.Bodies.Add(TestModel.Body(file, "# Loan\n\n## Transitions\n\n| From | To |\n| --- | --- |\n| - | Archived |\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "table-drift");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/lending.loan' holds a transitions table that differs from its frontmatter.", finding.Message);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    [Fact]
    public void Data_Table_Matching_Frontmatter_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "loan.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"states": [{"name": "Active", "meaning": "The loan is open."}], "data": [{"name": "dueDate", "meaning": "The day the loan ends.", "classification": "internal"}]}"""));
        model.Bodies.Add(TestModel.Body(file, "# Loan\n\n## Data\n\n| Name | Meaning | Classification |\n| --- | --- | --- |\n| dueDate | The day the loan ends. | internal |\n"));

        Assert.DoesNotContain("table-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Data_Table_Differing_From_Frontmatter_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "loan.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"states": [{"name": "Active", "meaning": "The loan is open."}], "data": [{"name": "dueDate", "meaning": "The day the loan ends.", "classification": "internal"}]}"""));
        model.Bodies.Add(TestModel.Body(file, "# Loan\n\n## Data\n\n| Name | Meaning | Classification |\n| --- | --- | --- |\n| dueDate | The day the loan ends. | personal |\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "table-drift");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/lending.loan' holds a data table that differs from its frontmatter.", finding.Message);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    [Fact]
    public void Aggregate_Without_A_Body_Table_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "loan.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", file, """{"states": [{"name": "Active", "meaning": "The loan is open."}], "transitions": [{"from": "-", "to": "Active"}]}"""));
        model.Bodies.Add(TestModel.Body(file, "# Loan\n\nA loan is one copy lent to one reader.\n"));

        Assert.DoesNotContain("table-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Retention_Table_Matching_Rows_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "retention.md");
        File.WriteAllText(file, RetentionPage("P7Y"));
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Bodies.Add(TestModel.Body(file, RetentionBody()));

        Assert.DoesNotContain("table-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Retention_Table_Differing_From_Rows_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "retention.md");
        File.WriteAllText(file, RetentionPage("P1Y"));
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Bodies.Add(TestModel.Body(file, RetentionBody()));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "table-drift");
        Assert.Equal("A", finding.Tier);
        Assert.Equal($"Retention schedule '{file}' holds a retention table that differs from its rows.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A retention-schedule page carrying one row with the period.</summary>
    /// <param name="period">The retention period written in the row, in ISO duration form.</param>
    /// <returns>The complete retention-schedule page text.</returns>
    static string RetentionPage(string period) =>
        "---\n"
        + "{\n"
        + "  \"type\": \"retention-schedule\",\n"
        + "  \"title\": \"Retention\",\n"
        + "  \"owner\": \"lending-team\",\n"
        + "  \"status\": \"draft\",\n"
        + "  \"rows\": [\n"
        + "    {\n"
        + "      \"dataClass\": \"loans\",\n"
        + "      \"covers\": [\"aggregate/lending.loan\"],\n"
        + "      \"basis\": \"business\",\n"
        + $"      \"period\": \"{period}\",\n"
        + "      \"enforcedBy\": \"job/lending.expiry-sweep\"\n"
        + "    }\n"
        + "  ]\n"
        + "}\n"
        + "---\n"
        + "\n"
        + RetentionBody();

    /// <summary>A retention body table holding the loans row with a seven-year period.</summary>
    /// <returns>The retention page body text.</returns>
    static string RetentionBody() =>
        "# Retention\n\n| Data Class | Covers | Basis | Period | Enforced By |\n| --- | --- | --- | --- | --- |\n| loans | aggregate/lending.loan | business | P7Y | job/lending.expiry-sweep |\n";
}
