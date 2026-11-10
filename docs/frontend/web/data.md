# Web Data and State

**Stack:** react, svelte
**Concern:** frontend-data

## Intent

The frontend keeps no copy of the API contract: it generates its client, types, schemas, and query options from the published OpenAPI document. The data rules live in [Frontend Data and State](../data.md); this page holds the TypeScript mechanism behind them that the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md) share. A team that applies it while writing data code keeps every application consistent with the contract and every failure recoverable by the reader.

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

The `code` field is the discriminator: the page branches on which code arrived, not on a message that rewording can change. A code this build has never seen is still handled: the page branches on the code's presence rather than on a list of known codes. The client invents codes for what the API cannot answer. `client.unreachable` names a request that received no response. `client.unauthenticated` names a refused authentication. `client.http-<status>` names a response status the client maps to no problem, with the status in place of `<status>`.

The problem carries the `traceId` the API returns for the request, or the `X-Trace-Id` response header when the body carries none. Every error the user sees shows that identifier in copyable form inside its detail, while the headline states the message, so the user can paste it to support without reading it as the message.

### TypeScript Mechanism

The generator behind `litespec/frontend/data#generate-the-client-from-the-published-contract` runs the hey-api plugins with the zod plugin, and each stack page names the TanStack Query plugin its client adds. The workspace regenerates with `pnpm api:types` and edits nothing under the generated directory, per `litespec/frontend/data#regenerate-the-client-rather-than-editing-it`.

```json
{
  "plugins": ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk", "zod"]
}
```

A successful command invalidates the TanStack Query entries its command changed, per `litespec/frontend/data#update-the-cache-a-command-changed`. A form validates against the generated zod schema extended with the application's own rules, per `litespec/frontend/data#validate-a-form-with-the-generated-schema`:

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

Failures map to the problem type above, per `litespec/frontend/data#map-every-failure-to-one-problem-type`. State ownership, locale dictionaries, the frame root, and the shared form entry point follow `litespec/frontend/data#assign-state-to-the-narrowest-owner`, `litespec/frontend/data#localize-copy-through-locale-dictionaries`, `litespec/frontend/data#keep-the-client-boundary-in-the-frame`, and `litespec/frontend/data#build-a-form-on-the-shared-form-library`; each stack page names the component state, the dictionary library, the frame root, and the form entry point for its stack.

## Standards

None.

## Conventions

None.
