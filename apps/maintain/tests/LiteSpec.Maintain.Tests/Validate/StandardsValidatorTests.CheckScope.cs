using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Check-scope cases: provisions stating the size and extension conditions of the checks they cite.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Provision checked at large size without the size condition.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked at large size without the size condition")]
    public void CheckScopeSizeMissing() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `use-case-scenario-missing` for a use case declaring no scenario.")),
        [MaintainerDiagnostics.Codes.ProvisionCheckScope]);

    /// <summary>Provision checked at large size opening with the size condition.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked at large size opening with the size condition")]
    public void CheckScopeSizeStated() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `use-case-scenario-missing` for a use case declaring no scenario.");
            page = StandardsFixture.ReplaceFirst(page, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** At large size, each topic MUST show its scenarios in the use cases that serve it.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Provision checked at large size without the size clause on result-scenario-missing.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked at large size or with an extension without the extension clause")]
    public void CheckScopeSizeExtensionMissing() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `result-scenario-missing` for a written result no scenario shows.");
            page = StandardsFixture.ReplaceFirst(page, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** Each written result MUST be shown by at least one scenario of its use case.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
        },
        [MaintainerDiagnostics.Codes.ProvisionCheckScope]);

    /// <summary>Provision checked at large size or with an extension opening with both conditions.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked at large size or with an extension opening with both conditions")]
    public void CheckScopeSizeExtensionStated() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `result-scenario-missing` for a written result no scenario shows.");
            page = StandardsFixture.ReplaceFirst(page, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** At large size or with the `bdd` extension, each written result MUST be shown by at least one scenario of its use case.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Provision citing a large-size code beside an every-size code.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision citing a large-size code beside an every-size code")]
    public void CheckScopeSizeMixed() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `record-invalid` for a topic outside its home and `result-scenario-missing` for a written result no scenario shows."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Provision checked only with an extension outside its page and Statement.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked only with an extension outside its page and Statement")]
    public void CheckScopeExtensionMissing() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `quality-alert-missing` for a quality requirement no alert names.")),
        [MaintainerDiagnostics.Codes.ProvisionCheckScope]);

    /// <summary>Provision checked only with an extension naming it in its Statement.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked only with an extension naming it in its Statement")]
    public void CheckScopeExtensionStated() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `quality-alert-missing` for a quality requirement no alert names.");
            page = StandardsFixture.ReplaceFirst(page, "**Statement:** Consumers MUST keep the topic inside its declared boundary.", "**Statement:** Consumers MUST name each quality requirement from a `delivery` alert or record a waiver.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Provision checked only with an extension sitting on its page.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision checked only with an extension sitting on its page")]
    public void CheckScopeExtensionPage() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/ext/delivery.md", DeliveryExtensionPage);
            StandardsFixture.Write(root, "docs/README.md", StandardsFixture.Readme(extra: "- [Delivery](ext/delivery.md)\n"));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>Provision citing an unknown code reports no check-scope diagnostic.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "provision citing an unknown code reports no check-scope diagnostic")]
    public void CheckScopeUnknownCodeIgnored() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "`litespec-maintain validate` confirms the topic stays inside its declared boundary.", "`litespec model check` reports `topic-cites-missing` for a topic naming no reason.")),
        [MaintainerDiagnostics.Codes.CheckedByUnknown],
        [MaintainerDiagnostics.Codes.ProvisionCheckScope]);

    /// <summary>Convention checked at large size without the size condition.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "convention checked at large size without the size condition")]
    public void CheckScopeConventionDefaultMissing() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Reviewed for:** Does the topic live in its owning documentation area?", "**Checked by:** `litespec model check` reports `use-case-scenario-missing` for a use case declaring no scenario.")),
        [MaintainerDiagnostics.Codes.ProvisionCheckScope]);

    /// <summary>Convention checked at large size opening its Default with the size condition.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "convention checked at large size opening its Default with the size condition")]
    public void CheckScopeConventionDefaultStated() => Check(
        root =>
        {
            var page = StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "**Reviewed for:** Does the topic live in its owning documentation area?", "**Checked by:** `litespec model check` reports `use-case-scenario-missing` for a use case declaring no scenario.");
            page = StandardsFixture.ReplaceFirst(page, "**Default:** Store the topic in its owning documentation area.", "**Default:** At large size, store the topic in its owning documentation area.");
            StandardsFixture.Write(root, "docs/core/topic.md", page);
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>The delivery extension page, whose provision sits on its own extension page.</summary>
    static readonly string DeliveryExtensionPage = """
        # Delivery Extension

        **Stack:** any
        **Concern:** operations

        This extension adds delivery alerts.

        ## Activation

        Activation scope: `local`.

        Applicable specification kinds: `use-case`.

        Enable this extension when delivery alerts are required.

        ## Baseline Relationship

        This extension replaces no baseline provision.

        ## Standards

        ### Cover Every Quality Requirement with an Alert or a Check

        **ID:** `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`

        **Strength:** required

        **Statement:** Consumers MUST record activation on each applicable specification.

        **Checked by:** `litespec model check` reports `quality-alert-missing` for a quality requirement no alert names.

        ## Conventions

        None.

        ## Dependencies

        None.
        """.Replace("\r\n", "\n") + "\n";
}
