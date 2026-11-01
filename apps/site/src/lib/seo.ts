// Search and sharing metadata: canonical URLs, meta descriptions, the social image,
// and the schema.org objects every page writes as JSON-LD.
import { execFileSync } from "node:child_process";
import { root, version } from "./spec.ts";

export const siteUrl = "https://litespec.dev";
const github = "https://github.com/Litenova-Solutions/LiteSpec";

/** The absolute URL of a site path. */
export function absoluteUrl(pathname: string): string {
  return new URL(pathname, siteUrl).toString();
}

/** The build date as YYYY-MM-DD, used when no git date is available. */
export function buildDate(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * A page title as "Page - Section - LiteSpec", or "Page - LiteSpec" without a
 * section. The section is dropped when the full form passes 60 characters, so
 * the page title itself is never truncated and titles stay unique.
 */
export function pageTitle(title: string, section?: string): string {
  const clean = title.replace(/\s+/g, " ").trim();
  const withSection = section ? `${clean} - ${section} - LiteSpec` : `${clean} - LiteSpec`;
  if (withSection.length <= 60) return withSection;
  return `${clean} - LiteSpec`;
}

/**
 * A meta description of at most 160 characters: the text up to its last sentence end
 * that fits, otherwise up to the last whole word, with an ellipsis written as three dots.
 */
export function metaDescription(text: string, limit = 160): string {
  const clean = text.replace(/\s+/g, " ").trim();
  if (clean.length <= limit) return clean;
  const cut = clean.slice(0, limit);
  const sentence = cut.lastIndexOf(". ");
  if (sentence >= 80) return cut.slice(0, sentence + 1);
  const word = cut.slice(0, limit - 3).lastIndexOf(" ");
  return `${cut.slice(0, word > 0 ? word : limit - 3).replace(/[,;:]$/, "")}...`;
}

export const socialImage = {
  url: absoluteUrl("/og.png"),
  width: 1200,
  height: 630,
  alt: "LiteSpec: the specification standard for building business applications with AI agents",
};

export const organization = {
  "@type": "Organization",
  "@id": `${siteUrl}/#organization`,
  name: "Litenova Solutions",
  url: "https://litenova.solutions",
  sameAs: [github],
};

export const website = {
  "@type": "WebSite",
  "@id": `${siteUrl}/#website`,
  name: "LiteSpec",
  url: `${siteUrl}/`,
  description:
    "The specification standard for building business applications with AI agents: versioned rules for architecture, code, records, and tests, with tools that verify a project against them.",
  inLanguage: "en",
  publisher: { "@id": `${siteUrl}/#organization` },
};

export const softwareSource = {
  "@type": "SoftwareSourceCode",
  "@id": `${siteUrl}/#source`,
  name: "LiteSpec",
  codeRepository: github,
  license: "https://opensource.org/licenses/MIT",
  version,
  programmingLanguage: ["C#", "TypeScript"],
  author: { "@id": `${siteUrl}/#organization` },
};

/** A breadcrumb trail, each item a name and an optional site path. An item
 * without a path is a visible crumb that is not a link, so it carries no item. */
export function breadcrumbs(items: { name: string; path?: string | null }[]): Record<string, unknown> {
  return {
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      ...(item.path ? { item: absoluteUrl(item.path) } : {}),
    })),
  };
}

/** A non-article page of the LiteSpec website, such as the selection page. */
export function webPage(options: {
  title: string;
  description: string;
  path: string;
}): Record<string, unknown> {
  return {
    "@type": "WebPage",
    "@id": `${absoluteUrl(options.path)}#webpage`,
    name: options.title,
    description: metaDescription(options.description),
    url: absoluteUrl(options.path),
    inLanguage: "en",
    isPartOf: { "@id": `${siteUrl}/#website` },
    publisher: { "@id": `${siteUrl}/#organization` },
  };
}

/** A docs page as a technical article in the LiteSpec website. */
export function techArticle(options: {
  title: string;
  description: string;
  path: string;
  modified?: string;
  section: string;
}): Record<string, unknown> {
  return {
    "@type": "TechArticle",
    "@id": `${absoluteUrl(options.path)}#article`,
    headline: options.title,
    description: metaDescription(options.description),
    url: absoluteUrl(options.path),
    inLanguage: "en",
    articleSection: options.section,
    version,
    dateModified: options.modified ?? buildDate(),
    mainEntityOfPage: { "@type": "WebPage", "@id": `${absoluteUrl(options.path)}#webpage` },
    image: socialImage.url,
    isPartOf: { "@id": `${siteUrl}/#website` },
    publisher: { "@id": `${siteUrl}/#organization` },
    author: { "@id": `${siteUrl}/#organization` },
  };
}

const modifiedCache = new Map<string, string | undefined>();

/**
 * The date of the last commit that changed a repository file, as YYYY-MM-DD, or undefined
 * when git is unavailable or the file is not committed yet.
 */
export function lastModified(file: string): string | undefined {
  if (modifiedCache.has(file)) return modifiedCache.get(file);
  let date: string | undefined;
  try {
    const out = execFileSync("git", ["log", "-1", "--format=%cs", "--", file], { cwd: root, encoding: "utf8" }).trim();
    date = /^\d{4}-\d{2}-\d{2}$/.test(out) ? out : undefined;
  } catch {
    date = undefined;
  }
  modifiedCache.set(file, date);
  return date;
}
