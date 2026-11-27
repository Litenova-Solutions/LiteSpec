using System;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>Recognizes the trigger types of a backend: the reactions and jobs that start a use case.</summary>
/// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
internal static class TriggerTypes
{
    /// <summary>
    ///     Returns true when the type is a reaction or a job: a type named *Job or *Worker, a type deriving from
    ///     BackgroundService, or a type implementing IEventHandler; otherwise false.
    /// </summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a reaction or a job; otherwise false.</returns>
    internal static bool IsReactionOrJob(INamedTypeSymbol type)
    {
        if (type.Name.EndsWith("Job", StringComparison.Ordinal)
            || type.Name.EndsWith("Worker", StringComparison.Ordinal))
        {
            return true;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "BackgroundService")
            {
                return true;
            }
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IEventHandler")
            {
                return true;
            }
        }

        return false;
    }
}
