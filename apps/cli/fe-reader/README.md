# LiteSpec Frontend Reader

The frontend reader reports the facts the CLI needs from Svelte and Playwright sources. It parses without executing anything, and answers each request with one JSON facts document.

## Audience

You maintain the frontend reader in `apps/cli/fe-reader/`, or you change a fact the CLI reads from a Svelte or Playwright source. After reading this page you know what the reader reports, how the CLI runs it, and how the corpus proves a change.

## What It Is

The frontend reader is a small TypeScript program that reports the facts the CLI needs from Svelte and Playwright sources:

- Use-case module marks.
- Route marks with addresses.
- The generated-client operations each route reaches.
- The full-page navigations each route takes.
- The annotations, navigation targets and assertions of browser tests.

It parses every file in syntax-only mode, never executes anything, and answers one JSON request on standard input with one JSON facts document on standard output.

## Quick Start

Run the build and the check from `apps/cli/fe-reader/`.

Build the single bundled file the CLI runs:

```bash
pnpm build
```

Check the sources in strict mode, which the bundle does not do:

```bash
pnpm check
```

Run the test suite from the repository root. The CLI test suite runs the corpus through the bundle:

```bash
dotnet test --project apps/cli/tests/LiteSpec.Cli.Tests
```

## Layout

The sources are strict TypeScript with one module per concern: protocol types, Svelte routes and modules, call collection, Playwright tests, and the cache.

The `corpus/` folder qualifies the reader: Svelte frontends with source trees, requests and expected responses covering every fact kind in the protocol, from route groups and parameter routes to proves annotations and refusal codes. The CLI test suite runs each case through the bundle and compares the response with its expected file, so the corpus stays the reader's executable contract. The corpus requests name no cache, so every case computes its facts. A real run names a cache folder and gets the same facts back.

The CLI runs the reader as `node dist/litespec-fe-reader.mjs` once per command run that builds the model. It writes the request to standard input and reads the response from standard output. Exit code zero means the response is complete; any other exit is a reader failure with one line on standard error. Parsed facts are cached per file under the request's cache folder by content hash and reader version.

The reader builds to a single bundled file with `pnpm build`, which runs esbuild over `src/main.ts` with `typescript` and `svelte` bundled inside, so consumers install nothing to run it. The bundle targets Node 20, so it runs on any Node 20 or later, the runtimes a consumer may have. `build.mjs` names the target.

## Limits

The reader never executes the code it reads. Files it cannot parse yield diagnostics in the response instead of failing the run. A new fact kind needs a corpus case with an expected response, and the CLI tests fail until the expected file matches.
