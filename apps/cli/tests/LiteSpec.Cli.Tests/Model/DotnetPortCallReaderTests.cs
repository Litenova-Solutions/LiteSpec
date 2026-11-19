using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the backend reader collects a handler's calls on a held external-system port, however the handler holds it.</summary>
public sealed class DotnetPortCallReaderTests
{
    /// <summary>Handler Call On A Port Field Initialized From Its Primary Constructor Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerCallOnAPortFieldInitializedFromItsPrimaryConstructorIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, PortFolder, "IExchangeRateSource.cs", MarkedPort);
        WriteSource(temp.Path, HandlerFolder, "PostEntryCommandHandler.cs", """
            namespace Library.App.Application.PostEntry;

            /// <implements>use-case/books.post-entry</implements>
            public sealed class PostEntryCommandHandler(
                IEntryRepository entries,
                IExchangeRateSource exchangeRates) : ICommandHandler<PostEntryCommand>
            {
                private readonly IExchangeRateSource _exchangeRates = exchangeRates;

                public async Task HandleAsync(PostEntryCommand command, CancellationToken cancellationToken)
                {
                    var rate = await _exchangeRates.ReadReferenceRateAsync(command.Currency, cancellationToken);
                }
            }
            """);

        var dispatch = Assert.Single(Read(temp.Path).Handlers.Single().Dispatches!);

        Assert.Equal("calls", dispatch.Verb);
        Assert.Equal("ReadReferenceRateAsync", dispatch.Target);
        Assert.Equal("IExchangeRateSource", dispatch.Detail);
        Assert.Equal(12, dispatch.Line);
    }

    /// <summary>Handler Call On A Port Field Assigned With A Null Guard Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerCallOnAPortFieldAssignedWithANullGuardIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, PortFolder, "IExchangeRateSource.cs", MarkedPort);
        WriteSource(temp.Path, HandlerFolder, "PostEntryCommandHandler.cs", """
            namespace Library.App.Application.PostEntry;

            /// <implements>use-case/books.post-entry</implements>
            public sealed class PostEntryCommandHandler : ICommandHandler<PostEntryCommand>
            {
                private readonly IExchangeRateSource _exchangeRates;

                public PostEntryCommandHandler(IExchangeRateSource exchangeRates)
                {
                    _exchangeRates = exchangeRates ?? throw new ArgumentNullException(nameof(exchangeRates));
                }

                public async Task HandleAsync(PostEntryCommand command, CancellationToken cancellationToken)
                {
                    var rate = await _exchangeRates.ReadReferenceRateAsync(command.Currency, cancellationToken);
                }
            }
            """);

        var dispatch = Assert.Single(Read(temp.Path).Handlers.Single().Dispatches!);

        Assert.Equal("calls", dispatch.Verb);
        Assert.Equal("ReadReferenceRateAsync", dispatch.Target);
        Assert.Equal("IExchangeRateSource", dispatch.Detail);
        Assert.Equal(15, dispatch.Line);
    }

    /// <summary>Handler Call On A Port Field Assigned In An Expression Bodied Constructor Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerCallOnAPortFieldAssignedInAnExpressionBodiedConstructorIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, PortFolder, "IExchangeRateSource.cs", MarkedPort);
        WriteSource(temp.Path, HandlerFolder, "PostEntryCommandHandler.cs", """
            namespace Library.App.Application.PostEntry;

            /// <implements>use-case/books.post-entry</implements>
            public sealed class PostEntryCommandHandler : ICommandHandler<PostEntryCommand>
            {
                private readonly IExchangeRateSource _exchangeRates;

                public PostEntryCommandHandler(IExchangeRateSource exchangeRates) => _exchangeRates = exchangeRates;

                public async Task HandleAsync(PostEntryCommand command, CancellationToken cancellationToken)
                {
                    var rate = await _exchangeRates.ReadReferenceRateAsync(command.Currency, cancellationToken);
                }
            }
            """);

        var dispatch = Assert.Single(Read(temp.Path).Handlers.Single().Dispatches!);

        Assert.Equal("calls", dispatch.Verb);
        Assert.Equal("ReadReferenceRateAsync", dispatch.Target);
        Assert.Equal("IExchangeRateSource", dispatch.Detail);
        Assert.Equal(12, dispatch.Line);
    }

    /// <summary>Call On An Unmarked Interface Is Not A Dispatch.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CallOnAnUnmarkedInterfaceIsNotADispatch()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, PortFolder, "IExchangeRateSource.cs", """
            namespace Library.App.Ports.ExchangeRateSource;

            public interface IExchangeRateSource
            {
                Task<decimal> ReadReferenceRateAsync(string currency, CancellationToken cancellationToken);
            }
            """);
        WriteSource(temp.Path, HandlerFolder, "PostEntryCommandHandler.cs", """
            namespace Library.App.Application.PostEntry;

            /// <implements>use-case/books.post-entry</implements>
            public sealed class PostEntryCommandHandler(
                IEntryRepository entries,
                IExchangeRateSource exchangeRates) : ICommandHandler<PostEntryCommand>
            {
                private readonly IExchangeRateSource _exchangeRates = exchangeRates;

                public async Task HandleAsync(PostEntryCommand command, CancellationToken cancellationToken)
                {
                    var rate = await _exchangeRates.ReadReferenceRateAsync(command.Currency, cancellationToken);
                }
            }
            """);

        Assert.Empty(Read(temp.Path).Handlers.Single().Dispatches!);
    }

    const string PortFolder = "App/Ports/ExchangeRateSource";
    const string HandlerFolder = "App/Application/PostEntry";

    const string MarkedPort = """
        namespace Library.App.Ports.ExchangeRateSource;

        /// <implements>external-system/exchange-rate-source</implements>
        public interface IExchangeRateSource
        {
            Task<decimal> ReadReferenceRateAsync(string currency, CancellationToken cancellationToken);
        }
        """;

    static CodeFacts Read(string root) => new DotnetBackendReader().Read(ConsumerConfig.Load(root));

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App",
              "webapi": "src/WebApi"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
