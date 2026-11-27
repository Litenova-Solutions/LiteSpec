using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace __NAMESPACE__.Platform.Http;

/// <summary>
/// The host telemetry: W3C trace context with ASP.NET Core, HTTP client and
/// runtime instrumentation. A fresh product points an OTLP exporter at its
/// collector; with no exporter configured the host keeps its traces local.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-technical-capabilities-in-the-platform</follows>
public static class HostTelemetry
{
    /// <summary>Uses the W3C trace context on every activity the host starts.</summary>
    public static void UseW3cTraceContext()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }

    /// <summary>Adds the OpenTelemetry tracing and metrics of the host.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="serviceName">The service name the telemetry carries.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddSampleTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        builder.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation());
        return builder;
    }
}
