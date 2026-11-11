using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new decision</c> and its number allocation.</summary>
public sealed class NewDecisionTests
{
    /// <summary>New decision numbers the first decision 1 in an empty consumer.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new decision numbers the first decision 1 in an empty consumer")]
    public void NewDecisionNumbersTheFirstDecisionOne()
    {
        using var consumer = TestCli.Temp("new-decision");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "{}");

        var result = TestCli.Run(
            "new", "decision", "receipt-threshold", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "decisions", "0001-receipt-threshold.yaml");

        Assert.True(File.Exists(page), $"Expected the decision page at '{page}'.");
        Assert.Contains("decision/0001-receipt-threshold", File.ReadAllText(page));
        Assert.True(
            File.Exists(Path.ChangeExtension(page, ".md")),
            $"Expected the explanation file beside '{page}'.");
    }

    /// <summary>New decision takes the next free number after the decisions in use.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new decision takes the next free number after the decisions in use")]
    public void NewDecisionTakesTheNextFreeNumber()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "decision", "hold-notices", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            fixture.Path, ".litespec", "intent", "decisions", "0002-hold-notices.yaml");

        Assert.True(File.Exists(page), $"Expected the decision page at '{page}'.");
        Assert.Contains("decision/0002-hold-notices", File.ReadAllText(page));
    }

    /// <summary>New decision allocates from one sequence across product and technical decisions.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new decision allocates from one sequence across product and technical decisions")]
    public void NewDecisionAllocatesAcrossBothDecisionHomes()
    {
        using var fixture = ModelFixture.Prepare("small");
        TestRecords.Write(fixture.Path, ".litespec/implementation/decisions/0002-notify-hold-ready.yaml", """
            {
              "type": "decision",
              "id": "decision/0002-notify-hold-ready",
              "title": "Notify hold ready"
            }
            """);

        var result = TestCli.Run(
            "new", "decision", "hold-notices", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "decision/0003-hold-notices",
            File.ReadAllText(Path.Combine(
                fixture.Path, ".litespec", "intent", "decisions", "0003-hold-notices.yaml")));
    }

    /// <summary>New decision refuses a name that already carries a number.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new decision refuses a name that already carries a number")]
    public void NewDecisionRefusesANameWithANumber()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "decision", "0005-hold-notices", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("takes a name without a number", result.Output);
        Assert.False(File.Exists(Path.Combine(
            fixture.Path, ".litespec", "intent", "decisions", "0005-hold-notices.yaml")));
    }

    public static TheoryData<string> RejectedNames() => new()
    {
        { "Bad_Name" },
        { "123" },
        { "a-b-c-d-e-f" },
    };

    /// <summary>New decision refuses an invalid name and writes nothing.</summary>
    /// <param name="name">One rejected decision name from the theory data.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "new decision refuses an invalid name and writes nothing")]
    [MemberData(nameof(RejectedNames))]
    public void NewDecisionRefusesAnInvalidName(string name)
    {
        using var fixture = ModelFixture.Prepare("small");
        var decisions = Path.Combine(fixture.Path, ".litespec", "intent", "decisions");
        var before = Directory.Exists(decisions)
            ? Directory.GetFiles(decisions).Order(StringComparer.Ordinal).ToArray()
            : [];

        var result = TestCli.Run(
            "new", "decision", name, "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        var after = Directory.Exists(decisions)
            ? Directory.GetFiles(decisions).Order(StringComparer.Ordinal).ToArray()
            : [];
        Assert.Equal(before, after);
    }

    /// <summary>New help documents the decision form.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new help documents the decision form")]
    public void HelpDocumentsDecisionForm()
    {
        var result = TestCli.Run("new", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("new decision <name>", result.Stdout);
    }
}
