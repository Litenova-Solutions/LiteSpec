// The docs reading order and the trails the frame draws from it: the sidebar
// tree, the breadcrumb, and the Previous / Next pager all read one order, so
// they cannot disagree about what comes next.
//
// Inside each folder, pages follow the order in which that folder's README
// lists them: the first occurrence of each list-item link to a page of the
// folder, README first; pages the README does not list come after, by title.
// Folders follow their parent README the same way, else alphabetical. A
// folder with no README keeps path order. Only list items count as order:
// prose links cite a page without placing it, so a mention in the intro text
// never moves a page ahead of the list the README declares.
import path from "node:path";
import { areaLabel, folderLabel, folderOverview, groupOf, groupOverview, groups, type Group } from "./areas.ts";
import { pageByFile, pages, pageUrl, standardUrl, type Page } from "./spec.ts";

const LIST_LINK = /^[-*]\s+\[[^\]]*\]\(\s*([^)\s]+)/;
const EXTERNAL_TARGET = /^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|\/\/)/;

/**
 * The link targets of a README's list items, in order, first occurrence of
 * each: `docs/learn/README.md` yields `why-litespec.md`,
 * `how-litespec-works.md`, and so on. Anchors are stripped; external,
 * absolute and non-Markdown targets are skipped.
 */
export function readmeLinkOrder(markdown: string): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  for (const line of markdown.split("\n")) {
    const match = LIST_LINK.exec(line);
    if (!match) continue;
    const hash = match[1].indexOf("#");
    const target = (hash < 0 ? match[1] : match[1].slice(0, hash)).trim();
    if (target === "" || !target.endsWith(".md") || target.startsWith("/") || EXTERNAL_TARGET.test(target)) continue;
    if (seen.has(target)) continue;
    seen.add(target);
    out.push(target);
  }
  return out;
}

/** The repository-relative files a README's lists link to, resolved against its folder. */
function linkedFiles(dir: string): string[] {
  const readme = pageByFile(`${dir}/README.md`);
  if (!readme) return [];
  return readmeLinkOrder(readme.markdown).map((target) => path.posix.normalize(path.posix.join(dir, target)));
}

/** The immediate subfolders of a folder that hold pages. */
function childDirs(dir: string): string[] {
  const out = new Set<string>();
  const prefix = `${dir}/`;
  for (const page of pages) {
    if (!page.file.startsWith(prefix)) continue;
    const slash = page.file.indexOf("/", prefix.length);
    if (slash >= 0) out.add(page.file.slice(0, slash));
  }
  return [...out];
}

/**
 * The pages directly in a folder, in reading order: its overview first, then
 * the pages its README lists, then the pages it does not list, by title. A
 * folder with no README keeps path order, overview first.
 */
export function pageOrder(dir: string): Page[] {
  const direct = pages.filter((page) => page.file.startsWith(`${dir}/`) && !page.file.slice(dir.length + 1).includes("/"));
  const overview = folderOverview(dir);
  const rest = direct.filter((page) => page !== overview);
  const links = linkedFiles(dir);
  if (links.length === 0 && !pageByFile(`${dir}/README.md`)) {
    rest.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0));
    return overview ? [overview, ...rest] : rest;
  }
  const rank = new Map(links.map((file, index) => [file, index] as const));
  rest.sort((a, b) => {
    const ra = rank.get(a.file);
    const rb = rank.get(b.file);
    if (ra !== undefined && rb !== undefined) return ra - rb;
    if (ra !== undefined) return -1;
    if (rb !== undefined) return 1;
    return a.title < b.title ? -1 : a.title > b.title ? 1 : a.file < b.file ? -1 : 1;
  });
  return overview ? [overview, ...rest] : rest;
}

/**
 * The immediate subfolders of a folder, in reading order: the folders its
 * README links first, by first link into each, then the folders it does not
 * link, alphabetical.
 */
export function folderOrder(dir: string): string[] {
  const subs = childDirs(dir);
  const links = linkedFiles(dir);
  if (links.length === 0) return subs.sort();
  const rank = (sub: string): number =>
    links.findIndex((file) => file === `${sub}.md` || file.startsWith(`${sub}/`));
  return subs.sort((a, b) => {
    const ra = rank(a);
    const rb = rank(b);
    if (ra >= 0 && rb >= 0) return ra - rb;
    if (ra >= 0) return -1;
    if (rb >= 0) return 1;
    return a < b ? -1 : a > b ? 1 : 0;
  });
}

/** A page that opens a subfolder of its folder, listed inside that subfolder instead. */
export function opensSubfolder(page: Page, dir: string): boolean {
  return folderOrder(dir).some((sub) => folderOverview(sub) === page);
}

/** One folder's pages in sidebar order: its overview, its pages, then its subfolders. */
function traverse(dir: string): Page[] {
  const overview = folderOverview(dir);
  const direct = pageOrder(dir).filter((page) => page !== overview && !opensSubfolder(page, dir));
  return [...(overview ? [overview] : []), ...direct, ...folderOrder(dir).flatMap(traverse)];
}

/** Every page of a group in reading order, area by area. */
export function groupPages(group: Group): Page[] {
  return group.areas.flatMap((area) => traverse(`docs/${area}`));
}

/** The overview page a group's breadcrumb and pager link to, when it is a docs page. */
export function groupHomePage(group: Group): Page | undefined {
  if (group.key === "standard") return undefined;
  return groupOverview(group) ?? pageOrder(`docs/${group.areas[0]}`)[0];
}

/** The URL a group's breadcrumb links to: its overview, or the first page of its first area. */
export function groupHomeUrl(group: Group): string | null {
  if (group.key === "standard") return standardUrl();
  const home = groupHomePage(group);
  return home ? pageUrl(home) : null;
}

export interface Crumb {
  name: string;
  url: string | null;
}

/**
 * The breadcrumb ancestors of a page: its group, its area unless the area
 * overview is the group home, and each folder below the area. Every ancestor
 * links to its overview; an ancestor whose overview is the page itself, or
 * that has no overview, carries no URL.
 */
export function crumbTrail(page: Page): Crumb[] {
  const group = groupOf(page.area);
  const home = groupHomePage(group);
  const homeUrl = groupHomeUrl(group);
  const crumbs: Crumb[] = [
    { name: group.label, url: home && home.file === page.file ? null : homeUrl },
  ];
  const areaDir = `docs/${page.area}`;
  const areaOverview = folderOverview(areaDir);
  const areaIsHome = home !== undefined && areaOverview !== undefined && home.file === areaOverview.file;
  const skipArea = areaIsHome || (group.areas.length === 1 && areaOverview === undefined);
  if (!skipArea) {
    crumbs.push({
      name: areaLabel(page.area),
      url: areaOverview && areaOverview.file !== page.file ? pageUrl(areaOverview) : null,
    });
  }
  const parts = page.file.split("/").slice(0, -1);
  for (let i = 3; i <= parts.length; i++) {
    const dir = parts.slice(0, i).join("/");
    const overview = folderOverview(dir);
    crumbs.push({
      name: folderLabel(dir).label,
      url: overview && overview.file !== page.file ? pageUrl(overview) : null,
    });
  }
  return crumbs;
}

export interface PageNeighbors {
  /** The previous page in the same group, in reading order. */
  prev: Page | null;
  /** The next page in the same group, in reading order. */
  next: Page | null;
  /** At the last page of a group, the next group's overview with that group's name. */
  nextGroup: { url: string; label: string } | null;
}

/** The Previous / Next pager of a page: within its group, stepping to the next group's overview at the end. */
export function pageNeighbors(page: Page): PageNeighbors {
  const group = groupOf(page.area);
  const order = groupPages(group);
  const at = order.findIndex((entry) => entry.file === page.file);
  const prev = at > 0 ? order[at - 1] : null;
  if (at >= 0 && at < order.length - 1) return { prev, next: order[at + 1], nextGroup: null };
  const atGroup = groups.indexOf(group);
  const next = atGroup >= 0 ? groups[atGroup + 1] : undefined;
  const url = next ? groupHomeUrl(next) : null;
  return { prev, next: null, nextGroup: next && url ? { url, label: next.label } : null };
}

/** The reading time of a page in minutes, at 220 words a minute, at least one. */
export function readingTime(page: Page): number {
  return Math.max(1, Math.round(page.markdown.split(/\s+/).length / 220));
}
