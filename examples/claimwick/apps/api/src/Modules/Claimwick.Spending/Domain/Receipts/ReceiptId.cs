using Claimwick.Kernel.Values;

namespace Claimwick.Spending.Domain.Receipts;

/// <summary>One receipt identity: a version 7 Guid the caller supplies. No other module carries it, so it lives in Domain.</summary>
internal readonly record struct ReceiptId : IStronglyTypedId, IParsable<ReceiptId>
{
    private ReceiptId(Guid value)
    {
        Value = value;
    }

    /// <summary>The Guid value.</summary>
    public Guid Value { get; }

    /// <summary>Builds the identity from a Guid, refusing an empty one.</summary>
    public static ReceiptId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A receipt identity must not be empty.", nameof(value));
        }

        return new ReceiptId(value);
    }

    /// <summary>Parses the identity from route or query text.</summary>
    public static ReceiptId Parse(string value, IFormatProvider? provider) =>
        From(Guid.Parse(value));

    /// <summary>Tries to parse the identity from route or query text.</summary>
    public static bool TryParse(
        string? value,
        IFormatProvider? provider,
        out ReceiptId result)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            result = new ReceiptId(parsed);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
