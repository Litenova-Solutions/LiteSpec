using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the project convention checks: placement where rules reads and an Applies to rules can match.</summary>
public sealed class ConventionChecksTests
{
    /// <summary>A convention outside the conventions folder reports convention-folder-invalid.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void ConventionOutsideConventionsFolderReportsFolderInvalid()
    {
        using var temp = WriteMisplacedConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "convention-folder-invalid"));

        Assert.Equal("Convention 'demo/conventions#confirm-large-payouts' sits in '.litespec/implementation/confirm-large-payouts.md', but rules reads conventions from '.litespec/governance/conventions'.", finding.Message);
        Assert.Equal("confirm-large-payouts.md", Path.GetFileName(finding.Path));
        Assert.Equal(3, finding.Line);
        Assert.Equal("demo/conventions#confirm-large-payouts", finding.Subject);
    }

    /// <summary>A convention inside the conventions folder reports nothing.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void ConventionInsideConventionsFolderReportsNothing()
    {
        using var temp = WriteMisplacedConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var misplaced = WithCode(Checker.Check(model).Findings, "convention-folder-invalid");

        Assert.DoesNotContain(misplaced, finding => finding.Subject == "demo/conventions#page-every-collection-read");
    }

    /// <summary>Markdown without an identifier line reports nothing.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void MarkdownWithoutConventionIdReportsNothing()
    {
        using var temp = WriteMisplacedConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var misplaced = WithCode(Checker.Check(model).Findings, "convention-folder-invalid");

        Assert.DoesNotContain(misplaced, finding => Path.GetFileName(finding.Path) == "notes.md");
    }

    /// <summary>An identifier line of another form reports nothing.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void NonConventionIdReportsNothing()
    {
        using var temp = WriteMisplacedConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var misplaced = WithCode(Checker.Check(model).Findings, "convention-folder-invalid");

        Assert.DoesNotContain(misplaced, finding => Path.GetFileName(finding.Path) == "decision-note.md");
    }

    /// <summary>A quoted convention identifier reports nothing.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void QuotedConventionIdReportsNothing()
    {
        using var temp = WriteMisplacedConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var misplaced = WithCode(Checker.Check(model).Findings, "convention-folder-invalid");

        Assert.DoesNotContain(misplaced, finding => Path.GetFileName(finding.Path) == "README.md");
    }

    /// <summary>A glob and a scope Applies to report nothing.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void ValidAppliesToReportsNothing()
    {
        using var temp = WriteScopeConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var invalid = WithCode(Checker.Check(model).Findings, "convention-scope-invalid");

        Assert.DoesNotContain(invalid, finding => finding.Subject == "demo/conventions#glob-rule");
        Assert.DoesNotContain(invalid, finding => finding.Subject == "demo/conventions#scope-rule");
    }

    /// <summary>Free text Applies to reports convention-scope-invalid.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void FreeTextAppliesToReportsScopeInvalid()
    {
        using var temp = WriteScopeConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(
            WithCode(Checker.Check(model).Findings, "convention-scope-invalid"),
            finding => finding.Subject == "demo/conventions#free-text");

        Assert.Equal("Convention 'demo/conventions#free-text' states Applies to 'Confirm payouts', which is neither a path glob nor the scope vocabulary, so rules never returns it.", finding.Message);
        Assert.Equal(7, finding.Line);
    }

    /// <summary>An unknown scope key reports convention-scope-invalid.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void UnknownScopeKeyReportsScopeInvalid()
    {
        using var temp = WriteScopeConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(
            WithCode(Checker.Check(model).Findings, "convention-scope-invalid"),
            finding => finding.Subject == "demo/conventions#unknown-key");

        Assert.Equal("Convention 'demo/conventions#unknown-key' states Applies to 'scope: everywhere', which is neither a path glob nor the scope vocabulary, so rules never returns it.", finding.Message);
        Assert.Equal(7, finding.Line);
    }

    /// <summary>A missing Applies to reports convention-scope-invalid.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void MissingAppliesToReportsScopeInvalid()
    {
        using var temp = WriteScopeConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(
            WithCode(Checker.Check(model).Findings, "convention-scope-invalid"),
            finding => finding.Subject == "demo/conventions#missing-scope");

        Assert.Equal("Convention 'demo/conventions#missing-scope' states no Applies to, so rules never returns it.", finding.Message);
        Assert.Equal(1, finding.Line);
    }

    /// <summary>No other convention-scope-invalid fires in the scope consumer.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void NoOtherScopeInvalidFires()
    {
        using var temp = WriteScopeConsumer();
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var invalid = WithCode(Checker.Check(model).Findings, "convention-scope-invalid");

        Assert.Equal(3, invalid.Count);
    }

    static TempFolder WriteMisplacedConsumer()
    {
        var temp = TestCli.Temp("checks");

        try
        {
            WriteProject(temp.Path);
            WriteConvention(temp.Path, ".litespec/implementation/confirm-large-payouts.md", "demo/conventions#confirm-large-payouts", "Confirm Large Payouts", "apps/api/src/**");
            WriteConvention(temp.Path, ".litespec/governance/conventions/page-every-collection-read.md", "demo/conventions#page-every-collection-read", "Page Every Collection Read", "apps/api/src/**");
            Write(temp.Path, ".litespec/implementation/notes.md", """
                # Lending Notes

                The loan cache region follows the infrastructure default.
                """);
            Write(temp.Path, ".litespec/implementation/decision-note.md", """
                # Decision Note

                **ID:** `decision/0001-loan-threshold`

                The loan threshold stays where the product decision holds it.
                """);
            Write(temp.Path, ".litespec/governance/README.md", """
                # Governance Notes

                A convention looks like this:

                ```markdown
                **ID:** `demo/conventions#quoted-example`
                ```
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static TempFolder WriteScopeConsumer()
    {
        var temp = TestCli.Temp("checks");

        try
        {
            WriteProject(temp.Path);
            WriteConvention(temp.Path, ".litespec/governance/conventions/glob-rule.md", "demo/conventions#glob-rule", "Glob Rule", "apps/api/src/**");
            WriteConvention(temp.Path, ".litespec/governance/conventions/scope-rule.md", "demo/conventions#scope-rule", "Scope Rule", "stack: dotnet, concern: application, artifact: command-handler");
            WriteConvention(temp.Path, ".litespec/governance/conventions/free-text.md", "demo/conventions#free-text", "Free Text", "Confirm payouts");
            WriteConvention(temp.Path, ".litespec/governance/conventions/unknown-key.md", "demo/conventions#unknown-key", "Unknown Key", "scope: everywhere");
            WriteConvention(temp.Path, ".litespec/governance/conventions/missing-scope.md", "demo/conventions#missing-scope", "Missing Scope", null);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void WriteProject(string root) =>
        Write(root, ".litespec/project.yaml", """
            {"project": {"name": "Demo"}, "owners": ["lending-team"],
             "paths": {"docs": ".litespec", "glossary": ".litespec/glossary.yaml"}}
            """);

    static void WriteConvention(string root, string relative, string id, string title, string? appliesTo)
    {
        var scope = appliesTo is null ? "" : $"**Applies to:** `{appliesTo}`\n\n";

        Write(root, relative, $"""
            # {title}

            **ID:** `{id}`

            **Strength:** default

            {scope}**Default:** Every change follows this rule.

            **Rationale:** One rule keeps every change readable.

            **Reviewed for:** Which changes break this rule? Look at the changed files.
            """);
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }

    static List<Finding> WithCode(IReadOnlyList<Finding> findings, string code) =>
        findings.Where(finding => finding.Code == code).ToList();
}
