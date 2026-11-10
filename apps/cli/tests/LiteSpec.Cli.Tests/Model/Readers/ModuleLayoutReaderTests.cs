using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the backend readers recognize kinds and layers under the modules root.</summary>
public sealed class ModuleLayoutReaderTests
{
    /// <summary>A handler in a use case folder reads as a handler.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Handler_In_Use_Case_Folder_Reads_As_Handler()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "BorrowCopyCommandHandler");

        Assert.Equal(CodeKind.Handler, element.Kind);
    }

    /// <summary>A marked interface under Contracts reads as a port.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Interface_In_Contracts_Reads_As_Port()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "ITextGateway");

        Assert.Equal(CodeKind.Port, element.Kind);
    }

    /// <summary>A marked class in a Domain folder reads as a domain service.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Class_In_Domain_Folder_Reads_As_Domain_Service()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "LoanPricing");

        Assert.Equal(CodeKind.DomainService, element.Kind);
    }

    /// <summary>An endpoint in a use case folder reads as an endpoint.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Endpoint_In_Use_Case_Folder_Reads_As_Endpoint()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        var element = Assert.Single(code.Elements, element => element.Class == "BorrowCopyEndpoint");

        Assert.Equal(CodeKind.Endpoint, element.Kind);
    }

    /// <summary>A marked interface in a Domain folder reads as no port.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Interface_In_Domain_Folder_Reads_As_No_Port()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var code = ReadBackend(temp.Path);

        Assert.DoesNotContain(code.Elements, element => element.Class == "ILoanPolicy");
    }

    /// <summary>Each file reads its layer from its nearest layer folder or its role suffix.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Layer_Comes_From_Layer_Folder_Or_Role_Suffix()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal("Application", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Contracts", "Ports", "ITextGateway.cs")));
        Assert.Equal("Domain", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Loans", "Domain", "LoanPricing.cs")));
        Assert.Equal("Application", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs")));
        Assert.Equal("WebApi", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyEndpoint.cs")));
        Assert.Equal("Infrastructure", SourceRoots.LayerOf(config, ProductFile(temp.Path, "Lending", "Loans", "Infrastructure", "LoanStore.cs")));
        Assert.Equal("WebApi", SourceRoots.LayerOf(config, Path.Combine(temp.Path, "apps", "api", "src", "Library.WebApi", "Protocol", "SignOutEndpoint.cs")));
    }

    /// <summary>Unmarked Domain types group by their aggregate folder under the modules root, and shared kernel types under the kernel.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Constituents_Group_By_Aggregate_Folder_In_Module_Layout()
    {
        using var temp = Cli.Temp("readers");
        WriteModuleProject(temp.Path);
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        var root = ProductFile(temp.Path, "Lending", "Loans", "Domain", "Loan.cs");
        var held = ProductFile(temp.Path, "Lending", "Loans", "Domain", "Money.cs");
        var kernel = Path.Combine(temp.Path, "apps", "api", "src", "Library.SharedKernel", "Domain", "Clock.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.AggregateRoot, "Loan", null, root, 3,
            [new CodeMark("aggregate/lending.loan", MarkStyle.Xml, MarkVerb.Implements, root, 1)],
            InTests: false));
        model.Code.DomainTypes.Add(new DomainType("Money", "Library.Lending.Loans", held, 3, false));
        model.Code.DomainTypes.Add(new DomainType("Clock", "Library.Domain", kernel, 3, false));

        Constituents.Compute(model);

        Assert.Equal("apps/api/src/Library.Modules/Lending/Loans/Domain/Money.cs:Money", Assert.Single(model.Constituents["aggregate/lending.loan"]));
        Assert.Equal("apps/api/src/Library.SharedKernel/Domain/Clock.cs:Clock", Assert.Single(model.Constituents["kernel"]));
        Assert.Empty(Constituents.Orphans(model));
    }

    /// <summary>The modules and shared kernel roots resolve without the layer-projects extension, leaving the layer roots empty.</summary>
    /// <proves>litespec/core/model#declare-size-and-stacks</proves>
    [Fact]
    public void Module_Layout_Resolves_The_Modules_And_Shared_Kernel_Roots()
    {
        using var temp = Cli.Temp("readers");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.SharedKernel"));
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\"}");
        var config = ConsumerConfig.Load(temp.Path);

        Assert.False(config.IsLayerProjects);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules"), config.ModulesRoot);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.SharedKernel"), config.SharedKernelRoot);
        Assert.Equal("", config.DomainRoot);
        Assert.Equal("", config.InfrastructureRoot);
    }

    /// <summary>The layer-projects extension resolves the five project roots from their defaults.</summary>
    /// <proves>litespec/core/model#declare-size-and-stacks</proves>
    [Fact]
    public void Layer_Projects_Extension_Resolves_Five_Roots()
    {
        using var temp = Cli.Temp("readers");
        foreach (var project in new[] { "Library.Application", "Library.Domain", "Library.Infrastructure", "Library.WebApi", "Library.Worker" })
        {
            Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", project));
        }

        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"selectedExtensions\": [{\"id\": \"layer-projects\", \"criterion\": \"five projects\"}]}");
        var config = ConsumerConfig.Load(temp.Path);

        Assert.True(config.IsLayerProjects);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Application"), config.ModulesRoot);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Domain"), config.DomainRoot);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Infrastructure"), config.InfrastructureRoot);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.WebApi"), Assert.Single(config.WebApiRoots));
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Library.Worker"), Assert.Single(config.WorkerRoots));
    }

    /// <summary>Reads the backend of the module-layout project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The code model the backend reader reads.</returns>
    static CodeModel ReadBackend(string root) =>
        new DotnetBackendReader().Read(ConsumerConfig.Load(root));

    /// <summary>Writes a module-layout consumer: the modules root with a use case, contracts and layer folders.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteModuleProject(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"sharedKernel\": \"apps/api/src/Library.SharedKernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        WriteSource(ProductFile(root, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs"), """
            namespace Library.Lending.Loans.UseCases.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Contracts", "Ports", "ITextGateway.cs"), """
            namespace Library.Lending.Contracts.Ports;

            /// <implements>external-system/lending.text-gateway</implements>
            public interface ITextGateway
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Loans", "Domain", "LoanPricing.cs"), """
            namespace Library.Lending.Loans;

            /// <implements>rule/loan.copy-available</implements>
            public sealed class LoanPricing
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyEndpoint.cs"), """
            namespace Library.Lending.Loans.UseCases.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyEndpoint : IEndpoint
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Loans", "Domain", "ILoanPolicy.cs"), """
            namespace Library.Lending.Loans;

            /// <implements>external-system/lending.text-gateway</implements>
            public interface ILoanPolicy
            {
            }
            """);
        WriteSource(ProductFile(root, "Lending", "Loans", "Infrastructure", "LoanStore.cs"), """
            namespace Library.Lending.Loans.Infrastructure;

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
