using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the backend readers recognize kinds and layers under the modules root.</summary>
public sealed class ModuleLayoutReaderTests
{
    /// <summary>A handler in a use case folder reads as a handler.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerInUseCaseFolderReadsAsHandler()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "BorrowCopyCommandHandler");

        Assert.Equal(CodeKind.Handler, element.Kind);
    }

    /// <summary>A marked interface in a module Ports folder reads as a port.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void InterfaceInPortsFolderReadsAsPort()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "IPayoutBank");

        Assert.Equal(CodeKind.Port, element.Kind);
    }

    /// <summary>A marked class in a Domain folder reads as a domain service.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ClassInDomainFolderReadsAsDomainService()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "LoanPricing");

        Assert.Equal(CodeKind.DomainService, element.Kind);
    }

    /// <summary>An endpoint in a use case folder reads as an endpoint.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EndpointInUseCaseFolderReadsAsEndpoint()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "BorrowCopyEndpoint");

        Assert.Equal(CodeKind.Endpoint, element.Kind);
    }

    /// <summary>A marked interface in a Domain folder reads as no port.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void InterfaceInDomainFolderReadsAsNoPort()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        Assert.DoesNotContain(code.Elements, element => element.Class == "ILoanPolicy");
    }

    /// <summary>Each file reads its layer from its nearest layer folder or its role suffix.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void LayerComesFromLayerFolderOrRoleSuffix()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal("Application", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Contracts", "Loans", "LoanBorrowed.cs")));
        Assert.Equal("Ports", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Ports", "PayoutBank", "IPayoutBank.cs")));
        Assert.Equal("Domain", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Domain", "Loans", "LoanPricing.cs")));
        Assert.Equal("Application", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Application", "BorrowCopy", "BorrowCopyCommandHandler.cs")));
        Assert.Equal("WebApi", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "WebApi", "BorrowCopy", "BorrowCopyEndpoint.cs")));
        Assert.Equal("Infrastructure", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Infrastructure", "Persistence", "LoanStore.cs")));
        Assert.Equal("WebApi", SourceRoots.LayerOf(config, Path.Combine(temp.Path, "apps", "api", "src", "Library.WebApi", "Protocol", "SignOutEndpoint.cs")));
    }

    /// <summary>Unmarked Domain types group by their aggregate folder under the modules root, and Kernel types under the kernel.</summary>
    /// <proves>litespec/core/backend-domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void PartsGroupByAggregateFolderInModuleLayout()
    {
        using var temp = TestCli.Temp("readers");
        WriteModuleProject(temp.Path);
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        var root = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Loan.cs");
        var held = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Money.cs");
        var kernel = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Clock.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.AggregateRoot, "Loan", null, root, 3,
            [new CodeMark("aggregate/lending.loan", MarkStyle.Xml, MarkVerb.Implements, root, 1)],
            InTests: false));
        model.Code.DomainTypes.Add(new DomainType("Money", "Library.Lending.Domain.Loans", held, 3, false));
        model.Code.DomainTypes.Add(new DomainType("Clock", "Library.Kernel", kernel, 3, false));

        Parts.Compute(model);

        Assert.Equal("apps/api/src/Library.Modules/Lending/Domain/Loans/Money.cs:Money", Assert.Single(model.Parts["aggregate/lending.loan"]));
        Assert.Equal("apps/api/src/Library.Kernel/Clock.cs:Clock", Assert.Single(model.Parts["kernel"]));
        Assert.Empty(Parts.Orphans(model));
    }

    /// <summary>The module, Kernel and Platform roots resolve from their defaults.</summary>
    /// <proves>litespec/core/model#declare-size-and-stacks</proves>
    [Fact]
    public void ModuleLayoutResolvesTheModuleKernelAndPlatformRoots()
    {
        using var temp = TestCli.Temp("readers");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform"));
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\"}");
        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal(
            [Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending")],
            config.ModulesRoots);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel"), config.KernelRoot);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform"), config.PlatformRoot);
    }

    /// <summary>Reads the backend of the module-layout project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The code model the backend reader reads.</returns>
    static CodeFacts ReadBackend(string root) =>
        new DotnetBackendReader().Read(ConsumerConfig.Load(root));

    /// <summary>Writes a module-layout consumer: the modules root with a use case, contracts and layer folders.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteModuleProject(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"kernel\": \"apps/api/src/Library.Kernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        WriteSource(ProductFile(root, "Lending", "Application", "BorrowCopy", "BorrowCopyCommandHandler.cs"), """
            namespace Library.Lending.Application.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Contracts", "Loans", "LoanBorrowed.cs"), """
            namespace Library.Lending.Contracts.Loans;

            /// <implements>module-event/lending.loan-borrowed</implements>
            public sealed record LoanBorrowed(Guid LoanId);
            """);
        WriteSource(ProductFile(root, "Lending", "Ports", "PayoutBank", "IPayoutBank.cs"), """
            namespace Library.Lending.Ports.PayoutBank;

            /// <implements>external-system/lending.payout-bank</implements>
            public interface IPayoutBank
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Domain", "Loans", "LoanPricing.cs"), """
            namespace Library.Lending.Domain.Loans;

            /// <implements>rule/loan.copy-available</implements>
            public sealed class LoanPricing
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "WebApi", "BorrowCopy", "BorrowCopyEndpoint.cs"), """
            namespace Library.Lending.WebApi.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyEndpoint : IEndpoint
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Domain", "Loans", "ILoanPolicy.cs"), """
            namespace Library.Lending.Domain.Loans;

            /// <implements>external-system/lending.text-gateway</implements>
            public interface ILoanPolicy
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Infrastructure", "Persistence", "LoanStore.cs"), """
            namespace Library.Lending.Infrastructure.Persistence;

            public sealed class LoanStore
            {
            }
            """);
    }

    /// <summary>Writes one source file, creating its folder.</summary>
    /// <param name="file">The full path of the file to write.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }

    /// <summary>One file under the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string ProductFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }
}
