using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Template source cases: each record template is checked against the Claimwick example record its identifier names.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>A record template equal to the example record it names.</summary>
    /// <proves>litespec/core/authoring#copy-each-template-from-the-example</proves>
    [Fact(DisplayName = "record template that copies its example record")]
    public void TemplateCopyingItsExampleRecord() => Check(
        root => WriteGoal(root, "A", "A"),
        []);

    /// <summary>A record template whose text differs from the example record it names.</summary>
    /// <proves>litespec/core/authoring#copy-each-template-from-the-example</proves>
    [Fact(DisplayName = "record template that differs from its example record")]
    public void TemplateDifferingFromItsExampleRecord() => Check(
        root => WriteGoal(root, "A", "B"),
        [MaintainerDiagnostics.Codes.TemplateSourceDiffers]);

    /// <summary>A record template whose identifier names no example record.</summary>
    /// <proves>litespec/core/authoring#copy-each-template-from-the-example</proves>
    [Fact(DisplayName = "record template naming no example record")]
    public void TemplateNamingNoExampleRecord() => Check(
        root =>
        {
            StandardsFixture.Write(root, "examples/claimwick/.litespec/intent/goals/x/x.yaml", GoalRecord("goal/x", "A"));
            StandardsFixture.Write(root, "templates/consumer/goal.yaml", GoalRecord("goal/y", "A"));
        },
        [MaintainerDiagnostics.Codes.TemplateSourceMissing]);

    /// <summary>Writes an example goal and a template goal with the given summaries.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="example">The summary the example record carries.</param>
    /// <param name="template">The summary the template carries.</param>
    private static void WriteGoal(string root, string example, string template)
    {
        StandardsFixture.Write(root, "examples/claimwick/.litespec/intent/goals/x/x.yaml", GoalRecord("goal/x", example));
        StandardsFixture.Write(root, "templates/consumer/goal.yaml", GoalRecord("goal/x", template));
    }

    /// <summary>The text of a small goal record.</summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="summary">The summary text.</param>
    /// <returns>The record text.</returns>
    private static string GoalRecord(string id, string summary) => $"type: goal\nid: {id}\ntitle: X\nsummary: {summary}\n";
}
