using System;

namespace LiteSpec.Model;

/// <summary>
///     Finds the layer of a file: the <c>LiteSpecLayer</c> MSBuild property when the
///     project sets it, otherwise the nearest layer folder or the file's role suffix.
///     A file's layer is its top folder in the module: Domain, Application,
///     Infrastructure or WebApi.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class ArchitectureLayerResolver
{
    /// <summary>The analyzer config key the package's build props make visible.</summary>
    public const string PropertyKey = "build_property.LiteSpecLayer";

    /// <summary>Returns the layer.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="assemblyName">The assembly name that names the application project.</param>
    /// <param name="declared">The declared layer name, or null when the project sets none.</param>
    /// <returns>The layer.</returns>
    public static ArchitectureLayer FileLayer(string? filePath, string assemblyName, string? declared)
    {
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return Parse(declared!.Trim());
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

        // A host names its layer: a file under the WebApi or Worker host is WebApi
        // or Worker even when its role suffix names another layer, so a misplaced
        // file reads by its physical home and the checks surface the misplacement.
        // A module project names no layer, and the role suffix decides.
        if (assemblyLayer != ArchitectureLayer.None)
        {
            return assemblyLayer;
        }

        if (RoleLayer(FileNameOf(filePath)) is ArchitectureLayer role)
        {
            return role;
        }

        return assemblyLayer;
    }

    /// <summary>
    ///     The use case folder holding the file: the folder holding the file when it sits below
    ///     Application, WebApi or Infrastructure, so a use case in its plural aggregate folder reads
    ///     as its own folder. A file directly in a layer folder, or outside every layer folder,
    ///     names no use case.
    /// </summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The use case folder name, or null when the file sits in no use case folder.</returns>
    public static string? UseCaseOf(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var segments = filePath!.Split('/', '\\');

        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (segments[index] is "Application" or "WebApi" or "Infrastructure")
            {
                return segments[segments.Length - 2];
            }
        }

        return null;
    }

    /// <summary>Reads a declared layer name; an unknown name opts the project out.</summary>
    /// <param name="value">The declared layer name text, with or without dots.</param>
    /// <returns>The layer.</returns>
    public static ArchitectureLayer Parse(string value)
    {
        switch (value.Replace(".", string.Empty).ToUpperInvariant())
        {
            case "DOMAIN": return ArchitectureLayer.Domain;
            case "APPLICATION": return ArchitectureLayer.Application;
            case "INFRASTRUCTURE": return ArchitectureLayer.Infrastructure;
            case "WEBAPI": return ArchitectureLayer.WebApi;
            case "WORKER": return ArchitectureLayer.Worker;
            case "TEST": return ArchitectureLayer.Test;
            case "TESTS": return ArchitectureLayer.Test;
            default: return ArchitectureLayer.None;
        }
    }

    /// <summary>
    ///     Reads the layer from the last segments of an assembly name. A business kernel reads
    ///     as Domain, so its values answer the Domain rules; a technical platform reads as
    ///     Infrastructure; an Api host reads as WebApi beside the WebApi and Worker hosts.
    ///     A module project names no layer: the layer of its files is their folder.
    /// </summary>
    /// <param name="assemblyName">The assembly name that names the application project.</param>
    /// <returns>The layer named by the last assembly name segments, or None when none names one.</returns>
    public static ArchitectureLayer FromAssemblyName(string assemblyName)
    {
        if (EndsWithSegment(assemblyName, "Tests") || EndsWithSegment(assemblyName, "Test")) return ArchitectureLayer.Test;
        if (EndsWithSegment(assemblyName, "Kernel")) return ArchitectureLayer.Domain;
        if (EndsWithSegment(assemblyName, "Platform")) return ArchitectureLayer.Infrastructure;
        if (EndsWithSegment(assemblyName, "WebApi")) return ArchitectureLayer.WebApi;
        if (EndsWithSegment(assemblyName, "Api")) return ArchitectureLayer.WebApi;
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
