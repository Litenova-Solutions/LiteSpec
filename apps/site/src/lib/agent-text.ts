// Shared plain-text builders for the agent-facing endpoints of the site.
//
// Every raw Markdown page, llms.txt index and layer bundle reads the release
// through lib/spec.ts and formats it here, so the plain-text URLs cannot
// disagree with each other. Endpoints stay thin: they pick a builder and set
// the content type.
import path from "node:path";
import { AGENT_PROTOCOL_PLACEHOLDERS, siteDefaultValues } from "./agent-protocol.ts";
import { groups } from "./areas.ts";
import { SITE_ORIGIN } from "./origin.ts";
import {
  appliedPages,
  backendStacks,
  extensionStack,
  frontendStacks,
  layerUrl,
  layers,
  manifest,
  pageByFile,
  pages,
  provisions,
  rawUrl,
  selectionIndexUrl,
  selectionKey,
  selectionLayers,
  selections,
  version,
  type Layer,
  type Page,
  type Selection,
} from "./spec.ts";

/** Join the origin with a site path from the URL helpers in spec.ts. */
export function absoluteUrl(sitePath: string): string {
  return `${SITE_ORIGIN}${sitePath}`;
}

/** Collapse Markdown to one line for llms.txt descriptions. */
export function oneLine(value: string): string {
  return value.replace(/\s+/g, " ").trim();
}

/** The first sentence of a one-line Intent, ending at the first mark followed by space or end. */
function firstSentence(value: string): string {
  const match = value.match(/^(.+?[.!?])(\s|$)/);
  return match ? match[1] : value;
}

/** The one-line description of a page: the first sentence of its intent with page links rewritten, or a fixed fallback. */
export function describe(page: Page): string {
  const first = firstSentence(oneLine(page.intent));
  if (first) return rewriteLine(first, page, path.posix.dirname(page.file));
  // Pages with no Intent, Purpose or Name section reach this fallback: the
  // glossary, the research pages and the decision records. The glossary
  // wording follows the page-style table in docs/core/authoring.md.
  if (page.style === "glossary") return "One heading per term, in alphabetical order.";
  return `The ${page.title} page of the LiteSpec ${version} release.`;
}

const EXTERNAL_TARGET = /^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|\/\/)/;

/** Resolve one Markdown link target to an absolute raw URL when it names a release page. */
function resolveTarget(target: string, page: Page, dir: string): string {
  if (target.startsWith("<") && target.endsWith(">")) {
    const inner = target.slice(1, -1);
    const resolved = resolveTarget(inner, page, dir);
    return resolved === inner ? target : `<${resolved}>`;
  }
  if (EXTERNAL_TARGET.test(target) || target.startsWith("/")) return target;
  const hashIndex = target.indexOf("#");
  const pathPart = hashIndex < 0 ? target : target.slice(0, hashIndex);
  const hash = hashIndex < 0 ? "" : target.slice(hashIndex);
  if (pathPart === "") return `${absoluteUrl(rawUrl(page))}${hash}`;
  if (!pathPart.endsWith(".md")) return target;
  const resolved = path.posix.normalize(path.posix.join(dir, pathPart));
  if (!resolved.startsWith("docs/")) return target;
  const found = pageByFile(resolved);
  if (!found) return target;
  return `${absoluteUrl(rawUrl(found))}${hash}`;
}

const INLINE_LINK = /(!?\[[^\]]*\]\(\s*)([^)\s]+)((?:\s+"[^"]*"\s*|\s+'[^']*'\s*)?\))/g;

/** Rewrite the relative page links of one line, leaving inline code untouched. */
function rewriteLine(line: string, page: Page, dir: string): string {
  return line
    .split("`")
    .map((part, index) =>
      index % 2 === 1
        ? part
        : part.replace(
            INLINE_LINK,
            (match, open: string, target: string, close: string) =>
              `${open}${resolveTarget(target, page, dir)}${close}`,
          ),
    )
    .join("`");
}

/**
 * The file's Markdown exactly as it is in docs/, with relative links between
 * pages rewritten to absolute raw URLs. Fenced code blocks keep every byte.
 */
export function rawMarkdown(page: Page): string {
  const dir = path.posix.dirname(page.file);
  const lines = page.markdown.split("\n");
  let fenced = false;
  return lines
    .map((line) => {
      if (/^\s*```/.test(line)) {
        fenced = !fenced;
        return line;
      }
      if (fenced) return line;
      return rewriteLine(line, page, dir);
    })
    .join("\n");
}

/**
 * Concatenate pages in the given order. Every page opens with a comment that
 * names its raw source URL, and pages are separated by a line holding ---.
 */
export function concatenatedMarkdown(entries: Page[]): string {
  const body = entries
    .map((entry) => `<!-- ${absoluteUrl(rawUrl(entry))} -->\n${rawMarkdown(entry).replace(/\s+$/, "")}`)
    .join("\n---\n");
  return `${body.replace(/\s+$/, "")}\n`;
}

/** The concatenated Markdown of one layer. */
export function layerText(layer: Layer): string {
  const entries = layer.files
    .map((file) => pageByFile(file))
    .filter((page): page is Page => page !== undefined);
  return concatenatedMarkdown(entries);
}

function finish(lines: string[]): string {
  return `${lines.join("\n").replace(/\n+$/, "")}\n`;
}

/** The /<version>/llms.txt index: every page grouped as the docs navigation groups it, with its raw URL. */
export function versionLlmsText(): string {
  const lines = [`# LiteSpec ${version}`, ""];
  lines.push(
    `> LiteSpec ${version} is the contract for one bounded-context business application. Every page below links its raw Markdown, described in one line.`,
    "",
    "This index lists every page of the release in docs group order. Fetch a page's raw Markdown link to read it as plain text.",
    "",
    `To read only what one project applies, start from the selection index in ${absoluteUrl("/llms.txt")} that matches the project's shape and stacks.`,
    "",
  );
  const rootIndex = pageByFile("docs/README.md");
  if (rootIndex) {
    lines.push(
      "## Start",
      "",
      `- [${rootIndex.title}](${absoluteUrl(rawUrl(rootIndex))}): ${describe(rootIndex)}`,
      "",
    );
  }
  for (const group of groups) {
    const entries = group.areas.flatMap((area) => pages.filter((page) => page.area === area));
    if (entries.length === 0) continue;
    lines.push(`## ${group.label}`, "");
    for (const page of entries) {
      lines.push(`- [${page.title}](${absoluteUrl(rawUrl(page))}): ${describe(page)}`);
    }
    lines.push("");
  }
  const records = pages.filter((page) => page.area === "intent");
  if (records.length > 0) {
    lines.push("## Research and Decisions", "");
    for (const page of records) {
      lines.push(`- [${page.title}](${absoluteUrl(rawUrl(page))}): ${describe(page)}`);
    }
    lines.push("");
  }
  return finish(lines);
}

/** "Server with dotnet and react": the human title of a selection. */
export function selectionTitle(selection: Selection): string {
  const shape = selection.shape === "server" ? "Server" : "Client-only";
  return `${shape} with ${selection.stacks.join(" and ")}`;
}

function stackRole(id: string): string {
  if (backendStacks.includes(id)) return "backend";
  if (frontendStacks.includes(id)) return "frontend";
  return "stack";
}

/** "A server project using the dotnet backend stack and the react frontend stack." */
export function selectionLine(selection: Selection): string {
  const shape = selection.shape === "server" ? "A server project" : "A client-only project";
  const stacks = selection.stacks.map((id) => `the ${id} ${stackRole(id)} stack`);
  const list = stacks.length > 1 ? `${stacks.slice(0, -1).join(", ")} and ${stacks.at(-1)}` : stacks[0];
  return `${shape} using ${list}.`;
}

/** The maintainer-only pages, listed in no selection index. */
const MAINTAINER_PAGES = new Set(["docs/core/maintaining.md"]);

/** The tool and reference pages every selection index lists after its layer groups. */
const SELECTION_TOOL_PAGES = [
  "docs/tools/rules.md",
  "docs/tools/model.md",
  "docs/tools/new.md",
  "docs/tools/skill.md",
  "docs/reference/published-urls.md",
];

/** The selection's applied pages without the maintainer-only page, in ordinal order. */
function selectionPages(selection: Selection): string[] {
  return appliedPages(selection).filter((file) => !MAINTAINER_PAGES.has(file));
}

/** Split a comma-separated provision scope from the provision index into its kinds. */
function scopeKinds(value: string): string[] {
  return value
    .split(",")
    .map((part) => part.trim())
    .filter((part) => part !== "");
}

/** Whether a provision stack scope applies to a selection: an empty scope or any applies everywhere, else one stack must match. */
function stackApplies(stack: string, stacks: string[]): boolean {
  const kinds = scopeKinds(stack);
  return kinds.length === 0 || kinds.includes("any") || kinds.some((kind) => stacks.includes(kind));
}

/**
 * The By Artifact lines of a selection: one line per artifact kind with the
 * selection pages that carry provisions for it, for the selection's stacks.
 * Kinds sort alphabetically and pages keep ordinal order. The any scope names
 * no kind, so it has no line.
 */
function artifactLines(selection: Selection): string[] {
  const covered = selectionPages(selection);
  const rank = new Map(covered.map((file, index) => [file, index] as const));
  const groups = new Map<string, Page[]>();
  for (const { provision, page } of provisions.values()) {
    if (!rank.has(page.file) || !stackApplies(provision.stack, selection.stacks)) continue;
    for (const kind of scopeKinds(provision.artifact)) {
      if (kind === "any") continue;
      const list = groups.get(kind) ?? [];
      if (!list.includes(page)) list.push(page);
      groups.set(kind, list);
    }
  }
  return [...groups.keys()].sort().map((kind) => {
    const titles = (groups.get(kind) ?? [])
      .sort((a, b) => (rank.get(a.file) ?? 0) - (rank.get(b.file) ?? 0))
      .map((page) => page.title);
    return `- ${kind}: ${titles.join(", ")}`;
  });
}

/** The /<version>/select/<key>/llms.txt index of one selection. */
export function selectionLlmsText(selection: Selection): string {
  const key = selectionKey(selection);
  const lines = [`# LiteSpec Selection: ${selectionTitle(selection)}`, ""];
  lines.push(
    `> ${selectionLine(selection)}`,
    "",
    `This index lists the pages the ${key} selection applies, grouped by layer. Each entry links the page's raw Markdown.`,
    "",
  );
  const applied = new Set(selectionPages(selection));
  for (const layerKey of selectionLayers(selection)) {
    const layer = layers().find((entry) => entry.key === layerKey);
    if (!layer) continue;
    const files = layer.files.filter((file) => applied.has(file));
    if (files.length === 0) continue;
    lines.push(`## [${layer.title}](${absoluteUrl(layerUrl(layer.key))})`, "");
    for (const file of files) {
      const page = pageByFile(file);
      if (!page) continue;
      lines.push(`- [${page.title}](${absoluteUrl(rawUrl(page))}): ${describe(page)}`);
    }
    lines.push("");
  }
  lines.push("## Tools and Reference", "");
  for (const file of SELECTION_TOOL_PAGES) {
    const page = pageByFile(file);
    if (!page) continue;
    lines.push(`- [${page.title}](${absoluteUrl(rawUrl(page))}): ${describe(page)}`);
  }
  lines.push("");
  lines.push(
    "## By Artifact",
    "",
    "Each line names the pages of this selection that carry provisions for one artifact kind. Every page also appears with its raw Markdown URL in the layer groups above.",
    "",
    ...artifactLines(selection),
    "",
  );
  lines.push("## Extensions This Selection May Add", "");
  const addable = Object.entries(manifest.extensions).filter(([id]) => {
    const owner = extensionStack(id);
    return owner === null || selection.stacks.includes(owner);
  });
  for (const [id, extension] of addable) {
    const page = pageByFile(extension.path);
    lines.push(`- [${page?.title ?? id}](${absoluteUrl(layerUrl(`ext/${id}`))})`);
  }
  lines.push("");
  if (selection.stacks.some((stack) => frontendStacks.includes(stack))) {
    lines.push("## UI Pattern Groups", "");
    for (const layer of layers().filter((entry) => entry.key.startsWith("patterns/"))) {
      lines.push(`- [${layer.title}](${absoluteUrl(layerUrl(layer.key))})`);
    }
    lines.push("");
  }
  lines.push(
    "## How to Read This",
    "",
    "Fetch the pages a task touches, not all of them. Start with the page that owns the decision, then follow its links. When a task spans a whole layer, each section heading links the layer bundle that carries the same text in fewer requests.",
    "",
  );
  return finish(lines);
}

/** The root /llms.txt index: what LiteSpec is, how to use it, every selection and layer, and the filled agent protocol block. */
export function rootLlmsText(agentProtocol: string): string {
  const defaults = siteDefaultValues();
  const lines = ["# LiteSpec", ""];
  lines.push(
    "> LiteSpec is the contract for one bounded-context business application. A project names one size, one shape, and its stacks, and the standard states every provision those choices apply.",
    "",
    "## How to Use This Site",
    "",
    "1. Pick the selection index below that matches the project's shape and stacks.",
    "2. Paste the AGENTS.md block under Agent protocol into the working context.",
    "3. Fetch only the pages a task touches, not the whole release. Every raw Markdown link below resolves without a browser.",
    "",
    "## Release",
    "",
    `- [LiteSpec ${version} index](${absoluteUrl(`/${version}/llms.txt`)}): every page of the current release with its raw Markdown URL.`,
    "",
    "## Selections",
    "",
  );
  for (const selection of selections()) {
    lines.push(
      `- [${selectionTitle(selection)}](${absoluteUrl(selectionIndexUrl(selection))}): ${selectionLine(selection)}`,
    );
  }
  lines.push("", "## Layer Bundles", "");
  for (const layer of layers()) {
    lines.push(
      `- [${layer.title}](${absoluteUrl(layerUrl(layer.key))}): ${layer.files.length} pages, concatenated in reading order.`,
    );
  }
  lines.push(
    "",
    "## Agent Protocol",
    "",
    `Paste the block below into the working context. It is the generated agent protocol region of templates/consumer/project-agents.md, filled for the default selection (size ${defaults.size}, shape ${defaults.shape}, stacks ${defaults.stacks}, extensions ${defaults.extensions}). Each placeholder takes the project's value:`,
    "",
  );
  for (const entry of AGENT_PROTOCOL_PLACEHOLDERS) {
    lines.push(`- ${entry.placeholder}: ${entry.takes}.`);
  }
  lines.push("", agentProtocol.trim(), "");
  return finish(lines);
}

