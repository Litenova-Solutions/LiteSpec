// The docs groups as the site presents them: order, label, icon and the areas of
// docs/ each one holds. The sidebar, the breadcrumb and the page metadata all take
// a group's label from here, and every page is named by its own title, so one
// page has one name everywhere. The reading order inside each group lives in
// lib/nav.ts.
import type { IconName } from "./icons.ts";
import { pages, type Page } from "./spec.ts";

export interface Group {
  key: string;
  label: string;
  icon: IconName;
  /** The first folders under docs/ this group holds, such as backend. */
  areas: string[];
}

// The five groups of the docs folders, in journey order: understand, do, look
// up. The folders stay where they are, because provision IDs derive from page
// paths; only the navigation groups them. docs/assets/ holds diagrams, not
// pages. docs/intent/ holds LiteSpec's own research and decisions, so it sits
// outside the groups with a footer link, and its pages keep their URLs.
export const groups: Group[] = [
  { key: "learn", label: "Learn", icon: "learn", areas: ["learn", "example"] },
  { key: "tutorial", label: "Tutorial", icon: "start", areas: ["tutorial"] },
  { key: "guides", label: "Guides", icon: "file", areas: ["guide"] },
  { key: "standard", label: "Standard", icon: "core", areas: ["core", "backend", "frontend", "quality", "workspace", "ext"] },
  { key: "reference", label: "Reference", icon: "book", areas: ["reference", "tools", "analyzers", "maintain"] },
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
  tools: "CLI Commands",
  maintain: "Maintainer Tool",
  learn: "Learn",
  guide: "Guides",
  tutorial: "Tutorial",
  example: "Worked Example: Claimwick",
  intent: "Research and Decisions",
};

/** The display label of one docs area, such as Backend. */
export function areaLabel(area: string): string {
  return AREA_LABELS[area] ?? area.charAt(0).toUpperCase() + area.slice(1);
}

const byFile = new Map(pages.map((page) => [page.file, page]));

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
  if (dir === "docs/example") return { label: areaLabel("example"), stack: false };
  const name = dir.slice(dir.lastIndexOf("/") + 1);
  if (name === "ext") return { label: "Extensions", stack: false };
  return { label: name.charAt(0).toUpperCase() + name.slice(1), stack: false };
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
