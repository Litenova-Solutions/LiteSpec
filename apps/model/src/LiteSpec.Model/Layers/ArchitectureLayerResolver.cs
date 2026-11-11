using System;

namespace LiteSpec.Model;

/// <summary>
///     Finds the layer of a file: the <c>LiteSpecLayer</c> MSBuild property when the
///     project sets it, otherwise the assembly name suffix under the layer-projects
///     layout, otherwise the nearest layer folder or the file's role suffix.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class ArchitectureLayerResolver
{
    /// <summary>The analyzer config key the package's build props make visible.</summary>
    public const string PropertyKey = "build_property.LiteSpecLayer";

    /// <summary>The analyzer config key carrying the backend layout the props read from the project file.</summary>
    public const string LayoutPropertyKey = "build_property.LiteSpecLayout";

    /// <summary>Returns the layer.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="assemblyName">The assembly name that names the application project.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="declared">The declared layer name, or null when the project sets none.</param>
    /// <returns>The layer.</returns>
    public static ArchitectureLayer FileLayer(string? filePath, string assemblyName, BackendLayout layout, string? declared)
    {
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return Parse(declared!.Trim());
        }

        if (layout == BackendLayout.LayerProjects)
        {
            return FromAssemblyName(assemblyName);
        }

        var assemblyLayer = FromAssemblyName(assemblyName);
        if (assemblyLayer == ArchitectureLayer.Test)
        {
            return ArchitectureLayer.Test;
        }

        if (FolderLayer(filePath) is ArchitectureLayer folder)
        {
            return folder;
        }

        // A layer project names its layer: a file under Library.Domain is Domain
        // even when its role suffix names another layer, so a misplaced file reads
        // by its physical home and the checks surface the misplacement. Under the
        // one product project the assembly names no layer, and the role suffix decides.
        if (assemblyLayer != ArchitectureLayer.None)
        {
            return assemblyLayer;
        }

        if (RoleLayer(FileNameOf(filePath)) is ArchitectureLayer role)
        {
            return role;
        }

        // Any other file in a use case, reaction or job folder is Application, so a helper
        // named without a role suffix is still held to the layer rules.
        if (InSliceFolder(filePath))
        {
            return ArchitectureLayer.Application;
        }

        return assemblyLayer;
    }

    /// <summary>Returns true when the file sits below a UseCases or Jobs folder; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when a folder of the path is named UseCases or Jobs; otherwise false.</returns>
    private static bool InSliceFolder(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        var path = "/" + filePath!.Replace('\\', '/');
        return path.Contains("/UseCases/") || path.Contains("/Jobs/");
    }

    /// <summary>Reads a declared layer name; an unknown name opts the project out.</summary>
    /// <param name="value">The declared layer name text, with or without dots.</param>
    /// <returns>The layer.</returns>
    public static ArchitectureLayer Parse(string value)
    {
        switch (value.Replace(".", string.Empty).ToUpperInvariant())
        {
            case "DOMAIN": return ArchitectureLayer.Domain;
            case "APPLICATIONABSTRACTIONS": return ArchitectureLayer.ApplicationAbstractions;
            case "APPLICATION": return ArchitectureLayer.Application;
            case "INFRASTRUCTURE": return ArchitectureLayer.Infrastructure;
            case "WEBAPI": return ArchitectureLayer.WebApi;
            case "WORKER": return ArchitectureLayer.Worker;
            case "TEST": return ArchitectureLayer.Test;
            case "TESTS": return ArchitectureLayer.Test;
            default: return ArchitectureLayer.None;
        }
    }

    /// <summary>Reads a declared layout name; anything unrecognized reads as the module layout.</summary>
    /// <param name="value">The declared layout name text, or null when the project sets none.</param>
    /// <returns>The layout.</returns>
    public static BackendLayout ParseLayout(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BackendLayout.Modules;
        }

        var text = value!.Trim().Replace(".", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        return text == "LAYERPROJECTS" ? BackendLayout.LayerProjects : BackendLayout.Modules;
    }

    /// <summary>Reads the layer from the last segments of an assembly name.</summary>
    /// <param name="assemblyName">The assembly name that names the application project.</param>
    /// <returns>The layer named by the last assembly name segments, or None when none names one.</returns>
    public static ArchitectureLayer FromAssemblyName(string assemblyName)
    {
        if (EndsWithSegment(assemblyName, "Tests") || EndsWithSegment(assemblyName, "Test")) return ArchitectureLayer.Test;
        if (EndsWithSegment(assemblyName, "Application.Abstractions")) return ArchitectureLayer.ApplicationAbstractions;
        if (EndsWithSegment(assemblyName, "Domain")) return ArchitectureLayer.Domain;
        if (EndsWithSegment(assemblyName, "Application")) return ArchitectureLayer.Application;
        if (EndsWithSegment(assemblyName, "Infrastructure")) return ArchitectureLayer.Infrastructure;
        if (EndsWithSegment(assemblyName, "WebApi")) return ArchitectureLayer.WebApi;
        if (EndsWithSegment(assemblyName, "Worker")) return ArchitectureLayer.Worker;
        return ArchitectureLayer.None;
    }

    /// <summary>Reads the layer from the nearest layer folder on the file's path.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The layer the nearest layer folder states, or null when no folder on the path states one.</returns>
    public static ArchitectureLayer? FolderLayer(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var segments = filePath!.Split('/', '\\');
        for (var index = segments.Length - 2; index >= 0; index--)
        {
            switch (segments[index])
            {
                case "Domain": return ArchitectureLayer.Domain;
                case "Application": return ArchitectureLayer.Application;
                case "Infrastructure": return ArchitectureLayer.Infrastructure;
                case "WebApi": return ArchitectureLayer.WebApi;
                case "Contracts": return ArchitectureLayer.Application;
            }
        }

        return null;
    }

    /// <summary>Reads the layer from the role suffix of a use case, reaction or job file.</summary>
    /// <param name="fileName">The file name with or without its extension, or null when the document has none.</param>
    /// <returns>The layer the role suffix states, or null when the name carries no role suffix.</returns>
    public static ArchitectureLayer? RoleLayer(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var name = fileName;
        var dot = name!.LastIndexOf('.');
        if (dot > 0)
        {
            name = name.Substring(0, dot);
        }

        if (name.EndsWith("Endpoint", StringComparison.Ordinal)
            || name.EndsWith("RequestModel", StringComparison.Ordinal)
            || name.EndsWith("ResponseModel", StringComparison.Ordinal))
        {
            return ArchitectureLayer.WebApi;
        }

        if (name.EndsWith("Command", StringComparison.Ordinal)
            || name.EndsWith("Query", StringComparison.Ordinal)
            || name.EndsWith("Result", StringComparison.Ordinal)
            || name.EndsWith("Handler", StringComparison.Ordinal)
            || name.EndsWith("Validator", StringComparison.Ordinal)
            || name.EndsWith("Job", StringComparison.Ordinal)
            || name.EndsWith("Worker", StringComparison.Ordinal))
        {
            return ArchitectureLayer.Application;
        }

        return null;
    }

    /// <summary>Returns the file name the path ends with.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The file name, or null when the path is null or empty.</returns>
    private static string? FileNameOf(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var slash = Math.Max(filePath!.LastIndexOf('/'), filePath!.LastIndexOf('\\'));
        return slash < 0 ? filePath : filePath.Substring(slash + 1);
    }

    /// <summary>Returns true when the ends-with-segment condition holds; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="suffix">The final segment the name must end with.</param>
    /// <returns>True when the ends-with-segment condition holds; otherwise false.</returns>
    private static bool EndsWithSegment(string name, string suffix) =>
        name.Equals(suffix, StringComparison.Ordinal)
        || name.EndsWith("." + suffix, StringComparison.Ordinal);
}
