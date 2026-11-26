# Web Data and State

**Stack:** svelte
**Concern:** frontend-data

The frontend keeps no copy of the API contract. It generates its client, types, schemas, and query options from the published OpenAPI document.

This page states the Svelte mechanism for client generation, failure mapping, and forms: the query plugin, the dictionary library, and the shared form entry point.

## Concepts

### The Problem Type

A failure from any source becomes one project-owned problem with the same fields. The page then decides what to show from one shape instead of handling each source separately.

```ts
type Problem = {
  status: number;
  code: string;
  detail: string;
  traceId?: string;
  fields: { field: string; code: string; message: string }[];
};
```

The `code` field is the discriminator: the page branches on which code arrived, not on a message that rewording can change. A code this build has never seen is still handled: the page branches on the code's presence rather than on a list of known codes. The client invents codes for what the API cannot answer:

- `client.unreachable` names a request that received no response.
- `client.unauthenticated` names a refused authentication.
- `client.http-<status>` names a response status the client maps to no problem, with the status in place of `<status>`.

The problem carries the `traceId` the API returns for the request, or the `X-Trace-Id` response header when the body carries none. Every error the user sees shows that identifier in copyable form inside its detail, while the headline states the message, so the user can paste the identifier to support without mistaking it for the message.

### Generating and Using the Client

The client generator runs the hey-api plugins with the zod plugin against the published contract, and adds the TanStack Query plugin for Svelte, per `litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator`. The workspace regenerates with `pnpm api:types` and edits nothing under the generated directory.

```json
{
  "plugins": ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk", "zod"]
}
```

A successful command invalidates the TanStack Query entries it changed. A form validates against the generated zod schema extended with the application's own rules:

```ts
zRedeliverOrderRequestModel.extend({ reason: zRedeliverOrderRequestModel.shape.reason.trim().min(1, words.ui.required) })
```

A refusal renders through the shared problem alert beside the refused record or control:

```svelte
<ProblemAlert error={mutation.error} describe={describe} />
```

Failures map to the problem type above. State ownership stays with the narrowest owner, copy resolves through the locale dictionaries, client state stays in the frame root, and forms build on the shared form entry point.

## Standards

### Add the Stack Query Plugin to the Client Generator

**ID:** `litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add `@tanstack/svelte-query`.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape.

**Example:** The Claimwick client generator adds `@tanstack/svelte-query`, giving each generated read a query key.

**Reviewed for:** Does the client generator configuration add the stated query plugin? Check the generator configuration of the Svelte frontend.

### Resolve Copy Through the Stack Dictionary Library

**ID:** `litespec/packs/svelte/data#resolve-copy-through-the-stack-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST meet the locale-dictionary contract that the frontend data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior.

**Example:** The Claimwick console renders its receipt-required sentence through `svelte-i18n`, in every language it ships.

**Reviewed for:** Does every copy call site use the dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites under `src`.

### Build a Web Form on the Shared Form Entry Point

**ID:** `litespec/packs/svelte/data#build-a-web-form-on-the-shared-form-entry-point`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A web form MUST build on the shared form creator.

**Rationale:** One entry point gives every form the same field setup and the same display for a refusal on its field.

**Example:** The Claimwick expense form builds on the shared form creator, with no form library of its own.

**Reviewed for:** Does every web form build on the stated form entry point? Check the form files under `src`.

## Conventions

None.
