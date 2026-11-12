# LiteSpec Book Client

The book client is the Svelte app that renders a project book. `litespec book` writes the data as script shards in `data/`, each calling `BookData.put`, and copies this app beside them as `assets/book.js` and `assets/book.css`. The app reads the shards and draws the outline, the record page and the trace beside it. It holds no project data and invents no words: every label comes from the lexicon shard or the record, as the [book page](../../../docs/tools/book.md) describes.

A book opens from `file://`, where browsers block module scripts and `fetch`. So `pnpm build` runs Vite in library mode and writes one classic script and one stylesheet to `dist/`, with the Latin subsets of Geist and Geist Mono inlined. Shards load through script elements. The CLI project embeds `dist/book.js` and `dist/book.css`, and refuses to pack without them.

The stack is Svelte 5 with runes, Tailwind CSS v4 with the typography plugin, shadcn-svelte components on bits-ui, Lucide icons, TanStack Virtual for the long navigation lists, and MiniSearch for the search index. Styling is Tailwind utilities and shadcn components only. The LiteSpec palette is set once, as shadcn theme tokens in `src/app.css`.

## Layout

| Path | Holds |
|:---|:---|
| `src/lib/frame/` | The book state and everything that reads the shards. `book.svelte.ts` holds the state and delegates to `lexicon.ts`, `relations.ts`, `completion.ts` and `filters.ts`. `shard-loader.ts` loads the shards and `shard-schema.ts` parses each one once; `shard-types.ts` describes them with the closed sets the CLI publishes. `proof-order.ts` holds the one proof order and its labels, `search.ts` the full-text index, `html.ts` links and inline text, `text.ts` the small text helpers and `cn.ts` the class merge. `theme.svelte.ts` and `preferences.svelte.ts` are the only code that touches browser storage, and `clipboard.ts` copies text. `index.ts` is the public file other modules import from. |
| `src/lib/record/` | Shared record pieces: the icon, line, link and prose renderers per record, type and proof marks, source links, glossary linking, and the one explanation popover. |
| `src/lib/record-page/` | The record page: the header, lead, completion, field table, evidence, per-type renderers in `types/`, shared blocks in `blocks/` and `shared/`, and the prose, HTML and value helpers. |
| `src/lib/outline/` | The outline: the left navigation panel with its rows, views, filters and tools. |
| `src/lib/trace/` | The trace: the rail beside the record page and the Intent tree. |
| `src/lib/shell/` | The shell: the top bar with search, shortcuts and run widgets, and `record-navigation.ts`, which opens records from `data-go` links, keeps the address and history, and sets the page title. |
| `src/lib/ui/` | shadcn-svelte components, added with `pnpm dlx shadcn-svelte@1.7.2 add <name>` and left as generated. |

Modules import downward only: `record-page`, `outline`, `trace` and `shell` reach `record`, `frame` and `ui`; `record` reaches `frame` and `ui`; `frame` reaches none. Another module is imported only through its `index.ts`, which exports only what another module, the App or a test uses. `src/architecture.test.ts` checks these rules and that no source uses `any`, a non-null assertion, a cast other than to a DOM type or `as const`, or browser storage outside the frame.

Two behaviors work by delegation, so they reach prose the CLI renders as HTML: an element with `data-go` opens that record, and an element with `data-explain` opens the LiteSpec explanation for its key.

## Working on It

```bash
pnpm install
pnpm check     # svelte-check over the TypeScript and Svelte sources; fails on warnings
pnpm test      # Vitest: the store, the parsers, the renderers and the structure rules
pnpm build     # dist/book.js and dist/book.css for the CLI to embed
```

To work against a real book, write one with the CLI and serve its data folder through the dev server:

```bash
dotnet run --project ../src/LiteSpec.Cli -- book --root <consumer> --out <folder>
BOOK_DATA=<folder>/data pnpm dev
```
