// The __PROJECT__ local stack: one command starts the whole local setup.
//
//   dotnet run --project deploy/__NAMESPACE__.AppHost
//
// The Aspire dashboard lists every resource with its logs and endpoints.
// The web app answers on http://localhost:5173 and the API on
// http://localhost:5005. PostgreSQL keeps its data in a persistent volume,
// so recorded check-ins survive a restart.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL 18 holds the Health check-ins and the outbox.
var database = builder.AddPostgres("database")
    .WithImageTag("18")
    .WithDataVolume("__SCOPE__-aspire-postgres")
    .WithLifetime(ContainerLifetime.Persistent);
var health = database.AddDatabase("__SCOPE__");

// The API serves the Health endpoints and the OpenAPI document.
var api = builder.AddProject<Projects.__NAMESPACE___WebApi>("api")
    .WithReference(health)
    .WaitFor(health)
    .WithHttpEndpoint(port: 5005, name: "http")
    .WithHttpHealthCheck("/health/ready");

// The SvelteKit development server. Its server-side code reaches the API
// through __NAMESPACE___BACKEND_URL. Port 5173 is fixed so the development
// sign-in returns to a stable address.
var web = builder.AddViteApp("web", "../../apps/web")
    .WithPnpm(install: false)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5173;
        endpoint.IsProxied = false;
    })
    .WithEnvironment("__NAMESPACE___BACKEND_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
