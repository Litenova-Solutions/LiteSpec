using System;
using System.Collections.Immutable;
using System.Linq;

namespace LiteSpec.Analyzers.Generation;

/// <summary>One role with the facts the generated entry carries.</summary>
/// <follows>litespec/packs/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
internal sealed class RoleFacts : IEquatable<RoleFacts>
{
    /// <summary>Initializes a new instance of the <see cref="RoleFacts"/> class.Creates role facts.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="scope">The reach of the grant, or empty when the record states none.</param>
    /// <param name="permissions">The permission identifiers granted to the role, in permission record order.</param>
    public RoleFacts(string id, string scope, ImmutableArray<string> permissions)
    {
        Id = id;
        Scope = scope;
        Permissions = permissions;
    }

    /// <summary>Gets the role identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the reach of the grant, or empty when the record states none.</summary>
    public string Scope { get; }

    /// <summary>Gets the permission identifiers granted to the role, in permission record order.</summary>
    public ImmutableArray<string> Permissions { get; }

    /// <inheritdoc />
    public bool Equals(RoleFacts? other) =>
        other is not null
        && string.Equals(Id, other.Id, StringComparison.Ordinal)
        && string.Equals(Scope, other.Scope, StringComparison.Ordinal)
        && Permissions.SequenceEqual(other.Permissions);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RoleFacts other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Id);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Scope);

            foreach (var permission in Permissions)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(permission);
            }

            return hash;
        }
    }
}
