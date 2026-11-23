# .NET HTTP API

**Stack:** dotnet

**Concern:** web-api

In a .NET backend, each HTTP operation runs through one small endpoint class in its use case folder, and the OpenAPI contract is generated from those classes at build time. This page defines how the shared [HTTP API](../api.md) rules apply to .NET: the endpoint contract, the metadata each operation declares, how transport models express closed sets, and how the global handler derives HTTP statuses from refusal codes.

## Concepts

### Endpoint Classes

A WebApi endpoint is a small class that serves one route operation. It converts HTTP input to an Application command or query, sends that message through the mediator, and maps the result back to HTTP.

Each endpoint implements the platform-owned `IEndpoint` contract, which has one method that maps the route on the route builder. At startup, the platform discovers every endpoint in the module assemblies the host adds, and the host maps each one exactly once, in type-name order.

One global handler maps each thrown failure to its HTTP status and Problem Details body. Endpoints carry no error mapping of their own.

### Operation Metadata

The OpenAPI generator builds the published contract from metadata declared on each operation:

- A stable `operationId`, which generated clients use to name their calls.
- The authorization policies that guard the operation.
- The request and success shapes.
- The failure statuses the operation can answer.
- A summary.

Comments on route lambdas do not reach the contract, so this metadata is declared explicitly with typed results or `Produces` entries.

## Standards

### Use One Endpoint per Operation

**ID:** `litespec/backend/dotnet/api#use-one-endpoint-per-operation`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A WebApi endpoint MUST implement `IEndpoint`, map one route operation, convert input to an Application message, dispatch through its mediator, and map the result.

**Rationale:** When one class serves one route, a reviewer finds the input mapping, the dispatch, and the result mapping for that route in one place. Without that rule, operations share classes or helpers, so a change to one route silently affects another, and a second routing pattern grows beside the first unnoticed. A use case a provider callback runs is the exception: the pipeline handler in the module `Infrastructure/Pipeline/` folder carries the use case mark as its endpoint, so it implements no `IEndpoint` and no screen calls it. This provision binds `litespec/backend/api#map-one-operation-per-endpoint` for .NET.

**Example:** The platform owns this contract:

```csharp
internal interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder endpoints);
}
```

At startup, the platform scans every module assembly the host adds through its entry type for non-abstract `IEndpoint` implementations, registers each one once as a singleton, and the host calls `MapEndpoint` in type-name order. The scan uses framework reflection and dependency injection, so no extra scanning package is needed.

**Reviewed for:** Does every mapped route run through one `IEndpoint` type that converts input, dispatches, and maps the result? Look at the endpoints in the use case folders.

### Exclude MVC Controllers from the Stack

**ID:** `litespec/backend/dotnet/api#exclude-mvc-controllers-from-the-stack`

**Strength:** required

**Applies to:** artifact: any

**Statement:** WebApi code MUST NOT route requests through MVC controllers or `ControllerBase`.

**Rationale:** Controllers and endpoint classes route, filter, and map errors through different models. When both exist, a reviewer checks two mappings for every behavior, and error responses drift apart. With one model, one place defines how requests reach handlers and how failures reach clients.

**Checked by:** `dotnet build` reports `LS2003` for a WebApi class deriving from `ControllerBase` or carrying `ApiController`.

### Declare Operation Metadata in the Contract

**ID:** `litespec/backend/dotnet/api#declare-operation-metadata-in-the-contract`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An operation MUST declare a stable `operationId`, a summary, its authorization, request, success, and Problem Details metadata, and typed results or `Produces` metadata.

**Rationale:** Generated clients name their calls from `operationId`, so an unnamed or unstable operation breaks every client regeneration. Authorization and Problem Details metadata tell a client which policies apply and which failures to handle; without them the client guesses. Route-lambda comments do not reach the contract, so the summary is declared explicitly. This provision binds `litespec/backend/api#declare-stable-operation-metadata-in-the-contract` for .NET.

**Reviewed for:** Does every operation declare a stable `operationId`, a summary, authorization, request, success, and Problem Details metadata, and typed results or `Produces` entries? Look at each operation.

### Derive HTTP Statuses from Refusal Codes

**ID:** `litespec/backend/dotnet/api#derive-http-statuses-from-refusal-codes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** WebApi MUST map each refusal code to its HTTP status in one global handler.

**Rationale:** The specification records each refusal with its stable `code` and no transport status, so each stack derives the status its transport needs. The outcome-to-status assignment is stated in `litespec/backend/api#use-consistent-status-codes`; this provision binds it for .NET.

The handler uses ASP.NET Core `AddProblemDetails` with one `IExceptionHandler`:

- Validation failures answer 400.
- Missing targets answer through the operation's 404 policy.
- Forbidden failures answer 403 or the declared 404 disclosure policy.
- Conflicts and state rejections answer 409.
- Unexpected failures answer 500 with code `internal_error`.

The derived `invalid-input` refusal answers 400 under the project's `refusals.invalidInput` code, and the derived `forbidden` refusal answers 403 or the declared 404 under the project's `refusals.forbidden` code. The derived `rule-not-stated` refusal answers 409. Known Domain exception types map individually, and a cancellation from a disconnected request is not reported as an application error.

**Example:** The handler registers through the framework's Problem Details services:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

The handler maps each known exception type to its status and code, and answers 500 with code `internal_error` for the rest.

A project that runs LiteBus maps its mediation refusals in LiteBus instead of writing that mapping itself. `AddLiteBusExceptionHandler()` registers `LiteBusExceptionHandler`, the one `IExceptionHandler` that answers each refusal from an `IRefusalProblemCatalog`. The project fills the catalog with `Map` for each refusal code, `Replace` for a deliberate override, and `MapInvalid` for the status of an invalid result with no code mapping. A domain exception becomes a refusal through the translator registered with `UseRefusalExceptionTranslator<TTranslator>()` on the message module, and `IProblemDetailsEnricher` adds members to each problem. The default catalog answers a single `validation.*` code with 422, so a project that keeps validation failures at 400 replaces that mapping:

```csharp
var catalog = RefusalProblemCatalog.CreateDefault();
catalog.Replace("validation.*", StatusCodes.Status400BadRequest, "Request validation failed");
services.AddSingleton<IRefusalProblemCatalog>(catalog);
services.AddLiteBusExceptionHandler();
```

**Reviewed for:** Does every refusal code map to its outcome-table status in the one `IExceptionHandler`? Look at the WebApi global handler.

### Convert Money with the Platform Converter

**ID:** `litespec/backend/dotnet/api#convert-money-with-the-platform-converter`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** WebApi MUST convert `Money` through the platform converter, which writes the amount as an invariant decimal string, reads only a string amount with a three-letter currency code, and registers once through `AddPlatform`.

**Rationale:** A `decimal` property serializes as a JSON number, breaking the wire shape `{ "amount": "12.50", "currency": "EUR" }` that `litespec/backend/domain#model-money-with-exact-decimal-values` fixes. One converter in `Platform/Http`, registered once for the host, keeps every serializer producing the contracted shape, including a test client using default options. It reads only a string amount with a three-letter code, so a number amount or a code of another length is refused instead of parsed.

**Example:**

```csharp
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
        return Money.Create(decimal.Parse(amount, CultureInfo.InvariantCulture), currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("amount", value.Amount.ToString(CultureInfo.InvariantCulture));
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
```

**Reviewed for:** Does every `Money` value on the wire pass through the platform converter, with a string amount and a three-letter code in both directions? Look at the converters registered for the host and the monetary fields in the contract.

### Map Failures to Statuses in the Global Exception Handler

**ID:** `litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The one `IExceptionHandler` MUST map each failure to the status the exception-to-status map declares for it.

**Rationale:** This provision binds `litespec/backend/api#map-every-failure-to-its-declared-status` for .NET: the one `IExceptionHandler` registered through `AddExceptionHandler` carries the whole map, so each failure reaches the client through the one map. The 500 problem detail names the exception type and message only when the host runs in the Development environment; a hosted environment answers the stable code with no exception detail.

**Example:** The global handler answers an unknown expense with 404 and its problem detail, a malformed expense identifier with 400, and a fault no refusal covers with 500. That 500 carries the exception type and message when the host runs in Development, and the stable `internal_error` code with no exception detail in any other environment.

**Checked by:** `dotnet build` reports `LS2035` for an exception type the global handler leaves outside the status map.

## Conventions

### Compose the Host in Program.cs

**ID:** `litespec/backend/dotnet/api#compose-the-host-in-programcs`

**Strength:** default

**Applies to:** artifact: any

**Default:** `Program.cs` calls `AddPlatform` and `AddModules`, then holds middleware order, endpoint mapping, health endpoints, OpenAPI, and host startup, ending with an empty partial `Program` class.

**Replacement:** A project names its startup layout in its WebApi documentation.

**Rationale:** Middleware runs in registration order, so authentication registered after endpoints does not run. When one file shows the whole sequence, a reviewer sees the order at a glance; when registration scatters across files, a misplaced call is hard to spot. This file binds `litespec/backend/architecture#compose-each-process-explicitly` for WebApi.

**Example:** The visible order is the `AddPlatform` and `AddModules` calls, then trusted forwarded headers, exception handling, transport security, authentication, authorization, endpoints, and then health. Forwarded headers appear only when the deployment boundary requires trusted proxies. OpenAPI and Scalar map only in Development.

**Reviewed for:** Does `Program.cs` hold the recorded composition order with every step present? Look at `Program.cs`.
