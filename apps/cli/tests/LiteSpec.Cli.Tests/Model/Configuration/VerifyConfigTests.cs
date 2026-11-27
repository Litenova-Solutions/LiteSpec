using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Configuration;

/// <summary>Proves the reader of the top-level verify block of the consumer declaration, which every verify command reads.</summary>
public sealed class VerifyConfigTests
{
    /// <summary>A full verify block reads its base address, sign-in requests, values, persistence harness and production settings.</summary>
    [Fact]
    public void FullBlockReadsEveryPart()
    {
        var config = Load("""
            verify:
              baseUrl: http://localhost:5000
              signIn:
              - method: GET
                path: /auth/development/{person}
              - method: POST
                path: /api/session/team
                body:
                  team: harbor-and-pine
              values:
                expense: 4f0c
                limit: 250
              persistence:
                command: dotnet run --project apps/api/tools/Snapshots --
              production:
                compose: deploy/compose.yaml
                envFile: .env.example
                baseUrl: http://localhost:8080
                health: /health/ready
                paths:
                - /api
                - /auth
            """);

        var verify = config.Verify;
        Assert.Equal("http://localhost:5000", verify.BaseUrl);
        Assert.Equal(2, verify.SignIn.Count);
        Assert.Equal("/auth/development/{person}", verify.SignIn[0].Path);
        Assert.Null(verify.SignIn[0].Body);
        Assert.Equal("harbor-and-pine", verify.SignIn[1].Body!["team"]!.GetValue<string>());
        Assert.Equal("4f0c", verify.Values["expense"]);
        Assert.Equal("250", verify.Values["limit"]);
        Assert.Equal("dotnet run --project apps/api/tools/Snapshots --", verify.Persistence!.Command);
        Assert.Equal("/health/ready", verify.Production!.Health);
        Assert.Null(verify.Production.Migrate);
        Assert.Equal(["/api", "/auth"], verify.Production.Paths);
    }

    /// <summary>A declaration with no verify block reads as the empty settings.</summary>
    [Fact]
    public void AbsentBlockReadsAsEmpty()
    {
        var config = Load("");

        Assert.Same(VerifyConfig.Empty, config.Verify);
        Assert.Empty(config.Verify.SignIn);
        Assert.Null(config.Verify.Production);
    }

    /// <summary>A production block missing its health path reads as no production settings.</summary>
    [Fact]
    public void ProductionWithoutHealthReadsAsAbsent()
    {
        var config = Load("""
            verify:
              production:
                compose: deploy/compose.yaml
                envFile: .env.example
                baseUrl: http://localhost:8080
            """);

        Assert.Null(config.Verify.Production);
    }

    /// <summary>Loads a consumer declaration holding the given top-level YAML after a minimal project head.</summary>
    /// <param name="tail">The YAML appended after the project head.</param>
    /// <returns>The loaded configuration.</returns>
    static ConsumerConfig Load(string tail)
    {
        using var temp = TestCli.Temp("verify-config");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "schemaVersion: 1\nproject:\n  name: Claimwick\nsize: small\nshape: server\n" + tail + "\n");

        return ConsumerConfig.Load(temp.Path);
    }
}
