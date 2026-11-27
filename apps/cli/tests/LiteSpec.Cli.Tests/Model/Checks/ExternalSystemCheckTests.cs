using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the external-system role-name check: a record named for its vendor reports, at tier G.</summary>
public sealed class ExternalSystemCheckTests
{
    /// <summary>External System Named For Its Role Passes.</summary>
    /// <proves>litespec/core/edges#name-each-external-system-for-its-role</proves>
    [Fact]
    public void ExternalSystemNamedForItsRolePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/mail-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/mail-relay", Path.Combine(temp.Path, "mail.yaml"), """{"vendor": "Postlane", "represents": "actor/mail-relay"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "external-system/mail-relay");
    }

    /// <summary>External System Named For Its Vendor Reports.</summary>
    /// <proves>litespec/core/edges#name-each-external-system-for-its-role</proves>
    [Fact]
    public void ExternalSystemNamedForItsVendorReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/identity-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/kinde", Path.Combine(temp.Path, "kinde.yaml"), """{"vendor": "Kinde", "represents": "actor/identity-relay"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "external-system/kinde");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("External system 'external-system/kinde' is named for its vendor 'Kinde'; name it for the role the system plays.", finding.Message);
    }

    /// <summary>Vendor Matching Only In Case And Separators Reports.</summary>
    /// <proves>litespec/core/edges#name-each-external-system-for-its-role</proves>
    [Fact]
    public void VendorMatchingOnlyInCaseAndSeparatorsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/meridian-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/meridian-bank", Path.Combine(temp.Path, "bank.yaml"), """{"vendor": "Meridian Bank", "represents": "actor/meridian-relay"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "external-system/meridian-bank");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("External system 'external-system/meridian-bank' is named for its vendor 'Meridian Bank'; name it for the role the system plays.", finding.Message);
    }

    /// <summary>External System Without Vendor Stays Silent Here: The Required-Field Check Owns The Missing Vendor.</summary>
    /// <proves>litespec/core/edges#name-each-external-system-for-its-role</proves>
    [Fact]
    public void ExternalSystemWithoutVendorStaysSilentHere()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/identity-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/kinde", Path.Combine(temp.Path, "kinde.yaml"), """{"represents": "actor/identity-relay"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("named for its vendor", StringComparison.Ordinal));
    }
}
