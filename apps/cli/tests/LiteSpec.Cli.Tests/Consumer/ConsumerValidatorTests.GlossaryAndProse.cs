namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the glossary file.</summary>
public sealed partial class ConsumerValidatorTests
{
    const string GlossaryFile = ".litespec/glossary.yaml";

    /// <summary>Glossary without its version.</summary>
    /// <proves>litespec/core/language#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "glossary without its version")]
    public void GlossaryFileWithoutItsVersion()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateYaml(folder.Path, GlossaryFile, record => record.Remove("schemaVersion"));
        AssertProblem(RunConsumer(folder.Path), "schemaVersion");
    }

    /// <summary>Glossary with the old version.</summary>
    /// <proves>litespec/core/language#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "glossary with the old version")]
    public void GlossaryFileWithTheOldVersion()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateYaml(folder.Path, GlossaryFile, record => record["schemaVersion"] = 1);
        AssertProblem(RunConsumer(folder.Path), "/schemaVersion");
    }

    /// <summary>Glossary findings carry their code.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact(DisplayName = "glossary findings carry their code")]
    public void GlossaryFindingsCarryTheirCode()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateYaml(folder.Path, GlossaryFile, record => record.Remove("schemaVersion"));
        var problems = Problems(RunConsumer(folder.Path));
        Assert.NotEmpty(problems);
        Assert.Contains(problems, problem => problem.Contains("[glossary-invalid]", StringComparison.Ordinal));
    }
}
