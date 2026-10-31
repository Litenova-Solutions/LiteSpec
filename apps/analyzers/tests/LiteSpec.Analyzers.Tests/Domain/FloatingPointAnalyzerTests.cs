using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1008, binary floating point in Domain.</summary>
public sealed class FloatingPointAnalyzerTests
{
    /// <summary>LS1008 reports double fields.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports double fields")]
    public Task ReportsDoubleFields() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private {|LS1008:double|} _total;
                private {|LS1008:System.Double|} _previous;
            }
            """);

    /// <summary>LS1008 reports float properties.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports float properties")]
    public Task ReportsFloatProperties() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                public {|LS1008:float|} Ratio { get; set; }
                public {|LS1008:System.Single|} Factor { get; set; }
            }
            """);

    /// <summary>LS1008 reports a method return and parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a method return and parameter")]
    public Task ReportsMethodReturnAndParameter() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                public {|LS1008:double|} Total({|LS1008:double|} rate) => rate;
            }
            """);

    /// <summary>LS1008 reports a local.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a local")]
    public Task ReportsLocal() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                public decimal Total()
                {
                    {|LS1008:double|} subtotal = 0;
                    return (decimal)subtotal;
                }
            }
            """);

    /// <summary>LS1008 accepts decimal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 accepts decimal")]
    public Task AcceptsDecimal() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private decimal _total;

                public decimal Total(decimal rate) => rate;
            }
            """);

    /// <summary>LS1008 reports nullable and array forms.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports nullable and array forms")]
    public Task ReportsNullableAndArrayForms() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private {|LS1008:double|}? _previous;
                private {|LS1008:float|}[] _samples = [];
            }
            """);

    /// <summary>LS1008 reports a generic argument.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a generic argument")]
    public Task ReportsGenericArgument() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Orders;

            public sealed class Pricer
            {
                private List<{|LS1008:double|}> _samples = new();
            }
            """);

    /// <summary>LS1008 reports a lambda parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a lambda parameter")]
    public Task ReportsLambdaParameter() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            using System;

            namespace Shop.Orders;

            public sealed class Pricer
            {
                private readonly Func<{|LS1008:double|}, {|LS1008:double|}> _square = ({|LS1008:double|} value) => value;
            }
            """);

    /// <summary>LS1008 reports a tuple element.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a tuple element")]
    public Task ReportsTupleElement() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private ({|LS1008:double|} Amount, int Count) _pair;
            }
            """);

    /// <summary>LS1008 reports a primary constructor parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 reports a primary constructor parameter")]
    public Task ReportsPrimaryConstructorParameter() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Priced({|LS1008:double|} discount)
            {
                public override string ToString() => discount.ToString();
            }
            """);

    /// <summary>LS1008 ignores floating point outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 ignores floating point outside Domain")]
    public Task IgnoresFloatingPointOutsideDomain() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private double _total;
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS1008 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "LS1008 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>("""
            namespace Shop.Core;

            public sealed class Pricer
            {
                private {|LS1008:double|} _total;
            }
            """, assemblyName: "Shop.Core", layer: "Domain");
}
