namespace __NAMESPACE__.Kernel.Values;

/// <summary>
/// A three-letter ISO 4217 currency code such as EUR.
/// Minor units, rounding and symbols are data of the business,
/// never constants beside this type.
/// </summary>
public readonly record struct CurrencyCode
{
    /// <summary>Creates a currency code from a three-letter ISO 4217 code such as EUR.</summary>
    /// <exception cref="ArgumentException">The value is not three ASCII uppercase letters.</exception>
    public CurrencyCode(string value)
    {
        if (!IsCurrencyCode(value))
        {
            throw new ArgumentException($"'{value}' is not a three-letter ISO 4217 currency code.", nameof(value));
        }

        Value = value;
    }

    /// <summary>The three-letter ISO 4217 currency code.</summary>
    public string Value { get; init; }

    /// <summary>Creates a currency code, throwing when the value is not valid.</summary>
    /// <param name="value">The three-letter ISO 4217 currency code.</param>
    /// <exception cref="ArgumentException">The value is not three ASCII uppercase letters.</exception>
    public static CurrencyCode Parse(string value)
    {
        return new CurrencyCode(value);
    }

    /// <summary>Creates a currency code without throwing, for the serializer and the API boundary.</summary>
    /// <param name="value">The three-letter ISO 4217 currency code.</param>
    /// <param name="code">The currency code when the value is valid; otherwise the default.</param>
    /// <returns>True when the value is three ASCII uppercase letters.</returns>
    public static bool TryParse(string? value, out CurrencyCode code)
    {
        if (!IsCurrencyCode(value))
        {
            code = default;
            return false;
        }

        code = new CurrencyCode(value!);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
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
