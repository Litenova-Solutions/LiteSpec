using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the setting-key and template-store checks against fixture projects.</summary>
public sealed class SettingsCheckTests
{
    /// <summary>Operator Setting Without Key Reports.</summary>
    /// <proves>litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void OperatorSettingWithoutKeyReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", null));

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-missing", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[G]", line, StringComparison.Ordinal);
    }

    /// <summary>Operator Setting With Key Passes.</summary>
    /// <proves>litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void OperatorSettingWithKeyPasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-missing]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Product Setting Without Key Passes.</summary>
    /// <proves>litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void ProductSettingWithoutKeyPasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "product", null));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-missing]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Keyed Setting Without Options Class Reports.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void KeyedSettingWithoutOptionsClassReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-unimplemented", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Keyed Setting With Marked Options Class Passes.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void KeyedSettingWithMarkedOptionsClassPasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Options Class With Wrong Section Reports.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void OptionsClassWithWrongSectionReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Loans", "MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-drift", "LendingOptions.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Options Class With Missing Property Reports.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void OptionsClassWithMissingPropertyReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxLoans"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-drift", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Options Class Spelling Key Passes.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void OptionsClassSpellingKeyPasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Nested Section Spelling Key Passes.</summary>
    /// <proves>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void NestedSectionSpellingKeyPasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:Loans:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending:Loans", "MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Methodless Options Class With Setting Mark Is Accepted.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MethodlessOptionsClassWithSettingMarkIsAccepted()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/settings/max-renewals.yaml", SettingJson(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain(check.Output.Split('\n'), line =>
            line.Contains("[mark-unknown]", StringComparison.Ordinal)
            && line.Contains("LendingOptions", StringComparison.Ordinal));
    }

    /// <summary>Methodless Class With Use Case Mark Reports.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MethodlessClassWithUseCaseMarkReports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopyHelper.cs", MethodlessHelper(
            "use-case/lending.borrow-copy", "BorrowCopyHelper"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "mark-unknown", "BorrowCopyHelper");

        Assert.Equal(1, check.Status);
        Assert.Contains("takes no mark", line, StringComparison.Ordinal);
    }

    /// <summary>Declared Template Missing From Store Reports.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void DeclaredTemplateMissingFromStoreReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("hold-ready-reader", "A hold is ready.", "Hello, a hold is ready.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unimplemented", "notification/lending.loan-due");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Declared Template Held By Store Passes.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void DeclaredTemplateHeldByStorePasses()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due.", "Hello, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Missing Template Store File Reports.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void MissingTemplateStoreFileReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unimplemented", "MessageTemplates.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Template Checks Skipped Without Store.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void TemplateChecksSkippedWithoutStore()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-param-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Stored Key Without Notification Reports.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void StoredKeyWithoutNotificationReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due.", "Hello, your loan ends soon."),
            ("hold-ready-reader", "A hold is ready.", "Hello, a hold is ready.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unrecorded", "MessageTemplates.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Stored Keys Matching Notifications Pass.</summary>
    /// <proves>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void StoredKeysMatchingNotificationsPass()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", PairFormStore(
            ("loan-due-reader", "Hello, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Unused Parameter Reports.</summary>
    /// <proves>litespec/core/edges#use-every-declared-template-parameter</proves>
    [Fact]
    public void UnusedParameterReports()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader", "readerName", "dueDate"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due, {{ReaderName}}.", "Hello {{ReaderName}}, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-param-drift", "dueDate");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <summary>Used Parameters Pass.</summary>
    /// <proves>litespec/core/edges#use-every-declared-template-parameter</proves>
    [Fact]
    public void UsedParametersPass()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "loan-due-reader", "readerName"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due, {{ReaderName}}.", "Hello {{ReaderName}}, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-param-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Template None Skips Template Checks.</summary>
    /// <proves>litespec/core/edges#use-every-declared-template-parameter</proves>
    [Fact]
    public void TemplateNoneSkipsTemplateChecks()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/notifications/loan-due.yaml", NotificationJson(
            "notification/lending.loan-due", "Loan due", "none", "readerName"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore());
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-param-drift]", check.Output, StringComparison.Ordinal);
    }

    static string FindingLine(string output, string code, string part) =>
        output.Split('\n').Single(candidate =>
            candidate.Contains($"[{code}]", StringComparison.Ordinal)
            && candidate.Contains(part, StringComparison.Ordinal));

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static void AddTemplateStore(string root, string store)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = RecordText.Read(file)!.AsObject();
        var dotnet = (JsonObject)((JsonObject)project["stackConfig"]!)["dotnet"]!;
        dotnet["templateStore"] = store;
        File.WriteAllText(file, project.ToJsonString());
    }

    static string SettingJson(string id, string title, string setBy, string? key)
    {
        var keyLine = key is null ? "" : ",\n  \"key\": \"" + key + "\"";

        return "{\n"
            + "  \"type\": \"setting\",\n"
            + "  \"id\": \"" + id + "\",\n"
            + "  \"title\": \"" + title + "\",\n"
            + "  \"valueType\": \"integer\",\n"
            + "  \"default\": 5,\n"
            + "  \"range\": \"1-10\",\n"
            + "  \"unit\": \"loans\",\n"
            + "  \"setBy\": \"" + setBy + "\"" + keyLine + ",\n"
            + "  \"atBound\": \"refuse\",\n"
            + "  \"fulfills\": [\n"
            + "    \"requirement/borrowing.max-active-loans\"\n"
            + "  ]\n"
            + "}\n";
    }

    static string NotificationJson(string id, string title, string template, params string[] parameters)
    {
        var paramsJson = parameters.Length == 0
            ? ""
            : ",\n  \"params\": [\n" + string.Join(",\n", parameters.Select(parameter => "    \"" + parameter + "\"")) + "\n  ]";

        return "{\n"
            + "  \"type\": \"notification\",\n"
            + "  \"id\": \"" + id + "\",\n"
            + "  \"title\": \"" + title + "\",\n"
            + "  \"template\": \"" + template + "\",\n"
            + "  \"addresses\": [\n    \"actor/reader\"\n  ],\n"
            + "  \"channel\": [\n    \"email\"\n  ],\n"
            + "  \"consent\": \"Readers accept notices at signup.\",\n"
            + "  \"frequency\": \"once\"" + paramsJson + ",\n"
            + "  \"fulfills\": [\n    \"requirement/borrowing.max-active-loans\"\n  ]\n"
            + "}\n";
    }

    static string OptionsClass(string settingId, string section, params string[] properties)
    {
        var declared = string.Join("\n", properties.Select(property => "    public int " + property + " { get; init; }"));

        return "namespace Library.Application.Lending.Loans;\n\n"
            + "/// <summary>The lending settings bound from configuration.</summary>\n"
            + "/// <implements>" + settingId + "</implements>\n"
            + "public sealed class LendingOptions\n{\n"
            + "    public const string SectionName = \"" + section + "\";\n\n"
            + declared + "\n"
            + "}\n";
    }

    static string MethodlessHelper(string recordId, string className)
    {
        return "namespace Library.Application.Lending.Loans;\n\n"
            + "/// <summary>Lending reading state held for one use case.</summary>\n"
            + "/// <implements>" + recordId + "</implements>\n"
            + "public sealed class " + className + "\n{\n"
            + "    public string Name { get; init; } = \"\";\n"
            + "}\n";
    }

    static string TemplateStore(params (string Key, string Subject, string Body)[] entries)
    {
        var declared = string.Join(
            "\n",
            entries.Select(entry =>
                "        [\"" + entry.Key + "\"] = new MessageTemplate(\n"
                + "            \"" + entry.Subject + "\",\n"
                + "            \"" + entry.Body + "\"),"));

        return "using System.Collections.Generic;\n\n"
            + "namespace Library.Application.Lending;\n\n"
            + "/// <summary>One notification template: its subject and its body.</summary>\n"
            + "public sealed record MessageTemplate(string Subject, string Body);\n\n"
            + "/// <summary>The notification templates, keyed by template key.</summary>\n"
            + "public static class MessageTemplates\n{\n"
            + "    /// <summary>The templates, keyed by template key.</summary>\n"
            + "    public static readonly Dictionary<string, MessageTemplate> Templates = new()\n"
            + "    {\n"
            + declared + "\n"
            + "    };\n"
            + "}\n";
    }

    static string PairFormStore(params (string Key, string Text)[] entries)
    {
        var declared = string.Join(
            "\n",
            entries.Select(entry => "        { \"" + entry.Key + "\", \"" + entry.Text + "\" },"));

        return "using System.Collections.Generic;\n\n"
            + "namespace Library.Application.Lending;\n\n"
            + "/// <summary>The notification templates, keyed by template key.</summary>\n"
            + "public static class MessageTemplates\n{\n"
            + "    /// <summary>The templates, keyed by template key.</summary>\n"
            + "    public static readonly Dictionary<string, string> Templates = new()\n"
            + "    {\n"
            + declared + "\n"
            + "    };\n"
            + "}\n";
    }
}
