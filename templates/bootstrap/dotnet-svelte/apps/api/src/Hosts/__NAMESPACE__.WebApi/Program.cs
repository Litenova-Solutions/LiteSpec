using __NAMESPACE__.Health;
using __NAMESPACE__.Platform.Http;
using __NAMESPACE__.Platform.Modules;
using __NAMESPACE__.WebApi.Composition;

HostTelemetry.UseW3cTraceContext();

var builder = WebApplication.CreateBuilder(args);
builder.AddSampleTelemetry("__SCOPE__-api");

var health = new HealthModule();
builder.Services
    .AddPlatform(builder.Configuration, new BackendHostOptions(builder.Environment.IsDevelopment()))
    .AddModules(health);
builder.Services.AddSampleWeb(builder.Configuration);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentSignIn();
}

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseCors(WebApiComposition.DevCorsPolicy);
    app.MapOpenApi("/openapi/v1.json").AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentSignIn();
}

app.MapSampleHealth();
health.MapEndpoints(app);

app.Run();

/// <summary>
/// The web host entry point, held open for the book and the checks.
/// </summary>
public partial class Program;
