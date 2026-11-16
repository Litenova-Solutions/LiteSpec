# LiteSpec Book Client

The book client is the Svelte app that renders a project book. It reads data shards and draws the outline, the record page and the trace beside it.

`litespec book` serves the data as script shards in `data/`, each calling `BookData.put`, beside this app as `assets/book.js` and `assets/book.css`. It serves them live from memory on localhost, or writes them to a folder with `--out`.

The app holds no project data and invents no words: every label comes from the lexicon shard or the record, as the [book command page](../../../docs/tools/book.md) describes.

A book opens from `file://`, where browsers block module scripts and `fetch`. So `pnpm build` runs Vite in library mode and writes one classic script and one stylesheet to `dist/`, with the Latin subsets of IBM Plex Sans and IBM Plex Mono inlined, and shards load through script elements.

A live book on localhost reads the same files and listens for the server-sent `events` stream, which reports each rebuild.

The CLI project embeds `dist/book.js` and `dist/book.css`, and refuses to pack without them.

The stack is Svelte 5 with runes, Tailwind CSS v4 with the typography plugin, shadcn-svelte components on bits-ui, Lucide icons, TanStack Virtual for the long navigation lists, and MiniSearch for the search index. Styling is Tailwind utilities and shadcn components only. The look follows IBM Carbon: its White and G100 palettes are set once in `src/app.css`, as Tailwind colors chosen by the reader's color scheme through `light-dark()`, with the shadcn theme tokens mapped onto them.

## Layout

| Path | Holds |
|:---|:---|
| `src/lib/frame/` | The book state and everything that reads the shards. `book.svelte.ts` holds the state and delegates to `lexicon.ts`, `relations.ts`, `completion.ts` and `filters.ts`. `shard-loader.ts` loads the shards and `shard-schema.ts` parses each one once; `shard-types.ts` describes them with the closed sets the CLI publishes. `proof-order.ts` holds the one proof order and its labels, `search.ts` the full-text index, `html.ts` links and inline text, `text.ts` the small text helpers and `cn.ts` the class merge. `clipboard.ts` copies text. `index.ts` is the public file other modules import from. |
| `src/lib/record/` | Shared record pieces: the icon, line, link and prose renderers per record, the kind badge with its short code per record type (`record-codes.ts`), proof marks, source links, glossary linking, the one explanation tooltip, and the list of page sections the rail reads. |
| `src/lib/record-page/` | The record page: the header, lead, completion, field table, evidence, per-type renderers in `types/`, shared blocks in `blocks/` and `shared/`, and the prose, HTML and value helpers. |
| `src/lib/outline/` | The outline: the left navigation panel with its rows, views, filters and tools. |
| `src/lib/trace/` | The trace: the rail beside the record page, with the cross-layer Trace, Completion and On this page, and the Intent tree. |
| `src/lib/shell/` | The shell: the top bar with search, the completion meter and the menus, the About this book page, the keyboard handling, and `record-navigation.ts`, which opens records from `data-go` links, keeps the address and history, and sets the page title. |
| `src/lib/wireframe/` | The wireframe renderer: the parser and the desktop SVG renderer for the wireframe language, drawn through the `Wireframe` figure. |
| `src/lib/ui/` | shadcn-svelte components, added with `pnpm dlx shadcn-svelte@1.7.2 add <name>` and left as generated. |

Modules import downward only:

- `record-page`, `outline`, `trace` and `shell` reach `record`, `frame` and `ui`.
- `record-page` also reaches `wireframe`.
- `record` reaches `frame` and `ui`.
- `frame` and `wireframe` reach none.

Another module is imported only through its `index.ts`, which exports only what another module or the App uses. No source uses `any`, a non-null assertion, a cast other than to a DOM type or `as const`, or browser storage.

Two behaviors work by delegation, so they apply to prose the CLI renders as HTML: an element with `data-go` opens that record, and an element with `data-explain` shows the LiteSpec explanation for its key on hover or focus.

## Working on It

The book client has no unit tests. A change is checked by type checking, by building, and by opening a real book in a browser and comparing it with the design.

```bash
pnpm install
pnpm check     # svelte-check over the TypeScript and Svelte sources; fails on warnings
pnpm build     # dist/book.js and dist/book.css for the CLI to embed
```

To work against a real book, serve one with the CLI and point the dev server at its data:

```bash
dotnet run --project ../src/LiteSpec.Cli -- book --root <consumer>
```

The live book listens on localhost port 7420 by default.

To work from a written folder instead, write it with `--out <folder>` and serve its data through the dev server with `BOOK_DATA=<folder>/data pnpm dev`.
