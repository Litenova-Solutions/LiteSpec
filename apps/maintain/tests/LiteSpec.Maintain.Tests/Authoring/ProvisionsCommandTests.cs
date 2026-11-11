using System.Text.Json;
using LiteSpec.Authoring;
using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Tests.Authoring;

/// <summary>Tests for <c>litespec-maintain provisions</c>.</summary>
public sealed class ProvisionsCommandTests
{
    /// <summary>Provisions --check on the repository is current.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions --check on the repository is current")]
    public void CheckOnRepositoryIsCurrent()
    {
        var result = Cli.Run("provisions", "--check", "--root", Cli.Repository);
        Assert.Equal(0, result.Status);
    }

    /// <summary>Provisions --check on a stale index fails.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions --check on a stale index fails")]
    public void CheckOnStaleIndexFails()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, Provisions.IndexPath, "# Provision Index\n\n## Intent\n\nThis page is out of date.\n");
        var result = Cli.Run("provisions", "--check", "--root", temp.Path);
        Assert.Equal(1, result.Status);
    }

    /// <summary>Provisions --check on a stale data file fails.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions --check on a stale data file fails")]
    public void CheckOnStaleJsonFails()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, Provisions.JsonPath, "{ \"provisions\": [] }\n");
        var result = Cli.Run("provisions", "--check", "--root", temp.Path);
        Assert.Equal(1, result.Status);
    }

    /// <summary>Provisions with an unknown option is a usage error.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions with an unknown option is a usage error")]
    public void UnknownOptionIsUsageError()
    {
        var result = Cli.Run("provisions", "--bogus");
        Assert.Equal(2, result.Status);
    }

    /// <summary>Provisions writes one section per area.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions writes one section per area")]
    public void WritesSectionPerArea()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        var index = Provisions.BuildIndex(temp.Path);

        Assert.Contains("| ID | Provision | Kind | Stack | Concern | Artifact | Layer | Trace | Strength | Mark | Evidence |", index);
        Assert.Contains("The active release states 5 provisions in 4 groups across 4 areas.", index);
        Assert.Contains("## Checked Share by Page", index);
        Assert.Contains("| Page | Rules | Checked | Checked Share |", index);
        Assert.Contains("| [backend/sample/stack](../backend/sample/stack.md) | 1 | 1 | 100% |", index);
        Assert.Contains("| [core/topic](../core/topic.md) | 1 | 1 | 100% |", index);
        Assert.Contains("| [ext/sample](../ext/sample.md) | 1 | 1 | 100% |", index);
        Assert.Contains("| [frontend/patterns/sample](../frontend/patterns/sample.md) | 0 | 0 | - |", index);
        Assert.Contains("litespec/core/topic#keep-the-topic-bounded", index);
        Assert.Contains("litespec/core/topic#use-the-default-location", index);
        Assert.Contains("| litespec/core/topic#keep-the-topic-bounded | [Keep the Topic Bounded](../core/topic.md#keep-the-topic-bounded) | rule | any | records | - | any | no | required | checkable | `litespec-maintain validate` confirms the topic stays inside its declared boundary. |", index);
        Assert.Contains("| litespec/core/topic#use-the-default-location | [Use the Default Location](../core/topic.md#use-the-default-location) | convention | any | records | - | any | no | default | guidance | Does the topic live in its owning documentation area? |", index);
        Assert.Contains("[Keep the Topic Bounded](../core/topic.md#keep-the-topic-bounded)", index);
        Assert.Contains("| litespec/backend/sample/stack#apply-the-complete-stack | [Apply the Complete Stack](../backend/sample/stack.md#apply-the-complete-stack) | rule | dotnet | domain | - | implementation | no | required |", index);
        Assert.Contains("| litespec/ext/sample#record-extension-activation | [Record Extension Activation](../ext/sample.md#record-extension-activation) | rule | any | operations | - | implementation | no | required |", index);
        Assert.Contains("| litespec/frontend/patterns/sample#record-header | [Record Header](../frontend/patterns/sample.md#record-header) | pattern | any | frontend-ui | - | implementation | no | default |", index);

        var standard = index.IndexOf("## Standard", StringComparison.Ordinal);
        var reference = index.IndexOf("## Reference", StringComparison.Ordinal);
        var learn = index.IndexOf("## Learn", StringComparison.Ordinal);
        var records = index.IndexOf("## LiteSpec's Own Records", StringComparison.Ordinal);
        Assert.True(standard >= 0 && reference > standard && learn > reference && records > learn);
        Assert.Contains("## Reference\n\nNone.\n", index);
        Assert.Contains("## Learn\n\nNone.\n", index);
        Assert.Contains("## LiteSpec's Own Records\n\nNone.\n", index);

        var backend = index.IndexOf("### backend", StringComparison.Ordinal);
        var core = index.IndexOf("### core", StringComparison.Ordinal);
        var ext = index.IndexOf("### ext", StringComparison.Ordinal);
        var frontend = index.IndexOf("### frontend", StringComparison.Ordinal);
        Assert.True(backend > standard && core > backend && ext > core && frontend > ext && frontend < reference);
    }

    /// <summary>Provisions writes one data row per provision.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "provisions writes one data row per provision")]
    public void WritesJsonPerProvision()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        using var document = JsonDocument.Parse(Provisions.BuildJson(temp.Path));
        var provisions = document.RootElement.GetProperty("provisions");
        Assert.Equal(5, provisions.GetArrayLength());

        var json = Provisions.BuildJson(temp.Path);
        var artifactAt = json.IndexOf("\"artifact\"", StringComparison.Ordinal);
        var layerAt = json.IndexOf("\"layer\"", StringComparison.Ordinal);
        var traceAt = json.IndexOf("\"trace\"", StringComparison.Ordinal);
        var strengthAt = json.IndexOf("\"strength\"", StringComparison.Ordinal);
        Assert.True(artifactAt >= 0 && layerAt > artifactAt && traceAt > layerAt && strengthAt > traceAt);

        var first = provisions[0];
        Assert.Equal("litespec/backend/sample/stack#apply-the-complete-stack", first.GetProperty("id").GetString());
        Assert.Equal("Apply the Complete Stack", first.GetProperty("heading").GetString());
        Assert.Equal("docs/backend/sample/stack.md", first.GetProperty("page").GetString());
        Assert.Equal("apply-the-complete-stack", first.GetProperty("anchor").GetString());
        Assert.Equal("rule", first.GetProperty("kind").GetString());
        Assert.Equal("dotnet", first.GetProperty("stack").GetString());
        Assert.Equal("domain", first.GetProperty("concern").GetString());
        Assert.Equal("", first.GetProperty("artifact").GetString());
        Assert.Equal("implementation", first.GetProperty("layer").GetString());
        Assert.False(first.GetProperty("trace").GetBoolean());
        Assert.Equal("required", first.GetProperty("strength").GetString());
        Assert.Equal("Consumers MUST apply every document in the selected stack composition.", first.GetProperty("statement").GetString());
        Assert.Equal("", first.GetProperty("rationale").GetString());
        Assert.Equal("", first.GetProperty("example").GetString());
        Assert.Equal("checkable", first.GetProperty("mark").GetString());

        var second = provisions[1];
        Assert.Equal("litespec/core/topic#keep-the-topic-bounded", second.GetProperty("id").GetString());
        Assert.Equal("any", second.GetProperty("stack").GetString());
        Assert.Equal("records", second.GetProperty("concern").GetString());
        Assert.Equal("any", second.GetProperty("layer").GetString());
        Assert.False(second.GetProperty("trace").GetBoolean());
        Assert.Equal("Consumers MUST keep the topic inside its declared boundary.", second.GetProperty("statement").GetString());
        Assert.Equal("The boundary gives reviewers one testable result.", second.GetProperty("rationale").GetString());
    }
    /// <summary>Page layer comes from the page table.</summary>
    /// <proves>litespec/core/scopes#give-every-provision-its-layer-and-trace</proves>
    [Fact(DisplayName = "page layer comes from the page table")]
    public void PageLayerComesFromTable()
    {
        Assert.Equal("any", Provisions.PageLayer("docs/core/model.md", []));
        Assert.Equal("intent", Provisions.PageLayer("docs/core/intent.md", []));
        Assert.Equal("behavior", Provisions.PageLayer("docs/core/access.md", []));
        Assert.Equal("implementation", Provisions.PageLayer("docs/core/release.md", ["operations"]));
        Assert.Equal("implementation", Provisions.PageLayer("docs/backend/api.md", ["web-api"]));
        Assert.Equal("implementation", Provisions.PageLayer("docs/ext/sample.md", ["operations"]));
        Assert.Equal("proof", Provisions.PageLayer("docs/backend/testing.md", ["tests"]));
        Assert.Equal("", Provisions.PageLayer("templates/sample.md", []));
    }

    /// <summary>Provision layer and trace override win over its page.</summary>
    /// <proves>litespec/core/scopes#give-every-provision-its-layer-and-trace</proves>
    [Fact(DisplayName = "provision layer and trace override win over its page")]
    public void ProvisionLayerOverrideWins()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** layer: behavior; trace: yes\n\n**Statement:**"));

        var rows = Provisions.Collect(temp.Path);
        var row = Assert.Single(rows, item => item.Id == "litespec/core/topic#keep-the-topic-bounded");
        Assert.Equal("behavior", row.Layer);
        Assert.True(row.Trace);
        Assert.Contains("\"trace\": true", Provisions.BuildJson(temp.Path));
        Assert.Contains("| behavior | yes | required | checkable |", Provisions.BuildIndex(temp.Path));
    }

    /// <summary>Provision layer override beside an artifact field.</summary>
    /// <proves>litespec/core/scopes#give-every-provision-its-layer-and-trace</proves>
    [Fact(DisplayName = "provision layer override beside an artifact field")]
    public void ProvisionLayerOverrideBesideArtifact()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Strength:** required\n\n**Statement:**", "**Strength:** required\n\n**Applies to:** artifact: aggregate; layer: implementation; trace: yes\n\n**Statement:**"));

        var row = Assert.Single(Provisions.Collect(temp.Path), item => item.Id == "litespec/core/topic#keep-the-topic-bounded");
        Assert.Equal("aggregate", row.Artifact);
        Assert.Equal("implementation", row.Layer);
        Assert.True(row.Trace);
    }


    /// <summary>Checked share counts only rules with a Checked by line.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact(DisplayName = "checked share counts only rules with a Checked by line")]
    public void CheckedShareCountsOnlyCheckedRules()
    {
        using var temp = Cli.Temp("provisions");
        StandardsFixture.Base(temp.Path);
        var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.", "**Reviewed for:** Does the topic stay inside its declared boundary?");
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", page);

        var index = Provisions.BuildIndex(temp.Path);
        Assert.Contains("| [core/topic](../core/topic.md) | 1 | 0 | 0% |", index);
    }
}
