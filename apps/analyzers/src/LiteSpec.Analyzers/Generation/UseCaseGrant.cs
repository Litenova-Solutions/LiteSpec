using System;

namespace LiteSpec.Analyzers.Generation;

/// <summary>One use case with its access grant.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class UseCaseGrant : IEquatable<UseCaseGrant>
{
    /// <summary>Initializes a new instance of the <see cref="UseCaseGrant"/> class.Creates a use-case grant.</summary>
    /// <param name="useCaseId">The use-case identifier.</param>
    /// <param name="grant">Its access grant.</param>
    public UseCaseGrant(string useCaseId, string grant)
    {
        UseCaseId = useCaseId;
        Grant = grant;
    }

    /// <summary>Gets the use-case identifier.</summary>
    public string UseCaseId { get; }

    /// <summary>Gets its access grant.</summary>
    public string Grant { get; }

    /// <inheritdoc />
    public bool Equals(UseCaseGrant? other) =>
        other is not null
        && string.Equals(UseCaseId, other.UseCaseId, StringComparison.Ordinal)
        && string.Equals(Grant, other.Grant, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is UseCaseGrant other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(UseCaseId);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Grant);

            return hash;
        }
    }
}
