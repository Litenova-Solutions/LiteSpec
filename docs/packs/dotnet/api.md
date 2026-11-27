# .NET HTTP API

**Stack:** dotnet

**Concern:** web-api

This page states how a .NET backend serves its use cases over HTTP. It is for developers who write endpoints, shape transport models, and map failures to HTTP statuses. One endpoint class serves one route, and one global handler turns every failure into its status.

## Concepts

### Endpoint Classes

Each WebApi endpoint is a small class serving one route operation. It converts HTTP input to an Application command or query, sends that message through the mediator, and maps the result back to HTTP. Each endpoint implements the platform-owned `IEndpoint` contract, which has one method mapping the route on the route builder.

At startup, the platform finds every endpoint in the module assemblies the host adds, and the host maps each one exactly once, in type-name order. The scan uses framework reflection and dependency injection, so no extra scanning package is needed. One global handler maps each thrown failure to its HTTP status and Problem Details body, and endpoints carry no error mapping of their own. A use case run by a provider callback is the exception: its pipeline handler in the module `Infrastructure/Pipeline/` folder carries the use case mark as its endpoint.

### Operation Metadata

The OpenAPI generator builds the published contract from metadata declared on each operation: a stable `operationId` that generated clients use to name their calls, the authorization policies guarding the operation, the request and success shapes, the failure statuses the operation answers, and a summary. Comments on route lambdas stay outside the contract, so this metadata appears explicitly through typed results or `Produces` entries.

### Statuses from Refusal Codes

The specification records each refusal with its stable `code` and no transport status, and each stack derives the status its transport needs. The global handler is the one place holding the map: validation failures answer 400, missing targets answer through the 404 policy, forbidden failures answer 403 or the declared 404 disclosure policy, conflicts and state rejections answer 409, and unexpected failures answer 500 with code `internal_error`. A cancellation from a disconnected request is not reported as an application error.

A project running on LiteBus registers `AddLiteBusExceptionHandler()` instead of writing that mapping itself. The handler answers each refusal from an `IRefusalProblemCatalog` filled with `Map` for each refusal code, `Replace` for a deliberate override, and `MapInvalid` for the status of an invalid result with no code mapping:

```csharp
var catalog = RefusalProblemCatalog.CreateDefault();
catalog.Replace("validation.*", StatusCodes.Status400BadRequest, "Request validation failed");
services.AddSingleton<IRefusalProblemCatalog>(catalog);
services.AddLiteBusExceptionHandler();
```

The default catalog answers a single `validation.*` code with 422, so a project keeping validation failures at 400 replaces that mapping. A domain exception becomes a refusal through the translator registered with `UseRefusalExceptionTranslator<TTranslator>()`, and `IProblemDetailsEnricher` adds members to each problem.

### BFF Authentication and Sessions

Browser traffic signs in through a backend-for-frontend: the browser holds only an HttpOnly, Secure, SameSite cookie naming a server-side session, while the identity tokens stay on the server, outside the browser. The backend exchanges the sign-in code with the identity provider, keeps the tokens on the server, and refreshes them there. Every cookie-authenticated write carries the marker header the API requires, so a forged form post without the header is refused.

A Claimwick approver signs in through the company identity provider and approves an expense from the browser. The browser sends only the session cookie with the approval; the backend holds the tokens, refreshes them when they expire, and the approval write carries the marker header.

### Host Composition

Each host composes its process in `Program.cs`: the `AddPlatform` call, then the `AddModules` call, then the endpoint map, with middleware in registration order. Authentication registered after endpoints misses the request, so the file shows the whole order at a glance. OpenAPI and Scalar map in Development only, and forwarded headers appear only where the deployment boundary uses trusted proxies.

## Standards

### Exclude MVC Controllers from the Stack

**ID:** `litespec/packs/dotnet/api#exclude-mvc-controllers-from-the-stack`

**Strength:** required

**Applies to:** artifact: any

**Statement:** WebApi code MUST NOT route requests through MVC controllers or `ControllerBase`.

**Rationale:** Controllers and endpoint classes route, filter, and map errors through different models. With both in the tree, a reviewer checks two mappings for every behavior, and error responses drift apart. With one model, one place defines how requests reach handlers and how failures reach clients.

**Checked by:** `dotnet build` reports `LS2003` for a WebApi class deriving from `ControllerBase` or carrying `ApiController`.

### Convert Money with the Platform Converter

**ID:** `litespec/packs/dotnet/api#convert-money-with-the-platform-converter`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** WebApi MUST convert `Money` through the platform converter, which writes the amount as an invariant decimal string, reads only a string amount with a three-letter currency code, and registers once through `AddPlatform`.

**Rationale:** A `decimal` property serializes as a JSON number, breaking the wire shape `{ "amount": "12.50", "currency": "EUR" }`. One converter in `Platform/Http`, registered once for the host, keeps every serializer producing the contracted shape, including a test client using default options. It reads only a string amount with a three-letter code, so a number amount or a code of another length is refused instead of parsed.

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

**ID:** `litespec/packs/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The one `IExceptionHandler` MUST map each failure to the status the exception-to-status map declares for it.

**Rationale:** The one `IExceptionHandler` registered through `AddExceptionHandler` carries the whole map, so each failure reaches the client through the one map. The 500 problem detail names the exception type and message only when the host runs in the Development environment; a hosted environment answers the stable code with no exception detail.

**Example:** The global handler answers an unknown expense with 404 and its problem detail, a malformed expense identifier with 400, and a fault no refusal covers with 500. That 500 carries the exception type and message when the host runs in Development, and the stable `internal_error` code with no exception detail in any other environment.

**Checked by:** `dotnet build` reports `LS2035` for an exception type the global handler leaves outside the status map.

## Conventions

None.
