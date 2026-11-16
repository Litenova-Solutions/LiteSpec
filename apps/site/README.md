# litespec.dev

The `litespec.dev` site publishes the LiteSpec standard as HTML for readers and plain text for agents, built from the standard at the repository root.

## Audience

You maintain the `litespec.dev` site in `apps/site/`. You change its design, its agent endpoints, its routes, or its deployment. After reading this page, you can run the site, find the code behind each behavior, and make the common changes without breaking the build.

## What It Is

This is the static Astro 7 site that publishes the LiteSpec standard at `https://litespec.dev`. The site is maintainer tooling, not part of a consumer project, which is why it lives in `apps/site/` rather than in the CLI. Everything it serves comes from the standard at the repository root through one loader, `src/lib/spec.ts`. Human readers get HTML pages. Agents get `/llms.txt`, a raw `.md` copy of every page, one index per selection of shape and stacks, and one bundle per layer. The styling is hand-written CSS with no framework. Wireframes come from a TypeScript port of the C# renderer, and golden SVGs hold the port byte-identical.

The project file is `apps/site/package.json`, naming the package `@litespec/site` with its dependencies and scripts. The site belongs to the pnpm workspace rooted at the repository: the root `package.json` names the package manager and the Node engine every package uses, and the root `pnpm-workspace.yaml` names the shared versions. The TypeScript config `apps/site/tsconfig.json` extends the Astro strict config and leaves `dist/` out. The ignore file `apps/site/.gitignore` leaves `node_modules/`, `dist/`, and `.astro/` out of git.

## Quick Start

Run every command in this section from the repository root.

Install the workspace dependencies once for the site, the book client, and the frontend reader together:

```bash
pnpm install
```

Start the dev server:

```bash
pnpm --dir apps/site dev
```

Build the static output with its search index:

```bash
pnpm --dir apps/site build
```

Preview what the build produced:

```bash
pnpm --dir apps/site preview
```

Behind these scripts, `dev` runs `astro dev`, `build` runs `astro build && pagefind --site dist`, and `preview` runs `astro preview`.

Run the site tests, which exercise its pure helpers:

```bash
pnpm --dir apps/site test
```

## Layout

Config files live at the top of `apps/site/`. Source code lives under `src/lib/`, `src/components/`, `src/layouts/`, `src/pages/`, and `src/styles/`, and static files live in `public/`.

| Path | What it owns |
|:---|:---|
| `astro.config.mjs` | Site origin `https://litespec.dev`, static output, trailing slashes, directory URLs, and the `_redirects` integration that maps `/latest` to the manifest version. |
| `package.json` | Scripts and dependencies named above. |
| `tsconfig.json` | The Astro strict extension and the `dist/` exclusion. |
| `public/favicon.svg`, `public/favicon-32.png`, `public/apple-touch-icon.png`, `public/icon-192.png`, `public/icon-512.png`, `public/og.png` | Copies of the logo files in `assets/`, described in `assets/README.md`: the browser icon, the touch and app icons, and the share image. Copy them again when the logo or the headline changes. |
| `src/lib/spec.ts` | The one loader every page and endpoint reads. It finds the repository root, reads `litespec.manifest.yaml` with the `yaml` package, parses every page under `docs/`, attaches provisions from the generated `docs/reference/provisions.json` index, and computes selections, layers, and URLs. |
| `src/lib/render-markdown.ts` | The Markdown to HTML pipeline with Shiki highlighting, provision cards, and wireframe option cards. |
| `src/lib/code-blocks.ts` | The code block figure, header strip, language label, and copy button. |
| `src/lib/agent-text.ts` | The plain-text builders for every agent endpoint, with `SITE_ORIGIN` set to `https://litespec.dev`. |
| `src/lib/areas.ts` | The five docs groups, their labels and icons, one label per docs area, and the folder overview lookups. |
| `src/lib/nav.ts` | The one reading order the sidebar and the pager share, the breadcrumb trail, the group home URLs, and the reading time helper. |
| `src/lib/icons.ts` | The fixed line icon set, drawn in the style of Lucide on a 24px grid. |
| `src/lib/wireframe/parse.ts` | The wireframe language parser, a port of the CLI parser. |
| `src/lib/wireframe/render.ts` | The wireframe SVG renderer, a port of the CLI renderer. |
| `src/lib/wireframe/index.ts` | The wireframe module boundary that re-exports the parser and the renderer. |
| `src/lib/wireframe/fixtures/` | One `.wireframe` input per option with its golden `.desktop.svg` and `.phone.svg` outputs, plus `errors.json` for the failure cases. |
| `src/components/` | The Astro components named under Components. |
| `src/layouts/Base.astro` | The shared layout with the head, the frame, the header, the footer, and the copy behavior. |
| `src/pages/` | The routes named under Routes. |
| `src/styles/site.css` | The palette, the semantic tokens, the frame, the header, the footer, and the shared primitives. |
| `src/styles/docs.css` | The three-column docs layout, the sidebar tree and its phone drawer, the breadcrumb, the page meta row, the pager, the `On this page` list and its narrow-screen disclosure, and the Standard overview list. |
| `src/styles/prose.css` | The rendered page body: headings, provision cards, tables, code, and option cards. |
| `src/styles/code.css` | The highlighted code blocks and the Shiki theme selection. |
| `src/styles/select.css` | The selection page header block. The form carries its own styles in `SelectForm.astro`. |

### How It Works

#### The Loader

No module except `src/lib/spec.ts` reads the standard. It locates the repository root by walking upward from the working directory until it finds `litespec.manifest.yaml`, so a bundled build never relies on its own URL. Its exports are `root`, `manifest`, `version` (the manifest version), `pages`, `provisions`, `selections()`, `layers()`, `appliedPages()`, `selectionLayers()`, `selectionKey()`, `extensionStack()`, `backendStacks`, `frontendStacks`, `schemaFiles`, and the URL helpers `htmlPath()`, `pageUrl()`, `rawUrl()`, `layerUrl()`, `standardUrl()`, and `selectionIndexUrl()`.

The loader parses each `.md` file under `docs/` a single time. A `Page` carries its repository-relative `file`, its `path` under `docs/` without `.md`, its `style`, its `area` (the first folder under `docs/`), its `title`, its `intent` (the opening paragraph of `Intent`, `Purpose`, or `Name`), its `sections`, its `provisions`, and its raw `markdown`. The `slug()` helper reproduces the CLI heading slug, keeping heading IDs and provision IDs in agreement with the validator. The `pages` array preserves page index order, while the sidebar and pager take their order from `src/lib/nav.ts` and never from this array. Provision data arrives from the generated `docs/reference/provisions.json` index written by `litespec-maintain provisions`, one entry per provision with `id`, `heading`, owning `page`, `anchor`, `kind`, `mark`, `evidence`, and `strength`, so the site reads a single file instead of parsing provisions out of every page. Selection and layer logic mirrors the CLI layout rules: the always-applied `docs/core`, `docs/workspace`, and `docs/quality` pages, the folder chain above each selected stack with its stack-free companion folders, the shape `backendPages`, and the selected extensions.

The reading order shared by the sidebar and the pager belongs to `src/lib/nav.ts`. Inside a folder, pages appear in the order that folder's README lists them: each list-item link to a page of the folder counts at its first occurrence, the README comes first, and pages no README mentions follow by title. Folders order by their parent README the same way, or alphabetically. A folder with no README keeps path order. Ordering counts list items only, so a prose link cites a page without placing it. This file also owns the breadcrumb trail (`crumbTrail()`), the group home URLs (`groupHomeUrl()`, where the Standard group aims at the generated overview), the pager neighbors (`pageNeighbors()`, which stay within the group and step to the next group's overview at the end), and the reading time (`readingTime()`, minutes at 220 words a minute, minimum one).

#### Rendering

One standard page becomes served HTML in `src/lib/render-markdown.ts`. The module creates its Markdown processor from `@astrojs/markdown-remark` with Shiki highlighting that skips `wireframe` blocks, the Shiki themes `github-light` and `github-dark` emitted as CSS variables, and `smartypants` off. HTML post-processing then runs in this order:

1. Heading IDs from the loader slug, with the `H1` removed (the template renders the title).
2. A `toc` of `H2` entries with provision `H3` entries nested inside (option `H4` entries on pattern pages, turning their right column into an option index).
3. Relative links between docs pages rewritten to site URLs.
4. Wireframe blocks swapped for option figures.
5. Option annotations folded into their cards.
6. Option `H4` IDs moved up onto their cards.
7. Tables wrapped in `table-wrap` (adding `table-provisions` when the first column holds provision IDs).
8. Code blocks decorated.
9. Provisions wrapped in cards carrying kind, strength, and verification badges with an ID footer and a copy button.

The card body omits the `Strength` line because the badge already shows it, while an `Applies to` line appears as a sub-line under the statement. The `Stack` and `Concern` scope lines under the title leave the body and surface as the `meta` pairs in the template's meta row.

`src/lib/code-blocks.ts` wraps each Shiki-highlighted block in a `code-block` figure with a header strip that shows the language name and a copy button. `languageLabel()` supplies the labels `Shell`, `PowerShell`, `JSON`, `TypeScript`, `YAML`, `Markdown`, and the rest of the supported set. Wireframe text blocks in plain form carry no `astro-code` class and pass through unchanged.

#### Agent Text

All plain-text endpoints are formatted in `src/lib/agent-text.ts` from the same loader data, which keeps them from ever disagreeing. `rawMarkdown()` hands back the file exactly as checked in under `docs/`, rewriting relative links between pages to absolute raw URLs. `concatenatedMarkdown()` joins pages with a comment per raw source URL and a `---` line between pages. `layerText()` concatenates a single layer. `versionLlmsText()` groups all pages by docs group with their raw URLs, and links onward to the layer bundles and selection indexes. `selectionLlmsText()` enumerates the pages one selection applies, first grouped by layer, then the tool and reference pages, then a By Artifact group naming the pages whose provisions govern each file kind, then the addable extensions and UI pattern groups as bare links, and finally the reading instructions. `rootLlmsText()` enumerates the release index, all selections, all layer bundles, and the agent protocol region filled in for the default selection. The helpers `selectionTitle()` and `selectionLine()` produce strings like `Server with dotnet and react` and `A server project using the dotnet backend stack and the react frontend stack.`

#### Routes

| Route file | URL | What it writes |
|:---|:---|:---|
| `src/pages/index.astro` | `/` | The landing page: the hero with the headline and the two calls to action to the learning pages and the selection page, the `How It Works` steps with Claimwick artifacts, and the `Where to Go` entry list. |
| `src/pages/llms.txt.ts` | `/llms.txt` | The root agent index: how to use the site, the release index, every selection, every layer bundle, and the `AGENTS.md` block. Content type `text/plain`. |
| `src/pages/[version]/standard/index.astro` | `/<version>/standard/` | The generated Standard overview: the six standard areas in order, then the stack folders under backend and frontend, each with its title, its first sentence and a link. It uses the normal docs frame with the sidebar and the breadcrumb. |
| `src/pages/[version]/[...page].astro` | `/<version>/<path>/` | One HTML page per spec page: the sidebar tree (a dialog drawer with a filter on phones), the breadcrumb, the title, the meta row with the version and the scope pairs, the lead (the first sentence of the intent), the copy and Markdown tools, the rendered body, and the Previous and Next cards within the group. Pattern pages keep the same frame with the right column and add the pattern viewer. |
| `src/pages/[version]/[...page].md.ts` | `/<version>/<path>.md` | The raw Markdown of one page with links rewritten to absolute raw URLs. Content type `text/markdown`. |
| `src/pages/[version]/llms.txt.ts` | `/<version>/llms.txt` | The version index: every page grouped by area with its raw Markdown link. Content type `text/plain`. |
| `src/pages/[version]/layers/[...layer].md.ts` | `/<version>/layers/<key>.md` | One layer bundle such as `core` or `backend/dotnet`, with a comment naming each raw source URL and `---` between pages. Content type `text/markdown`. |
| `src/pages/[version]/provisions.json.ts` | `/<version>/provisions.json` | The generated provision index, `docs/reference/provisions.json`, byte for byte. Content type `application/json`, marked `noindex` in `public/_headers`. |
| `src/pages/[version]/schemas/[file].ts` | `/<version>/schemas/<file>` | A byte copy of one file from `schemas/`, such as `record.schema.yaml`. Content type `application/yaml`. Returns `Not found` with status 404 for a name outside the manifest release. |
| `src/pages/[version]/select/index.astro` | `/<version>/select/` | The project selection page with presets from `templates/presets/`, size, shape, backend and frontend stacks, extensions, and live outputs for the agent index URL, the `AGENTS.md` block, and the starter prompt, plus the list of every selection. |
| `src/pages/[version]/select/[key]/llms.txt.ts` | `/<version>/select/<key>/llms.txt` | The index of one selection such as `server.dotnet.react`. Content type `text/plain`. Returns `Not found` with status 404 for an unknown key. |

`astro.config.mjs` sets `site` to `https://litespec.dev`, `output` to `static`, `trailingSlash` to `always`, and `build.format` to `directory`. Its `litespec-latest-redirects` integration writes the `_redirects` file during `astro:build:done`, pointing `/latest` and `/latest/*` at the manifest version so Cloudflare Pages sends unversioned requests to the current release.

#### Design System

Palette and semantic tokens live in `src/styles/site.css`, and every component takes its colors from tokens alone. The palette combines warm neutrals down to charcoal `#3e3643`, an amber ramp centered on `#f8c258`, and three muted support hues (sage, slate, clay) sharing one lightness at low chroma. Litenova Solutions colors belong to the logo only: navy `#0F1B33`, paper `#F6F7FA`, and saffron `#EFA73A`, with nothing but the mark using them. Each token names a role instead of a value: `--bg`, `--outside`, `--border`, `--muted`, `--fg`, `--primary`, `--accent`, `--rule-*`, `--conv-*`, `--pat-*`, `--warn-*`, `--code-bg`, `--mark-ink`, and `--mark-symbol`. Amber marks rules, sage marks conventions, and slate marks patterns, while elsewhere amber appears only on the current item, important text, and focus.

Dark mode has two equivalent paths: the `prefers-color-scheme` media query applying to `:root` without `data-theme="light"`, and the explicit `:root[data-theme="dark"]` override setting the same values. `ThemeSwitcher.astro` steps through `auto`, `light`, and `dark`, persisting the pick in `localStorage` under `litespec-theme`. `Base.astro` consults that store before first paint, so the theme applies without a flash.

The page frame is a centered column flanked by visible side rails: `.frame` limits width to `1280px`, stretches at least to viewport height, and contains the header and footer. The header `.site-top` carries the logo, the Learn, Guides, Standard, UI Patterns, and Reference links, the search trigger, the Use with Agents button, the GitHub link, the theme switcher, and the phone menu. The footer carries the release version, the MIT license note, the Litenova Solutions link, and the `llms.txt`, GitHub, Changelog, and Research and Decisions links. The shared classes in `site.css` are `.btn` with `.btn-outline`, `.btn-ghost`, `.btn-sm`, and `.btn-icon`; `.badge` with the kind badges `.badge-rule`, `.badge-convention`, and `.badge-pattern`, the strength badges `.badge-strength-required`, `.badge-strength-default`, and `.badge-strength-advisory`, and `.badge-warn`; `.ver`, `.dot`, `.kbd`, `.must`, and `.muted`; and `.icon` with `.icon-sm`. The typefaces are Geist and Geist Mono, served from `public/fonts/` as Latin variable fonts under the SIL Open Font License (`public/fonts/OFL.txt`), with the body font preloaded.

The fixed line icon set in `src/lib/icons.ts` follows the Lucide style on a 24px grid. Each named entry stores inner SVG markup: subject names such as `start`, `core`, `folder`, `server`, `monitor`, `shield`, `plug`, `book`, and `terminal`, and UI names such as `search`, `copy`, `check`, `eye`, `code`, and `expand`. `src/components/Icon.astro` renders one entry inline. An icon stays decorative unless given a `label`, and `github` keeps its own filled path.

`src/lib/areas.ts` defines the five docs groups in display order with a label and icon per group: Learn (`learn`, `example`), Tutorial (`tutorial`), Guides (`guide`), Standard (`core`, `backend`, `frontend`, `quality`, `workspace`, `ext`), and Reference (`reference`, `tools`, `analyzers`, `maintain`). Grouping affects navigation only. The folders themselves never move. The same file maps each docs area to one display label through `areaLabel()`. The `intent` folder stands outside the groups behind a footer link, yet its pages still build at their URLs.

#### Components

| Component | What it owns |
|:---|:---|
| `src/layouts/Base.astro` | The shared layout: the head with the title from `pageTitle`, the description trimmed to 160 characters, the canonical URL (omitted on a `noindex` page), robots, theme color, Open Graph and Twitter cards with the social image and its alt text, icons, the manifest, the raw Markdown alternate link, the font preload, and one JSON-LD graph from `src/lib/seo.ts`; the theme script before first paint; the framed column with the header and the footer; and the `data-copy` click behavior that copies text and flashes `Copied`. |
| `src/components/DocsSidebar.astro` | The left navigation: the full documentation tree grouped by docs group, in the reading order of `src/lib/nav.ts`, with an area label above each area inside a multi-area group, collapsible folders, `stack` tags, and an `Overview` link per folder. The current page carries `aria-current="page"` and scrolls into view. On phones the same markup opens as a dialog drawer with a filter input. |
| `src/components/Breadcrumbs.astro` | The breadcrumb: the group, area and folders as an ordered list, each ancestor linked to its overview, with the current page as text. |
| `src/components/PageToc.astro` | The right `On this page` list of `H2` headings with their `H3` entries nested beneath (options on pattern pages), with the section in view marked, plus the `View as Markdown` and `Edit on GitHub` links. On narrow screens it renders as a collapsed disclosure above the body instead. |
| `src/components/Search.astro` | The search trigger and dialog. |
| `src/components/SelectForm.astro` | The selection form with live outputs. It renders statically with the default size `small`, shape `server`, and stacks `dotnet` and `react`, then one inline script updates the tiles and the outputs as you answer. |
| `src/components/ThemeSwitcher.astro` | The `auto`, `light`, and `dark` cycle button. |
| `src/components/Mark.astro` | The LiteSpec mark drawn inline from `assets/logo`: the frame and the L in `--mark-ink` (navy on light, paper on dark) and the three rules in `--mark-symbol` (saffron). With `name`, the horizontal lockup, which the header shows at 30px; the footer shows the mark at 24px, the smallest size the brand allows. |
| `src/components/PatternViewer.astro` | The full-screen option viewer on pattern pages. |

#### Search

Indexing happens in the build through `pagefind --site dist`, the second half of the `build` script. `Search.astro` fetches `/pagefind/pagefind.js` lazily on first use and queries through its `debouncedSearch()`. Results show at most 8 pages with excerpts and 3 sub results per page. The dialog answers `Ctrl K`, `/`, arrows, `Enter`, and `Esc`, and polite live text such as `Type to search pages and rules.` announces state. Against a dev server with no index, the dialog reads `Search is available on the built site.`

Pagefind indexes the text under `data-pagefind-body` on the `.docs-body` div in `[...page].astro`, titled by `data-pagefind-meta="title"` on its `H1`. The `data-pagefind-ignore` marker excludes interface text: provision badges, tool buttons, code block headers, option tabs, and the `lbl` spans in provision bodies.

#### The Pattern Viewer and the Wireframes

Each pattern option renders as a card with `Preview` and `Wireframe` tabs plus an expand button. `Preview` shows the desktop drawing at 1200 pixels and the phone drawing at 390 pixels, while `Wireframe` shows the wireframe text with its copy button. Tabs and the expand button start with a `hidden` attribute for the viewer script to remove, and the wireframe panel remains visible until that script hides it, so the card reads in full with JavaScript off. The script contributes arrow-key tab movement with `Home` and `End`, opens each drawing on `Enter` or `Space`, and tags the page `pattern-js` for the zoom cursor.

`PatternViewer.astro` opens those cards in a full-screen dialog. It presents the option ID and name, the `Default` badge when the head carries one, and the position in the form `3 of 12`. Its switch selects the `Desktop`, `Phone`, or `Wireframe` view, its arrows step between options, and its keyboard map uses `1`, `2`, and `3` for views with `ArrowLeft` and `ArrowRight` for options. Closing hands focus back to the opener.

The parser in `src/lib/wireframe/parse.ts` and the renderer in `src/lib/wireframe/render.ts` are ports of the CLI wireframe parser and renderer. Neither touches a Node API, so both run at build time and in the browser. The renderer turns one wireframe into an SVG sketch at `DesktopWidth` 1200 or `PhoneWidth` 390 pixels, drawn in a single neutral style with tone fills for `neutral`, `positive`, `caution`, `critical`, and `info`. It estimates text width from character counts instead of measuring fonts, and at phone width it applies the compact reflow rules together with the page's `Compact` block when one exists. `wireframe.test.ts` pins the port down: each fixture `.wireframe` must reproduce its checked-in `.desktop.svg` and `.phone.svg` byte for byte, and each `errors.json` entry must fail parsing.

### Common Changes

#### Add a Docs Page

1. Create the Markdown file under `docs/` with a lowercase name, one `H1`, and the sections its page style requires.
2. List the page in its folder `README.md` at the position you want in reading order. Sidebar and pager follow the README lists, and a page missing from every README sorts after them by title.
3. Rebuild the site. Since `[...page].astro` derives its paths from `pages`, the new page needs no route file of its own.
4. Confirm the sidebar places it within its folder order, with a `stack.md` or `README.md` file serving as the folder `Overview`, and confirm the pager reaches it.

#### Add a Site Route

1. Create the file under `src/pages/[version]/`: `src/pages/[version]/help.astro` for HTML or `src/pages/[version]/help.txt.ts` for text, with `getStaticPaths()` returning the version param.
2. Read the standard exclusively through `src/lib/spec.ts`, and format agent text exclusively through `src/lib/agent-text.ts`.
3. Declare the content type on API routes: `text/plain; charset=utf-8` for an index, `text/markdown; charset=utf-8` for Markdown.
4. Rebuild, then open the new URL with its version prefix.

#### Add an Icon

1. Append the inner SVG markup to `paths` in `src/lib/icons.ts`, staying on the 24px grid in the 2px stroke style.
2. Render it with `<Icon name="your-name" />`, adding a `label` only when the icon must carry meaning on its own.
3. When the icon labels a docs group, reference the same name from `src/lib/areas.ts`.
4. Rebuild, then inspect the icon under both color schemes.

#### Add a Docs Area

1. Create the folder under `docs/` with its pages and a `README.md` listing them in reading order.
2. Register the area in its docs group in `groups` in `src/lib/areas.ts`, in display order, and add its label to `AREA_LABELS` in the same file.
3. Rebuild, then check the sidebar group, the breadcrumb, and the pager across the new pages.

#### Change a Color

1. Add or edit the ramp and its semantic token in `src/styles/site.css`, supplying both the light value and the dark override.
2. Use the token as `var(--your-token)` in the component stylesheet.
3. Leave raw hex values out of component stylesheets entirely. `site.css` is the one file allowed to hold them.
4. Verify the result in light mode, in dark mode, and under the system theme on both.

### Deployment

Cloudflare Workers hosts the site as a static-assets-only Worker called `litespec-site` on the LiteNova Solutions account. No script executes, so requests cost nothing on the free plan. The root `wrangler.jsonc` names the Worker, serves `apps/site/dist/` with trailing-slash HTML handling and `404.html` for unknown paths, and binds the custom domain `litespec.dev`. With the `workers.dev` address switched off, search engines index a single copy. A companion Worker, `litespec-www` (configured by `wrangler.www.jsonc` with `workers/www-redirect.js`), answers `www.litespec.dev` by redirecting with a 301 to the identical path and query on `litespec.dev`.

Workers Builds ships the `litespec-site` Worker on each push to `main` and builds a Preview for every other branch, all from the repository root where `wrangler.jsonc` and `.node-version` live. The Worker declares no build command of its own: `wrangler.jsonc` points at the site build as its custom build, so plain `npx wrangler deploy` and `npx wrangler preview` install dependencies and build the site into `apps/site/dist` before uploading. Each Preview appears on a subdomain of `litespec.dev` enabled by the `previews_enabled` route. Because every page declares its `litespec.dev` address canonical, no Preview gets indexed as a duplicate site, and `workers.dev` serves neither the site nor its Previews.

For a manual deploy, build the site first and then run its deploy script:

```bash
pnpm --dir apps/site build
pnpm --dir apps/site run deploy
pnpm --dir apps/site exec wrangler deploy --config wrangler.www.jsonc   # only when the redirect Worker changes
```

The build emits `dist/_redirects` (sending `/latest` to the manifest version) and copies `public/_headers`. Those headers set the security policy (`X-Content-Type-Options: nosniff`, `Referrer-Policy`, and the rest of the `/*` block), long-lived caching for `/_astro/*` and `/fonts/*`, and `X-Robots-Tag: noindex` across the raw Markdown pages, layer bundles, and agent index files so that search results point at the HTML page. The noindex rules address the release segment through the `:version` placeholder, matching every published version without writing a release number by hand. The zone enforces Always Use HTTPS, SSL Full (strict), and TLS 1.2 or later.

### Search Engines

Metadata comes from `src/lib/seo.ts`. The `pageTitle` helper joins page title, section, and `LiteSpec`, shedding the section past 60 characters so the page title itself is never cut. `absoluteUrl` forms a canonical link on `https://litespec.dev` with a trailing slash, `metaDescription` trims a description to 160 characters at a sentence or word boundary, and `techArticle`, `breadcrumbs`, `webPage`, `website`, `organization`, and `softwareSource` assemble the schema.org objects.

Every page declares `<html lang="en">`, carries one `H1` with a unique title and description, and exposes Open Graph tags with the 1200 by 630 `public/og.png` share image (with width, height, and alt text) plus `summary_large_image` Twitter cards. Each page serializes the Organization together with its own objects into a single JSON-LD graph. Docs pages emit a TechArticle (headline, description, URL, `dateModified` from the page file's last git commit via `lastModified`, or the build date via `buildDate` when git offers no date, and `isPartOf` the WebSite) and a BreadcrumbList mirroring the visible breadcrumb, where crumbs without a link omit `item`. The landing page emits the WebSite and the SoftwareSourceCode, and the selection page a WebPage with a BreadcrumbList.

Diagrams render as inlined SVG with a title and description for assistive technology, captioned by their Markdown alt text, while icons stay `aria-hidden` unless labeled. Internal links keep trailing slashes, the Geist body font preloads with `font-display: swap`, and the head holds no script except the theme read and the JSON-LD. The landing page defines LiteSpec in its first sentence and links to the learn overview, the getting-started tutorial, the GitHub repository, and the NuGet package.

`src/pages/sitemap.xml.ts` enumerates each indexable HTML page (the landing page, the selection page, and every docs page) as an absolute URL with trailing slash and `lastmod` date. It leaves out everything that redirects or carries `noindex`: the release root, the raw Markdown copies, the layer bundles, the `llms.txt` indexes, and the 404 page. `public/robots.txt` permits everything and names the sitemap, while the 404 page itself is `noindex` with no canonical link. A short page intent picks up the page title plus a sentence about LiteSpec, giving every description enough weight for a search snippet.

### Diagrams

Diagrams are SVGs under `docs/assets/diagrams/`, referenced from their pages as Markdown images. `src/lib/diagrams.ts` inlines each one into the HTML: colors expressed as site tokens with fallbacks track the theme, and the alt text turns into the caption. The route `src/pages/[version]/assets/[...file].ts` serves the same files at `/<version>/assets/`, keeping image links in the raw Markdown copy resolvable for agents.

### Testing

Pure helpers are tested with `node --test`. `src/lib/nav.test.ts` pins the reading order, breadcrumb trail, pager, and reading time, while `spec.test.ts`, `seo.test.ts`, `trail.test.ts`, and `pins.test.ts` cover the loader, metadata, trail fence, and pinned versions. Broken pages, broken links, and a broken search index fail the build. A browser pass over the built output verifies layout, wireframes, and theme in both light and dark mode. CI runs the build in its `web` job in `.github/workflows/ci.yml`, after a single `pnpm install --frozen-lockfile` at the repository root, using the Node and pnpm versions named once by `.node-version` and the root `package.json`.

## Limits

The site renders only the release it builds from. It runs no server code of its own, migrates no data, and validates no consumer. Search exists solely on built output, since Pagefind writes its index during `build`. JavaScript only enhances the page: theme choice, copy buttons, search, the table of contents marker, the docs drawer with its filter, and the pattern viewer. Without it, every page remains complete. The `_redirects` file defines no alias besides `/latest`. Agent endpoints serve the checked-in standard as plain text. They compute no project model and draw no new wireframe.
