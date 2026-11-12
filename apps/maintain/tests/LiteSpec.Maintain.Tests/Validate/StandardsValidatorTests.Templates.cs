using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Template citation cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Template citing a provision the index holds.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "template citing a provision the index holds")]
    public void TemplateCitingKnownProvision() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample Template\n\nFollow `litespec/core/topic#keep-the-topic-bounded` when writing.\n"),
        []);

    /// <summary>Template citing a provision the index lacks.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "template citing a provision the index lacks")]
    public void TemplateCitingUnknownProvision() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample Template\n\nFollow `litespec/core/topic#no-such-provision` when writing.\n"),
        [MaintainerDiagnostics.Codes.TemplateCitationUnknown],
        [MaintainerDiagnostics.Codes.IdUnknownReference]);

    /// <summary>Template placeholder shaped like a citation.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "template placeholder shaped like a citation")]
    public void TemplatePlaceholderIsNotCitation() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample Template\n\n**ID:** `{litespec/page#state-one-action}`\n"),
        []);

    /// <summary>Record template citing a provision the index holds.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "record template citing a provision the index holds")]
    public void TemplateYamlCitingKnownProvision() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/sample.yaml", "covers:\n- litespec/core/topic#keep-the-topic-bounded\n"),
        []);

    /// <summary>Record template citing a provision the index lacks.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "record template citing a provision the index lacks")]
    public void TemplateYamlCitingUnknownProvision() => Check(
        root => StandardsFixture.Write(root, "templates/consumer/sample.yaml", "covers:\n- litespec/core/topic#no-such-provision\n"),
        [MaintainerDiagnostics.Codes.TemplateCitationUnknown]);
}
