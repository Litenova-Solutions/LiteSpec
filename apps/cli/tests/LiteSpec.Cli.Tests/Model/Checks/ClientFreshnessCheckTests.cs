using System.Security.Cryptography;
using System.Text;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a generated API client records the current contract document's normalized SHA-256.</summary>
public sealed class ClientFreshnessCheckTests
{
    /// <summary>A declared generated client with no hash file reports client-drift.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void MissingHashFileReports()
    {
        using var temp = TestCli.Temp("client-freshness");
        Configure(temp);
        WriteContract(temp, Contract);

        var finding = Assert.Single(Check(temp));

        Assert.Equal("client-drift", finding.Code);
        Assert.Equal("S", finding.Tier);
        Assert.Equal(HashFile(temp), finding.Path);
        Assert.Contains("contract.sha256", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A generated client recording the current LF-normalized contract hash reports nothing.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void MatchingHashPasses()
    {
        using var temp = TestCli.Temp("client-freshness");
        Configure(temp);
        WriteContract(temp, Contract);
        WriteHash(temp, Digest(Encoding.UTF8.GetBytes(Contract)));

        Assert.Empty(Check(temp));
    }

    /// <summary>A generated client recording another contract hash reports client-drift.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void DifferentHashReports()
    {
        using var temp = TestCli.Temp("client-freshness");
        Configure(temp);
        WriteContract(temp, Contract);
        WriteHash(temp, new string('0', 64));

        var finding = Assert.Single(Check(temp));

        Assert.Equal("client-drift", finding.Code);
        Assert.Equal("S", finding.Tier);
        Assert.Equal(HashFile(temp), finding.Path);
        Assert.Contains(Digest(Encoding.UTF8.GetBytes(Contract)), finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A CRLF contract matches the hash of the same document with LF line endings.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void CrlfContractMatchesLfHash()
    {
        using var temp = TestCli.Temp("client-freshness");
        Configure(temp);
        WriteContract(temp, Contract.Replace("\n", "\r\n", StringComparison.Ordinal));
        WriteHash(temp, Digest(Encoding.UTF8.GetBytes(Contract)));

        Assert.Empty(Check(temp));
    }

    /// <summary>A frontend without a client declaration is not checked.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void FrontendWithoutClientIsSkipped()
    {
        using var temp = TestCli.Temp("client-freshness");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"frontends":[{"name":"web","path":"apps/web","platform":"blazor"}]}""");

        Assert.Empty(Check(temp));
    }

    /// <summary>A declared client whose contract document is missing reports client-drift.</summary>
    /// <proves>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</proves>
    [Fact]
    public void MissingContractReports()
    {
        using var temp = TestCli.Temp("client-freshness");
        Configure(temp);
        WriteHash(temp, new string('0', 64));

        var finding = Assert.Single(Check(temp));

        Assert.Equal("client-drift", finding.Code);
        Assert.Equal("S", finding.Tier);
        Assert.Equal(ContractFile(temp), finding.Path);
    }

    /// <summary>Runs the client freshness check against one temporary consumer.</summary>
    /// <param name="temp">The temporary consumer holding the project declaration and client files.</param>
    /// <returns>The findings the check raises.</returns>
    static List<Finding> Check(TempFolder temp)
    {
        var findings = new List<Finding>();
        ClientFreshnessChecks.Run(TestModel.Model(TestModel.Config(temp.Path)), findings);
        return findings;
    }

    /// <summary>Declares one Blazor frontend with a generated API client.</summary>
    /// <param name="temp">The temporary consumer to configure.</param>
    static void Configure(TempFolder temp) =>
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"frontends":[{"name":"web","path":"apps/web","platform":"blazor","client":{"contract":"apps/api/openapi.json","generated":"packages/api/src/generated"}}]}""");

    /// <summary>Writes a contract document as UTF-8 bytes.</summary>
    /// <param name="temp">The temporary consumer receiving the document.</param>
    /// <param name="contents">The contract document text.</param>
    static void WriteContract(TempFolder temp, string contents) =>
        Write(ContractFile(temp), Encoding.UTF8.GetBytes(contents));

    /// <summary>Writes the generated client's recorded contract hash.</summary>
    /// <param name="temp">The temporary consumer receiving the hash.</param>
    /// <param name="hash">The lowercase hexadecimal hash to record.</param>
    static void WriteHash(TempFolder temp, string hash) =>
        Write(HashFile(temp), Encoding.UTF8.GetBytes(hash + "\n"));

    /// <summary>Writes one file under a temporary consumer, creating its folder first.</summary>
    /// <param name="file">The absolute file path to write.</param>
    /// <param name="bytes">The file contents.</param>
    static void Write(string file, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, bytes);
    }

    /// <summary>Gets the temporary consumer's contract document path.</summary>
    /// <param name="temp">The temporary consumer holding the document.</param>
    /// <returns>The absolute contract document path.</returns>
    static string ContractFile(TempFolder temp) =>
        Path.Combine(temp.Path, "apps", "api", "openapi.json");

    /// <summary>Gets the temporary consumer's generated-client hash path.</summary>
    /// <param name="temp">The temporary consumer holding the generated client.</param>
    /// <returns>The absolute hash file path.</returns>
    static string HashFile(TempFolder temp) =>
        Path.Combine(temp.Path, "packages", "api", "src", "generated", "contract.sha256");

    /// <summary>Computes a SHA-256 independently of the check's byte normalization.</summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>The lowercase hexadecimal SHA-256.</returns>
    static string Digest(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>A small contract document with LF line endings.</summary>
    private const string Contract = "{\n  \"openapi\": \"3.1.0\"\n}\n";
}
