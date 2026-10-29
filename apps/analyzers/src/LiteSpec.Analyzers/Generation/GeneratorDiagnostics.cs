using LiteSpec.Analyzers.Rules;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>Every diagnostic the generator reports.</summary>
internal static class GeneratorDiagnostics
{
    private static readonly Provision RuntimeValues =
        Provision.Of("backend/dotnet/stack", "generate-runtime-values-from-the-records");

    private static readonly Provision RegistryPermissionsAndRoles =
        Provision.Of("backend/dotnet/stack", "generate-the-registry-permissions-and-roles-from-the-records");

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

    /// <summary>LS5009: two role records produce one constant name.</summary>
    public static readonly DiagnosticDescriptor RoleCollision = new(
        "LS5009",
        "Role constant collision",
        "Role constant '{0}' is produced by both '{1}' and '{2}'",
        "LiteSpec.Traceability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "One constant per role record names the record by PascalCasing each kebab word of the role name. Provision: " + RuntimeValues.Id,
        helpLinkUri: RuntimeValues.HelpLink,
        customTags: [RuntimeValues.Id]);

    /// <summary>LS5012: a role allows a permission no permission record declares.</summary>
    public static readonly DiagnosticDescriptor RoleAllowsUndeclaredPermission = new(
        "LS5012",
        "Role allows an undeclared permission",
        "Role '{0}' allows '{1}', which no permission record declares",
        "LiteSpec.Traceability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A role allows only permissions the shared permission list declares. Provision: " + RegistryPermissionsAndRoles.Id,
        helpLinkUri: RegistryPermissionsAndRoles.HelpLink,
        customTags: [RegistryPermissionsAndRoles.Id]);
}
