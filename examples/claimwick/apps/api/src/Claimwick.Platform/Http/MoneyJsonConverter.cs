using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Claimwick.Kernel.Values;

namespace Claimwick.Platform.Http;

/// <summary>Converts money on the wire as a string amount with a three-letter currency code.</summary>
/// <follows>litespec/backend/dotnet/api#convert-money-with-the-platform-converter</follows>
internal sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var amount = document.RootElement.GetProperty("amount").GetString();
        var currency = document.RootElement.GetProperty("currency").GetString();
        if (amount is null || currency is null || currency.Length != 3)
        {
            throw new JsonException("A monetary value carries a string amount with a three-letter currency code.");
        }

        return new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("amount", value.Amount.ToString(CultureInfo.InvariantCulture));
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
