// Reads the LiteSpec standard from the repository root at build time. The page
// index, which litespec-maintain writes from the CLI's Layout, holds every page's
// style, title, intent and sections, the stacks and extensions the manifest ships,
// the layers, and each selection's applied pages and layers. This file reads that
// index and attaches provisions and Markdown. It computes none of the layout, so the
// site and the CLI cannot disagree about which pages a selection applies.
import fs from "node:fs";
import path from "node:path";
import { parse } from "yaml";

// The repository root is the nearest folder above the working directory that holds
// litespec.manifest.yaml. A bundled build moves this file, so its own URL cannot be used.
function findRoot(): string {
  for (let dir = process.cwd(); ; dir = path.dirname(dir)) {
    if (fs.existsSync(path.join(dir, "litespec.manifest.yaml"))) return dir;
    if (path.dirname(dir) === dir) throw new Error(`No litespec.manifest.yaml above ${process.cwd()}`);
  }
}

/** The repository root that holds the standard. */
export const root = findRoot();

export interface Manifest {
  version: string;
  stacks: Record<string, { folder: string; requires?: string[]; pairsWith?: string[] }>;
  shapes: Record<string, { backendStacks: number; backendPages?: string[] }>;
  extensions: Record<string, { path: string; activationScope?: string; replaces?: string[]; applicableKinds?: string[] }>;
}

export const manifest: Manifest = parse(fs.readFileSync(path.join(root, "litespec.manifest.yaml"), "utf8"));

/** The release this build publishes, such as 6.0.0. */
export const version = manifest.version;

/** The heading slug LiteSpec uses for provision IDs and anchors (Provisions.HeadingSlug in the CLI). */
export function slug(heading: string): string {
  return heading
    .replace(/\s+\{#[^}]+\}\s*$/, "")
    .toLowerCase()
    .replace(/`/g, "")
    .replace(/[^a-z0-9\s-]/g, "")
    .trim()
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-");
}

export type ProvisionKind = "rule" | "convention" | "pattern";

export interface Provision {
  /** litespec/<page>#<slug> */
  id: string;
  slug: string;
  heading: string;
  kind: ProvisionKind;
  /** The inline verification mark: checkable for a Checked by line, guidance for a Reviewed for line. */
  mark: string;
  /** The inline verification evidence: the text after the verification label. */
  evidence: string;
  /** How binding the provision is: required, default or advisory. Empty when the index states none. */
  strength: string;
  /** The comma-separated artifact kinds the provision applies to, such as endpoint or record:use-case. Empty when the index states none. */
  artifact: string;
  /** The comma-separated stacks the provision applies to, or any. Empty when the index states none. */
  stack: string;
}

export type PageStyle = "standard" | "stack" | "extension" | "pattern" | "reference" | "command" | "how-to" | "tutorial" | "explanation" | "index" | "glossary" | "research" | "decision";

export interface Page {
  /** Repository-relative file path, such as docs/backend/domain.md. */
  file: string;
  /** The path under docs/ without .md, such as backend/domain. The ID page segment and the URL path. */
  path: string;
  style: PageStyle;
  /** The first folder under docs/, such as backend. */
  area: string;
  title: string;
  /** The first paragraph of the Intent or Purpose section, as Markdown. */
  intent: string;
  /** The H2 section names, in order. */
  sections: string[];
  provisions: Provision[];
  /** The raw Markdown file. */
  markdown: string;
}

/** A selection the site publishes, with the pages and layers it applies. */
export interface Selection {
  /** The URL key: the shape and the stacks joined with dots, such as server.dotnet.react. */
  key: string;
  shape: string;
  stacks: string[];
  /** Repository-relative pages the selection applies, in ordinal order. */
  pages: string[];
  /** The keys of the layers the selection applies, in reading order, without extensions and pattern groups. */
  layers: string[];
}

/** A layer: the bundle of one folder's pages, one extension or one UI pattern group. */
export interface Layer {
  /** The URL key, such as core, backend/dotnet or ext/outbox. */
  key: string;
  title: string;
  /** Repository-relative pages in the layer, in reading order. */
  files: string[];
}

interface StackEntry {
  id: string;
  folder: string;
  kind: "backend" | "frontend" | "";
}

interface ExtensionEntry {
  id: string;
  path: string;
  /** The stack the extension supports, or null when it supports every stack. */
  stack: string | null;
}

/** The generated page index, written by litespec-maintain provisions from the CLI's Layout. */
interface PageIndexFile {
  version: string;
  pages: Omit<Page, "provisions" | "markdown">[];
  stacks: StackEntry[];
  extensions: ExtensionEntry[];
  layers: Layer[];
  selections: Selection[];
}

const pageIndex: PageIndexFile = (() => {
  const file = path.join(root, "docs/reference/pages.json");
  if (!fs.existsSync(file)) throw new Error(`docs/reference/pages.json is missing. Run: dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions`);
  return JSON.parse(fs.readFileSync(file, "utf8")) as PageIndexFile;
})();

export interface ProvisionIndexEntry {
  id: string;
  heading: string;
  /** The owning page, relative to the repository root, such as docs/backend/api.md. */
  page: string;
  /** The heading slug the index links to. */
  anchor: string;
  kind: string;
  mark?: string;
  evidence?: string;
  strength?: string;
  artifact?: string;
  stack?: string;
}

interface ProvisionIndexFile {
  version: string;
  provisions: ProvisionIndexEntry[];
}

// The generated provision index, by owning page file. litespec-maintain
// provisions writes it from the standards pages, so the site reads one file
// instead of parsing every page for its provisions.
function loadProvisions(): Map<string, Provision[]> {
  const file = path.join(root, "docs/reference/provisions.json");
  if (!fs.existsSync(file)) throw new Error(`docs/reference/provisions.json is missing. Run: dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions`);
  const parsed = JSON.parse(fs.readFileSync(file, "utf8")) as ProvisionIndexFile;
  const out = new Map<string, Provision[]>();
  for (const entry of parsed.provisions ?? []) {
    const list = out.get(entry.page) ?? [];
    list.push({
      id: entry.id,
      slug: entry.anchor,
      heading: entry.heading,
      kind: entry.kind as ProvisionKind,
      mark: entry.mark ?? "",
      evidence: entry.evidence ?? "",
      strength: entry.strength ?? "",
      artifact: entry.artifact ?? "",
      stack: entry.stack ?? "",
    });
    out.set(entry.page, list);
  }
  return out;
}

const provisionsByPage = loadProvisions();

/** Every page under docs/, in page index order. The sidebar and pager order comes from lib/nav.ts, never from this array. */
export const pages: Page[] = pageIndex.pages.map((entry) => ({
  ...entry,
  provisions: provisionsByPage.get(entry.file) ?? [],
  markdown: fs.readFileSync(path.join(root, entry.file), "utf8").replace(/\r\n/g, "\n"),
}));

const byFile = new Map(pages.map((page) => [page.file, page]));

/** The page for a repository-relative file path, such as docs/core/scope.md. */
export function pageByFile(file: string): Page | undefined {
  return byFile.get(file);
}

/** Every provision of the release, keyed by ID. */
export const provisions = new Map(pages.flatMap((page) => page.provisions.map((provision) => [provision.id, { provision, page }] as const)));

// ---- Selection: the pages, layers and stacks the page index publishes ----

/** The stack ids of the backend kind the manifest ships, in manifest order. */
export const backendStacks = pageIndex.stacks.filter((stack) => stack.kind === "backend").map((stack) => stack.id);

/** The stack ids of the frontend kind the manifest ships, in manifest order. */
export const frontendStacks = pageIndex.stacks.filter((stack) => stack.kind === "frontend").map((stack) => stack.id);

/** The stack folder an extension supports, or null when it supports every stack. */
export function extensionStack(id: string): string | null {
  return pageIndex.extensions.find((extension) => extension.id === id)?.stack ?? null;
}

/** Every selection the site publishes an index for. Extensions are separate lines in the AGENTS.md block, so they do not multiply the selections. */
export function selections(): Selection[] {
  return pageIndex.selections;
}

/** The URL key of a selection, such as server.dotnet.react. */
export const selectionKey = (selection: Selection) => selection.key;

/** The repository-relative pages a selection applies, in ordinal order. */
export const appliedPages = (selection: Selection) => selection.pages;

/** The layer keys a selection applies, without extensions and pattern groups, in reading order. */
export const selectionLayers = (selection: Selection) => selection.layers;

// ---- URLs ----

/** A page's path in HTML URLs: a folder index is served at its folder, so learn/README is learn. */
export const htmlPath = (page: Page | string) => (typeof page === "string" ? page : page.path).replace(/(^|\/)README$/, "");

/** The HTML URL of a page: /6.0.0/backend/domain/ */
export const pageUrl = (page: Page | string) => `/${version}/${htmlPath(page)}/`.replace(/\/\/$/, "/");

/** The raw Markdown URL of a page: /6.0.0/backend/domain.md */
export const rawUrl = (page: Page | string) => `/${version}/${typeof page === "string" ? page : page.path}.md`;

/** The generated Standard overview URL: /6.0.0/standard/ */
export const standardUrl = () => `/${version}/standard/`;

/** Every layer the site publishes, in the order the page index gives them. */
export function layers(): Layer[] {
  return pageIndex.layers;
}

/** The bundle URL of a layer: /6.0.0/layers/backend/dotnet.md */
export const layerUrl = (key: string) => `/${version}/layers/${key}.md`;

/** The index URL of a selection: /6.0.0/select/server.dotnet.react/llms.txt */
export const selectionIndexUrl = (selection: Selection | string) => `/${version}/select/${typeof selection === "string" ? selection : selectionKey(selection)}/llms.txt`;

/** The schema files the release ships, from schemas/. */
export const schemaFiles = fs.readdirSync(path.join(root, "schemas")).filter((file) => file.endsWith(".schema.yaml")).sort();
