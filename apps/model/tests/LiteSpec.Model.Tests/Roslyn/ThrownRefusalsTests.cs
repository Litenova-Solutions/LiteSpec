using System.Collections.Generic;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for the refusal codes a method body passes directly when it constructs a refusal type.</summary>
public sealed class ThrownRefusalsTests
{
    /// <summary>A literal argument matching the refusal-code grammar is collected.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A literal argument matching the refusal-code grammar is collected")]
    public void LiteralArgumentIsCollected()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException("lending.price-refused", "No quote.");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);

        var code = Assert.Single(ThrownRefusals.CodesOf(method, tree, new Dictionary<string, string>()));

        Assert.Equal("lending.price-refused", code.Code);
        Assert.Equal(5, code.Line);
    }

    /// <summary>A member access argument resolves through the constants map.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A member access argument resolves through the constants map")]
    public void ConstantArgumentResolvesThroughMap()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException(LendingRefusalCodes.PriceRefused, "No quote.");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);
        var constants = new Dictionary<string, string>
        {
            ["LendingRefusalCodes.PriceRefused"] = "lending.price-refused",
        };

        var code = Assert.Single(ThrownRefusals.CodesOf(method, tree, constants));

        Assert.Equal("lending.price-refused", code.Code);
        Assert.Equal(5, code.Line);
    }

    /// <summary>A bare identifier reads the constant of that name in the enclosing type.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A bare identifier reads the constant of that name in the enclosing type")]
    public void BareIdentifierResolvesInEnclosingType()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException(Held, "No quote.");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);
        var constants = new Dictionary<string, string>
        {
            ["Pricing.Held"] = "lending.price-refused",
        };

        var code = Assert.Single(ThrownRefusals.CodesOf(method, tree, constants));

        Assert.Equal("lending.price-refused", code.Code);
        Assert.Equal(5, code.Line);
    }

    /// <summary>A constant holding no refusal code is ignored.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A constant holding no refusal code is ignored")]
    public void NonCodeConstantIsIgnored()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException(PricingNotes.Sentence, "No quote.");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);
        var constants = new Dictionary<string, string>
        {
            ["PricingNotes.Sentence"] = "No quote is available.",
        };

        Assert.Empty(ThrownRefusals.CodesOf(method, tree, constants));
    }

    /// <summary>A constant the map holds nothing for is ignored.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A constant the map holds nothing for is ignored")]
    public void UnresolvableConstantIsIgnored()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException(LendingRefusalCodes.PriceRefused, "No quote.");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);

        Assert.Empty(ThrownRefusals.CodesOf(method, tree, new Dictionary<string, string>()));
    }

    /// <summary>A construction of no refusal type is ignored.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact(DisplayName = "A construction of no refusal type is ignored")]
    public void NonExceptionConstructionIsIgnored()
    {
        var tree = TestTrees.Parse("""
            public sealed class Pricing
            {
                public static object Quote(decimal amount)
                {
                    return new PricingResult("lending.price-refused");
                }
            }
            """);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);

        Assert.Empty(ThrownRefusals.CodesOf(method, tree, new Dictionary<string, string>()));
    }
}
