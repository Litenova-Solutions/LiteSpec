using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves the artifact a rules target path derives for each settled code kind.</summary>
public sealed class PathContextTests
{
    /// <summary>A command validator file derives the validator artifact.</summary>
    [Fact]
    public void RulesValidatorFileDerivesValidatorArtifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandValidator.cs", "sealed class SendInvoiceCommandValidator;\n");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandValidator.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: application, artifact: validator", result.Output);
    }

    /// <summary>A repository file derives the repository artifact.</summary>
    [Fact]
    public void RulesRepositoryFileDerivesRepositoryArtifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/Domain/InvoiceRepository.cs", "sealed class InvoiceRepository;\n");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Domain/InvoiceRepository.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: domain, artifact: repository", result.Output);
    }

    /// <summary>A port file under a Ports folder derives the port artifact.</summary>
    [Fact]
    public void RulesPortFileDerivesPortArtifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Contracts/Ports/IPayoutBank.cs", "interface IPayoutBank;\n");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Contracts/Ports/IPayoutBank.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: application, artifact: port", result.Output);
    }

    /// <summary>A command message file keeps the use-case artifact.</summary>
    [Fact]
    public void RulesMessageFileKeepsUseCaseArtifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommand.cs", "sealed class SendInvoiceCommand;\n");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommand.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: application, artifact: use-case", result.Output);
    }

    /// <summary>Copies the rules-basic fixture to a temporary root and adds one file.</summary>
    /// <param name="relative">The added file path, with forward-slash segments.</param>
    /// <param name="contents">The added file contents.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixtureWithFile(string relative, string contents)
    {
        var source = TestCli.Fixture("rules-basic");
        var temp = TestCli.Temp("rules-artifact");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var added = Path.Combine(temp.Path, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(added)!);
        File.WriteAllText(added, contents);
        return temp;
    }
}
