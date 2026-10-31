using System;
using System.Collections.Immutable;
using System.Linq;

namespace LiteSpec.Analyzers.Generation;

/// <summary>One permission with the facts the generated entry carries.</summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/core/access#state-the-scope-of-every-permission</follows>
internal sealed class PermissionFacts : IEquatable<PermissionFacts>
{
    /// <summary>Initializes a new instance of the <see cref="PermissionFacts"/> class.Creates permission facts.</summary>
    /// <param name="id">The permission identifier.</param>
    /// <param name="scope">The reach at which a role grants it, or empty when the record states none.</param>
    /// <param name="group">The group ordering it beside its peers, or null when the record states none.</param>
    /// <param name="risks">The risks the record marks, in record order.</param>
    public PermissionFacts(string id, string scope, string? group, ImmutableArray<string> risks)
    {
        Id = id;
        Scope = scope;
        Group = group;
        Risks = risks;
    }

    /// <summary>Gets the permission identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the reach at which a role grants it, or empty when the record states none.</summary>
    public string Scope { get; }

    /// <summary>Gets the group ordering it beside its peers, or null when the record states none.</summary>
    public string? Group { get; }

    /// <summary>Gets the risks the record marks, in record order.</summary>
    public ImmutableArray<string> Risks { get; }

    /// <inheritdoc />
    public bool Equals(PermissionFacts? other) =>
        other is not null
        && string.Equals(Id, other.Id, StringComparison.Ordinal)
        && string.Equals(Scope, other.Scope, StringComparison.Ordinal)
        && string.Equals(Group, other.Group, StringComparison.Ordinal)
        && Risks.SequenceEqual(other.Risks);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PermissionFacts other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Id);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Scope);

            if (Group is not null)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Group);
            }

            foreach (var risk in Risks)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(risk);
            }

            return hash;
        }
    }
}
