# Web Data and State

**Stack:** react, svelte
**Concern:** frontend-data

The frontend keeps no copy of the API contract. It generates its client, types, schemas, and query options from the published OpenAPI document.

[Frontend Data and State](../data.md) states the data rules; this page states the TypeScript mechanism behind them that the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md) share.

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

The client generator runs the hey-api plugins with the zod plugin, per `litespec/frontend/data#generate-the-client-from-the-published-contract`, and adds the TanStack Query plugin its stack states, per `litespec/frontend/web/data#add-the-stack-query-plugin-to-the-client-generator`. The workspace regenerates with `pnpm api:types` and edits nothing under the generated directory, per `litespec/frontend/data#regenerate-the-client-rather-than-editing-it`.

```json
{
  "plugins": ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk", "zod"]
}
```

A successful command invalidates the TanStack Query entries it changed, per `litespec/frontend/data#update-the-cache-a-command-changed`. A form validates against the generated zod schema extended with the application's own rules, per `litespec/frontend/data#validate-a-form-with-the-generated-schema`:

```ts
zRedeliverOrderRequestModel.extend({ reason: zRedeliverOrderRequestModel.shape.reason.trim().min(1, words.ui.required) })
```

A refusal renders through the shared problem alert beside the refused record or control, per `litespec/frontend/data#place-a-refusal-beside-what-was-refused`:

```tsx
<ProblemAlert error={mutation.error} describe={describe} />
```

```svelte
<ProblemAlert error={mutation.error} describe={describe} />
```

Failures map to the problem type above, per `litespec/frontend/data#map-every-failure-to-one-problem-type`. State ownership, locale dictionaries, the frame root, and the shared form entry point follow `litespec/frontend/data#assign-state-to-the-narrowest-owner`, `litespec/frontend/data#localize-copy-through-locale-dictionaries`, `litespec/frontend/data#keep-the-client-boundary-in-the-frame`, and `litespec/frontend/data#build-a-form-on-the-shared-form-library` respectively. The dictionary library and the form entry point for each stack follow `litespec/frontend/web/data#resolve-copy-through-the-stack-dictionary-library` and `litespec/frontend/web/data#build-a-web-form-on-the-shared-form-entry-point`, and each stack page names its component state and frame root in its Concepts.

### Record Field Types in TypeScript

A contract field maps to exactly one TypeScript type through the published OpenAPI document, so the generated client carries the same shape the backend publishes. The table states the mapping from the record type to the OpenAPI schema and the generated TypeScript.

| Record type | OpenAPI | TypeScript |
|:---|:---|:---|
| `text`, `email` | `type: string` | `string` |
| `integer` | `type: integer` | `number` |
| `decimal` | `type: number` | `number` |
| `money` | object with `amount` and `currency` | `{ amount: string; currency: string }` |
| `boolean` | `type: boolean` | `boolean` |
| `date` | `type: string`, `format: date` | `string` |
| `instant` | `type: string`, `format: date-time` | `string` |
| `duration` | `type: string`, `format: duration` | `string` |
| `choice` | `type: string` with the codes as enum | Union of string literals |
| `id` | `type: string`, `format: uuid` | `string` |
| `list` | `type: array` | `T[]` with `T` from the row shape |
| `file` | `type: string`, `format: binary` | The generator binary type |

A money amount travels as a string, as `litespec/backend/dotnet/domain#hold-every-monetary-amount-in-the-shared-money-object` states, so no reader parses it as binary floating point. An identifier travels as a UUID string, as `litespec/backend/dotnet/domain#keep-id-representations-aligned-at-every-boundary` states. The record constraints map to the matching OpenAPI facets, which the zod plugin turns into the generated schema a form validates against, per `litespec/frontend/data#validate-a-form-with-the-generated-schema`.

In Claimwick, the generated client reads an expense amount as a string with its currency and the handled time as an ISO string.

## Standards

### Add the Stack Query Plugin to the Client Generator

**ID:** `litespec/frontend/web/data#add-the-stack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add the query plugin its stack states: `@tanstack/react-query` on React and `@tanstack/svelte-query` on Svelte.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape on that stack.

**Example:** The library client generator adds `@tanstack/react-query` on React, and the Claimwick client generator adds `@tanstack/svelte-query` on Svelte, giving each generated read a query key.

**Reviewed for:** Does every client generator configuration add the query plugin its stack states? Check the generator configuration of each web frontend.

### Resolve Copy Through the Stack Dictionary Library

**ID:** `litespec/frontend/web/data#resolve-copy-through-the-stack-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST meet the locale-dictionary contract that the frontend data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior. The table names the library each stack uses. This realizes litespec/frontend/data#localize-copy-through-locale-dictionaries.

**Example:**

| Stack | Dictionary library |
|:---|:---|
| React | A library meeting the contract |
| Svelte | `svelte-i18n` or equal machinery |

The library product resolves its overdue-loan sentence through its dictionary library on React, and the Claimwick console renders its receipt-required sentence through `svelte-i18n` on Svelte, each in every language it ships.

**Reviewed for:** Does every copy call site use the dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites under `src`.

### Build a Web Form on the Shared Form Entry Point

**ID:** `litespec/frontend/web/data#build-a-web-form-on-the-shared-form-entry-point`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A web form MUST build on the form entry point its stack states: the form hook on React and the form creator on Svelte.

**Rationale:** One entry point per stack gives every form the same field setup and the same display for a refusal on its field. Each stack names its entry point, so every form on that stack starts from the same setup.

**Example:** The library loan request form builds on the shared form hook on React, and the Claimwick expense form builds on the shared form creator on Svelte, each with no form library of its own.

**Reviewed for:** Does every web form build on the form entry point its stack states? Check the form files under `src`.

## Conventions

None.
