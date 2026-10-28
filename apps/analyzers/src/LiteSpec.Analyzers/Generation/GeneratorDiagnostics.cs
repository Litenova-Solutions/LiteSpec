using LiteSpec.Analyzers.Rules;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>Every diagnostic the generator reports.</summary>
internal static class GeneratorDiagnostics
{
    private static readonly Provision RuntimeValues =
        Provision.Of("backend/dotnet/stack", "generate-runtime-values-from-the-records");

    /// <summary>LS5009: two permission records produce one constant name.</summary>
    public static readonly DiagnosticDescriptor PermissionCollision = new(
        "LS5009",
        "Permission constant collision",
        "Permission constant '{0}' is produced by both '{1}' and '{2}'",
        "LiteSpec.Traceability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "One constant per permission record names the record by PascalCasing each kebab word of the permission name. Provision: " + RuntimeValues.Id,
        helpLinkUri: RuntimeValues.HelpLink,
        customTags: [RuntimeValues.Id]);
}
