# __PROJECT__

A runnable starter for a __PROJECT__ product: a Health sample with check-ins,
a .NET API, a SvelteKit web app, and local and container runs. The records
under `.litespec/` hold the whole specification.

## Prerequisites

- .NET 10 SDK
- Node 24 and pnpm 11.28.5
- Docker with Compose v2

## Run Locally

One command starts PostgreSQL, the API and the web app through the Aspire
AppHost, with Docker running and `pnpm install` done once:

```bash
dotnet run --project deploy/__NAMESPACE__.AppHost
```

It prints the Aspire dashboard link and starts the web app on
http://localhost:5173 and the API on http://localhost:5005 (OpenAPI at
`/openapi/v1.json`, health at `/health/ready`). PostgreSQL keeps its data in
a persistent volume, so recorded check-ins survive a restart.

The container images build and run without Aspire (web on port 3000, API on
port 5005):

```bash
docker compose -f deploy/compose.yaml up --build
```

`deploy/.env.example` lists the knobs (`API_PORT`, `WEB_PORT`,
`POSTGRES_PASSWORD`); every knob already has a working default in
`deploy/compose.yaml`, so no file is needed. The web container reaches the API
as `http://api:8080` through `__NAMESPACE___BACKEND_URL`.

## Checks

```bash
dotnet build apps/api/__NAMESPACE__.slnx --configuration Release
pnpm --filter web check
dotnet litespec model check --root .
dotnet litespec sync --root . --check
```

`dotnet build` compiles the backend, `pnpm --filter web check` type-checks
the web app, and `dotnet litespec model check` reports findings on the records
and the code. With the stack running, `dotnet litespec verify --root .` runs
the recorded probe against the API.

## Layout

- `apps/api/` - the .NET backend: the solution, the shared kernel, the Health
  module, and the WebApi host.
- `apps/web/` - the SvelteKit web app: the home page, the check-ins list, and
  the check-in record page.
- `packages/ui/` - the shared UI package: tokens and the locked floorplans.
- `packages/api/` - the typed API client, generated from the OpenAPI document.
- `deploy/` - how the applications run together: the AppHost for local work
  and the container stack for production.
- `.litespec/` - the whole specification: the project file, the Health module
  records, the screens, and the cast.
