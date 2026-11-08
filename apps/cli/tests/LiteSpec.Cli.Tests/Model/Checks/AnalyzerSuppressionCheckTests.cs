using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the analyzer suppression check against the lowering and suppression places and the exceptions that cover them.</summary>
public sealed class AnalyzerSuppressionCheckTests
{
    /// <summary>An analyzer rule lowered in .editorconfig with no covering exception reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void EditorConfig_Lowering_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Equal("analyzer-lowering-unrecorded", finding.Code);
        Assert.Equal("S", finding.Tier);
        Assert.Contains("LS1001", finding.Message);
        Assert.Contains("litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns", finding.Message);
        Assert.Contains("no live exception covering", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, ".editorconfig"), finding.Path);
        Assert.Equal(2, finding.Line);
    }

    /// <summary>An analyzer rule kept at error in .editorconfig reports no analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void EditorConfig_Error_Severity_Passes()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = error\n");

        Assert.Empty(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));
    }

    /// <summary>An analyzer rule lowered in a nested .editorconfig reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Nested_EditorConfig_Lowering_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "src/.editorconfig", "[*.cs]\ndotnet_diagnostic.LS1002.severity = warning\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS1002", finding.Message);
        Assert.Contains("litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums", finding.Message);
    }

    /// <summary>An analyzer rule lowered in .globalconfig reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void GlobalConfig_Lowering_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".globalconfig", "is_global = true\ndotnet_diagnostic.LS2004.severity = silent\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS2004", finding.Message);
        Assert.Contains("litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories", finding.Message);
        Assert.Equal(2, finding.Line);
    }

    /// <summary>An analyzer rule named in a Directory.Build.props NoWarn reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Directory_Build_Props_NoWarn_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "Directory.Build.props", "<Project>\n    <PropertyGroup>\n        <NoWarn>$(NoWarn);LS5001</NoWarn>\n    </PropertyGroup>\n</Project>\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS5001", finding.Message);
        Assert.Contains("NoWarn", finding.Message);
        Assert.Contains("litespec/core/system#point-code-and-tests-up-to-records", finding.Message);
        Assert.Equal(3, finding.Line);
    }

    /// <summary>An analyzer rule named in a project file NoWarn reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Project_File_NoWarn_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "src/Library.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n    <PropertyGroup>\n        <NoWarn>LS3001</NoWarn>\n    </PropertyGroup>\n</Project>\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS3001", finding.Message);
        Assert.Contains("Library.csproj", finding.Message);
    }

    /// <summary>An analyzer rule named by a pragma warning disable reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Pragma_Warning_Disable_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "src/Widget.cs", "#pragma warning disable LS1002\n\nnamespace Fixture;\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS1002", finding.Message);
        Assert.Contains("#pragma warning disable", finding.Message);
        Assert.Equal(1, finding.Line);
    }

    /// <summary>Two analyzer rules named by one pragma warning disable report one finding each.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Pragma_Warning_Disable_With_Two_Rules_Reports_Both()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "src/Widget.cs", "#pragma warning disable LS1001, LS1002\n");

        var findings = Lowerings(TestModel.Model(TestModel.Config(temp.Path)));

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, finding => finding.Message.Contains("LS1001", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Message.Contains("LS1002", StringComparison.Ordinal));
    }

    /// <summary>An analyzer rule named by a SuppressMessage reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void SuppressMessage_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "src/Widget.cs", "using System.Diagnostics.CodeAnalysis;\n\nnamespace Fixture;\n\ninternal sealed class Widget\n{\n    [SuppressMessage(\"LiteSpec\", \"LS2012\")]\n    public string Describe() => \"Widget\";\n}\n");

        var finding = Assert.Single(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));

        Assert.Contains("LS2012", finding.Message);
        Assert.Contains("SuppressMessage", finding.Message);
        Assert.Contains("litespec/backend/application#enforce-target-authorization-in-the-pipeline", finding.Message);
        Assert.Equal(7, finding.Line);
    }

    /// <summary>Suppressions naming no LiteSpec analyzer rule report no analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Non_LiteSpec_Suppressions_Pass()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.CS1591.severity = none\n");
        Write(temp.Path, "Directory.Build.props", "<Project>\n    <PropertyGroup>\n        <NoWarn>CS1591</NoWarn>\n    </PropertyGroup>\n</Project>\n");
        Write(temp.Path, "src/Widget.cs", "#pragma warning disable CS1591\n\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace Fixture;\n\ninternal sealed class Widget\n{\n    [SuppressMessage(\"Design\", \"CA1000\")]\n    public string Describe() => \"Widget\";\n}\n");

        Assert.Empty(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));
    }

    /// <summary>An analyzer rule lowered under a skipped build output folder reports no analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Suppression_Under_Bin_Passes()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, "bin/.editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");
        Write(temp.Path, "obj/Widget.cs", "#pragma warning disable LS1001\n");

        Assert.Empty(Lowerings(TestModel.Model(TestModel.Config(temp.Path))));
    }

    /// <summary>A live exception covering the provision the lowered rule enforces reports no analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Live_Exception_Covering_Provision_Passes()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            LoweringException(temp.Path, "\"litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns\"", "2999-01-01"));

        Assert.Empty(Lowerings(model));
    }

    /// <summary>A live exception covering this finding code reports no analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Live_Exception_Covering_Finding_Code_Passes()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/lower-analyzer-rule", Path.Combine(temp.Path, "lower-analyzer-rule.yaml"), """{"kind": "gap", "covers": ["analyzer-lowering-unrecorded"], "subject": ["use-case/lending.list-loans"], "reason": "The lowering waits for its departure record.", "expiresOn": "2999-01-01"}"""));

        Assert.Empty(Lowerings(model));
    }

    /// <summary>An expired exception covering the provision does not cover the lowering, which reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Expired_Exception_Covering_Provision_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            LoweringException(temp.Path, "\"litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns\"", "2000-01-01"));

        var finding = Assert.Single(Lowerings(model));

        Assert.Contains("LS1001", finding.Message);
    }

    /// <summary>A live exception covering another provision does not cover the lowering, which reports analyzer-lowering-unrecorded.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Exception_Covering_Another_Provision_Reports()
    {
        using var temp = Cli.Temp("checks");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            LoweringException(temp.Path, "\"litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums\"", "2999-01-01"));

        var finding = Assert.Single(Lowerings(model));

        Assert.Contains("LS1001", finding.Message);
        Assert.Contains("keep-domain-free-of-outer-layer-concerns", finding.Message);
    }

    /// <summary>Every suppression in the analyzer-lowering-unrecorded fixture reports, one per rule and place.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Fixture_Suppressions_Report_Each_Rule()
    {
        using var temp = Cli.Temp("checks");
        CopyFixture(temp.Path);

        var findings = Lowerings(TestModel.Model(TestModel.Config(temp.Path)));

        Assert.Equal(6, findings.Count);

        foreach (var rule in new[] { "LS1001", "LS2004", "LS5001", "LS3001", "LS1002", "LS2012" })
        {
            Assert.Contains(findings, finding => finding.Message.Contains(rule, StringComparison.Ordinal));
        }
    }

    /// <summary>A model check over a project lowering LS1001 with no exception reports analyzer-lowering-unrecorded as a blocking S finding.</summary>
    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact]
    public void Model_Check_Reports_Unrecorded_Lowering()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ".editorconfig", "[*.cs]\ndotnet_diagnostic.LS1001.severity = none\n");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').First(candidate => candidate.Contains("[analyzer-lowering-unrecorded]"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", finding);
        Assert.Contains("LS1001", finding);
        Assert.Contains("litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns", finding);
    }

    static List<Finding> Lowerings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == "analyzer-lowering-unrecorded").ToList();

    static Record LoweringException(string root, string coversJson, string expiresOn) =>
        TestModel.Record(
            "exception",
            "exception/lower-analyzer-rule",
            Path.Combine(root, "lower-analyzer-rule.yaml"),
            $$"""{"kind": "departure", "covers": {{coversJson}}, "subject": ["use-case/lending.list-loans"], "reason": "The lowering is recorded as a departure.", "expiresOn": "{{expiresOn}}"}""");

    static void CopyFixture(string root)
    {
        var fixture = Cli.Fixture("analyzer-lowering-unrecorded");

        foreach (var file in Directory.GetFiles(fixture, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(root, Path.GetRelativePath(fixture, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
