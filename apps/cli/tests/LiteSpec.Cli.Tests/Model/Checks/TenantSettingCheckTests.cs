using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that a tenant setting needs a change use case.</summary>
public sealed class TenantSettingCheckTests
{
    /// <summary>A tenant setting with no change use case reports.</summary>
    /// <proves>litespec/core/use-cases#give-every-tenant-setting-a-change-use-case</proves>
    [Fact]
    public void TenantSettingWithoutChangeUseCaseReports()
    {
        var model = ModelWith(TenantSetting(), null);

        var finding = Assert.Single(Findings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("setting/lending.max-renewals", finding.Subject);
        Assert.Contains("set by the tenant and changed by no change use case", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A tenant setting named in reads of a change use case passes.</summary>
    /// <proves>litespec/core/use-cases#give-every-tenant-setting-a-change-use-case</proves>
    [Fact]
    public void TenantSettingWithChangeUseCasePasses()
    {
        var model = ModelWith(TenantSetting(), """{"kind": "command", "reads": ["setting/lending.max-renewals"]}""");

        Assert.Empty(Findings(model));
    }

    /// <summary>A change use case reading a different setting does not cover this one.</summary>
    /// <proves>litespec/core/use-cases#give-every-tenant-setting-a-change-use-case</proves>
    [Fact]
    public void ChangeUseCaseReadingAnotherSettingReports()
    {
        var model = ModelWith(TenantSetting(), """{"kind": "command", "reads": ["setting/lending.other"]}""");

        Assert.Single(Findings(model));
    }

    /// <summary>An operator setting needs no change use case.</summary>
    /// <proves>litespec/core/use-cases#give-every-tenant-setting-a-change-use-case</proves>
    [Fact]
    public void OperatorSettingWithoutChangeUseCasePasses()
    {
        var model = ModelWith(
            """{"valueType": "integer", "default": 5, "setBy": "operator", "configurationPath": "Lending:MaxRenewals", "outOfRange": "refuse"}""",
            null);

        Assert.Empty(Findings(model));
    }

    /// <summary>A model holding one setting and, when given, one use case.</summary>
    /// <param name="setting">The setting record fields as JSON.</param>
    /// <param name="useCase">The use-case record fields as JSON, or null for no use case.</param>
    /// <returns>The hand-built project model.</returns>
    static ProjectModel ModelWith(string setting, string? useCase)
    {
        var temp = TestCli.Temp("checks");
        var settingRecord = TestModel.Record(
            "setting",
            "setting/lending.max-renewals",
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "settings", "max-renewals.yaml"),
            setting);

        if (useCase is null)
        {
            return TestModel.Model(TestModel.Config(temp.Path), settingRecord);
        }

        return TestModel.Model(
            TestModel.Config(temp.Path),
            settingRecord,
            TestModel.Record(
                "use-case",
                "use-case/lending.change-max-renewals",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "change-max-renewals", "change-max-renewals.yaml"),
                useCase));
    }

    /// <summary>The tenant-setting findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is tenant-setting-change-missing.</returns>
    static List<Finding> Findings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "tenant-setting-change-missing").ToList();

    /// <summary>One tenant setting with the fields its type requires.</summary>
    /// <returns>The setting record fields as JSON.</returns>
    static string TenantSetting() =>
        """{"valueType": "integer", "default": 5, "setBy": "tenant", "outOfRange": "refuse"}""";
}
