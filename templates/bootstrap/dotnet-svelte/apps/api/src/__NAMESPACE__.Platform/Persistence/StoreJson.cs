using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using __NAMESPACE__.Kernel.Domain;
using __NAMESPACE__.Platform.Http;

namespace __NAMESPACE__.Platform.Persistence;

/// <summary>
/// Builds the document store serializer: the shared money and typed-identifier
/// converters, the module converters for closed sets and other stored types,
/// and stored-member support so a document loads without running its constructor.
/// The store registration calls this once; modules contribute data, never resolvers.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-technical-capabilities-in-the-platform</follows>
public static class StoreJson
{
    /// <summary>The staged domain events member, which never persists.</summary>
    private const string StagedDomainEventsName = nameof(IAggregateRootEvents.StagedDomainEvents);

    /// <summary>Configures the store serializer with the shared converters.</summary>
    /// <param name="options">The serializer options.</param>
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Converters.Add(new MoneyJsonConverter());
        options.Converters.Add(new TypedIdJsonConverterFactory());
        options.Converters.Add(new CodedClosedSetJsonConverterFactory(new HashSet<Type>()));

        var resolver = StoredResolver(options);
        resolver.Modifiers.Add(ApplyStoredMembers);
    }

    private static DefaultJsonTypeInfoResolver StoredResolver(JsonSerializerOptions options)
    {
        if (options.TypeInfoResolver is DefaultJsonTypeInfoResolver shared)
        {
            return shared;
        }

        var created = new DefaultJsonTypeInfoResolver();
        if (options.TypeInfoResolver is null)
        {
            options.TypeInfoResolver = created;
        }
        else
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
                new StoreTypeInfoResolver(created), options.TypeInfoResolver);
        }

        return created;
    }

    private static void ApplyStoredMembers(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind is not JsonTypeInfoKind.Object || typeInfo.Type.IsAbstract)
        {
            return;
        }

        if (typeInfo.CreateObject is null)
        {
            var empty = typeInfo.Type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == 0);
            if (empty is not null)
            {
                typeInfo.CreateObject = () => empty.Invoke([])!;
            }
            else if (!typeInfo.Type.IsValueType
                && typeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
            {
                // A root with only a non-public constructor loads without running it:
                // the stored members fill it through their setters or backing fields below,
                // as a load restores state rather than creating it.
                var type = typeInfo.Type;
                typeInfo.CreateObject = () => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
            }
        }

        for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            var property = typeInfo.Properties[index];
            if (property.AttributeProvider is not PropertyInfo backing)
            {
                continue;
            }

            if (IsStagedDomainEvents(backing))
            {
                typeInfo.Properties.RemoveAt(index);
                continue;
            }

            if (property.Set is not null)
            {
                continue;
            }

            var setter = backing.GetSetMethod(nonPublic: true);
            if (setter is not null)
            {
                property.Set = (obj, value) => setter.Invoke(obj, [value]);
                continue;
            }

            var field = FindBackingField(backing);
            if (field is not null)
            {
                property.Set = (obj, value) => field.SetValue(obj, value);
            }
        }
    }

    private static bool IsStagedDomainEvents(PropertyInfo member)
    {
        return string.Equals(member.Name, StagedDomainEventsName, StringComparison.Ordinal)
            && member.DeclaringType is not null
            && member.DeclaringType.IsGenericType
            && member.DeclaringType.GetGenericTypeDefinition() == typeof(AggregateRoot<>);
    }

    private static FieldInfo? FindBackingField(PropertyInfo member)
    {
        var declaring = member.DeclaringType;
        while (declaring is not null)
        {
            var field = declaring.GetField(
                $"<{member.Name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null)
            {
                return field;
            }

            declaring = declaring.BaseType;
        }

        return null;
    }

    private static bool IsStoredType(Type type)
    {
        return type.FullName is string name
            && name.StartsWith("__NAMESPACE__.", StringComparison.Ordinal);
    }

    /// <summary>
    /// Answers type metadata only for the stored product types, so the store
    /// resolver never shadows another resolver for other types when the store
    /// resolver is not the default one.
    /// </summary>
    private sealed class StoreTypeInfoResolver : IJsonTypeInfoResolver
    {
        private readonly DefaultJsonTypeInfoResolver _inner;

        /// <summary>Creates the resolver over the shared default resolver.</summary>
        /// <param name="inner">The shared default resolver.</param>
        public StoreTypeInfoResolver(DefaultJsonTypeInfoResolver inner)
        {
            _inner = inner;
        }

        /// <inheritdoc />
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            return IsStoredType(type) ? _inner.GetTypeInfo(type, options) : null;
        }
    }
}
