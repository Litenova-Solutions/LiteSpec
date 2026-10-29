// Reads the LiteSpec standard from the repository root at build time: the manifest,
// every page under docs/, its provisions, and which pages a selection applies.
// Every page and endpoint of the site reads from here, so the site and the
// release it is built from cannot disagree.
import fs from "node:fs";
import path from "node:path";
import { fromMarkdown } from "mdast-util-from-markdown";
import { gfm } from "micromark-extension-gfm";
import { gfmFromMarkdown } from "mdast-util-gfm";
import type { Heading, Paragraph, RootContent } from "mdast";
// The repository root is the nearest folder above the working directory that holds
// litespec.manifest.json. A bundled build moves this file, so its own URL cannot be used.
function findRoot(): string {
  for (let dir = process.cwd(); ; dir = path.dirname(dir)) {
    if (fs.existsSync(path.join(dir, "litespec.manifest.json"))) return dir;
    if (path.dirname(dir) === dir) throw new Error(`No litespec.manifest.json above ${process.cwd()}`);
  }
}

/** The repository root that holds the standard. */
export const root = findRoot();

const slash = (value: string) => value.replace(/\\/g, "/");

export interface Manifest {
  version: string;
  stacks: Record<string, { folder: string; requires?: string[]; pairsWith?: string[] }>;
  shapes: Record<string, { backendStacks: number; backendPages?: string[] }>;
  extensions: Record<string, { path: string; activationScope?: string; replaces?: string[]; applicableKinds?: string[] }>;
}

export const manifest: Manifest = JSON.parse(fs.readFileSync(path.join(root, "litespec.manifest.json"), "utf8"));

/** The release this build publishes, such as 2.0.0. */
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
}

export type PageStyle = "topic" | "stack" | "extension" | "pattern" | "reference" | "command" | "how-to" | "tutorial" | "explanation" | "index" | "glossary";

export interface Page {
  /** Repository-relative file path, such as docs/backend/domain.md. */
  file: string;
  /** The path under docs/ without .md, such as backend/domain. The ID page segment and the URL path. */
  path: string;
  style: PageStyle;
  /** The area: learn, core, workspace, quality, backend, frontend, ext, reference, tools, tutorial or guide. */
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

function styleOf(rel: string): PageStyle {
  const segments = rel.split("/");
  if (rel.endsWith("/README.md")) return "index";
  if (rel === "docs/reference/glossary.md") return "glossary";
  if (rel.includes("/ext/")) return "extension";
  if (rel.startsWith("docs/frontend/patterns/")) return "pattern";
  if (segments[1] === "tools" || segments[1] === "maintain") return "command";
  if (segments[1] === "reference") return "reference";
  if (segments[1] === "tutorial") return "tutorial";
  if (segments[1] === "guide") return "how-to";
  if (segments[1] === "analyzers") return segments[2] === "rules" && segments.length > 3 ? "reference" : "how-to";
  if (segments[1] === "learn" || segments[1] === "example") return "explanation";
  if (segments.at(-1) === "stack.md") return "stack";
  return "topic";
}

/** The source text of a heading after its opening hashes, trimmed. */
function headingText(node: Heading, lines: string[]): string {
  const line = lines[node.position!.start.line - 1] ?? "";
  let hashes = 0;
  while (line[hashes] === "#") hashes += 1;
  return line.slice(hashes).trim();
}

/** An H2 section title with a trailing `{#id}` dropped. */
function sectionTitle(node: Heading, lines: string[]): string {
  const raw = headingText(node, lines);
  const id = raw.indexOf("{#");
  if (id > 0 && raw.endsWith("}")) return raw.slice(0, id).trim();
  return raw;
}

/** The verification mark and evidence when a paragraph opens with a verification label. */
function verificationOf(node: Paragraph, lines: string[]): { mark: string; evidence: string } | null {
  const first = node.children[0];
  if (first === undefined || first.type !== "strong") return null;
  const label = first.children.map((child) => (child.type === "text" ? child.value : "")).join("");
  const mark = label === "Checked by:" ? "checkable" : label === "Reviewed for:" ? "guidance" : null;
  if (mark === null || first.position === undefined || node.position === undefined) return null;
  const start = node.position.start.line;
  const end = node.position.end.line;
  const head = (lines[start - 1] ?? "").slice(first.position.end.column - 1).trim();
  const rest = lines.slice(start, end).map((line) => line.trim());
  const evidence = (head === "" ? rest : [head, ...rest]).join(" ").trim();
  return { mark, evidence };
}

/** The first paragraph under an H2 section, as one line of source text. */
function sectionParagraph(children: RootContent[], lines: string[], section: string): string {
  let inside = false;
  for (const node of children) {
    if (node.type === "heading" && node.depth === 2) {
      if (inside) break;
      if (sectionTitle(node, lines) === section) inside = true;
    } else if (inside && node.type === "paragraph" && node.position !== undefined) {
      const start = node.position.start.line;
      const end = node.position.end.line;
      return lines.slice(start - 1, end).map((line) => line.trim()).join(" ");
    }
  }
  return "";
}

/** Parses one page from its Markdown source. Exported for the parser tests. */
export function parsePageContent(rel: string, markdown: string): Page {
  const text = markdown.replace(/\r\n/g, "\n");
  const lines = text.split("\n");
  const children = fromMarkdown(text, { extensions: [gfm()], mdastExtensions: [gfmFromMarkdown()] }).children;
  const pagePath = rel.slice("docs/".length, -".md".length);
  const style = styleOf(rel);
  const sections: string[] = [];
  const provisions: Provision[] = [];
  let title: string | null = null;
  let section = "";
  let current: Provision | null = null;

  for (const node of children) {
    if (node.type === "heading") {
      if (node.depth === 1) {
        title ??= headingText(node, lines);
      } else if (node.depth === 2) {
        section = sectionTitle(node, lines);
        sections.push(section);
        current = null;
      } else if (node.depth === 3) {
        const kind: ProvisionKind | null = section === "Standards" ? "rule" : section === "Conventions" ? "convention" : section === "Patterns" && style === "pattern" ? "pattern" : null;
        if (kind && style !== "index" && !["reference", "command", "tutorial", "how-to"].includes(style)) {
          const heading = headingText(node, lines);
          const s = slug(heading);
          current = { id: `litespec/${pagePath}#${s}`, slug: s, heading, kind, mark: "", evidence: "" };
          provisions.push(current);
        } else {
          current = null;
        }
      }
      continue;
    }
    // The verification paragraph closes the provision: its label names the mark.
    if (node.type === "paragraph" && current !== null) {
      const verification = verificationOf(node, lines);
      if (verification) {
        current.mark = verification.mark;
        current.evidence = verification.evidence;
      }
    }
  }

  return {
    file: rel,
    path: pagePath,
    style,
    area: pagePath.split("/")[0],
    title: title ?? pagePath,
    intent: sectionParagraph(children, lines, "Intent") || sectionParagraph(children, lines, "Purpose") || sectionParagraph(children, lines, "Name"),
    sections,
    provisions,
    markdown: text,
  };
}

function parsePage(rel: string): Page {
  return parsePageContent(rel, fs.readFileSync(path.join(root, rel), "utf8"));
}

function walk(dir: string): string[] {
  const out: string[] = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true }).sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0))) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (entry.name.endsWith(".md")) out.push(slash(path.relative(root, full)));
  }
  return out;
}

// The learn/ and example/ pages read in the order their index lists them; every
// other page sorts by path.
function readingOrder(all: Page[]): Page[] {
  let out = all;
  for (const area of ["learn", "example"]) {
    const index = out.find((page) => page.file === `docs/${area}/README.md`);
    if (!index) continue;
    const listed = [...index.markdown.matchAll(/\]\(([a-z0-9-]+)\.md\)/g)].map((match) => `docs/${area}/${match[1]}.md`);
    const rank = (page: Page) => (page.file === index.file ? -1 : listed.indexOf(page.file) < 0 ? listed.length : listed.indexOf(page.file));
    const ordered = out.filter((page) => page.area === area).sort((x, y) => rank(x) - rank(y));
    const first = out.findIndex((page) => page.area === area);
    const rest = out.filter((page) => page.area !== area);
    out = [...rest.slice(0, first), ...ordered, ...rest.slice(first)];
  }
  return out;
}

/** Every page under docs/: the learn/ and example/ pages in reading order, the rest in ordinal path order. */
export const pages: Page[] = readingOrder(walk(path.join(root, "docs")).map(parsePage));

const byFile = new Map(pages.map((page) => [page.file, page]));

/** The page for a repository-relative file path, such as docs/core/scope.md. */
export function pageByFile(file: string): Page | undefined {
  return byFile.get(file);
}

/** Every provision of the release, keyed by ID. */
export const provisions = new Map(pages.flatMap((page) => page.provisions.map((provision) => [provision.id, { provision, page }] as const)));

// ---- Selection: which pages a project applies (Layout.AppliedPages in the CLI) ----

export interface Selection {
  /** The project size. It decides record types, not pages, so selection URLs leave it out. */
  size?: "small" | "large";
  shape: string;
  stacks: string[];
  extensions: string[];
}

const kindOf = (folder: string) => (folder === "docs/backend" || folder.startsWith("docs/backend/") ? "backend" : folder === "docs/frontend" || folder.startsWith("docs/frontend/") ? "frontend" : "");

function holdsStack(dir: string): boolean {
  if (fs.existsSync(path.join(dir, "stack.md"))) return true;
  return fs.readdirSync(dir, { withFileTypes: true }).some((entry) => entry.isDirectory() && entry.name !== "ext" && holdsStack(path.join(dir, entry.name)));
}

/** Subfolders, at any depth, that apply with a folder: not ext/ and holding no stack. */
export function companionFolders(folder: string): string[] {
  const dir = path.join(root, folder);
  if (!fs.existsSync(dir)) return [];
  const out: string[] = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true }).filter((e) => e.isDirectory()).sort((a, b) => (a.name < b.name ? -1 : 1))) {
    if (entry.name === "ext" || holdsStack(path.join(dir, entry.name))) continue;
    const rel = `${folder}/${entry.name}`;
    out.push(rel, ...companionFolders(rel));
  }
  return out;
}

function chain(folder: string, area: string): string[] {
  const out: string[] = [];
  let current = folder.replace(/\/+$/, "");
  while (current === area || current.startsWith(area + "/")) {
    out.push(current);
    if (current === area) break;
    current = current.slice(0, current.lastIndexOf("/"));
  }
  return out;
}

const directPages = (folder: string) => pages.filter((page) => path.posix.dirname(page.file) === folder && page.style !== "index").map((page) => page.file);

/** The repository-relative pages a selection applies, in ordinal order. */
export function appliedPages(selection: Selection): string[] {
  const applied = new Set<string>();
  for (const area of ["docs/core", "docs/workspace", "docs/quality"]) directPages(area).forEach((file) => applied.add(file));
  for (const id of selection.stacks) {
    const stack = manifest.stacks[id];
    if (!stack) continue;
    const kind = kindOf(stack.folder);
    if (!kind) continue;
    for (const folder of chain(stack.folder, `docs/${kind}`)) {
      for (const f of [folder, ...companionFolders(folder)]) directPages(f).forEach((file) => applied.add(file));
    }
  }
  for (const file of manifest.shapes[selection.shape]?.backendPages ?? []) applied.add(file);
  for (const id of selection.extensions) {
    const extension = manifest.extensions[id];
    if (extension) applied.add(extension.path);
  }
  return [...applied].sort();
}

/** The backend and frontend stack ids the manifest ships. */
export const backendStacks = Object.keys(manifest.stacks).filter((id) => kindOf(manifest.stacks[id].folder) === "backend");
export const frontendStacks = Object.keys(manifest.stacks).filter((id) => kindOf(manifest.stacks[id].folder) === "frontend");

/** The stack folder an extension supports, or null when it supports every stack. */
export function extensionStack(id: string): string | null {
  const file = manifest.extensions[id]?.path ?? "";
  const owner = Object.entries(manifest.stacks).find(([, stack]) => file.startsWith(stack.folder + "/ext/"));
  return owner ? owner[0] : null;
}

/**
 * Every selection the site publishes an index for: each shape, one
 * backend stack for a server project or none for a client-only one, and zero or
 * one frontend stack. Extensions are separate lines in the AGENTS.md block, so
 * they do not multiply the selections.
 */
export function selections(): Selection[] {
  const out: Selection[] = [];
  {
    for (const [shape, def] of Object.entries(manifest.shapes)) {
      const backends = def.backendStacks > 0 ? backendStacks : [null];
      for (const backend of backends) {
        for (const frontend of [null, ...frontendStacks]) {
          const stacks = [backend, frontend].filter((s): s is string => s !== null);
          if (stacks.length === 0) continue;
          if (shape !== "server" && frontend === null) continue;
          const ok = stacks.every((id) => {
            const stack = manifest.stacks[id];
            return (stack.requires ?? []).every((r) => stacks.includes(r)) && (!backend || !stack.pairsWith || stack.pairsWith.includes(backend));
          });
          if (ok) out.push({ shape, stacks, extensions: [] });
        }
      }
    }
  }
  return out;
}

/** The URL key of a selection, such as server.dotnet.react. */
export const selectionKey = (selection: Selection) => [selection.shape, ...selection.stacks].join(".");

// ---- URLs ----

/** The HTML URL of a page: /2.0.0/backend/domain/ */
/** A page's path in HTML URLs: a folder index is served at its folder, so learn/README is learn. */
export const htmlPath = (page: Page | string) => (typeof page === "string" ? page : page.path).replace(/(^|\/)README$/, "");

export const pageUrl = (page: Page | string) => `/${version}/${htmlPath(page)}/`.replace(/\/\/$/, "/");

/** The raw Markdown URL of a page: /2.0.0/backend/domain.md */
export const rawUrl = (page: Page | string) => `/${version}/${typeof page === "string" ? page : page.path}.md`;

// ---- Layers: the bundles a selection is made of ----

export interface Layer {
  /** The URL key, such as core, backend/dotnet or ext/outbox. */
  key: string;
  title: string;
  /** Repository-relative pages in the layer, in ordinal order. */
  files: string[];
}

const stackLayerTitle = (id: string) => `${id[0].toUpperCase()}${id.slice(1)} Stack`;

/**
 * Every layer the site publishes. A layer is one folder's pages: the always-applied
 * areas together, each principle folder, each stack folder, each extension and each
 * UI pattern group. Pattern groups are separate because they are large and only UI
 * work needs them.
 */
export function layers(): Layer[] {
  const out: Layer[] = [
    { key: "core", title: "Core, Workspace and Quality", files: ["docs/core", "docs/workspace", "docs/quality"].flatMap(directPages) },
    { key: "backend", title: "Backend Principles", files: directPages("docs/backend") },
    { key: "frontend", title: "Frontend Principles", files: directPages("docs/frontend") },
  ];
  const groups = new Set<string>();
  for (const [id, stack] of Object.entries(manifest.stacks)) {
    // The folders between the area root and the stack folder, such as docs/frontend/web.
    for (const folder of chain(stack.folder, `docs/${kindOf(stack.folder)}`).reverse().slice(1, -1)) {
      const key = folder.slice("docs/".length);
      if (key === "backend" || key === "frontend" || groups.has(key)) continue;
      groups.add(key);
      out.push({ key, title: `${key.split("/").at(-1)!.replace(/^./, (c) => c.toUpperCase())} Shared Pages`, files: directPages(folder) });
    }
    out.push({ key: stack.folder.slice("docs/".length), title: stackLayerTitle(id), files: [stack.folder, ...companionFolders(stack.folder)].flatMap(directPages) });
  }
  for (const page of pages.filter((p) => p.style === "pattern")) {
    out.push({ key: `patterns/${page.path.split("/").at(-1)}`, title: page.title, files: [page.file] });
  }
  for (const [id, extension] of Object.entries(manifest.extensions)) {
    out.push({ key: `ext/${id}`, title: pageByFile(extension.path)?.title ?? id, files: [extension.path] });
  }
  return out.filter((layer) => layer.files.length > 0);
}

/** The layer keys a selection applies, without extensions and pattern groups, in reading order. */
export function selectionLayers(selection: Selection): string[] {
  const applied = new Set(appliedPages(selection));
  return layers()
    .filter((layer) => !layer.key.startsWith("ext/") && !layer.key.startsWith("patterns/"))
    .filter((layer) => layer.files.some((file) => applied.has(file)))
    .map((layer) => layer.key);
}

/** The bundle URL of a layer: /2.0.0/layers/backend/dotnet.md */
export const layerUrl = (key: string) => `/${version}/layers/${key}.md`;

/** The index URL of a selection: /2.0.0/select/server.dotnet.react/llms.txt */
export const selectionIndexUrl = (selection: Selection | string) => `/${version}/select/${typeof selection === "string" ? selection : selectionKey(selection)}/llms.txt`;

/** The schema files the release ships, from schemas/. */
export const schemaFiles = fs.readdirSync(path.join(root, "schemas")).filter((file) => file.endsWith(".json")).sort();
