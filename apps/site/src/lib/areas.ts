// The docs groups as the site presents them: order, label, icon and the areas of
// docs/ each one holds. The sidebar, the breadcrumb and the page metadata all take
// a group's label from here, and every page is named by its own title, so one
// page has one name everywhere.
import type { IconName } from "./icons";
import { pages, type Page } from "./spec";

export interface Group {
  key: string;
  label: string;
  icon: IconName;
  /** The first folders under docs/ this group holds, such as backend. */
  areas: string[];
}

// The four groups of the docs folders, in display order. The folders stay where
// they are, because provision IDs derive from page paths; only the navigation
// groups them. docs/assets/ holds diagrams, not pages. docs/maintain/ documents
// the maintainer tool, so it sits with the other command pages in Reference.
export const groups: Group[] = [
  { key: "standard", label: "Standard", icon: "core", areas: ["core", "backend", "frontend", "quality", "workspace", "ext"] },
  { key: "reference", label: "Reference", icon: "book", areas: ["analyzers", "reference", "tools", "maintain"] },
  { key: "learn", label: "Learn", icon: "learn", areas: ["learn", "guide", "tutorial", "example"] },
  { key: "records", label: "LiteSpec Own Records", icon: "folder", areas: ["intent"] },
];

export function groupOf(area: string): Group {
  return groups.find((group) => group.areas.includes(area)) ?? { key: area, label: areaLabel(area), icon: "file", areas: [area] };
}

const AREA_LABELS: Record<string, string> = {
  core: "Core",
  backend: "Backend",
  frontend: "Frontend",
  quality: "Quality",
  workspace: "Workspace",
  ext: "Extensions",
  analyzers: "Analyzers",
  reference: "Reference",
  tools: "CLI",
  maintain: "Maintain",
  learn: "Learn",
  guide: "Guide",
  tutorial: "Tutorial",
  example: "Example",
  intent: "Research and Decisions",
};

/** The display label of one docs area, such as Backend. */
export function areaLabel(area: string): string {
  return AREA_LABELS[area] ?? area.charAt(0).toUpperCase() + area.slice(1);
}

const byFile = new Map(pages.map((page) => [page.file, page]));

/** Pages directly in a folder, in reading order. */
export const inDir = (dir: string): Page[] =>
  pages.filter((page) => page.file.startsWith(`${dir}/`) && !page.file.slice(dir.length + 1).includes("/"));

/** A folder's overview page: its stack page, its README, or the page beside the
 *  folder that carries its name, such as frontend/patterns.md for frontend/patterns/. */
export function folderOverview(dir: string): Page | undefined {
  return byFile.get(`${dir}/stack.md`) ?? byFile.get(`${dir}/README.md`) ?? byFile.get(`${dir}.md`);
}

/** A group's overview page: the README of the first of its areas that has one. */
export function groupOverview(group: Group): Page | undefined {
  for (const area of group.areas) {
    const readme = byFile.get(`docs/${area}/README.md`);
    if (readme) return readme;
  }
  return undefined;
}

/** The label of a folder below a group: a stack folder takes its stack's name. */
export function folderLabel(dir: string): { label: string; stack: boolean } {
  const stack = byFile.get(`${dir}/stack.md`);
  if (stack) return { label: stack.title.replace(/ Stack$/, ""), stack: true };
  const name = dir.slice(dir.lastIndexOf("/") + 1);
  if (name === "ext") return { label: "Extensions", stack: false };
  return { label: name.charAt(0).toUpperCase() + name.slice(1), stack: false };
}

/** The page a group's breadcrumb links to: its overview, or the first page of its first area. */
export function groupHome(group: Group): Page | undefined {
  return groupOverview(group) ?? inDir(`docs/${group.areas[0]}`)[0];
}

/** A page's name where it appears without its group around it: in the browser
 *  tab, search results and Previous / Next cards. An overview page is titled
 *  Overview, so there it carries its area's or folder's name: Backend Overview. */
export function pageName(page: Page): string {
  if (page.style !== "index" || !page.file.endsWith("/README.md")) return page.title;
  const dir = page.file.slice(0, -"/README.md".length);
  const isArea = dir.split("/").length === 2;
  const label = isArea ? areaLabel(page.area) : folderLabel(dir).label;
  return `${label} ${page.title}`;
}

/** The folders between a page's area and the page, such as docs/backend/dotnet. */
export function foldersOf(page: Page): string[] {
  const parts = page.file.split("/").slice(0, -1);
  const out: string[] = [];
  for (let i = 3; i <= parts.length; i++) out.push(parts.slice(0, i).join("/"));
  return out;
}
