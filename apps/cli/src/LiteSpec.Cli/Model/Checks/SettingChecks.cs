using System.Text;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The setting-key and template-store checks: every operator setting names its key, every keyed setting is bound by a marked options class spelling it, and the notification templates and the template store stay one set with every parameter used.</summary>
/// <follows>litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting</follows>
/// <follows>litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class</follows>
/// <follows>litespec/core/edges#keep-notification-templates-and-the-template-store-one-set</follows>
/// <follows>litespec/core/edges#use-every-declared-template-parameter</follows>
internal static class SettingChecks
{
    /// <summary>Reports setting-key-missing, setting-unimplemented, setting-key-drift, template-unimplemented, template-unrecorded and template-param-drift.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckSettingKeyMissing(model, findings);
        CheckSettingBound(model, findings);
        CheckTemplates(model, findings);
    }

    /// <summary>Reports an operator setting naming no configuration key.</summary>
    private static void CheckSettingKeyMissing(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            if (setting.Field(RecordFields.SetBy) == "operator" && setting.Field(RecordFields.Key) is not { Length: > 0 })
            {
                findings.Add(new Finding(
                    "setting-key-missing",
                    $"Setting '{setting.Id}' is set by the operator and names no key.",
                    setting.File,
                    setting.Line,
                    SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports a keyed setting no options class marks, and a marked options class not spelling its setting's key.</summary>
    private static void CheckSettingBound(ProjectModel model, List<Finding> findings)
    {
        var options = Readers.Settings.OptionsClassReader.Read(model.Config);
        var marked = new HashSet<string>(StringComparer.Ordinal);

        foreach (var bound in options)
        {
            foreach (var id in bound.Settings)
            {
                marked.Add(id);
            }
        }

        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            if (setting.Field(RecordFields.Key) is not { Length: > 0 } key)
            {
                continue;
            }

            if (!marked.Contains(setting.Id))
            {
                findings.Add(new Finding(
                    "setting-unimplemented",
                    $"Setting '{setting.Id}' names key '{key}', which no options class marks.",
                    setting.File,
                    setting.Line,
                    SubjectOf(setting)));
            }
        }

        foreach (var bound in options)
        {
            foreach (var id in bound.Settings)
            {
                if (!model.ById.TryGetValue(id, out var setting) || setting.Type != RecordTypes.Setting)
                {
                    continue;
                }

                if (setting.Field(RecordFields.Key) is not { Length: > 0 } key)
                {
                    continue;
                }

                if (!SpellsKey(bound, key))
                {
                    findings.Add(new Finding(
                        "setting-key-drift",
                        $"Setting '{id}' names key '{key}', which options class '{bound.Class}' does not spell with its SectionName and properties.",
                        bound.File,
                        bound.Line,
                        id));
                }
            }
        }
    }

    /// <summary>Whether the section value joined by <c>:</c> with one of the properties spells the key.</summary>
    private static bool SpellsKey(Readers.Settings.OptionsClass bound, string key)
    {
        if (bound.SectionName is not { Length: > 0 })
        {
            return false;
        }

        foreach (var property in bound.Properties)
        {
            if (string.Concat(bound.SectionName, ":", property).Equals(key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports a declared template the store lacks, a stored key no notification declares, and a declared parameter no text uses.</summary>
    private static void CheckTemplates(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.TemplateStore.Length == 0)
        {
            return;
        }

        var store = Readers.Settings.TemplateStoreReader.Read(model.Config.TemplateStore);

        if (store is null)
        {
            findings.Add(new Finding(
                "template-unimplemented",
                $"Template store '{model.Config.Relative(model.Config.TemplateStore)}' names a file that does not exist.",
                model.Config.TemplateStore,
                1,
                null));
            return;
        }

        var held = new Dictionary<string, Readers.Settings.StoredTemplate>(StringComparer.Ordinal);

        foreach (var entry in store)
        {
            held.TryAdd(entry.Key, entry);
        }

        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            foreach (var key in TemplateKeys(notification))
            {
                declared.Add(key);

                if (!held.ContainsKey(key))
                {
                    findings.Add(new Finding(
                        "template-unimplemented",
                        $"Notification '{notification.Id}' declares template '{key}', which the template store lacks.",
                        notification.File,
                        notification.Line,
                        SubjectOf(notification)));
                }
            }
        }

        foreach (var entry in store)
        {
            if (!declared.Contains(entry.Key))
            {
                findings.Add(new Finding(
                    "template-unrecorded",
                    $"Template store '{model.Config.Relative(model.Config.TemplateStore)}' holds '{entry.Key}', which no notification declares.",
                    model.Config.TemplateStore,
                    entry.Line,
                    null));
            }
        }

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            CheckTemplateParams(notification, held, findings);
        }
    }

    /// <summary>The template keys a notification declares, without <c>none</c>; a <c>template: none</c> notification declares none.</summary>
    private static IReadOnlyList<string> TemplateKeys(Record notification)
    {
        if (notification.Field(RecordFields.Template) is not { Length: > 0 } template)
        {
            return [];
        }

        return [.. template
            .Split(',')
            .Select(key => key.Trim())
            .Where(key => key.Length > 0 && !key.StartsWith("none", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Reports a declared parameter whose placeholder none of the notification's stored texts uses.</summary>
    private static void CheckTemplateParams(
        Record notification, Dictionary<string, Readers.Settings.StoredTemplate> held, List<Finding> findings)
    {
        var keys = TemplateKeys(notification);
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var present = false;

        foreach (var key in keys)
        {
            if (held.TryGetValue(key, out var entry))
            {
                present = true;
                placeholders.UnionWith(entry.Placeholders);
            }
        }

        if (!present)
        {
            return;
        }

        var where = keys.Count == 1 ? $"template '{keys[0]}'" : "its templates";

        foreach (var param in notification.Strings(RecordFields.Params))
        {
            var pascal = ToPascalCase(param);

            if (!placeholders.Contains(pascal))
            {
                findings.Add(new Finding(
                    "template-param-drift",
                    $"Notification '{notification.Id}' declares parameter '{param}', but {where} uses no '{{" + pascal + "}}' placeholder.",
                    notification.File,
                    notification.Line,
                    SubjectOf(notification)));
            }
        }
    }

    /// <summary>The PascalCase form of a declared parameter: each part capitalized and joined.</summary>
    private static string ToPascalCase(string name)
    {
        var built = new StringBuilder();

        foreach (var part in name.Split(['_', '-', ' ', '.'], StringSplitOptions.RemoveEmptyEntries))
        {
            built.Append(char.ToUpperInvariant(part[0]));

            if (part.Length > 1)
            {
                built.Append(part.Substring(1));
            }
        }

        return built.ToString();
    }
}
