// Tests that the manifest's npm pins equal the versions this repository's own
// packages use. A package names a catalog version as "catalog:", and the root
// pnpm-workspace.yaml holds that version, so the test reads the catalog too.
import { describe, it } from "node:test";
import assert from "node:assert";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "yaml";

// This file sits in apps/site/src/lib, so the repository root is four folders up.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../../..");

const readJson = (file: string) => JSON.parse(fs.readFileSync(path.join(root, file), "utf8")) as Record<string, any>;
const readYaml = (file: string) => parse(fs.readFileSync(path.join(root, file), "utf8")) as Record<string, any>;

/** The catalog versions in the root pnpm-workspace.yaml: the lines under "catalog:" with two-space indentation. */
function catalog(): Map<string, string> {
  const lines = fs.readFileSync(path.join(root, "pnpm-workspace.yaml"), "utf8").split("\n");
  const out = new Map<string, string>();
  let inside = false;
  for (const line of lines) {
    if (/^catalog:\s*$/.test(line)) {
      inside = true;
      continue;
    }
    if (inside && /^\S/.test(line)) break;
    const match = inside ? /^ {2}(\S+):\s*(\S+)\s*$/.exec(line) : null;
    if (match) out.set(match[1], match[2]);
  }
  return out;
}

const packageFiles = ["apps/site/package.json", "apps/cli/book/package.json", "apps/cli/fe-reader/package.json"];

describe("npm pins", () => {
  const manifest = readYaml("litespec.manifest.yaml");
  const pins = (manifest.packages?.npm ?? {}) as Record<string, string>;
  const versions = catalog();

  it("reads a catalog of at least the shared packages", () => {
    assert.ok(versions.size > 0, "pnpm-workspace.yaml names no catalog versions");
  });

  it("gives every catalog version the manifest pin, where the manifest names the package", () => {
    for (const [name, version] of versions) {
      if (name in pins) assert.strictEqual(pins[name], version, `manifest pins ${name} at ${pins[name]}, the catalog at ${version}`);
    }
  });

  it("gives every repository package the manifest pin, where the manifest names the package", () => {
    for (const file of packageFiles) {
      const pkg = readJson(file);
      for (const section of ["dependencies", "devDependencies"]) {
        for (const [name, declared] of Object.entries((pkg[section] ?? {}) as Record<string, string>)) {
          const version = declared === "catalog:" ? versions.get(name) : declared;
          assert.ok(version, `${file} names ${name} as catalog:, but the catalog does not hold it`);
          if (name in pins) assert.strictEqual(version, pins[name], `${file} ${section} pins ${name} at ${version}, the manifest at ${pins[name]}`);
        }
      }
    }
  });
});
