import { describe, expect, it } from "vitest";

// The book's structure rules, checked over the source text so a change that
// breaks one fails the test run instead of a later review. The modules are the
// folders under src/lib; each one is imported from outside only through its
// index.ts. The vendored ui/ folder is exempt from the code rules.

const modules = ["frame", "record", "record-page", "outline", "trace", "shell"];

// Every source file under src, as Vite reads it, keyed by its path below src.
const raw: Record<string, string> = import.meta.glob(["./**/*.ts", "./**/*.svelte", "!./**/*.test.ts", "!./lib/ui/**"], { query: "?raw", import: "default", eager: true });
const sources = Object.entries(raw).map(([key, text]) => ({ file: key.slice("./".length), text }));
const indexes = new Set(Object.keys(import.meta.glob("./lib/*/index.ts")).map((key) => key.slice("./".length)));

function moduleOf(file: string): string | null {
  const match = /^lib\/([^/]+)\//.exec(file);
  return match?.[1] ?? null;
}

function violations(test: (file: string, text: string) => string[]): string[] {
  return sources.flatMap(({ file, text }) => test(file, text).map((detail) => `${file}: ${detail}`));
}

describe("the book's structure", () => {
  it("reads the whole source tree", () => {
    expect(sources.length).toBeGreaterThan(100);
  });

  it("has the modules the design names, each with one index.ts", () => {
    for (const name of modules) {
      expect(indexes.has(`lib/${name}/index.ts`), `${name}/index.ts`).toBe(true);
    }
  });

  it("imports another module only through its index.ts", () => {
    const found = violations((file, text) =>
      [...text.matchAll(/from\s+["']\$lib\/([^"']+)["']/g)]
        .map((match) => match[1] ?? "")
        .filter((target) => {
          const [name, ...rest] = target.split("/");
          if (name === "ui") return false;
          return name !== undefined && modules.includes(name) && rest.length > 0 && name !== moduleOf(file);
        }),
    );
    expect(found).toEqual([]);
  });

  it("reaches no other module by a relative path", () => {
    const found = violations((file, text) => {
      const own = moduleOf(file);
      if (!own) return [];
      return [...text.matchAll(/from\s+["'](\.\.?\/[^"']+)["']/g)]
        .map((match) => match[1] ?? "")
        .filter((target) => {
          const resolved = new URL(target, `file:///${file}`).pathname.slice(1);
          const other = moduleOf(resolved);
          return other !== null && other !== own;
        });
    });
    expect(found).toEqual([]);
  });

  it("writes no any, no non-null assertion and no cast but as const and DOM targets", () => {
    const found = violations((_file, source) => {
      // Comments and import or export lines use "as" in prose and aliases, not casts.
      const text = source
        .replace(/\/\*[\s\S]*?\*\//g, "")
        .replace(/<!--[\s\S]*?-->/g, "")
        .replace(/^\s*\/\/.*$/gm, "")
        .replace(/^\s*(import|export)\b[\s\S]*?;\s*$/gm, "");
      const out: string[] = [];
      if (/:\s*any\b|<any>|as any\b|Record<string,\s*any>/.test(text)) out.push("any");
      if (/[\w)\]]!\.|[\w)\]]!\)|[\w)\]]!;|[\w)\]]!,/.test(text.replace(/!==?/g, ""))) out.push("non-null assertion");
      for (const match of text.matchAll(/\bas\s+([A-Z][\w.<>[\]| ]*|const\b)/g)) {
        const target = (match[1] ?? "").trim();
        if (target === "const" || /^(HTML\w*Element|Element|Node|KeyboardEvent|MouseEvent)( \| null)?$/.test(target)) continue;
        out.push(`cast to ${target}`);
      }
      return out;
    });
    expect(found).toEqual([]);
  });

  it("touches browser storage only in the frame's theme and preferences", () => {
    const allowed = new Set(["lib/frame/theme.svelte.ts", "lib/frame/preferences.svelte.ts"]);
    const found = violations((file, text) => (!allowed.has(file) && /\b(localStorage|sessionStorage)\b/.test(text) ? ["browser storage"] : []));
    expect(found).toEqual([]);
  });
});
