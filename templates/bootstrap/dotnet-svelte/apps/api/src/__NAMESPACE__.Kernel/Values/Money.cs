using System.Globalization;

namespace __NAMESPACE__.Kernel.Values;

/// <summary>
/// An amount with its ISO 4217 currency code. Minor units, rounding and symbols
/// are data of the business, never constants beside this type.
/// </summary>
public readonly record struct Money
{
    /// <summary>Creates money from an amount and a three-letter currency code such as EUR.</summary>
    /// <exception cref="ArgumentException">The currency code is not three ASCII uppercase letters.</exception>
    public Money(decimal amount, string currency)
    {
        if (!IsCurrencyCode(currency))
        {
            throw new ArgumentException($"'{currency}' is not a three-letter ISO 4217 currency code.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    /// <summary>The amount in the currency's own units.</summary>
    public decimal Amount { get; init; }

    /// <summary>The three-letter ISO 4217 currency code.</summary>
    public string Currency { get; init; }

    /// <summary>Zero in the given currency.</summary>
    public static Money Zero(string currency)
    {
        return new Money(0m, currency);
    }

    /// <summary>Creates money without throwing, for the serializer and the API boundary.</summary>
    /// <param name="amount">The amount in the currency's own units.</param>
    /// <param name="currency">The three-letter ISO 4217 currency code.</param>
    /// <param name="money">The money when the currency code is valid; otherwise the default.</param>
    /// <returns>True when the currency code is three ASCII uppercase letters.</returns>
    public static bool TryCreate(decimal amount, string? currency, out Money money)
    {
        if (!IsCurrencyCode(currency))
        {
            money = default;
            return false;
        }

        money = new Money(amount, currency!);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Amount} {Currency}");
    }

    private static bool IsCurrencyCode(string? value)
    {
        if (value is null || value.Length != 3)
        {
            return false;
        }

        return value.All(static c => c is >= 'A' and <= 'Z');
    }
}
