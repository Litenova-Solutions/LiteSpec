import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "vitest";
import { proves } from "../../src/lib/app/litespec-records.ts";

// Marks what this file proves for the model reader; Vitest ignores the annotation.
proves("litespec/frontend/testing#prove-the-module-mirror-in-a-structure-test");

const here = dirname(fileURLToPath(import.meta.url));
const appRoot = join(here, "..", "..", "src");
const applicationRoot = join(here, "..", "..", "..", "api", "src", "Modules");

const refusedNames = new Set([
  "shared",
  "common",
  "utils",
  "util",
  "helpers",
  "misc",
  "components",
  "hooks",
  "types",
  "services",
]);

const kebabCase = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function isDirectory(path: string): boolean {
  try {
    return statSync(path).isDirectory();
  } catch {
    return false;
  }
}

function isFile(path: string): boolean {
  try {
    return statSync(path).isFile();
  } catch {
    return false;
  }
}

function pascalCase(segment: string): string {
  return segment
    .split("-")
    .map((part) => part.slice(0, 1).toUpperCase() + part.slice(1))
    .join("");
}

function checkUseCase(
  useCaseDir: string,
  module: string,
  useCase: string,
  applicationDir: string,
  violations: string[],
): void {
  const backendDir = join(applicationDir, pascalCase(useCase));
  if (!isDirectory(backendDir)) {
    violations.push(`use-case folder '${useCaseDir}' names no backend folder '${backendDir}'`);
  }
  const markFile = join(useCaseDir, "litespec.ts");
  if (!isFile(markFile)) {
    violations.push(`use-case folder '${useCaseDir}' holds no litespec.ts mark`);
  } else if (!readFileSync(markFile, "utf8").includes(`implements: "use-case/${module}.${useCase}"`)) {
    violations.push(`use-case folder '${useCaseDir}' marks no use-case/${module}.${useCase}`);
  }
  for (const name of readdirSync(useCaseDir)) {
    if (isDirectory(join(useCaseDir, name))) {
      violations.push(`use-case folder '${useCaseDir}' holds a subfolder '${name}' instead of files only`);
    }
  }
}

function checkModuleStructure(appRoot: string, applicationRoot: string): string[] {
  const violations: string[] = [];

  const visit = (dir: string): void => {
    for (const name of readdirSync(dir)) {
      const path = join(dir, name);
      if (!isDirectory(path)) {
        continue;
      }
      if (refusedNames.has(name)) {
        violations.push(`refused folder name '${name}' at '${path}'`);
      }
      visit(path);
    }
  };
  visit(appRoot);

  const backendModuleDir = (module: string): string | null => {
    for (const name of readdirSync(applicationRoot)) {
      if (!isDirectory(join(applicationRoot, name))) {
        continue;
      }
      const last = name.split(".").pop() ?? "";
      if (last.toLowerCase() === module) {
        return join(applicationRoot, name);
      }
    }
    return null;
  };

  const modulesRoot = join(appRoot, "lib", "modules");
  for (const module of readdirSync(modulesRoot)) {
    const moduleDir = join(modulesRoot, module);
    if (!isDirectory(moduleDir)) {
      continue;
    }
    if (!kebabCase.test(module)) {
      violations.push(`module folder '${moduleDir}' is not kebab-case`);
    }
    const backend = backendModuleDir(module);
    if (backend === null) {
      violations.push(`module folder '${moduleDir}' names no backend module`);
      continue;
    }
    for (const child of readdirSync(moduleDir)) {
      const childDir = join(moduleDir, child);
      if (!isDirectory(childDir)) {
        continue;
      }
      if (!kebabCase.test(child)) {
        violations.push(`folder '${childDir}' is not kebab-case`);
      }
      if (isFile(join(childDir, "litespec.ts"))) {
        checkUseCase(childDir, module, child, join(backend, "Application"), violations);
      } else {
        const aggregateDir = join(backend, "Application", pascalCase(child));
        if (!isDirectory(aggregateDir)) {
          violations.push(`aggregate folder '${childDir}' names no backend folder '${aggregateDir}'`);
        }
        for (const useCase of readdirSync(childDir)) {
          const useCaseDir = join(childDir, useCase);
          if (!isDirectory(useCaseDir)) {
            continue;
          }
          if (!kebabCase.test(useCase)) {
            violations.push(`folder '${useCaseDir}' is not kebab-case`);
          }
          checkUseCase(useCaseDir, module, useCase, aggregateDir, violations);
        }
      }
    }
  }

  return violations;
}

test("every module folder names a backend use case, and no folder is named for nothing", () => {
  expect(checkModuleStructure(appRoot, applicationRoot)).toEqual([]);
});
