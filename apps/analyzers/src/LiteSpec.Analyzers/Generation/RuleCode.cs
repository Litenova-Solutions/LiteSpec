using System;

namespace LiteSpec.Analyzers.Generation;

/// <summary>One rule with the refusal code its exception carries.</summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
internal sealed class RuleCode : IEquatable<RuleCode>
{
    /// <summary>Initializes a new instance of the <see cref="RuleCode"/> class.Creates a rule code.</summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="code">The code of its refusal result, or its bare refusal code.</param>
    public RuleCode(string ruleId, string code)
    {
        RuleId = ruleId;
        Code = code;
    }

    /// <summary>Gets the rule identifier.</summary>
    public string RuleId { get; }

    /// <summary>Gets the code of its refusal result, or its bare refusal code.</summary>
    public string Code { get; }

    /// <inheritdoc />
    public bool Equals(RuleCode? other) =>
        other is not null
        && string.Equals(RuleId, other.RuleId, StringComparison.Ordinal)
        && string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RuleCode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(RuleId);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Code);

            return hash;
        }
    }
}
