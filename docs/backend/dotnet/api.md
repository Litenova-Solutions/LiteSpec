# .NET HTTP API

**Stack:** dotnet

**Concern:** web-api

Every HTTP operation in a .NET backend runs through one small endpoint class in its use case folder, and the OpenAPI contract is generated from those classes at build time. This page defines the .NET form of the shared [HTTP API](../api.md) rules: the endpoint contract, the metadata each operation declares, how transport models express closed sets, and how the global handler derives HTTP statuses from refusal codes.

## Concepts

### Endpoint Classes

A WebApi endpoint is a small class that owns one route operation. It converts HTTP input to an Application command or query, sends that message through the mediator, and maps the result back to HTTP.

Each endpoint implements the product-owned `IEndpoint` contract: one method that maps the route on the route builder. At startup, the host discovers every endpoint and maps each one exactly once, in type-name order.

One global handler maps each thrown failure to its HTTP status and Problem Details body, so endpoints carry no error mapping of their own.

### Operation Metadata

The OpenAPI generator builds the published contract from metadata declared on each operation: a stable `operationId` that generated clients use to name their calls, the authorization policies that guard it, the request and success shapes, the failure statuses it can answer, and a summary. Comments on route lambdas never reach the contract, so this metadata is declared explicitly with typed results or `Produces` entries.

## Standards

### Use One Endpoint per Operation

**ID:** `litespec/backend/dotnet/api#use-one-endpoint-per-operation`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A WebApi endpoint MUST implement `IEndpoint`, map one route operation, convert input to an Application message, dispatch through its mediator, and map the result.

**Rationale:** When one class owns one route, a reviewer finds the input mapping, the dispatch, and the result mapping for that route in one place. Without that rule, operations share classes or helpers, and a change to one route silently affects another, while a second routing pattern grows beside the first unnoticed. This provision binds `litespec/backend/api#map-one-operation-per-endpoint` for .NET.

**Example:** The WebApi project owns this contract:

```csharp
internal interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder endpoints);
}
```

At startup, the host scans the modules assembly through its one public marker for non-abstract `IEndpoint` implementations, registers each one once as a singleton, and calls `MapEndpoint` in type-name order. The scan uses framework reflection and dependency injection, so it needs no extra scanning package.

**Reviewed for:** Does every mapped route run through one `IEndpoint` type that converts input, dispatches, and maps the result? Look at the endpoints in the use case folders.

### Exclude MVC Controllers from the Stack

**ID:** `litespec/backend/dotnet/api#exclude-mvc-controllers-from-the-stack`

**Strength:** required

**Applies to:** artifact: any

**Statement:** WebApi code MUST NOT route requests through MVC controllers or `ControllerBase`.

**Rationale:** Controllers and endpoint classes route, filter, and map errors through different models. When both exist, a reviewer checks two mappings for every behavior, and error responses drift apart. One model means one place defines how requests reach handlers and how failures reach clients.

**Checked by:** `dotnet build` reports `LS2003` for a WebApi class deriving from `ControllerBase` or carrying `ApiController`.

### Declare Operation Metadata in the Contract

**ID:** `litespec/backend/dotnet/api#declare-operation-metadata-in-the-contract`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An operation MUST declare a stable `operationId`, a summary, its authorization, request, success, and Problem Details metadata, and typed results or `Produces` metadata.

**Rationale:** Generated clients name their calls from `operationId`, so an unnamed or unstable operation breaks every client regeneration. Authorization and Problem Details metadata tell a client which policies apply and which failures to handle; without them the client guesses. Route-lambda comments never reach the contract, so the summary and description are declared explicitly. This provision binds `litespec/backend/api#declare-stable-operation-metadata-in-the-contract` for .NET.

**Reviewed for:** Does every operation declare a stable `operationId`, a summary, authorization, request, success, and Problem Details metadata, and typed results or `Produces` entries? Look at each operation.

### Derive HTTP Statuses from Refusal Codes

**ID:** `litespec/backend/dotnet/api#derive-http-statuses-from-refusal-codes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** WebApi MUST map each refusal code to its HTTP status in one global handler.

**Rationale:** The specification records each refusal with its stable `code` and no transport status, so the stack derives the status its transport needs. The outcome-to-status assignment lives in `litespec/backend/api#use-consistent-status-codes`; this provision binds it for .NET.

The handler uses ASP.NET Core `AddProblemDetails` with one `IExceptionHandler`:

- Validation failures answer 400.
- Missing targets answer through the operation's 404 policy.
- Forbidden failures answer 403 or the declared 404 disclosure policy.
- Conflicts and state rejections answer 409.
- Unexpected failures answer 500 with code `internal_error`.

The derived `invalid-input` refusal answers 400 under the project's `refusals.invalidInput` code, and the derived `forbidden` refusal answers 403 or the declared 404 under the project's `refusals.forbidden` code. Known Domain exception types map individually, and a cancellation from a disconnected request is not reported as an application error.

**Example:** The handler registers through the framework's Problem Details services:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

The handler maps each known exception type to its status and code, and answers 500 with code `internal_error` for the rest.

**Reviewed for:** Does every refusal code map to its outcome-table status in the one `IExceptionHandler`? Look at the WebApi global handler.

### Mark Polymorphic Transport Models with Discriminators

**ID:** `litespec/backend/dotnet/api#mark-polymorphic-transport-models-with-discriminators`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A data-bearing set MUST map to an abstract base record with one sealed record per case, carrying the discriminator that `litespec/backend/api#mark-polymorphic-models-with-discriminators` requires.

**Rationale:** A concrete base record cannot mark the discriminator as required, so a client could send a payload that names no case. This provision binds `litespec/backend/api#mark-polymorphic-models-with-discriminators` for .NET and adds no discriminator rule of its own.

**Example:**

- Declare an abstract base record carrying `JsonPolymorphic` and one `JsonDerivedType` per case, on the type itself rather than in host options.
- Name the discriminator through `TypeDiscriminatorPropertyName`, using a name such as `type` or `outcome`.
- Complete discriminator metadata that the generator omits with a document or schema transformer.

**Reviewed for:** Does every data-bearing set map to an abstract base record with one sealed record per case and the discriminator the generic provision requires? Look at each data-bearing set.

## Conventions

### Place Endpoints in Their Use Case Folders

**ID:** `litespec/backend/dotnet/api#place-endpoints-in-their-use-case-folders`

**Strength:** default

**Applies to:** artifact: endpoint

**Default:** Place each endpoint with its request and response models and mappings in the `WebApi/` folder of its use case inside the `UseCases/` folder of its module and aggregate.

**Replacement:** A project names its endpoint layout in its WebApi documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Transport types take the names their boundary roles assign under `litespec/backend/api#name-transport-types-by-their-boundary-role`, and transport mapping lives in WebApi under `litespec/backend/application#keep-mappings-at-the-owning-boundary`. Response models carry the query's declared `include` groups. Each use case owns its transport models, so a shape two use cases need is declared in each under `litespec/backend/architecture#give-each-use-case-its-own-models`. Module nesting, including the single-aggregate flat form, follows `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`.

**Example:** The endpoint layout is:

```text
{Product}.Modules/
  Posts/                            single aggregate whose name matches the module
    UseCases/
      CreateDraft/
        Application/                message, result, handler, validator
          CreateDraftCommand.cs
          CreateDraftCommandResult.cs
          CreateDraftCommandValidator.cs
          CreateDraftCommandHandler.cs
        WebApi/
          CreateDraftEndpoint.cs
          CreateDraftRequestModel.cs
          CreateDraftResponseModel.cs
          CreateDraftApiMappings.cs
      GetPost/
        Application/
          GetPostQuery.cs
          GetPostQueryResult.cs
          GetPostQueryValidator.cs
          GetPostQueryHandler.cs
        WebApi/
          GetPostEndpoint.cs
          GetPostResponseModel.cs
          GetPostApiMappings.cs
  Customers/                        two aggregates: use cases nest under each aggregate UseCases folder
    CustomerAccounts/
      UseCases/
        RestrictAccount/
          Application/
            RestrictAccountCommand.cs
            RestrictAccountCommandHandler.cs
          WebApi/
            RestrictAccountEndpoint.cs
            RestrictAccountRequestModel.cs
            RestrictAccountApiMappings.cs
    Consents/
      UseCases/
        GrantConsent/
          Application/
            GrantConsentCommand.cs
            GrantConsentCommandHandler.cs
          WebApi/
            GrantConsentEndpoint.cs
            GrantConsentRequestModel.cs
            GrantConsentApiMappings.cs
{Product}.WebApi/
  Errors/
    GlobalExceptionHandler.cs
    ProblemDetailsApiMappings.cs
  Security/
    CurrentActor.cs
  OpenApi/
  Program.cs
```

**Reviewed for:** Does every use case `WebApi/` folder hold the endpoint, request and response models, and mappings named for the use case? Look at each use case folder.

### Compose the Host in Program.cs

**ID:** `litespec/backend/dotnet/api#compose-the-host-in-programcs`

**Strength:** default

**Applies to:** artifact: any

**Default:** `Program.cs` holds module registration, middleware order, endpoint discovery, health endpoints, OpenAPI, and host startup, ending with an empty partial `Program` class.

**Replacement:** A project names its startup layout in its WebApi documentation.

**Rationale:** Middleware runs in registration order, so authentication registered after endpoints never runs. When one file shows the whole sequence, a reviewer sees the order at a glance; when registration scatters across files, a misplaced call hides. This file binds `litespec/backend/architecture#compose-each-process-explicitly` for WebApi.

**Example:** The visible order is trusted forwarded headers, exception handling, transport security, authentication, authorization, endpoints, and then health. Forwarded headers appear only when the deployment boundary requires trusted proxies. OpenAPI and Scalar map only in Development.

**Reviewed for:** Does `Program.cs` hold the recorded composition order with every step present? Look at `Program.cs`.
