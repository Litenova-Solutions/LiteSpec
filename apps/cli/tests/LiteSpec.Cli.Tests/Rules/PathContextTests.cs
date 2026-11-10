using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Rules;

/// <summary>Proves the artifact a rules target path derives for each settled code kind.</summary>
public sealed class PathContextTests
{
    /// <summary>A command validator file derives the validator artifact.</summary>
    [Fact]
    public void Rules_Validator_File_Derives_Validator_Artifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandValidator.cs", "sealed class SendInvoiceCommandValidator;\n");
        var result = Cli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandValidator.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: application, artifact: validator", result.Output);
    }

    /// <summary>A repository file derives the repository artifact.</summary>
    [Fact]
    public void Rules_Repository_File_Derives_Repository_Artifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/Domain/InvoiceRepository.cs", "sealed class InvoiceRepository;\n");
        var result = Cli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Domain/InvoiceRepository.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: domain, artifact: repository", result.Output);
    }

    /// <summary>A port file under a Ports folder derives the port artifact.</summary>
    [Fact]
    public void Rules_Port_File_Derives_Port_Artifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Contracts/Ports/IPayoutBank.cs", "interface IPayoutBank;\n");
        var result = Cli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Contracts/Ports/IPayoutBank.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("concern: application, artifact: port", result.Output);
    }

    /// <summary>A command message file keeps the use-case artifact.</summary>
    [Fact]
    public void Rules_Message_File_Keeps_Use_Case_Artifact()
    {
        using var root = CopyFixtureWithFile(
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommand.cs", "sealed class SendInvoiceCommand;\n");
        var result = Cli.Run(
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
        var source = Cli.Fixture("rules-basic");
        var temp = Cli.Temp("rules-artifact");

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
