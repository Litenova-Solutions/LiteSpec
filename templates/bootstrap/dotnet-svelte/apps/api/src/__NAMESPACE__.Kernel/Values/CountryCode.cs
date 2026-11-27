namespace __NAMESPACE__.Kernel.Values;

/// <summary>
/// A two-letter ISO 3166-1 alpha-2 country code such as NL.
/// Names and other country facts are data of the business,
/// never constants beside this type.
/// </summary>
public readonly record struct CountryCode
{
    /// <summary>Creates a country code from a two-letter ISO 3166-1 alpha-2 code such as NL.</summary>
    /// <exception cref="ArgumentException">The value is not two ASCII uppercase letters.</exception>
    public CountryCode(string value)
    {
        if (!IsCountryCode(value))
        {
            throw new ArgumentException($"'{value}' is not a two-letter ISO 3166-1 alpha-2 country code.", nameof(value));
        }

        Value = value;
    }

    /// <summary>The two-letter ISO 3166-1 alpha-2 country code.</summary>
    public string Value { get; init; }

    /// <summary>Creates a country code, throwing when the value is not valid.</summary>
    /// <param name="value">The two-letter ISO 3166-1 alpha-2 country code.</param>
    /// <exception cref="ArgumentException">The value is not two ASCII uppercase letters.</exception>
    public static CountryCode Parse(string value)
    {
        return new CountryCode(value);
    }

    /// <summary>Creates a country code without throwing, for the serializer and the API boundary.</summary>
    /// <param name="value">The two-letter ISO 3166-1 alpha-2 country code.</param>
    /// <param name="code">The country code when the value is valid; otherwise the default.</param>
    /// <returns>True when the value is two ASCII uppercase letters.</returns>
    public static bool TryParse(string? value, out CountryCode code)
    {
        if (!IsCountryCode(value))
        {
            code = default;
            return false;
        }

        code = new CountryCode(value!);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    private static bool IsCountryCode(string? value)
    {
        if (value is null || value.Length != 2)
        {
            return false;
        }

        return value.All(static c => c is >= 'A' and <= 'Z');
    }
}
