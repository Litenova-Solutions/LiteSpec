using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Tests.Style;

/// <summary>Tests for LS3001, asynchronous method names.</summary>
public sealed class AsyncMethodAnalyzerTests
{
    /// <summary>LS3001 reports Task and ValueTask methods without Async.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 reports the same breach in WebApi.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 ignores an override and reports its base.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 ignores the Main entry point.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
    [Fact(DisplayName = "LS3001 ignores the Main entry point")]
    public Task IgnoresTheMainEntryPoint() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Program
            {
                public static Task Main() => Task.CompletedTask;
            }
            """);

    /// <summary>LS3001 reports a local function but not a lambda.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 ignores a framework contract implementation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 reports a controller action.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
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
            """);

    /// <summary>LS3001 ignores a breach outside the five projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
    [Fact(DisplayName = "LS3001 ignores a breach outside the five projects")]
    public Task IgnoresABreachOutsideTheFiveProjects() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task Place() => Task.CompletedTask;
            }
            """, assemblyName: "Shop.Unknown");

    /// <summary>LS3001 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
    [Fact(DisplayName = "LS3001 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Core;

            public sealed class Worker
            {
                public Task {|LS3001:Place|}() => Task.CompletedTask;
            }
            """, assemblyName: "Shop.Core", layer: "Domain");

    /// <summary>LS3001 ignores the top-level statements entry point.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
    [Fact(DisplayName = "LS3001 ignores the top-level statements entry point")]
    public Task IgnoresTopLevelStatementsEntryPoint() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            await Task.Delay(1);
            return 0;
            """, assemblyName: "Shop.WebApi", outputKind: OutputKind.ConsoleApplication);

    /// <summary>LS3001 reports a synchronous Task.FromResult factory.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-asynchronous-methods-completely</proves>
    [Fact(DisplayName = "LS3001 reports a synchronous Task.FromResult factory")]
    public Task ReportsASynchronousTaskFactory() =>
        Verify.AnalyzerAsync<AsyncMethodAnalyzer>("""
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Worker
            {
                public Task<string> {|LS3001:Refused|}(string reason) => Task.FromResult(reason);
            }
            """);

}
