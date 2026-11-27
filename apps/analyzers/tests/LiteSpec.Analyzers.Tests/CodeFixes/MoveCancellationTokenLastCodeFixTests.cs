using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the move token fix: the before and after, and the Fix All policy.</summary>
public sealed class MoveCancellationTokenLastCodeFixTests
{
    /// <summary>Moves the token last and reorders its call site in a second file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "Moves the token last and reorders its call site in a second file")]
    public Task MovesTheTokenLastAndReordersItsCallSiteInASecondFile() =>
        Verify.CodeFixFilesAsync<AsyncMethodAnalyzer, MoveCancellationTokenLastCodeFixProvider>(
            [
                ("Shop.Spending/Domain/Saving.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;

                    namespace Shop.Orders;

                    public sealed class SavingWorker
                    {
                        public Task SaveOrderAsync(CancellationToken cancellationToken, string name) => Task.CompletedTask;
                    }
                    """),
                ("Shop.Spending/Domain/Checkout.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;

                    namespace Shop.Orders;

                    public sealed class Checkout
                    {
                        public Task RunAsync(SavingWorker worker, CancellationToken cancellationToken) =>
                            worker.SaveOrderAsync(cancellationToken, "book");
                    }
                    """),
            ],
            [
                ("Shop.Spending/Domain/Saving.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;

                    namespace Shop.Orders;

                    public sealed class SavingWorker
                    {
                        public Task SaveOrderAsync(string name, CancellationToken cancellationToken) => Task.CompletedTask;
                    }
                    """),
                ("Shop.Spending/Domain/Checkout.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;

                    namespace Shop.Orders;

                    public sealed class Checkout
                    {
                        public Task RunAsync(SavingWorker worker, CancellationToken cancellationToken) =>
                            worker.SaveOrderAsync("book", cancellationToken);
                    }
                    """),
            ],
            "Move CancellationToken parameter last",
            []);

    /// <summary>Leaves a token already sitting last.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "Leaves a token already sitting last")]
    public Task LeavesATokenAlreadySittingLast() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>(
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task SaveAsync(string name, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Moves the token before a params array past an optional default parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "Moves the token before a params array past an optional default parameter")]
    public Task MovesTheTokenBeforeAParamsArrayPastAnOptionalDefaultParameter() =>
        Verify.CodeFixAsync<AsyncMethodAnalyzer, MoveCancellationTokenLastCodeFixProvider>(
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class HandlingWorker
            {
                public Task HandleOrderAsync(CancellationToken cancellationToken, string? state = null, params string[] tags) => Task.CompletedTask;
            }

            public sealed class Checkout
            {
                private readonly HandlingWorker _worker = new();

                public Task RunAsync(CancellationToken cancellationToken) =>
                    _worker.HandleOrderAsync(cancellationToken, "open", "a", "b");
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class HandlingWorker
            {
                public Task HandleOrderAsync(string? state = null, CancellationToken cancellationToken, params string[] tags) => Task.CompletedTask;
            }

            public sealed class Checkout
            {
                private readonly HandlingWorker _worker = new();

                public Task RunAsync(CancellationToken cancellationToken) =>
                    _worker.HandleOrderAsync("open", cancellationToken, "a", "b");
            }
            """,
            "Move CancellationToken parameter last",
            path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Move offers Fix All.</summary>
    /// <proves>litespec/packs/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "Move offers Fix All")]
    public void MoveOffersFixAll()
    {
        Assert.NotNull(new MoveCancellationTokenLastCodeFixProvider().GetFixAllProvider());
    }
}
