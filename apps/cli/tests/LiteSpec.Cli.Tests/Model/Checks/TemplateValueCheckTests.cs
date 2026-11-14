using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves <c>model check</c> reports a record that <c>litespec new</c> scaffolded and that still keeps a value its template copied.</summary>
public sealed class TemplateValueCheckTests
{
    /// <summary>A goal scaffolded and left as the template wrote it reports template-value-unchanged.</summary>
    /// <proves>litespec/core/authoring#copy-each-template-from-the-example</proves>
    [Fact(DisplayName = "model check reports a scaffold that keeps its template values")]
    public void ReportsAScaffoldThatKeepsItsTemplateValues()
    {
        using var consumer = TestCli.Temp("template-value");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "{\"explanations\":{\"layers\":[\"intent\"]}}");
        Assert.Equal(0, TestCli.Run("new", "goal", "faster-reimbursement", "--root", consumer.Path).Status);

        var result = TestCli.Run("model", "check", "--root", consumer.Path, "--all");

        Assert.Contains("template-value-unchanged", result.Output);
    }
}
