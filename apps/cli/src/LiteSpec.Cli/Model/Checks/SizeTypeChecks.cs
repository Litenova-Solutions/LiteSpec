using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The size checks: every record type the project size requires is held, and the product has one record.</summary>
/// <follows>litespec/core/model#declare-size-stacks-and-optional-types</follows>
/// <follows>litespec/core/intent#state-one-product</follows>
internal static class SizeTypeChecks
{
    /// <summary>Reports size-type-missing and product-count-invalid.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var declaration = Path.Combine(model.Config.Root, "litespec.project.json");
        var required = model.Config.Size == "small"
            ? RecordTypes.SmallRequired
            : RecordTypes.SmallRequired.Concat(RecordTypes.LargeAdded).ToList();

        foreach (var type in required.Concat(model.Config.OptionalTypes).Distinct(StringComparer.Ordinal))
        {
            if (type is "runbook" && !model.Config.OptionalTypes.Contains(type, StringComparer.Ordinal))
            {
                continue;
            }

            if (RecordTypes.All.Contains(type) && !model.Records.Any(record => record.Type == type))
            {
                findings.Add(new Finding("size-type-missing", $"Record type '{type}' is required by the {model.Config.Size} size and is held by no record.", declaration, 1, null));
            }
        }

        if (model.Records.Count(record => record.Type == RecordTypes.Product) != 1)
        {
            findings.Add(new Finding("product-count-invalid", "Product 'product' has no single record.", declaration, 1, null));
        }
    }
}
