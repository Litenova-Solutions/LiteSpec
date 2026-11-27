using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the rename Async fix: the before and after, and the no Fix All policy.</summary>
public sealed class RenameAsyncMethodCodeFixTests
{
    /// <summary>Renames the method and its call site in a second file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "Renames the method and its call site in a second file")]
    public Task RenamesTheMethodAndItsCallSiteInASecondFile() =>
        Verify.CodeFixFilesAsync<AsyncMethodAnalyzer, RenameAsyncMethodCodeFixProvider>(
            [
                ("Shop.Spending/Domain/Billing.cs", """
                    using System.Threading.Tasks;

                    namespace Shop.Billing;

                    public sealed class BillingWorker
                    {
                        public Task PlaceOrder(string name) => Task.CompletedTask;
                    }
                    """),
                ("Shop.Spending/Domain/Checkout.cs", """
                    using System.Threading.Tasks;

                    namespace Shop.Billing;

                    public sealed class Checkout
                    {
                        public Task RunAsync(BillingWorker worker, string name) => worker.PlaceOrder(name);
                    }
                    """),
            ],
            [
                ("Shop.Spending/Domain/Billing.cs", """
                    using System.Threading.Tasks;

                    namespace Shop.Billing;

                    public sealed class BillingWorker
                    {
                        public Task PlaceOrderAsync(string name) => Task.CompletedTask;
                    }
                    """),
                ("Shop.Spending/Domain/Checkout.cs", """
                    using System.Threading.Tasks;

                    namespace Shop.Billing;

                    public sealed class Checkout
                    {
                        public Task RunAsync(BillingWorker worker, string name) => worker.PlaceOrderAsync(name);
                    }
                    """),
            ],
            "Rename to 'PlaceOrderAsync'",
            []);

    /// <summary>Leaves a method already ending in Async.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "Leaves a method already ending in Async")]
    public Task LeavesAMethodAlreadyEndingInAsync() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>(
            """
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task PlaceAsync(string name) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Renames a method with an optional default parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "Renames a method with an optional default parameter")]
    public Task RenamesAMethodWithAnOptionalDefaultParameter() =>
        Verify.CodeFixAsync<AsyncMethodAnalyzer, RenameAsyncMethodCodeFixProvider>(
            """
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task PlaceOrder(string name = "guest") => Task.CompletedTask;
            }
            """,
            """
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task PlaceOrderAsync(string name = "guest") => Task.CompletedTask;
            }
            """,
            "Rename to 'PlaceOrderAsync'",
            path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Rename offers no Fix All.</summary>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "Rename offers no Fix All")]
    public void RenameOffersNoFixAll()
    {
        Assert.Null(new RenameAsyncMethodCodeFixProvider().GetFixAllProvider());
    }
}
