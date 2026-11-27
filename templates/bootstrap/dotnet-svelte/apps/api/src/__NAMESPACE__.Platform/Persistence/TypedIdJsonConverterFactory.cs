using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using __NAMESPACE__.Kernel.Values;

namespace __NAMESPACE__.Platform.Persistence;

/// <summary>
/// Stores every typed identifier as its bare GUID, the form the store LINQ provider
/// reads when a query compares an identifier member, such as
/// <c>checkIn.CheckInId.Value == checkInId.Value</c>. A typed identifier written as
/// an object would make that comparison fail as soon as the table holds a row.
/// </summary>
/// <follows>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</follows>
public sealed class TypedIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsValueType && typeof(ITypedId).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        var converterType = typeof(TypedIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    /// <summary>Reads and writes one typed identifier type as its bare GUID.</summary>
    /// <typeparam name="TId">The typed identifier.</typeparam>
    private sealed class TypedIdJsonConverter<TId> : JsonConverter<TId>
        where TId : struct, ITypedId
    {
        /// <summary>The constructor that takes the GUID value.</summary>
        private static readonly ConstructorInfo Constructor =
            typeof(TId).GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException($"The typed identifier '{typeof(TId).Name}' has no constructor taking its GUID value.");

        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Create(reader.GetGuid());
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Guid? value = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString();
                    reader.Read();
                    if (string.Equals(name, nameof(ITypedId.Value), StringComparison.OrdinalIgnoreCase))
                    {
                        value = reader.GetGuid();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return value is { } guid
                    ? Create(guid)
                    : throw new JsonException($"The stored '{typeof(TId).Name}' holds no Value.");
            }

            throw new JsonException($"A '{typeof(TId).Name}' is stored as a GUID string.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStringValue(value.Value);
        }

        /// <inheritdoc />
        public override TId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Create(Guid.Parse(reader.GetString()!));
        }

        /// <inheritdoc />
        public override void WriteAsPropertyName(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WritePropertyName(value.Value.ToString());
        }

        /// <summary>Builds the typed identifier from its GUID value.</summary>
        /// <param name="value">The GUID value.</param>
        /// <returns>The typed identifier.</returns>
        private static TId Create(Guid value)
        {
            return (TId)Constructor.Invoke([value]);
        }
    }
}
