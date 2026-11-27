using System.Text.Json.Serialization;
using LiteBus.Extensions.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using __NAMESPACE__.Platform.Http;
using __NAMESPACE__.Platform.Persistence;

namespace __NAMESPACE__.WebApi.Composition;

/// <summary>
/// Registers the web services: the development sign-in scheme, the fallback
/// authorization policy, errors, the OpenAPI document, CORS for local
/// development, and the JSON converters the HTTP layer shares with the store.
/// The hosting order lives in Program.cs.
/// </summary>
/// <follows>litespec/packs/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</follows>
public static class WebApiComposition
{
    /// <summary>The name of the development-only CORS policy.</summary>
    public const string DevCorsPolicy = "SampleDev";

    /// <summary>Adds the web services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSampleWeb(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddLiteBusExceptionHandler(_ => { });
        services.AddProblemDetails();
        services.AddOpenApi();
        services.ConfigureHttpJsonOptions(json =>
        {
            json.SerializerOptions.Converters.Add(new MoneyJsonConverter());
            json.SerializerOptions.Converters.Add(new TypedIdJsonConverterFactory());
            json.SerializerOptions.Converters.Add(new CodedClosedSetJsonConverterFactory(new HashSet<Type>()));
            json.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        services.AddCors(cors => cors.AddPolicy(DevCorsPolicy, policy =>
        {
            var origins = configuration.GetSection("Sample:Web:DevCorsOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Content-Type", "traceparent", "tracestate")
                .AllowCredentials();
        }));
        return services;
    }
}
