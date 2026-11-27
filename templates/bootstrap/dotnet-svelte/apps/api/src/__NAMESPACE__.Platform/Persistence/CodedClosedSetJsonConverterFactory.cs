using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace __NAMESPACE__.Platform.Persistence;

/// <summary>
/// Stores every closed set that names itself by a code as its bare code string, and reads it back
/// through the set's own <c>FromCode</c>. A closed set is an abstract record with a static
/// <c>FromCode(string)</c> returning the set and a string <c>Code</c> property, such as a
/// check-in state. Without this, a closed set would store as an empty object and fail
/// to load, as an abstract type cannot be created.
/// </summary>
/// <follows>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</follows>
public sealed class CodedClosedSetJsonConverterFactory : JsonConverterFactory
{
    private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly IReadOnlySet<Type> _mappedElsewhere;

    /// <summary>Creates the factory, skipping the closed sets stored under another mapping.</summary>
    /// <param name="mappedElsewhere">The closed sets a module already stores under a discriminator or its own converter.</param>
    public CodedClosedSetJsonConverterFactory(IReadOnlySet<Type> mappedElsewhere)
    {
        _mappedElsewhere = mappedElsewhere ?? throw new ArgumentNullException(nameof(mappedElsewhere));
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsAbstract
            && typeToConvert.IsClass
            && !_mappedElsewhere.Contains(typeToConvert)
            && FromCodeOf(typeToConvert) is not null
            && CodeOf(typeToConvert) is not null;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        var converterType = typeof(Converter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(
            converterType, FromCodeOf(typeToConvert)!, CodeOf(typeToConvert)!)!;
    }

    /// <summary>Finds the static <c>FromCode(string)</c> that returns the closed set.</summary>
    /// <param name="type">The closed-set type.</param>
    /// <returns>The method, or null when the type has none.</returns>
    private static MethodInfo? FromCodeOf(Type type)
    {
        var method = type.GetMethod("FromCode", StaticMembers, [typeof(string)]);
        return method is not null && type.IsAssignableFrom(method.ReturnType) ? method : null;
    }

    /// <summary>Finds the string <c>Code</c> property of the closed set.</summary>
    /// <param name="type">The closed-set type.</param>
    /// <returns>The property, or null when the type has none.</returns>
    private static PropertyInfo? CodeOf(Type type)
    {
        var property = type.GetProperty("Code", InstanceMembers);
        return property is not null && property.PropertyType == typeof(string) ? property : null;
    }

    /// <summary>Reads and writes one closed set as its code string.</summary>
    /// <typeparam name="T">The closed-set type.</typeparam>
    private sealed class Converter<T> : JsonConverter<T>
        where T : class
    {
        private readonly MethodInfo _fromCode;
        private readonly PropertyInfo _code;

        /// <summary>Creates the converter over the set's code members.</summary>
        /// <param name="fromCode">The set's <c>FromCode(string)</c>.</param>
        /// <param name="code">The set's <c>Code</c> property.</param>
        public Converter(MethodInfo fromCode, PropertyInfo code)
        {
            _fromCode = fromCode ?? throw new ArgumentNullException(nameof(fromCode));
            _code = code ?? throw new ArgumentNullException(nameof(code));
        }

        /// <inheritdoc />
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var code = reader.GetString()
                ?? throw new JsonException($"A stored '{typeof(T).Name}' holds no code.");
            return (T?)_fromCode.Invoke(null, [code])
                ?? throw new JsonException($"'{code}' is no code of '{typeof(T).Name}'.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);
            writer.WriteStringValue((string)_code.GetValue(value)!);
        }
    }
}
