using Claimwick.Kernel.Values;

namespace Claimwick.Spending.Contracts.Expenses;

/// <summary>One expense identity: a version 7 Guid the caller supplies. Reporting carries it, so it lives in Contracts.</summary>
public readonly record struct ExpenseId : IStronglyTypedId, IParsable<ExpenseId>
{
    private ExpenseId(Guid value)
    {
        Value = value;
    }

    /// <summary>The Guid value.</summary>
    public Guid Value { get; }

    /// <summary>Builds the identity from a Guid, refusing an empty one.</summary>
    public static ExpenseId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An expense identity must not be empty.", nameof(value));
        }

        return new ExpenseId(value);
    }

    /// <summary>Parses the identity from route or query text.</summary>
    public static ExpenseId Parse(string value, IFormatProvider? provider) =>
        From(Guid.Parse(value));

    /// <summary>Tries to parse the identity from route or query text.</summary>
    public static bool TryParse(
        string? value,
        IFormatProvider? provider,
        out ExpenseId result)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            result = new ExpenseId(parsed);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
