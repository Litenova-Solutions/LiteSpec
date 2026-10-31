using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the setting-key and template-store checks against fixture projects.</summary>
public sealed class SettingsCheckTests
{
    /// <proves>litespec/core/behavior#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void Operator_Setting_Without_Key_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", null));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-missing", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[G]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void Operator_Setting_With_Key_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-missing]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#name-the-configuration-key-of-every-operator-setting</proves>
    [Fact]
    public void Decider_Setting_Without_Key_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "decider", null));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-missing]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Keyed_Setting_Without_Options_Class_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-unimplemented", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Keyed_Setting_With_Marked_Options_Class_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Options_Class_With_Wrong_Section_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Loans", "MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-drift", "LendingOptions.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Options_Class_With_Missing_Property_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxLoans"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "setting-key-drift", "setting/lending.max-renewals");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Options_Class_Spelling_Key_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class</proves>
    [Fact]
    public void Nested_Section_Spelling_Key_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:Loans:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending:Loans", "MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Methodless_Options_Class_With_Setting_Mark_Is_Accepted()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/settings/max-renewals.md", SettingPage(
            "setting/lending.max-renewals", "Max renewals", "operator", "Lending:MaxRenewals"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/LendingOptions.cs", OptionsClass(
            "setting/lending.max-renewals", "Lending", "MaxRenewals"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[setting-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[setting-key-drift]", check.Output, StringComparison.Ordinal);
        Assert.Empty(check.Output.Split('\n').Where(line =>
            line.Contains("[mark-unknown]", StringComparison.Ordinal)
            && line.Contains("LendingOptions", StringComparison.Ordinal)));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Methodless_Class_With_Use_Case_Mark_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/src/Library.Application/Lending/Loans/BorrowCopyHelper.cs", MethodlessHelper(
            "use-case/lending.borrow-copy", "BorrowCopyHelper"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "mark-unknown", "BorrowCopyHelper");

        Assert.Equal(1, check.Status);
        Assert.Contains("takes no mark", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Declared_Template_Missing_From_Store_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("hold-ready-reader", "A hold is ready.", "Hello, a hold is ready.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unimplemented", "notification/lending.loan-due");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Declared_Template_Held_By_Store_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due.", "Hello, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Missing_Template_Store_File_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unimplemented", "MessageTemplates.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Template_Checks_Skipped_Without_Store()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-param-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Stored_Key_Without_Notification_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due.", "Hello, your loan ends soon."),
            ("hold-ready-reader", "A hold is ready.", "Hello, a hold is ready.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-unrecorded", "MessageTemplates.cs");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set</proves>
    [Fact]
    public void Stored_Keys_Matching_Notifications_Pass()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", PairFormStore(
            ("loan-due-reader", "Hello, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-unrecorded]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[template-unimplemented]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#use-every-declared-template-parameter</proves>
    [Fact]
    public void Unused_Parameter_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader", "readerName", "dueDate"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due, {{ReaderName}}.", "Hello {{ReaderName}}, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var line = FindingLine(check.Output, "template-param-drift", "dueDate");

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", line, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#use-every-declared-template-parameter</proves>
    [Fact]
    public void Used_Parameters_Pass()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "loan-due-reader", "readerName"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore(
            ("loan-due-reader", "Your loan is due, {{ReaderName}}.", "Hello {{ReaderName}}, your loan ends soon.")));
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.DoesNotContain("[template-param-drift]", check.Output, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#use-every-declared-template-parameter</proves>
    [Fact]
    public void Template_None_Skips_Template_Checks()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/notifications/loan-due.md", NotificationPage(
            "notification/lending.loan-due", "Loan due", "none", "readerName"));
        Write(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs", TemplateStore());
        AddTemplateStore(temp.Path, "apps/api/src/Library.Application/Lending/MessageTemplates.cs");

        var check = Cli.Run("model", "check", "--root", temp.Path);

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
        var file = Path.Combine(root, "litespec.project.json");
        var text = File.ReadAllText(file);
        File.WriteAllText(file, text.Replace(
            "\"webapi\": \"apps/api/src/Library.WebApi\"",
            "\"webapi\": \"apps/api/src/Library.WebApi\",\n      \"templateStore\": \"" + store + "\"",
            StringComparison.Ordinal));
    }

    static string SettingPage(string id, string title, string setBy, string? key)
    {
        var keyLine = key is null ? "" : ",\n  \"key\": \"" + key + "\"";

        return "---\n{\n"
            + "  \"type\": \"setting\",\n"
            + "  \"id\": \"" + id + "\",\n"
            + "  \"title\": \"" + title + "\",\n"
            + "  \"owner\": \"lending-team\",\n"
            + "  \"status\": \"reviewed\",\n"
            + "  \"valueType\": \"integer\",\n"
            + "  \"default\": 5,\n"
            + "  \"range\": \"1-10\",\n"
            + "  \"unit\": \"loans\",\n"
            + "  \"setBy\": \"" + setBy + "\"" + keyLine + ",\n"
            + "  \"atBound\": \"refuse\",\n"
            + "  \"fulfills\": [\n"
            + "    \"requirement/borrowing.max-active-loans\"\n"
            + "  ]\n"
            + "}\n---\n\n# " + title + "\n\nLending runs within this setting.\n";
    }

    static string NotificationPage(string id, string title, string template, params string[] parameters)
    {
        var paramsJson = parameters.Length == 0
            ? ""
            : ",\n  \"params\": [\n" + string.Join(",\n", parameters.Select(parameter => "    \"" + parameter + "\"")) + "\n  ]";

        return "---\n{\n"
            + "  \"type\": \"notification\",\n"
            + "  \"id\": \"" + id + "\",\n"
            + "  \"title\": \"" + title + "\",\n"
            + "  \"owner\": \"lending-team\",\n"
            + "  \"status\": \"reviewed\",\n"
            + "  \"template\": \"" + template + "\",\n"
            + "  \"addresses\": [\n    \"actor/reader\"\n  ],\n"
            + "  \"channel\": [\n    \"email\"\n  ],\n"
            + "  \"consent\": \"Readers accept notices at signup.\",\n"
            + "  \"frequency\": \"once\"" + paramsJson + ",\n"
            + "  \"fulfills\": [\n    \"requirement/borrowing.max-active-loans\"\n  ]\n"
            + "}\n---\n\n# " + title + "\n\nThe reader learns when the loan ends.\n";
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
