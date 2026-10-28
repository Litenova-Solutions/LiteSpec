using LiteSpec.Analyzers.Rules.Tests;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Tests;

/// <summary>Tests for LS4001, assertion presence in tests.</summary>
public sealed class TestAssertionAnalyzerTests
{
    [Fact(DisplayName = "LS4001 reports a test without an assertion")]
    public Task ReportsATestWithoutAnAssertion() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class FactAttribute : Attribute;

            public sealed class Order
            {
                public void Place() { }
            }

            public sealed class OrderTests
            {
                [Fact]
                public void {|LS4001:Place_WhenDraft_MarksPlaced|}()
                {
                    var order = new Order();
                    order.Place();
                }
            }
            """, assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS4001 accepts a test that asserts")]
    public Task AcceptsATestThatAsserts() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class FactAttribute : Attribute;

            public static class Assert
            {
                public static void True(bool condition) { }
            }

            public sealed class Order
            {
                public bool Placed { get; private set; }

                public void Place() => Placed = true;
            }

            public sealed class OrderTests
            {
                [Fact]
                public void Place_WhenDraft_MarksPlaced()
                {
                    var order = new Order();
                    order.Place();
                    Assert.True(order.Placed);
                }
            }
            """, assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS4001 accepts a test calling a custom Assert helper")]
    public Task AcceptsACustomAssertHelper() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class FactAttribute : Attribute;

            public sealed class Order
            {
                public void Place() { }
            }

            public sealed class OrderTests
            {
                [Fact]
                public void Place_WhenDraft_MarksPlaced()
                {
                    var order = new Order();
                    order.Place();
                    AssertOrderPlaced(order);
                }

                private static void AssertOrderPlaced(Order order) { }
            }
            """, assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS4001 accepts a theory that asserts")]
    public Task AcceptsATheoryThatAsserts() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class TheoryAttribute : Attribute;

            public static class Assert
            {
                public static void True(bool condition) { }
            }

            public sealed class OrderTests
            {
                [Theory]
                public void Place_WhenDraft_MarksPlaced()
                {
                    Assert.True(true);
                }
            }
            """, assemblyName: "Shop.Integration.Tests");

    [Fact(DisplayName = "LS4001 ignores a helper without a test attribute")]
    public Task IgnoresAHelperWithoutAnAttribute() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            namespace Shop.Orders.Tests;

            public sealed class Order
            {
                public void Place() { }
            }

            public sealed class OrderTests
            {
                public void Place_WhenDraft_MarksPlaced()
                {
                    var order = new Order();
                    order.Place();
                }
            }
            """, assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS4001 ignores a test outside test projects")]
    public Task IgnoresATestOutsideTestProjects() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders;

            public sealed class FactAttribute : Attribute;

            public sealed class Order
            {
                public void Place() { }
            }

            public sealed class OrderTests
            {
                [Fact]
                public void Place_WhenDraft_MarksPlaced()
                {
                    var order = new Order();
                    order.Place();
                }
            }
            """, assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS4001 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Core;

            public sealed class FactAttribute : Attribute;

            public sealed class Order
            {
                public void Place() { }
            }

            public sealed class OrderTests
            {
                [Fact]
                public void {|LS4001:Place_WhenDraft_MarksPlaced|}()
                {
                    var order = new Order();
                    order.Place();
                }
            }
            """, assemblyName: "Shop.Core", layer: "Test");

    [Fact(DisplayName = "LS4001 reports a scenario whose steps assert nothing")]
    public Task ReportsAScenarioWithoutAnAssertion() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class ScenarioAttribute : Attribute;

            public sealed class OrderSteps
            {
                public void Given_a_draft_order() { }

                public void When_the_order_is_placed() { }
            }

            public sealed class PlaceOrderScenarios
            {
                private readonly OrderSteps _steps = new();

                [Scenario]
                public void {|LS4001:Placing_a_draft_order_succeeds|}()
                {
                    _steps.Given_a_draft_order();
                    _steps.When_the_order_is_placed();
                }
            }
            """, assemblyName: "Shop.Acceptance.Tests");

    [Fact(DisplayName = "LS4001 accepts a scenario whose step asserts")]
    public Task AcceptsAScenarioWhoseStepAsserts() =>
        Verify.AnalyzerAsync<TestAssertionAnalyzer>("""
            using System;

            namespace Shop.Orders.Tests;

            public sealed class ScenarioAttribute : Attribute;

            public static class Assert
            {
                public static void True(bool condition) { }
            }

            public sealed class OrderSteps
            {
                public bool Placed { get; private set; }

                public void When_the_order_is_placed() => Placed = true;

                public void Then_the_order_is_placed() => Assert.True(Placed);
            }

            public sealed class PlaceOrderScenarios
            {
                private readonly OrderSteps _steps = new();

                [Scenario]
                public void Placing_a_draft_order_succeeds()
                {
                    _steps.When_the_order_is_placed();
                    _steps.Then_the_order_is_placed();
                }
            }
            """, assemblyName: "Shop.Acceptance.Tests");
}
