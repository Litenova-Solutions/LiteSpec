using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Audit;

/// <summary>Tests for <c>litespec ui audit</c> option parsing: usage, option errors, and the no-route run.</summary>
public sealed class UiAuditCommandTests
{
    /// <summary>Ui --help prints the usage text and exits zero.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "ui --help prints the usage text and exits zero")]
    public void UiHelpPrintsUsage()
    {
        var result = TestCli.Run("ui", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec ui audit", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Ui audit --help prints the usage text and exits zero without a consumer.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "ui audit --help prints the usage text and exits zero without a consumer")]
    public void AuditHelpPrintsUsage()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--help", "--root", bare.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec ui audit", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Ui with no subcommand prints the usage text and exits 2.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "ui with no subcommand prints the usage text and exits 2")]
    public void UiWithNoSubcommandPrintsUsage()
    {
        var result = TestCli.Run("ui");

        Assert.Equal(2, result.Status);
        Assert.Contains("Usage: litespec ui audit", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An unknown subcommand is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an unknown subcommand is a usage error")]
    public void UnknownSubcommandIsAUsageError()
    {
        var result = TestCli.Run("ui", "update");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown command 'update'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing --base-url is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing --base-url is a usage error")]
    public void MissingBaseUrlIsAUsageError()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --base-url needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A base URL that is not absolute http or https is a usage error.</summary>
    /// <param name="baseUrl">The invalid base URL.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a base URL that is not absolute http or https is a usage error")]
    [InlineData("expenses")]
    [InlineData("ftp://files/expenses")]
    public void InvalidBaseUrlIsAUsageError(string baseUrl)
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", baseUrl, "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Invalid base URL '{baseUrl}': use an absolute http or https URL.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A width that is not a positive number is a usage error.</summary>
    /// <param name="widths">The invalid widths value.</param>
    /// <param name="width">The width the error names.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a width that is not a positive number is a usage error")]
    [InlineData("390,wide", "wide")]
    [InlineData("0", "0")]
    [InlineData("-390", "-390")]
    public void InvalidWidthIsAUsageError(string widths, string width)
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--widths", widths, "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Invalid width '{width}': use a positive number.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A scheme outside light and dark is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a scheme outside light and dark is a usage error")]
    public void UnknownSchemeIsAUsageError()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--schemes", "light,sepia", "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown scheme 'sepia'. Use light or dark.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A --fail-on outside blocking and none is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a --fail-on outside blocking and none is a usage error")]
    public void UnknownFailOnIsAUsageError()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--fail-on", "sometimes", "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown fail-on 'sometimes'. Use blocking or none.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an unknown option is a usage error")]
    public void UnknownOptionIsAUsageError()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--headed", "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--headed'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing project file is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing project file is a usage error")]
    public void MissingProjectFileIsAUsageError()
    {
        using var bare = TestCli.Temp("audit-bare");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--root", bare.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.yaml", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing routes file is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing routes file is a usage error")]
    public void MissingRoutesFileIsAUsageError()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var missing = Path.Combine(consumer.Path, "routes.yaml");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--routes", missing, "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Routes file '{missing}' does not exist.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A routes file that is not valid JSON is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a routes file that is not valid JSON is a usage error")]
    public void InvalidRoutesFileIsAUsageError()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = Path.Combine(consumer.Path, "routes.json");
        File.WriteAllText(routes, "{oops");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes, "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Routes file '{routes}' is not valid YAML or JSON", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A routes file holding no list of routes is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a routes file holding no list of routes is a usage error")]
    public void RoutesFileWithoutListIsAUsageError()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = Path.Combine(consumer.Path, "routes.yaml");
        File.WriteAllText(routes, "path: /expenses\n");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes, "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Routes file '{routes}' holds no list of routes", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A routes entry without a path is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a routes entry without a path is a usage error")]
    public void RoutesEntryWithoutPathIsAUsageError()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = Path.Combine(consumer.Path, "routes.yaml");
        File.WriteAllText(routes, "- name: Expenses\n");

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes, "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Routes file '{routes}' entry 1 needs a path.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing storage state file is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing storage state file is a usage error")]
    public void MissingStorageStateIsAUsageError()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = Path.Combine(consumer.Path, "routes.yaml");
        File.WriteAllText(routes, "- path: /expenses\n");
        var missing = Path.Combine(consumer.Path, "storage.json");

        var result = TestCli.Run(
            "ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes,
            "--storage-state", missing, "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Storage state file '{missing}' does not exist.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A consumer with no screen routes audits nothing and exits zero.</summary>
    /// <proves>litespec/frontend/ui#pass-every-route-suite-check</proves>
    [Fact(DisplayName = "a consumer with no screen routes audits nothing and exits zero")]
    public void NoRoutesExitsZero()
    {
        using var consumer = TestCli.Temp("audit-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run("ui", "audit", "--base-url", "http://localhost:4173", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("No routes to audit.", result.Stdout, StringComparison.Ordinal);
    }
}
