using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book says only true things about proof, steps and checks: never-run proof, automation serving, walked steps and embedded findings.</summary>
public sealed class BookTruthTests
{
    /// <summary>A model with no test run renders not-run wording on the home and use-case pages, and no flat verified zero.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Book_With_No_Test_Run_Says_Not_Run()
    {
        using var temp = Cli.Temp("book-truth");
        var model = WriteReviewModel(temp, root => root["testRun"] = null);
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var home = File.ReadAllText(Path.Combine(Out(temp), "index.html"));
        Assert.Contains("not run", home, StringComparison.Ordinal);
        Assert.Contains("The tests have not been run in this model", home, StringComparison.Ordinal);
        Assert.Contains("paths.testResults", home, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>0</b>", home, StringComparison.Ordinal);

        var useCase = File.ReadAllText(Path.Combine(Out(temp), "use-case", "lending", "borrow-copy.html"));
        Assert.Contains("not run", useCase, StringComparison.Ordinal);
        Assert.Contains("The tests have not been run in this model", useCase, StringComparison.Ordinal);
        Assert.DoesNotContain("a scenario is unproved or failing", useCase, StringComparison.Ordinal);

        var scenario = File.ReadAllText(Path.Combine(
            Out(temp), "scenario", "borrow-copy", "borrowing-an-available-copy-succeeds.html"));
        Assert.Contains("The tests have not been run in this model", scenario, StringComparison.Ordinal);
    }

    /// <summary>A journey step served only by a reaction renders the automation sentence with a link to the reaction.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Journey_Step_Served_Only_By_A_Reaction_Reads_As_Automation()
    {
        using var temp = Cli.Temp("book-truth");
        var model = WriteReviewModel(temp, root =>
        {
            var reaction = Records(root).Single(record => Text(record["id"]) == "reaction/lending.send-due-reminder");
            ((JsonObject)reaction["links"]!)["serves"] = new JsonArray("journey/borrowing#s2");
        });
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var journey = File.ReadAllText(Path.Combine(Out(temp), "journey", "borrowing.html"));
        Assert.Contains("served by automation", journey, StringComparison.Ordinal);
        Assert.Contains("reaction/lending/send-due-reminder.html", journey, StringComparison.Ordinal);
        Assert.DoesNotContain("no use case serves this step yet", journey, StringComparison.Ordinal);
    }

    /// <summary>A journey step a passing test walked renders as walked; the step no passing test walked does not.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Exercised_Step_Renders_As_Walked()
    {
        using var temp = Cli.Temp("book-truth");
        var model = WriteReviewModel(temp, root => root["exercisedSteps"] = new JsonArray("journey/borrowing#s1"));
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var journey = File.ReadAllText(Path.Combine(Out(temp), "journey", "borrowing.html"));
        Assert.Contains("A passing test walked this step.", journey, StringComparison.Ordinal);
        Assert.Contains("No passing test walked this step.", journey, StringComparison.Ordinal);
    }

    /// <summary>Findings embedded in the model reach the review page.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Embedded_Findings_Reach_The_Review_Page()
    {
        using var temp = Cli.Temp("book-truth");
        var model = ModelWithEmbeddedFindings(temp);
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var review = File.ReadAllText(Path.Combine(Out(temp), "review", "index.html"));
        Assert.Contains("use-case-unimplemented", review, StringComparison.Ordinal);
        Assert.Contains("has no handler, reaction or job", review, StringComparison.Ordinal);
    }

    /// <summary>The review fixture with its findings and known embedded at the model's top level.</summary>
    internal static string ModelWithEmbeddedFindings(TempFolder temp) =>
        WriteReviewModel(temp, root =>
        {
            var findings = (JsonObject)JsonNode.Parse(File.ReadAllText(Cli.Fixture("Book/findings.json")))!;
            root["findings"] = findings["findings"]?.DeepClone();
            root["known"] = findings["known"]?.DeepClone();
        });

    static string WriteReviewModel(TempFolder temp, Action<JsonObject> mutate)
    {
        var root = (JsonObject)JsonNode.Parse(File.ReadAllText(Cli.Fixture("Book/review.json")))!;
        mutate(root);
        var path = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(path, root.ToJsonString());
        return path;
    }

    static IEnumerable<JsonObject> Records(JsonObject root) =>
        ((JsonArray)root["records"]!).OfType<JsonObject>();

    static string Text(JsonNode? node) => node?.GetValue<string>() ?? "";

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");
}
