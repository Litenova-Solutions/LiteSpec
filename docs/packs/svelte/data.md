# Web Data and State

**Stack:** svelte
**Concern:** frontend-data

A Svelte frontend generates its client from the published contract instead of copying it by hand. This page is for developers who wire reads, copy, and forms in a Svelte frontend. A hand copy of the contract drifts from the API, and a form on its own library drifts from every other form.

## Concepts

### How the Client Maps Failure

```ts
type Problem = {
  status: number;
  code: string;
  detail: string;
  traceId?: string;
  fields: { field: string; code: string; message: string }[];
};
```

Every failure becomes one project-owned problem with the same fields. The page branches on the `code` field, not on message text that rewording changes. An unknown code still maps through the presence of the code, not through a list of known codes.

The client invents codes for what the API leaves unanswered: `client.unreachable` for a request with no response, `client.unauthenticated` for refused authentication, and `client.http-<status>` for an unmapped status. The problem carries the `traceId` from the body or the `X-Trace-Id` header. The headline states the message and the detail shows the identifier in copyable form, so the reader pastes it to support.

### How Reads and Forms Reach the API

```json
{
  "plugins": ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk", "zod"]
}
```

```ts
zRedeliverOrderRequestModel.extend({ reason: zRedeliverOrderRequestModel.shape.reason.trim().min(1, words.ui.required) })
```

```svelte
<ProblemAlert error={mutation.error} describe={describe} />
```

The generator runs the hey-api plugins with the zod plugin against the published contract and adds the TanStack Query plugin for Svelte. The workspace regenerates with `pnpm api:types` and edits nothing under the generated directory. A successful command invalidates the entries it changed, and a form validates against the generated zod schema extended with app rules.

A refusal renders through the shared problem alert beside the refused record or control. State ownership stays with the narrowest owner, copy resolves through the locale dictionaries, and client state stays in the frame root. Every form builds on the shared form creator, so field setup and field-level refusal display match across the product.

### How the Browser Keeps the Session

Tokens stay behind the API, and the browser holds no token value. The browser calls same-origin endpoints and the session cookie travels with each call. Write calls carry the marker header beside the cookie.

A 401 answer means the session has ended. The page returns the person to sign in, with the current address kept for return after sign in. Reads retry after sign in, and writes wait for the person to resubmit.

## Standards

### Add the Stack Query Plugin to the Client Generator

**ID:** `litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add `@tanstack/svelte-query`.

**Rationale:** The plugin gives every generated read a query key. One key shape lets loads prefetch and pages read back through the same cache entries.

**Example:** The Claimwick client generator adds `@tanstack/svelte-query`, giving each generated read a query key. The expense list load and the expense page read share that key.

**Reviewed for:** Does the client generator configuration add the stated query plugin? Check the generator configuration of the Svelte frontend.

### Resolve Copy Through the Stack Dictionary Library

**ID:** `litespec/packs/svelte/data#resolve-copy-through-the-stack-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST meet the locale-dictionary contract that the frontend data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language. Fill, plural, and fallback behave the same in every language the product ships.

**Example:** The Claimwick console renders its receipt-required sentence through `svelte-i18n`, in every language it ships. Each copy call site uses fill, plural, typed keys, and a declared fallback.

**Reviewed for:** Does every copy call site use the dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites under `src`.

### Build a Web Form on the Shared Form Entry Point

**ID:** `litespec/packs/svelte/data#build-a-web-form-on-the-shared-form-entry-point`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A web form MUST build on the shared form creator.

**Rationale:** One entry point gives every form the same field setup. Refusal display on a field matches across forms because it comes from the same creator.

**Example:** The Claimwick expense form builds on the shared form creator, with no form library of its own. Its fields validate against the generated schema extended with app rules.

**Reviewed for:** Does every web form build on the stated form entry point? Check the form files under `src`.

## Conventions

None.
