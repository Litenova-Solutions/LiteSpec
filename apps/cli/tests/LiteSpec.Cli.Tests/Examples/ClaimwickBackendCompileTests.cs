using System.Collections.Immutable;
using System.Text;
using LiteSpec.Cli.Tests.Generate;
using LiteSpec.Cli.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Cli.Tests.Examples;

/// <summary>Proves the whole Claimwick backend compiles: every hand-written and generated file under the example API source, with no errors.</summary>
public sealed class ClaimwickBackendCompileTests
{
    /// <summary>The .NET SDK implicit usings for a class library, which Claimwick relies on: its files name only the namespaces beyond these.</summary>
    internal const string SdkImplicitUsings = """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Net.Http;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """;

    /// <summary>Faithful copies of the LiteBus APIs the Claimwick backend compiles against beyond the validator API, which <see cref="GenerateValidatorCompileTests"/> already stubs: the same namespaces, type and member names, signatures and constraints, with minimal bodies. Copied from LiteBus 8.0.0-dev.20261125013608: <c>LiteBus.Messaging.Abstractions/Handler/Main/IAsyncMessageHandler[TMessage].cs</c>, <c>IAsyncMessageHandler[TMessage,TMessageResult].cs</c>, <c>IMessageHandler.cs</c>, <c>IMessageHandler[TMessage,TMessageResult].cs</c>, <c>IProducesResult[TMessageResult].cs</c>, <c>Definitions/IMessageDefinition.cs</c>, <c>IMultiMessageDefinition.cs</c>, <c>IMessageDeclarationCatalog.cs</c>, <c>IMessageDeclarations.cs</c>, <c>Idempotency/IdempotencyDeclaration.cs</c>, <c>LiteBus.Commands.Abstractions/ICommand.cs</c>, <c>ICommand[TCommandResult].cs</c>, <c>ICommandHandler[TCommand,TCommandResult].cs</c>, <c>ICommandMediator.cs</c>, <c>CommandMediationSettings.cs</c>, <c>Extensions/CommandMediatorExtensions.cs</c>, <c>LiteBus.Queries.Abstractions/IQuery[TQueryResult].cs</c>, <c>IQueryHandler[TQuery,TQueryResult].cs</c>, <c>IQueryMediator.cs</c>, <c>QueryMediationSettings.cs</c>, <c>Extensions/QueryMediatorExtensions.cs</c>, <c>LiteBus.Events.Abstractions/IEventHandler[TEvent].cs</c>, and <c>LiteBus.Auditing.Abstractions/Declarations/AuditDeclaration.cs</c>, <c>AuditedDeclaration.cs</c>, <c>AuditExemptDeclaration.cs</c>, <c>MessageDeclarationsAuditExtensions.cs</c>. Only the members Claimwick uses are stubbed. The ASP.NET Core and Marten APIs resolve from the real shared framework and the Marten package instead.</summary>
    internal const string LiteBusBackendStubs = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using LiteBus.Messaging.Abstractions;

        namespace LiteBus.Messaging.Abstractions
        {
            public interface IMessageHandler;

            public interface IMessageHandler<in TMessage, out TMessageResult> : IMessageHandler
                where TMessage : notnull
                where TMessageResult : notnull
            {
                TMessageResult Handle(TMessage message);
            }

            public interface IAsyncMessageHandler<in TMessage, TMessageResult> : IMessageHandler<TMessage, Task<TMessageResult>>
                where TMessage : notnull
            {
                Task<TMessageResult> IMessageHandler<TMessage, Task<TMessageResult>>.Handle(TMessage message) => HandleAsync(message);

                Task<TMessageResult> HandleAsync(TMessage message, CancellationToken cancellationToken = default);
            }

            public interface IAsyncMessageHandler<in TMessage> : IMessageHandler<TMessage, Task>
                where TMessage : notnull
            {
                Task IMessageHandler<TMessage, Task>.Handle(TMessage message) => HandleAsync(message);

                Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
            }

            public interface IProducesResult<out TMessageResult>;

            public interface IMessageDefinition;

            public interface IMultiMessageDefinition : IMessageDefinition
            {
                void Describe(IMessageDeclarationCatalog catalog);
            }

            public interface IMessageDeclarationCatalog
            {
                IMessageDeclarationCatalog Describe<TMessage>(Action<IMessageDeclarations> describe)
                    where TMessage : notnull;

                IMessageDeclarationCatalog Describe(Type messageType, Action<IMessageDeclarations> describe);

                IMessageDeclarationCatalog Replace<TMessage>(Action<IMessageDeclarations> describe)
                    where TMessage : notnull;

                IMessageDeclarationCatalog Replace(Type messageType, Action<IMessageDeclarations> describe);
            }

            public interface IMessageDeclarations
            {
                IMessageDeclarations Declare<TValue>(TValue value)
                    where TValue : notnull;

                IMessageDeclarations Authorized(string policy);

                IMessageDeclarations Exempt<TValue>(string rationale)
                    where TValue : notnull;
            }

            public sealed record IdempotencyDeclaration
            {
                public IdempotencyDeclaration(Func<object, string> keySelector)
                {
                    KeySelector = keySelector;
                }

                public Func<object, string>? KeySelector { get; init; }

                public Func<object, string>? FingerprintSelector { get; init; }

                public string? Scope { get; init; }

                public bool ReplayResult { get; init; }

                public static IdempotencyDeclaration KeyedBy<TMessage>(Func<TMessage, string> keySelector)
                    where TMessage : notnull
                {
                    ArgumentNullException.ThrowIfNull(keySelector);

                    return new IdempotencyDeclaration(message => keySelector((TMessage)message));
                }

                public static IdempotencyDeclaration KeyedByContext() => new(_ => string.Empty);

                public IdempotencyDeclaration WithFingerprint<TMessage>(Func<TMessage, string> fingerprintSelector)
                    where TMessage : notnull
                {
                    ArgumentNullException.ThrowIfNull(fingerprintSelector);

                    return this with { FingerprintSelector = message => fingerprintSelector((TMessage)message) };
                }
            }
        }

        namespace LiteBus.Commands.Abstractions
        {
            public interface ICommand<TCommandResult> : ICommand, IProducesResult<TCommandResult>;

            public interface ICommandHandler<in TCommand, TCommandResult> : IAsyncMessageHandler<TCommand, TCommandResult>
                where TCommand : ICommand<TCommandResult>;

            public interface ICommandMediator
            {
                Task SendAsync(ICommand command, CommandMediationSettings? commandMediationSettings = null, CancellationToken cancellationToken = default);

                Task<TCommandResult> SendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings? commandMediationSettings = null, CancellationToken cancellationToken = default);
            }

            public sealed class CommandMediationSettings;

            public static class CommandMediatorExtensions
            {
                public static Task SendAsync(this ICommandMediator commandMediator, ICommand command, CancellationToken cancellationToken = default) =>
                    commandMediator.SendAsync(command, null, cancellationToken);

                public static Task<TCommandResult> SendAsync<TCommandResult>(this ICommandMediator commandMediator, ICommand<TCommandResult> command, CancellationToken cancellationToken = default) =>
                    commandMediator.SendAsync(command, null, cancellationToken);
            }
        }

        namespace LiteBus.Queries.Abstractions
        {
            public interface IQuery<TQueryResult> : IQuery, IProducesResult<TQueryResult>;

            public interface IQueryHandler<in TQuery, TQueryResult> : IAsyncMessageHandler<TQuery, TQueryResult>
                where TQuery : IQuery<TQueryResult>;

            public interface IQueryMediator
            {
                Task<TQueryResult> QueryAsync<TQueryResult>(IQuery<TQueryResult> query, QueryMediationSettings? queryMediationSettings = null, CancellationToken cancellationToken = default);
            }

            public sealed class QueryMediationSettings;

            public static class QueryMediatorExtensions
            {
                public static Task<TQueryResult> QueryAsync<TQueryResult>(this IQueryMediator queryMediator, IQuery<TQueryResult> query, CancellationToken cancellationToken = default) =>
                    queryMediator.QueryAsync(query, null, cancellationToken);
            }
        }

        namespace LiteBus.Events.Abstractions
        {
            public interface IEventHandler<in TEvent> : IAsyncMessageHandler<TEvent>
                where TEvent : notnull;
        }

        namespace LiteBus.Auditing.Abstractions
        {
            public abstract record AuditDeclaration
            {
                internal AuditDeclaration()
                {
                }

                public abstract bool IsAudited { get; }

                public static AuditedDeclaration Audited(string action, string? category = null, string? targetKind = null, bool reasonRequired = false) => throw new NotImplementedException();

                public static AuditExemptDeclaration Exempt(string rationale) => throw new NotImplementedException();
            }

            public sealed record AuditedDeclaration(string Action) : AuditDeclaration
            {
                public override bool IsAudited => true;

                public string? Category { get; init; }

                public string? TargetKind { get; init; }

                public bool ReasonRequired { get; init; }

                public Func<object, string?>? TargetSelector { get; init; }

                public Func<object, string?>? ReasonSelector { get; init; }

                public AuditedDeclaration WithTargetFrom<TMessage>(Func<TMessage, string?> targetSelector) => throw new NotImplementedException();

                public AuditedDeclaration WithReasonFrom<TMessage>(Func<TMessage, string?> reasonSelector) => throw new NotImplementedException();
            }

            public sealed record AuditExemptDeclaration(string Rationale) : AuditDeclaration
            {
                public override bool IsAudited => false;
            }

            public static class MessageDeclarationsAuditExtensions
            {
                public static IMessageDeclarations Audited(this IMessageDeclarations declarations, string action, string? category = null, string? targetKind = null, bool reasonRequired = false) => throw new NotImplementedException();

                public static IMessageDeclarations NotAudited(this IMessageDeclarations declarations, string rationale) => throw new NotImplementedException();
            }
        }
        """;

    /// <summary>A faithful copy of the one OpenAPI API the Claimwick host calls, from Microsoft.AspNetCore.OpenApi 10.0 (<c>Microsoft.AspNetCore.Builder.OpenApiEndpointRouteBuilderExtensions.MapOpenApi</c>), with a minimal body. The package is not referenced: its source generator and framework reference would change the test output the CLI runs from.</summary>
    const string OpenApiStubs = """
        namespace Microsoft.AspNetCore.Builder;

        public static class OpenApiEndpointRouteBuilderExtensions
        {
            public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapOpenApi(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern = "/openapi/{documentName}.json") => throw new System.NotSupportedException();
        }
        """;

    /// <summary>The Claimwick API source the test compiles, hand-written and generated files together.</summary>
    static readonly string SourceRoot = Path.Combine(TestCli.Repository, "examples", "claimwick", "apps", "api", "src");

    /// <summary>The running .NET's own assemblies, with the ASP.NET Core shared framework and the Marten package the test project references.</summary>
    static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>The whole Claimwick backend compiles with no errors.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClaimwickBackendCompilesWithNoErrors()
    {
        var paths = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(paths);
        Assert.Contains(paths, path => Path.GetFileName(path) == "Program.cs");

        var files = paths
            .Select(path => (Path: Path.GetRelativePath(TestCli.Repository, path), Source: File.ReadAllText(path)))
            .Concat(
            [
                (Path: "SdkImplicitUsings.cs", Source: SdkImplicitUsings),
                (Path: "LiteBusValidatorStubs.cs", Source: GenerateValidatorCompileTests.LiteBusStubs),
                (Path: "LiteBusBackendStubs.cs", Source: LiteBusBackendStubs),
                (Path: "OpenApiStubs.cs", Source: OpenApiStubs),
            ])
            .ToList();
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(
                SourceText.From(file.Source, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
                file.Path))
            .ToList();
        var compilation = CSharpCompilation.Create(
            "ClaimwickBackend",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, "The Claimwick backend does not compile:\n" + string.Join("\n", errors));
    }

    /// <summary>The running .NET's own assemblies (every trusted platform assembly the test process loaded with, which carries the framework and the referenced packages), the ASP.NET Core reference assemblies from the SDK targeting pack matching the running runtime. The test process runs without the ASP.NET Core shared framework, so the CLI it starts keeps the assemblies it ships with.</summary>
    /// <returns>The metadata references the Claimwick backend compiles against.</returns>
    static ImmutableArray<MetadataReference> LoadReferences()
    {
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var platformNames = platform.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var paths = platform
            .Concat(AspNetCoreReferencePaths().Where(path => !platformNames.Contains(Path.GetFileName(path))));
        return [.. paths.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    /// <summary>The ASP.NET Core reference assemblies of the targeting pack whose major version matches the running runtime, the newest such pack the SDK installs.</summary>
    /// <returns>The reference assembly paths.</returns>
    static IEnumerable<string> AspNetCoreReferencePaths()
    {
        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dotnetRoot = Path.GetFullPath(Path.Combine(runtime, "..", "..", ".."));
        var packs = Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref");
        var major = Environment.Version.Major;
        var pack = Directory.GetDirectories(packs)
            .Select(directory => (Directory: directory, Version: Version.TryParse(Path.GetFileName(directory).Split('-')[0], out var version) ? version : null))
            .Where(candidate => candidate.Version is not null && candidate.Version.Major == major)
            .OrderByDescending(candidate => candidate.Version)
            .Select(candidate => candidate.Directory)
            .First();
        return Directory.GetFiles(Path.Combine(pack, "ref", $"net{major}.0"), "*.dll");
    }

}
