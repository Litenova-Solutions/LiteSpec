using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Tests.Style;

/// <summary>Tests for LS3001 and LS3002, asynchronous method names and the token last.</summary>
public sealed class AsyncMethodAnalyzerTests
{
    /// <summary>LS3001 reports Task and ValueTask methods without Async.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 reports Task and ValueTask methods without Async")]
    public Task ReportsTaskMethodsWithoutAsync() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task {|LS3001:Place|}() => Task.CompletedTask;

                public ValueTask<int> {|LS3001:Count|}() => new ValueTask<int>(0);
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 reports the same breach in WebApi.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 reports the same breach in WebApi")]
    public Task ReportsTheSameBreachInWebApi() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Api;

            public sealed class Worker
            {
                public Task {|LS3001:Place|}() => Task.CompletedTask;
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS3001 accepts a complete asynchronous signature.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 accepts a complete asynchronous signature")]
    public Task AcceptsACompleteSignature() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task PlaceAsync(CancellationToken cancellationToken) => Task.CompletedTask;

                public int Count() => 0;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 ignores an override and reports its base.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 ignores an override and reports its base")]
    public Task IgnoresAnOverrideAndReportsItsBase() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public class Recorder
            {
                public virtual Task {|LS3001:Save|}() => Task.CompletedTask;
            }

            public sealed class OrderRecorder : Recorder
            {
                public override Task Save() => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 ignores the Main entry point.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 ignores the Main entry point")]
    public Task IgnoresTheMainEntryPoint() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Program
            {
                public static Task Main() => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 reports a local function but not a lambda.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 reports a local function but not a lambda")]
    public Task ReportsALocalFunctionButNotALambda() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public async Task RunAsync()
                {
                    async Task {|LS3001:Place|}()
                    {
                        await Task.Delay(1);
                    }

                    Func<Task> work = async () =>
                    {
                        await Task.Delay(1);
                    };

                    await Place();
                    await work();
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 ignores a framework contract implementation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 ignores a framework contract implementation")]
    public Task IgnoresAFrameworkContractImplementation() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 reports a controller action.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 reports a controller action")]
    public Task ReportsAControllerAction() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Mvc;

            namespace Microsoft.AspNetCore.Mvc
            {
                public class Controller { }
            }

            namespace Shop.Api
            {
                public sealed class OrdersController : Controller
                {
                    public Task {|LS3001:Index|}() => Task.CompletedTask;
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3001 ignores a breach in a file the layer rules cannot place.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 ignores a breach in a file the layer rules cannot place")]
    public Task IgnoresABreachInAnUnplacedFile() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task Place() => Task.CompletedTask;
            }
            """, assemblyName: "Shop.Unknown");


    /// <summary>LS3001 ignores the top-level statements entry point.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 ignores the top-level statements entry point")]
    public Task IgnoresTopLevelStatementsEntryPoint() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            await Task.Delay(1);
            return 0;
            """, assemblyName: "Shop.WebApi", outputKind: OutputKind.ConsoleApplication);

    /// <summary>LS3001 reports a synchronous Task.FromResult factory.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</proves>
    [Fact(DisplayName = "LS3001 reports a synchronous Task.FromResult factory")]
    public Task ReportsASynchronousTaskFactory() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task<string> {|LS3001:Refused|}(string reason) => Task.FromResult(reason);
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 reports a misnamed token and a token in the middle.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 reports a misnamed token and a token in the middle")]
    public Task ReportsAMisnamedTokenAndATokenInTheMiddle() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task SaveAsync(CancellationToken {|LS3002:ct|}) => Task.CompletedTask;

                public Task LoadAsync(CancellationToken {|LS3002:cancellationToken|}, string name) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 accepts the token last with a trailing params array.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 accepts the token last with a trailing params array")]
    public Task AcceptsTheTokenLastWithATrailingParamsArray() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task SaveAsync(CancellationToken cancellationToken, params string[] names) => Task.CompletedTask;

                public void Ping(CancellationToken cancellationToken) { }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 ignores an override and reports its base.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 ignores an override and reports its base")]
    public Task IgnoresATokenOverrideAndReportsItsBase() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;

            namespace Shop.Orders;

            public class Recorder
            {
                public virtual void Save(CancellationToken {|LS3002:cancellationToken|}, string name) { }
            }

            public sealed class OrderRecorder : Recorder
            {
                public override void Save(CancellationToken cancellationToken, string name) { }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 ignores an explicit interface implementation and reports its contract.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 ignores an explicit interface implementation and reports its contract")]
    public Task IgnoresAnExplicitInterfaceImplementation() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;

            namespace Shop.Orders;

            public interface ISaver
            {
                void Save(CancellationToken {|LS3002:cancellationToken|}, string name);
            }

            public sealed class Worker : ISaver
            {
                void ISaver.Save(CancellationToken cancellationToken, string name) { }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 reports a misnamed token in a local function.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 reports a misnamed token in a local function")]
    public Task ReportsAMisnamedTokenInALocalFunction() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public async Task RunAsync(CancellationToken cancellationToken)
                {
                    async Task PlaceAsync(CancellationToken {|LS3002:ct|})
                    {
                        await Task.Delay(1, ct);
                    }

                    await PlaceAsync(cancellationToken);
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 ignores token position in an endpoint handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 ignores token position in an endpoint handler")]
    public Task IgnoresTokenPositionInAnEndpointHandler() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Api;

            public interface IEndpoint { }

            public sealed class ReadGuestListEndpoint : IEndpoint
            {
                private static Task HandleAsync(Guid listId, CancellationToken cancellationToken, string? state = null) =>
                    Task.CompletedTask;
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS3002 reports the same position in an ordinary class.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 reports the same position in an ordinary class")]
    public Task ReportsTheSamePositionInAnOrdinaryClass() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task HandleAsync(Guid listId, CancellationToken {|LS3002:cancellationToken|}, string? state = null) =>
                    Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 reports a misnamed token in an endpoint handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 reports a misnamed token in an endpoint handler")]
    public Task ReportsAMisnamedTokenInAnEndpointHandler() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Api;

            public interface IEndpoint { }

            public sealed class ReadGuestListEndpoint : IEndpoint
            {
                private static Task HandleAsync(Guid listId, CancellationToken {|LS3002:ct|}, string? state = null) =>
                    Task.CompletedTask;
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS3002 accepts stoppingToken in a hosted service.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 accepts stoppingToken in a hosted service")]
    public Task AcceptsStoppingTokenInAHostedService() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public abstract class BackgroundService { }

            public sealed class PlanPriceListWorker : BackgroundService
            {
                private Task WaitForDatabaseAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Infrastructure/Source.cs");

    /// <summary>LS3002 reports stoppingToken outside a hosted service.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 reports stoppingToken outside a hosted service")]
    public Task ReportsStoppingTokenOutsideAHostedService() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task SaveAsync(CancellationToken {|LS3002:stoppingToken|}) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS3002 ignores an ExecuteAsync override.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 ignores an ExecuteAsync override")]
    public Task IgnoresAnExecuteAsyncOverride() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public abstract class BackgroundService
            {
                protected virtual Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }

            public sealed class PlanPriceListWorker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """, path: "Shop.Spending/Infrastructure/Source.cs");

    /// <summary>LS3002 ignores a token breach in a file the layer rules cannot place.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#put-the-cancellation-token-last</proves>
    [Fact(DisplayName = "LS3002 ignores a token breach in a file the layer rules cannot place")]
    public Task IgnoresATokenBreachInAnUnplacedFile() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task SaveAsync(CancellationToken cancellationToken, string name) => Task.CompletedTask;
            }
            """, assemblyName: "Shop.Unknown");

}
