using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the grouping checks: each kind of place holds only its closed grouping folders, and a folder past 20 files with none of them reports folder-ungrouped.</summary>
public sealed class FolderGroupingCheckTests
{
    /// <summary>Kernel Domain Values And Auditing Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelDomainValuesAndAuditingPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Domain", "DomainException.cs"), "namespace Library.Kernel.Domain;\n\npublic abstract class DomainException\n{\n}\n");
        WriteSource(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Values", "Money.cs"), "namespace Library.Kernel.Values;\n\npublic sealed record Money(decimal Amount);\n");
        WriteSource(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Auditing", "AuditEntry.cs"), "namespace Library.Kernel.Auditing;\n\npublic sealed record AuditEntry(string Action);\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Kernel Folder Outside Groupings Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelFolderOutsideGroupingsReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Shared"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("Domain/, Values/ and Auditing/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Kernel Root File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelRootFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Money.cs");
        WriteSource(file, "namespace Library.Kernel;\n\npublic sealed record Money(decimal Amount);\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(file, finding.Path);
        Assert.Contains("holds no files; move it into Domain/, Values/ or Auditing/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Kernel Root Files Report Drift At Any Count.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelRootFilesReportDriftAtAnyCount()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel"), "KernelPart", 21, ".cs");

        var findings = Run(model);

        Assert.Equal(21, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
    }

    /// <summary>Kernel Root Files With Grouping Still Report Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelRootFilesWithGroupingStillReportDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel"), "KernelPart", 21, ".cs");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Domain"));

        var findings = Run(model);

        Assert.Equal(21, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
    }

    /// <summary>Platform Auditing Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformAuditingReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Auditing"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no Platform folder", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Platform Country Packs Areas Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformCountryPacksAreasPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "CountryPacks", "Vat", "VatRates.cs"), "namespace Library.Platform.CountryPacks.Vat;\n\npublic sealed class VatRates\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Platform Country Packs Area Outside Set Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformCountryPacksAreaOutsideSetReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "CountryPacks", "Tax"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("Vat/, Charts/, Banking/, Filing/, Documents/, Country/ and Retention/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Platform Capability Past Twenty Files Reports Ungrouped.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformCapabilityPastTwentyFilesReportsUngrouped()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Access"), "AccessPart", 21, ".cs");

        var finding = Assert.Single(Run(model));

        Assert.Equal("folder-ungrouped", finding.Code);
        Assert.Contains("one folder per sub-area", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Platform Capability Past Twenty Files With Subarea Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformCapabilityPastTwentyFilesWithSubareaPasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Access"), "AccessPart", 21, ".cs");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Access", "Policies"));

        Assert.Empty(Run(model));
    }

    /// <summary>Platform Root File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformRootFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "CompositionExtensions.cs");
        WriteSource(file, "namespace Library.Platform;\n\npublic static class CompositionExtensions\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(file, finding.Path);
        Assert.Contains("holds no files; move it into its capability folder", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Platform Root Project File Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformRootProjectFilePasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Library.Platform.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Parts And Facts Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainPartsAndFactsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Parts", "LoanLine.cs"), "namespace Library.Lending.Domain.Loans.Parts;\n\npublic sealed class LoanLine\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Facts", "CountryRate.cs"), "namespace Library.Lending.Domain.Loans.Facts;\n\npublic sealed class CountryRate\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Act Folder With Partial Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainActFolderWithPartialPasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "PostLoan", "Loan.PostLoan.cs"), "namespace Library.Lending.Domain.Loans.PostLoan;\n\npublic sealed partial class Loan\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Act Folder Without Partial Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainActFolderWithoutPartialReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "PostLoan", "LoanNotes.cs"), "namespace Library.Lending.Domain.Loans.PostLoan;\n\npublic sealed class LoanNotes\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("root-action folder", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Invented Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainInventedFolderReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "x.cs"))!, "Helpers"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("Parts, Facts or root-action folder", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Aggregate Past Twenty Files Reports Ungrouped.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainAggregatePastTwentyFilesReportsUngrouped()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var aggregate = Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "x.cs"))!;
        WriteFiles(aggregate, "LoanPart", 21, ".cs");

        var finding = Assert.Single(Run(model));

        Assert.Equal("folder-ungrouped", finding.Code);
        Assert.Contains("21 files", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Aggregate Twenty Files Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainAggregateTwentyFilesPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var aggregate = Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "x.cs"))!;
        WriteFiles(aggregate, "LoanPart", 20, ".cs");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain State Beside Root Reports Parts.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainStateBesideRootReportsParts()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var state = ModuleFile(temp.Path, "Lending", "Domain", "Loans", "LoanActiveState.cs");
        WriteSource(state, "namespace Library.Lending.Domain.Loans;\n\npublic sealed record LoanActiveState : LoanState;\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(state, finding.Path);
        Assert.Contains("belongs in 'Parts/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain States In Parts Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainStatesInPartsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Parts", "LoanState.cs"), "namespace Library.Lending.Domain.Loans.Parts;\n\npublic abstract record LoanState;\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Parts", "LoanActiveState.cs"), "namespace Library.Lending.Domain.Loans.Parts;\n\npublic sealed record LoanActiveState : LoanState;\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Partial File Beside Root Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainPartialFileBesideRootPasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Loan.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic sealed partial class Loan\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "ILoanRepository.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic interface ILoanRepository\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Loan.Lines.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic sealed partial class Loan\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Single Store Interface Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainSingleStoreInterfacePasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Loan.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic sealed class Loan\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "ILoanRepository.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic interface ILoanRepository\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Domain Second Store Interface Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainSecondStoreInterfaceReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "ILoanRepository.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic interface ILoanRepository\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Domain", "Loans", "ILoanArchiveRepository.cs"), "namespace Library.Lending.Domain.Loans;\n\npublic interface ILoanArchiveRepository\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("declares 2 store interfaces ('ILoanArchiveRepository', 'ILoanRepository')", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Parts Event File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainPartsEventFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var stray = ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Parts", "LoanFiledEvent.cs");
        WriteSource(stray, "namespace Library.Lending.Domain.Loans.Parts;\n\npublic sealed record LoanFiledEvent;\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("belongs in 'Events/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Facts Exception File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainFactsExceptionFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var stray = ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Facts", "LoanLimitException.cs");
        WriteSource(stray, "namespace Library.Lending.Domain.Loans.Facts;\n\npublic sealed class LoanLimitException : Exception\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("belongs in 'Exceptions/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Domain Events Exception File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void DomainEventsExceptionFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var stray = ModuleFile(temp.Path, "Lending", "Domain", "Loans", "Events", "LoanLimitException.cs");
        WriteSource(stray, "namespace Library.Lending.Domain.Loans.Events;\n\npublic sealed class LoanLimitException : Exception\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("belongs in 'Exceptions/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Port Models With Named Types Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortModelsWithNamedTypesPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<PackPage> CurrentPage(string countryCode);\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "Models", "PackPage.cs"),
            "namespace Library.Lending.Ports.CountryPack.Models;\n\npublic sealed record PackPage(int Total);\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Port Type Beside Interface Reports Models.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortTypeBesideInterfaceReportsModels()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<PackPage> CurrentPage(string countryCode);\n}\n");
        var stray = ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "PackPage.cs");
        WriteSource(stray, "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackPage(int Total);\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("belongs in 'Models/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Port Models Type No Signature Names Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortModelsTypeNoSignatureNamesReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        var stray = ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "Models", "RateCache.cs");
        WriteSource(stray, "namespace Library.Lending.Ports.CountryPack.Models;\n\npublic sealed class RateCache\n{\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("named by no signature", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Port Folder Outside Models Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderOutsideModelsReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "x.cs"))!, "Cache"));
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("only its Models/ grouping", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Adapter Models Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterModelsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter : ICountryPack\n{\n    public Task<PackResponse> FetchAsync() => throw new NotImplementedException();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "Models", "PackResponse.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack.Models;\n\npublic sealed record PackResponse(int Total);\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Adapter Folder Outside Models Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterFolderOutsideModelsReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "x.cs"))!, "Cache"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("only its Models/ and Parts/ groupings", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Adapter Wire Type Beside Adapter Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterWireTypeBesideAdapterReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");
        var stray = ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "PackResponse.cs");
        WriteSource(stray, "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed record PackResponse(int Total);\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("belongs in 'Models/' when the external system shapes it, or in 'Parts/' when the adapter calls it", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Adapter Implementing Port Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterImplementingPortPasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "MailRelay", "IMailRelay.cs"),
            "namespace Library.Lending.Ports.MailRelay;\n\npublic interface IMailRelay\n{\n    Task SendAsync(string key);\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "MailRelay", "MailRelayMessenger.cs"),
            "namespace Library.Lending.Infrastructure.MailRelay;\n\npublic sealed class MailRelayMessenger : IMailRelay\n{\n    public Task SendAsync(string key) => Task.CompletedTask;\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Several Adapters Side By Side Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void SeveralAdaptersSideBySidePass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CsvCountryPack.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CsvCountryPack : ICountryPack\n{\n    public Task<int> Ping() => Task.FromResult(0);\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "JsonCountryPack.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class JsonCountryPack : ICountryPack\n{\n    public Task<int> Ping() => Task.FromResult(0);\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Adapter Options And Parts Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterOptionsAndPartsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter : ICountryPack\n{\n    public CountryPackAdapter(PackRetry retry, CountryPackAdapterOptions options)\n    {\n    }\n\n    public Task<PackResponse> FetchAsync() => throw new NotImplementedException();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapterOptions.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapterOptions\n{\n    public string Endpoint { get; set; } = \"\";\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "Models", "PackResponse.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack.Models;\n\npublic sealed record PackResponse(int Total);\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "Parts", "PackRetry.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack.Parts;\n\npublic sealed class PackRetry\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Adapter Models Type No Adapter Names Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterModelsTypeNoAdapterNamesReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");
        var stray = ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "Models", "RateCache.cs");
        WriteSource(stray, "namespace Library.Lending.Infrastructure.CountryPack.Models;\n\npublic sealed class RateCache\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("is named by no adapter file", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Adapter Parts Type No Adapter Names Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterPartsTypeNoAdapterNamesReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");
        var stray = ModuleFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "Parts", "RateCache.cs");
        WriteSource(stray, "namespace Library.Lending.Infrastructure.CountryPack.Parts;\n\npublic sealed class RateCache\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("is named by no adapter file", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Persistence Root Files Report Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceRootFilesReportDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        var persistence = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Infrastructure", "Persistence");
        WriteFiles(persistence, "LoanStore", 21, ".cs");

        var findings = Run(model);

        Assert.Equal(21, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
        Assert.All(findings, finding => Assert.Contains("holds only module-wide store setup", finding.Message, StringComparison.Ordinal));
    }

    /// <summary>Persistence Root Files Without Aggregates Report Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceRootFilesWithoutAggregatesReportDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/reporting", "reporting", temp.Path));
        var persistence = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Reporting", "Infrastructure", "Persistence");
        WriteFiles(persistence, "ReportStore", 21, ".cs");

        var findings = Run(model);

        Assert.Equal(21, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
    }

    /// <summary>Persistence Root Mapping File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceRootMappingFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        var file = ModuleFile(temp.Path, "Lending", "Infrastructure", "Persistence", "LoanStoreConfiguration.cs");
        WriteSource(file, "namespace Library.Lending.Infrastructure.Persistence;\n\npublic sealed class LoanStoreConfiguration\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(file, finding.Path);
        Assert.Contains("holds only module-wide store setup ('LendingStoreContribution.cs')", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Persistence Nested Mapping File Passes.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceNestedMappingFilePasses()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "Persistence", "Loans", "LoanStoreConfiguration.cs"),
            "namespace Library.Lending.Infrastructure.Persistence.Loans;\n\npublic sealed class LoanStoreConfiguration\n{\n}\n");
        WriteSource(
            ModuleFile(temp.Path, "Lending", "Infrastructure", "Persistence", "LendingStoreContribution.cs"),
            "namespace Library.Lending.Infrastructure.Persistence;\n\npublic sealed class LendingStoreContribution\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Persistence Adapter File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceAdapterFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        var nested = ModuleFile(temp.Path, "Lending", "Infrastructure", "Persistence", "Loans", "MailRelayAdapter.cs");
        WriteSource(nested, "namespace Library.Lending.Infrastructure.Persistence.Loans;\n\npublic sealed class MailRelayAdapter\n{\n}\n");
        var root = ModuleFile(temp.Path, "Lending", "Infrastructure", "Persistence", "PayoutBankClient.cs");
        WriteSource(root, "namespace Library.Lending.Infrastructure.Persistence;\n\npublic sealed class PayoutBankClient\n{\n}\n");

        var findings = Run(model);

        Assert.Equal(2, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
        Assert.All(findings, finding => Assert.Contains("holds an adapter, which sits in its Infrastructure/{Port}/ folder, not in Persistence/", finding.Message, StringComparison.Ordinal));
    }

    /// <summary>Infrastructure Pipeline Files Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructurePipelineFilesPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "CurrentActorMiddleware.cs"), "namespace Library.Lending.Infrastructure.Pipeline;\n\npublic sealed class CurrentActorMiddleware\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "RecentSignInCondition.cs"), "namespace Library.Lending.Infrastructure.Pipeline;\n\npublic sealed class RecentSignInCondition\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Infrastructure Pipeline Protocol Folders Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructurePipelineProtocolFoldersPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "OAuth", "OAuthHandler.cs"), "namespace Library.Lending.Infrastructure.Pipeline.OAuth;\n\npublic sealed class OAuthHandler\n{\n}\n");
        WriteSource(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "OAuth", "OAuthToken.cs"), "namespace Library.Lending.Infrastructure.Pipeline.OAuth;\n\npublic sealed record OAuthToken(string Value);\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Infrastructure Pipeline Past Twenty Files Reports Ungrouped.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructurePipelinePastTwentyFilesReportsUngrouped()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var pipeline = Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "x.cs"))!;
        WriteFiles(pipeline, "PipelinePart", 21, ".cs");

        var finding = Assert.Single(Run(model));

        Assert.Equal("folder-ungrouped", finding.Code);
        Assert.Contains("group them into one folder per protocol", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Infrastructure Pipeline Protocol Nested Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructurePipelineProtocolNestedFolderReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Infrastructure", "Pipeline", "OAuth", "x.cs"))!, "Cache"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("which holds files only", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Web Api Pipeline Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void WebApiPipelineFolderReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        model.Records.Add(UseCaseRec("use-case/lending.borrow-copy", "lending", "loans", "borrow-copy", temp.Path));
        WriteSource(ModuleFile(temp.Path, "Lending", "WebApi", "Pipeline", "CurrentActorMiddleware.cs"), "namespace Library.Lending.WebApi.Pipeline;\n\npublic sealed class CurrentActorMiddleware\n{\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no declared use case", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Infrastructure Messaging Files Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructureMessagingFilesPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(ModuleFile(temp.Path, "Lending", "Infrastructure", "Messaging", "LoanEventMapping.cs"), "namespace Library.Lending.Infrastructure.Messaging;\n\npublic sealed class LoanEventMapping\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Infrastructure Messaging Subfolder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void InfrastructureMessagingSubfolderReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(ModuleFile(temp.Path, "Lending", "Infrastructure", "Messaging", "x.cs"))!, "Cache"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("which holds files only", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Use Case Past Twenty Files Reports Ungrouped.</summary>
    /// <proves>litespec/core/backend-application#use-this-use-case-layout</proves>
    [Fact]
    public void UseCasePastTwentyFilesReportsUngrouped()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        model.Records.Add(UseCaseRec("use-case/lending.borrow-copy", "lending", "loans", "borrow-copy", temp.Path));
        var useCase = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Application", "Loans", "BorrowCopy");
        WriteFiles(useCase, "BorrowCopyPart", 21, ".cs");

        var finding = Assert.Single(Run(model));

        Assert.Equal("folder-ungrouped", finding.Code);
        Assert.Contains("move fan-out into a job", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Use Case Twenty Files Pass.</summary>
    /// <proves>litespec/core/backend-application#use-this-use-case-layout</proves>
    [Fact]
    public void UseCaseTwentyFilesPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        model.Records.Add(Rec("module/lending", "lending", temp.Path));
        model.Records.Add(AggregateRec("aggregate/lending.loan", "lending", "loans", temp.Path));
        model.Records.Add(UseCaseRec("use-case/lending.borrow-copy", "lending", "loans", "borrow-copy", temp.Path));
        var useCase = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Application", "Loans", "BorrowCopy");
        WriteFiles(useCase, "BorrowCopyPart", 20, ".cs");

        Assert.Empty(Run(model));
    }

    /// <summary>Shared Components Groups Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedComponentsGroupsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(Path.Combine(temp.Path, "packages", "ui", "src", "components", "fields", "text-field.svelte"), "<input />\n");
        WriteSource(Path.Combine(temp.Path, "packages", "ui", "src", "components", "index.ts"), "export {};\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Shared Components Folder Outside Groups Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedComponentsFolderOutsideGroupsReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "packages", "ui", "src", "components", "widgets"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("fields/, overlays/, layout/, data/, values/ and feedback/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Shared Components Past Twenty Files Reports Ungrouped.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedComponentsPastTwentyFilesReportsUngrouped()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "packages", "ui", "src", "components"), "widget", 21, ".svelte");

        var finding = Assert.Single(Run(model));

        Assert.Equal("folder-ungrouped", finding.Code);
        Assert.Contains("21 files", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Shared App Groups Pass.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedAppGroupsPass()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteSource(Path.Combine(temp.Path, "apps", "web", "src", "lib", "app", "session", "session.ts"), "export {};\n");
        WriteSource(Path.Combine(temp.Path, "apps", "web", "src", "lib", "app", "litespec-records.ts"), "export {};\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Shared App Root File Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedAppRootFileReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "web", "src", "lib", "app", "money.ts");
        WriteSource(file, "export type Money = {\n  amount: string;\n  currency: string;\n};\n\nexport function formatMoney(money: Money): string {\n  return `${money.currency} ${money.amount}`;\n}\n");

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(file, finding.Path);
        Assert.Contains("move it into session/, i18n/, api/ or ui/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Shared App Folder Outside Groups Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedAppFolderOutsideGroupsReportsDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "web", "src", "lib", "app", "helpers"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("session/, i18n/, api/ and ui/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Shared App Root Files Report Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SharedAppRootFilesReportDrift()
    {
        using var temp = TestCli.Temp("grouping");
        var model = GroupingModel(temp.Path);
        WriteFiles(Path.Combine(temp.Path, "apps", "web", "src", "lib", "app"), "shared", 21, ".ts");

        var findings = Run(model);

        Assert.Equal(21, findings.Count);
        Assert.All(findings, finding => Assert.Equal("code-placement-drift", finding.Code));
    }

    /// <summary>Runs the closed folder check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The closed folder findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        ClosedFolderChecks.Run(model, findings);

        return findings;
    }

    /// <summary>A model with the modules, Kernel, Platform, frontend and shared package roots.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="platform">The frontend platform.</param>
    /// <returns>The project model holding no records.</returns>
    static ProjectModel GroupingModel(string root, string platform = "svelte")
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"marks\": \"xml\", \"frontends\": [{\"name\": \"web\", \"path\": \"apps/web\", \"platform\": \"" + platform + "\"}], \"ui\": {\"package\": {\"path\": \"packages/ui\"}}, \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"kernel\": \"apps/api/src/Library.Kernel\", \"platform\": \"apps/api/src/Library.Platform\"}}}");

        return new ProjectModel { Config = ConsumerConfig.Load(root) };
    }

    /// <summary>One file under the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string ModuleFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>Writes one source file with the text as given.</summary>
    /// <param name="file">The full path of the file to write.</param>
    /// <param name="text">The file contents.</param>
    static void WriteSource(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }

    /// <summary>Writes numbered source files into the folder.</summary>
    /// <param name="folder">The folder to write into.</param>
    /// <param name="stem">The file name stem.</param>
    /// <param name="count">How many files to write.</param>
    /// <param name="extension">The file extension.</param>
    static void WriteFiles(string folder, string stem, int count, string extension)
    {
        Directory.CreateDirectory(folder);

        for (var at = 0; at < count; at++)
        {
            File.WriteAllText(Path.Combine(folder, $"{stem}{at:00}{extension}"), "namespace Grouping;\n\npublic sealed class " + stem + at + "\n{\n}\n");
        }
    }

    /// <summary>One module record.</summary>
    /// <param name="id">The module identifier.</param>
    /// <param name="module">The module name.</param>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The record.</returns>
    static Record Rec(string id, string module, string root) => Placed(RecordTypes.Module, id, module, $".litespec/behavior/modules/{module}", root);

    /// <summary>One aggregate record under its aggregate folder.</summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="module">The module name.</param>
    /// <param name="aggregate">The aggregate folder name.</param>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The record.</returns>
    static Record AggregateRec(string id, string module, string aggregate, string root) => Placed(RecordTypes.Aggregate, id, module, $".litespec/behavior/modules/{module}/aggregates/{aggregate}", root);

    /// <summary>One use-case record under its aggregate folder.</summary>
    /// <param name="id">The use-case identifier.</param>
    /// <param name="module">The module name.</param>
    /// <param name="aggregate">The aggregate folder name.</param>
    /// <param name="name">The use-case folder name.</param>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The record.</returns>
    static Record UseCaseRec(string id, string module, string aggregate, string name, string root) => Placed(RecordTypes.UseCase, id, module, $".litespec/behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}", root);

    /// <summary>One record with its folder and module.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="module">The module name.</param>
    /// <param name="folder">The folder the record file sits in, relative to the consumer root.</param>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The record.</returns>
    static Record Placed(string type, string id, string module, string folder, string root)
    {
        using var document = JsonDocument.Parse("""{"status": "reviewed"}""");

        return new Record
        {
            Type = type,
            Id = id,
            Folder = folder,
            Module = module,
            Title = id,
            File = Path.Combine(root, ".litespec", "record.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
