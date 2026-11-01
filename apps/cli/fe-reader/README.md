# LiteSpec Frontend Reader

The frontend reader is a small TypeScript program that reports the facts the CLI needs from Svelte, React and Playwright sources: use-case module marks, route marks with addresses, the generated-client operations each route reaches, and the annotations, navigation targets and assertions of browser tests. It parses every file in syntax-only mode, never executes anything, and answers one JSON request on standard input with one JSON facts document on standard output.

The CLI runs the reader as `node dist/litespec-fe-reader.mjs` once per `model build` or `model check`, writing the request to standard input and reading the response from standard output. Exit code zero means the response is complete; any other exit is a reader failure with one line on standard error. Files the reader cannot parse yield diagnostics in the response instead of failing the run, and parsed facts are cached per file under the request's cache folder by content hash and reader version.

The reader builds to a single bundled file with `pnpm build`, which runs esbuild over `src/main.ts` for Node 20 with `typescript` and `svelte` bundled inside, so consumers install nothing to run it. The sources are strict TypeScript with one module per concern: protocol types, Svelte routes and modules, React routes, call collection, Playwright tests, and the cache.

The `corpus/` folder qualifies the reader: a Svelte frontend and a React frontend with source trees, requests and expected responses covering every fact kind in the protocol, from route groups and parameter routes to proves annotations and refusal codes. The CLI test suite runs each case through the bundle and compares the response with its expected file, so the corpus stays the reader's executable contract.
