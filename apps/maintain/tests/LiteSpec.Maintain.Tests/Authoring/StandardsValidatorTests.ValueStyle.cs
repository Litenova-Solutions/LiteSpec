using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Value-style cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    static void WriteExample(string root, string name, string frontmatter) =>
        StandardsFixture.Write(root, $"examples/sample/docs/{name}", TemplatePage(frontmatter));

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "goal statement without a period")]
    public void GoalStatementWithoutPeriod() => Check(
        root => WriteExample(root, "goal.md",
            "{\"type\":\"goal\",\"id\":\"goal/sample\",\"title\":\"Sample Goal\"," +
            "\"statement\":\"Every expense is decided within five days\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "requirement statement holding a modal word")]
    public void RequirementStatementHoldingModalWord() => Check(
        root => WriteExample(root, "approval.md",
            "{\"type\":\"topic\",\"id\":\"topic/sample\",\"title\":\"Sample\"," +
            "\"records\":[{\"type\":\"requirement\",\"id\":\"requirement/sample.entry\"," +
            "\"title\":\"Decided Within Five Days\"," +
            "\"statement\":\"Each expense should be decided within five days.\"," +
            "\"rationale\":\"Late decisions stall payouts.\"}]}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "step statement without an actor start or a period")]
    public void StepStatementWithoutActorStartOrPeriod() => Check(
        root => WriteExample(root, "journey.md",
            "{\"type\":\"journey\",\"id\":\"journey/sample\",\"title\":\"Sample Journey\"," +
            "\"steps\":[{\"id\":\"s1\",\"statement\":\"submits the expense\"}]}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "setting meaning without an article")]
    public void SettingMeaningWithoutArticle() => Check(
        root => WriteExample(root, "settings.md",
            "{\"type\":\"setting\",\"id\":\"setting/sample.entry\",\"title\":\"Sample Setting\"," +
            "\"meaning\":\"Confirmed reservation of one room.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "setting meaning over twenty-five words")]
    public void SettingMeaningOverTwentyFiveWords() => Check(
        root => WriteExample(root, "settings.md",
            "{\"type\":\"setting\",\"id\":\"setting/sample.entry\",\"title\":\"Sample Setting\"," +
            "\"meaning\":\"A room hold window policy expense claim audit trail queue batch export import sync retry delay cache lock stamp seal tag chip card pass key badge.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "rationale starting with because")]
    public void RationaleStartingWithBecause() => Check(
        root => WriteExample(root, "approval.md",
            "{\"type\":\"topic\",\"id\":\"topic/sample\",\"title\":\"Sample\"," +
            "\"records\":[{\"type\":\"requirement\",\"id\":\"requirement/sample.entry\"," +
            "\"title\":\"Decided Within Five Days\"," +
            "\"statement\":\"Each expense is decided within five days.\"," +
            "\"rationale\":\"Because late decisions stall payouts.\"}]}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "Because reason starting with because")]
    public void BecauseReasonStartingWithBecause() => Check(
        root => WriteExample(root, "constraint.md",
            "{\"type\":\"constraint\",\"id\":\"constraint/sample\",\"title\":\"Sample Constraint\"," +
            "\"appliesBecause\":\"Because partners keep the no-show risk.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "When condition starting with when")]
    public void WhenConditionStartingWithWhen() => Check(
        root => WriteExample(root, "journey.md",
            "{\"type\":\"journey\",\"id\":\"journey/sample\",\"title\":\"Sample Journey\"," +
            "\"finishedWhen\":\"When the employee holds the reimbursement.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "except condition starting with if")]
    public void ExceptConditionStartingWithIf() => Check(
        root => WriteExample(root, "approval.md",
            "{\"type\":\"topic\",\"id\":\"topic/sample\",\"title\":\"Sample\"," +
            "\"records\":[{\"type\":\"requirement\",\"id\":\"requirement/sample.entry\"," +
            "\"title\":\"Decided Within Five Days\"," +
            "\"statement\":\"Each expense is decided within five days.\"," +
            "\"rationale\":\"Late decisions stall payouts.\"," +
            "\"except\":[{\"condition\":\"If the expense waits past Friday.\"}]}]}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "failure holding a refusal code")]
    public void FailureHoldingRefusalCode() => Check(
        root => WriteExample(root, "external-system.md",
            "{\"type\":\"external-system\",\"id\":\"external-system/sample\",\"title\":\"Sample System\"," +
            "\"failure\":\"The call fails with spending.receipt-missing.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "question without a question mark")]
    public void QuestionWithoutQuestionMark() => Check(
        root => WriteExample(root, "research.md",
            "{\"type\":\"research\",\"id\":\"research/sample\",\"title\":\"Sample Study\"," +
            "\"question\":\"How fast are expenses decided\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "title starting lower case with a final period")]
    public void TitleStartingLowerCaseWithFinalPeriod() => Check(
        root => WriteExample(root, "goal.md",
            "{\"type\":\"goal\",\"id\":\"goal/sample\",\"title\":\"sample goal.\"," +
            "\"statement\":\"Every expense is decided within five days.\"}"),
        ["VALUE_STYLE"]);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "template placeholders skip the value style")]
    public void TemplatePlaceholdersSkipValueStyle() => Check(
        root => WriteExample(root, "goal.md",
            "{\"type\":\"goal\",\"id\":\"goal/<name>\",\"title\":\"<Goal display title>\"," +
            "\"statement\":\"<The measurable outcome in one sentence>\"}"),
        []);

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact(DisplayName = "shipped templates and examples carry the value style")]
    public void ShippedTemplatesAndExamplesCarryValueStyle()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        CopyDirectory(Path.Combine(Cli.Repository, "templates", "consumer"), Path.Combine(temp.Path, "templates", "consumer"));
        CopyDirectory(Path.Combine(Cli.Repository, "examples", "claimwick"), Path.Combine(temp.Path, "examples", "claimwick"));

        var result = StandardsValidator.Validate(temp.Path);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "VALUE_STYLE");
    }

    static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}
