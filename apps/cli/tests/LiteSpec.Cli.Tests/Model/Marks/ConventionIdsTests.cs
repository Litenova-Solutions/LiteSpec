using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the project convention identifier shape a follows or proves mark names.</summary>
public sealed class ConventionIdsTests
{
    /// <summary>IsConventionId takes the project conventions form.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "IsConventionId takes the project conventions form")]
    public void IsConventionIdTakesConventionForm()
    {
        Assert.True(ConventionIds.IsConventionId("demo/conventions#confirm-large-payouts"));
        Assert.True(ConventionIds.IsConventionId("claimwick/conventions#confirm-large-payouts"));
        Assert.True(ConventionIds.IsConventionId("demo/conventions#x"));
    }

    /// <summary>IsConventionId rejects provisions, records and malformed identifiers.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "IsConventionId rejects provisions, records and malformed identifiers")]
    public void IsConventionIdRejectsOtherForms()
    {
        Assert.False(ConventionIds.IsConventionId(null));
        Assert.False(ConventionIds.IsConventionId(""));
        Assert.False(ConventionIds.IsConventionId("litespec/backend/persistence#commit-each-command-pipeline-once"));
        Assert.False(ConventionIds.IsConventionId("decision/0002-store-loans-as-documents"));
        Assert.False(ConventionIds.IsConventionId("departure/export-review-bypass"));
        Assert.False(ConventionIds.IsConventionId("use-case/lending.borrow-copy"));
        Assert.False(ConventionIds.IsConventionId("journey/walk#start"));
        Assert.False(ConventionIds.IsConventionId("demo/conventions#"));
        Assert.False(ConventionIds.IsConventionId("/conventions#confirm-large-payouts"));
        Assert.False(ConventionIds.IsConventionId("demo/conventions"));
        Assert.False(ConventionIds.IsConventionId("demo/decisions#confirm-large-payouts"));
        Assert.False(ConventionIds.IsConventionId("demo/conventions#confirm#large-payouts"));
        Assert.False(ConventionIds.IsConventionId("demo/team/conventions#confirm-large-payouts"));
    }
}
