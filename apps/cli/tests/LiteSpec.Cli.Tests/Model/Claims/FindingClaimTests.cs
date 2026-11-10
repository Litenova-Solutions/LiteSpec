using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Claims;

/// <summary>Proves each finding code applies exactly to the layer claims that cover its record layer, trace and extension.</summary>
public sealed class FindingClaimTests
{
    /// <summary>A code applies when its layer is claimed, or when its layer is any; an Implementation or Proof code with trace yes applies to the layer's claim.</summary>
    /// <param name="claim">The claim name, as <see cref="Claim" /> reads it.</param>
    /// <param name="code">The finding code under test.</param>
    /// <param name="applies">Whether the claim covers the code.</param>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Theory]
    [InlineData("intent-only", "record-invalid", true)]
    [InlineData("intent-only", "intent-unreached", false)]
    [InlineData("intent-and-behavior", "intent-unreached", true)]
    [InlineData("intent-and-behavior", "mark-missing", false)]
    [InlineData("intent-and-behavior", "use-case-unverified", false)]
    [InlineData("own-code-without-proof", "use-case-unimplemented", true)]
    [InlineData("own-code-without-proof", "use-case-unverified", false)]
    [InlineData("own-code-without-proof", "code-placement-drift", false)]
    [InlineData("proof-without-code", "mark-missing", false)]
    [InlineData("proof-without-code", "use-case-unverified", true)]
    [InlineData("own-code-and-proof", "mark-missing", true)]
    [InlineData("own-code-and-proof", "code-placement-drift", false)]
    [InlineData("own-code-and-proof", "ui-route-suite-missing", false)]
    [InlineData("litespec-code-own-proof", "code-placement-drift", true)]
    [InlineData("litespec-code-own-proof", "ui-route-suite-missing", false)]
    [InlineData("litespec-code-and-proof", "code-placement-drift", true)]
    [InlineData("litespec-code-and-proof", "ui-route-suite-missing", true)]
    public void ClaimCoversItsCodes(string claim, string code, bool applies)
    {
        Assert.Equal(applies, Claim(claim).Covers(code, []));
    }

    /// <summary>A delivery code applies only when the project selects the delivery extension.</summary>
    /// <param name="code">The delivery code under test.</param>
    /// <proves>litespec/core/scope#select-conditional-extensions-explicitly</proves>
    [Theory]
    [InlineData("quality-alert-missing")]
    [InlineData("alert-link-missing")]
    [InlineData("alert-threshold-drift")]
    [InlineData("runbook-unused")]
    public void DeliveryCodesApplyOnlyWithTheDeliveryExtension(string code)
    {
        var claim = Claim("litespec-code-and-proof");

        Assert.False(claim.Covers(code, []));
        Assert.False(claim.Covers(code, ["layer-projects"]));
        Assert.True(claim.Covers(code, ["delivery"]));
    }

    /// <summary>The claim gate reads each code's own row, so a code the table names gates by its layer, trace and extension.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void KnownCodesGateByTheirRow()
    {
        var claim = Claim("own-code-and-proof");

        Assert.Equal(
            claim.Covers(FindingCodes.Of("mark-missing"), []),
            claim.Covers("mark-missing", []));
    }

    /// <summary>An unknown code applies to every claim, so a new code is never silently dropped.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void UnknownCodeAppliesToEveryClaim()
    {
        Assert.True(Claim("intent-only").Covers("no-such-finding-code", []));
    }

    /// <summary>The claim names: an unclaimed layer is absent from the claim, and each practice reads as its name.</summary>
    /// <param name="name">The claim name.</param>
    /// <returns>The layer claims the name stands for.</returns>
    static LayerClaims Claim(string name) => name switch
    {
        "intent-only" => new LayerClaims(Intent: true, Behavior: false, Implementation: null, Proof: null),
        "intent-and-behavior" => new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: null),
        "own-code-without-proof" => new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: null),
        "proof-without-code" => new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: LayerPractice.Own),
        "own-code-and-proof" => new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: LayerPractice.Own),
        "litespec-code-own-proof" => new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.LiteSpec, Proof: LayerPractice.Own),
        _ => new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.LiteSpec, Proof: LayerPractice.LiteSpec),
    };
}
