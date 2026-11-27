import { defineConfig } from "@hey-api/openapi-ts";

// The generator of the __PROJECT__ API client: the TanStack Query plugin
// gives every generated read a query key, so loads prefetch and pages read
// back through the same cache entries. Regenerate with
// pnpm --filter @__SCOPE__/api api:codegen and edit nothing under src/generated.
export default defineConfig({
  input: "./openapi/openapi.json",
  output: "src/generated",
  plugins: [
    "@hey-api/client-fetch",
    "@hey-api/typescript",
    "@hey-api/sdk",
    "zod",
    "@tanstack/svelte-query",
  ],
});
