using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the floating point code fix: every reported type becomes decimal, and Fix All covers the scope.</summary>
public sealed class ReplaceFloatingPointWithDecimalCodeFixTests
{
    /// <summary>Replace with decimal turns a double property into a decimal property.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Replace with decimal turns a double property into a decimal property")]
    public Task ReplacesADoublePropertyWithDecimal() =>
        Verify.CodeFixAsync<FloatingPointAnalyzer, ReplaceFloatingPointWithDecimalCodeFixProvider>(
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                public double Total { get; set; }
            }
            """,
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                public decimal Total { get; set; }
            }
            """,
            "Replace with decimal",
            [],
            "Shop.Spending", path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Replace with decimal turns a double field and its initializer into decimal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Replace with decimal turns a double field and its initializer into decimal")]
    public Task ReplacesADoubleFieldAndItsInitializerWithDecimal() =>
        Verify.CodeFixAsync<FloatingPointAnalyzer, ReplaceFloatingPointWithDecimalCodeFixProvider>(
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private double _total = 1.5;
            }
            """,
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private decimal _total = 1.5m;
            }
            """,
            "Replace with decimal",
            [],
            "Shop.Spending", path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Replace with decimal turns a nullable double field and its initializer into decimal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Replace with decimal turns a nullable double field and its initializer into decimal")]
    public Task ReplacesANullableDoubleFieldWithDecimal() =>
        Verify.CodeFixAsync<FloatingPointAnalyzer, ReplaceFloatingPointWithDecimalCodeFixProvider>(
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private double? _previous = 2;
            }
            """,
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private decimal? _previous = 2m;
            }
            """,
            "Replace with decimal",
            [],
            "Shop.Spending", path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Replace with decimal turns a generic argument of double into decimal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Replace with decimal turns a generic argument of double into decimal")]
    public Task ReplacesAGenericArgumentOfDoubleWithDecimal() =>
        Verify.CodeFixAsync<FloatingPointAnalyzer, ReplaceFloatingPointWithDecimalCodeFixProvider>(
            """
            using System.Collections.Generic;

            namespace Shop.Orders;

            public sealed class Pricer
            {
                private List<double> _samples = new();
            }
            """,
            """
            using System.Collections.Generic;

            namespace Shop.Orders;

            public sealed class Pricer
            {
                private List<decimal> _samples = new();
            }
            """,
            "Replace with decimal",
            [],
            "Shop.Spending", path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Fix All replaces every floating point type across the solution.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Fix All replaces every floating point type across the solution")]
    public Task FixAllReplacesEveryFloatingPointType() =>
        Verify.CodeFixAllAsync<FloatingPointAnalyzer, ReplaceFloatingPointWithDecimalCodeFixProvider>(
            [
                ("Shop.Spending/Domain/Ratios.cs", """
                    namespace Shop.Orders;

                    public sealed class RatioKeeper
                    {
                        private float _ratio = 1.5f;
                    }
                    """),
                ("Shop.Spending/Domain/Factors.cs", """
                    namespace Shop.Orders;

                    public sealed class FactorKeeper
                    {
                        public double Factor { get; set; }
                    }
                    """),
            ],
            [
                ("Shop.Spending/Domain/Ratios.cs", """
                    namespace Shop.Orders;

                    public sealed class RatioKeeper
                    {
                        private decimal _ratio = 1.5m;
                    }
                    """),
                ("Shop.Spending/Domain/Factors.cs", """
                    namespace Shop.Orders;

                    public sealed class FactorKeeper
                    {
                        public decimal Factor { get; set; }
                    }
                    """),
            ],
            [],
            "ReplaceFloatingPointWithDecimal",
            "Shop.Spending");

    /// <summary>Double outside Domain is not reported so no fix is offered.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</proves>
    [Fact(DisplayName = "Double outside Domain is not reported so no fix is offered")]
    public Task IgnoresDoubleOutsideDomain() =>
        Verify.AnalyzerAsync<FloatingPointAnalyzer>(
            """
            namespace Shop.Orders;

            public sealed class Pricer
            {
                private double _total;
            }
            """, assemblyName: "Shop.WebApi");
}
