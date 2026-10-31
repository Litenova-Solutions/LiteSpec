using System.Text.Json.Nodes;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a published operation names the use case its endpoint implements.</summary>
public sealed class OperationUseCaseCheckTests
{
    /// <summary>An operation carrying its endpoint's use case reports nothing.</summary>
    /// <proves>litespec/backend/api#name-the-use-case-on-every-published-operation</proves>
    [Fact]
    public void Operation_With_The_Right_Extension_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ContractFile, RightContract);
        SetContract(temp.Path, ContractFile);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("operation-use-case-drift", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An operation without the extension reports its endpoint's use case.</summary>
    /// <proves>litespec/backend/api#name-the-use-case-on-every-published-operation</proves>
    [Fact]
    public void Operation_Without_An_Extension_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ContractFile, MissingContract);
        SetContract(temp.Path, ContractFile);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var mismatch = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[operation-use-case-drift]", StringComparison.Ordinal)
            && candidate.Contains("BorrowCopy", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", mismatch, StringComparison.Ordinal);
        Assert.Contains("carries no x-litespec-use-case", mismatch, StringComparison.Ordinal);
        Assert.Contains("use-case/lending.borrow-copy", mismatch, StringComparison.Ordinal);
    }

    /// <summary>An operation naming another use case reports the mismatch.</summary>
    /// <proves>litespec/backend/api#name-the-use-case-on-every-published-operation</proves>
    [Fact]
    public void Operation_Naming_Another_Use_Case_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ContractFile, WrongContract);
        SetContract(temp.Path, ContractFile);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var mismatch = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[operation-use-case-drift]", StringComparison.Ordinal)
            && candidate.Contains("BorrowCopy", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", mismatch, StringComparison.Ordinal);
        Assert.Contains("names 'use-case/lending.list-loans'", mismatch, StringComparison.Ordinal);
        Assert.Contains("use-case/lending.borrow-copy", mismatch, StringComparison.Ordinal);
    }

    /// <summary>Without a contract path the check reports nothing.</summary>
    /// <proves>litespec/backend/api#name-the-use-case-on-every-published-operation</proves>
    [Fact]
    public void Without_A_Contract_Path_Reports_Nothing()
    {
        using var temp = ModelFixture.Prepare("small");

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("operation-use-case-drift", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A contract path pointing at a missing file reports the path.</summary>
    /// <proves>litespec/backend/api#name-the-use-case-on-every-published-operation</proves>
    [Fact]
    public void Missing_Contract_File_Reports_One_Finding()
    {
        using var temp = ModelFixture.Prepare("small");
        SetContract(temp.Path, "apps/api/openapi/missing.json");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var mismatches = check.Output.Split('\n')
            .Where(candidate => candidate.Contains("[operation-use-case-drift]", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(1, check.Status);
        var mismatch = Assert.Single(mismatches);
        Assert.Contains("[S]", mismatch, StringComparison.Ordinal);
        Assert.Contains("missing.json", mismatch, StringComparison.Ordinal);
    }

    static void SetContract(string root, string relative)
    {
        var projectFile = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(projectFile))!.AsObject();
        project["paths"]!["contract"] = relative;
        File.WriteAllText(projectFile, project.ToJsonString());
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string ContractFile = "apps/api/openapi/library.json";

    const string RightContract = """
        {
          "openapi": "3.1.0",
          "paths": {
            "/api/lending/borrow-copy": {
              "post": {
                "operationId": "BorrowCopy",
                "x-litespec-use-case": "use-case/lending.borrow-copy"
              }
            }
          }
        }
        """;

    const string MissingContract = """
        {
          "openapi": "3.1.0",
          "paths": {
            "/api/lending/borrow-copy": {
              "post": {
                "operationId": "BorrowCopy"
              }
            }
          }
        }
        """;

    const string WrongContract = """
        {
          "openapi": "3.1.0",
          "paths": {
            "/api/lending/borrow-copy": {
              "post": {
                "operationId": "BorrowCopy",
                "x-litespec-use-case": "use-case/lending.list-loans"
              }
            }
          }
        }
        """;
}
