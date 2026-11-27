using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using __NAMESPACE__.Kernel.Values;

namespace __NAMESPACE__.Platform.Http;

/// <summary>
/// Writes and reads money as <c>{ "amount": "12.50", "currency": "EUR" }</c>.
/// The amount is a decimal string, so no client reads it as binary floating point.
/// A request whose amount is not a decimal or whose currency is not a three-letter
/// ISO 4217 code fails binding, which the host reports as invalid input.
/// </summary>
/// <follows>litespec/packs/dotnet/api#convert-money-with-the-platform-converter</follows>
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    /// <inheritdoc />
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Money is an object with an amount and a currency.");
        }

        string? amountText = null;
        string? currency = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Money is an object with an amount and a currency.");
            }

            var name = reader.GetString();
            reader.Read();
            if (string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase))
            {
                amountText = reader.TokenType == JsonTokenType.String
                    ? reader.GetString()
                    : throw new JsonException("The money amount is a decimal string, such as \"12.50\".");
            }
            else if (string.Equals(name, "currency", StringComparison.OrdinalIgnoreCase))
            {
                currency = reader.TokenType switch
                {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Null => null,
                    _ => throw new JsonException("The money currency is a three-letter ISO 4217 code."),
                };
            }
            else
            {
                reader.Skip();
            }
        }

        if (amountText is null
            || !decimal.TryParse(amountText, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            throw new JsonException("The money amount is a decimal string, such as \"12.50\".");
        }

        if (currency is null && amount == 0)
        {
            return default;
        }

        if (currency is null || currency.Length != 3 || !currency.All(static c => c is >= 'A' and <= 'Z'))
        {
            throw new JsonException("The money currency is a three-letter ISO 4217 code.");
        }

        return new Money(amount, currency);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteString("amount", value.Amount.ToString(CultureInfo.InvariantCulture));
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
