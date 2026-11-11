# LiteSpec Book Client

The book client is the Svelte app that renders a project book. `litespec book` writes the data as script shards in `data/`, each calling `BookData.put`, and copies this app beside them as `assets/book.js` and `assets/book.css`. The app reads the shards and draws the navigation, the record page, the rail and the review bar. It holds no project data and invents no words: every label comes from the lexicon shard or the record, as the [book page](../../../docs/tools/book.md) describes.

A book opens from `file://`, where browsers block module scripts and `fetch`. So `pnpm build` runs Vite in library mode and writes one classic script and one stylesheet to `dist/`, with the Latin subsets of Geist and Geist Mono inlined. Shards load through script elements. The CLI project embeds `dist/book.js` and `dist/book.css`, and refuses to pack without them.

The stack is Svelte 5 with runes, Tailwind CSS v4 with the typography plugin, shadcn-svelte components on bits-ui, Lucide icons, TanStack Virtual for the long navigation lists, and MiniSearch for the search index. Styling is Tailwind utilities and shadcn components only. The LiteSpec palette is set once, as shadcn theme tokens in `src/app.css`.

## Layout

| Path | Holds |
|:---|:---|
| `src/lib/frame/` | The store (`book.svelte.ts`): shard loading, the lexicon, relations, review state, filters, sorting and navigation. `search.ts` builds the full-text index, `html.ts` formats links and inline text, `shard-types.ts` describes the shards, `shard-loader.ts` loads them, `text.ts` holds the small text helpers and `cn.ts` the class merge. `index.ts` is the public file other modules import from. |
| `src/lib/record/` | Shared record pieces: the icon, line, link and prose renderers per record, type and proof marks, source links, glossary linking, and the one explanation popover. |
| `src/lib/record-page/` | The record page: the header, lead, completion, field table, evidence, per-type renderers in `types/`, shared blocks in `blocks/` and `shared/`, and the prose, HTML and value helpers. |
| `src/lib/outline/` | The outline: the left navigation panel with its rows, views, filters and tools. |
| `src/lib/trace/` | The trace: the rail beside the record page and the Intent tree. |
| `src/lib/shell/` | The shell: the top bar with search, shortcuts and run widgets. |
| `src/lib/ui/` | shadcn-svelte components, added with `pnpm dlx shadcn-svelte@1.7.2 add <name>` and left as generated. |

Two behaviors work by delegation, so they reach prose the CLI renders as HTML: an element with `data-go` opens that record, and an element with `data-explain` opens the LiteSpec explanation for its key.

## Working on It

```bash
pnpm install
pnpm check     # svelte-check over the TypeScript and Svelte sources
pnpm test      # Vitest unit tests over the store and the link helpers
pnpm build     # dist/book.js and dist/book.css for the CLI to embed
```

To work against a real book, write one with the CLI and serve its data folder through the dev server:

```bash
dotnet run --project ../src/LiteSpec.Cli -- book --model <consumer>/.litespec/model/model.json --out <folder>
BOOK_DATA=<folder>/data pnpm dev
```
