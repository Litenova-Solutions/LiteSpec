// Reader entry point. Reads one JSON request from standard input, writes one
// JSON response to standard output, and exits zero. Any failure exits non-zero
// with exactly one line on standard error; unparseable source files never fail
// the run but yield diagnostics in the response.

import { readFileSync, writeFileSync } from "node:fs";
import { exit } from "node:process";
import { collectOperations } from "./calls.js";
import { absolute, canonical, ensureDir, joinPath, relativeTo, toPosix } from "./files.js";
import { readTests } from "./tests.js";
import { parseRequest, Diagnostic, FrontendFacts, FrontendRequest, PROTOCOL_VERSION, READER_VERSION, ReaderContext, ReaderResponse } from "./protocol.js";
import * as ts from "typescript";
import { VERSION as SVELTE_VERSION } from "svelte/compiler";
import { readReactRoutes } from "./react.js";
import { readModules, readSvelteRoutes } from "./svelte.js";

function readStdin(): string {
  return readFileSync(0, "utf8");
}

function fail(message: string): never {
  writeFileSync(2, `${message.replace(/[\r\n]+/g, " ").trim() || "reader error"}\n`, "utf8");
  exit(1);
}

function readFrontend(request: FrontendRequest, cacheDir: string | null): FrontendFacts {
  const root = request.root === "" ? absolute(".") : absolute(request.root);
  const diagnostics: Diagnostic[] = [];
  const attempted = new Set<string>();
  const context: ReaderContext = {
    root,
    cacheDir,
    diagnose: (absPath: string, kind: "typescript" | "svelte"): void => {
      if (attempted.has(canonical(absPath))) return;
      attempted.add(canonical(absPath));
      const file = relativeTo(root, absPath) ?? toPosix(absPath);
      diagnostics.push({ file, line: 1, message: `parse error: ${kind} syntax error` });
    },
  };

  const missing = joinPath(root, ".litespec-missing");
  const at = (path: string): string => (path === "" ? missing : absolute(path));
  const modulesAbs = at(request.modules);
  const scan = readModules(modulesAbs, context);
  const operations = collectOperations(at(request.apiClient), context);
  const aliases = { ...request.aliases };
  const routesAbs = at(request.routes);
  const routes =
    request.platform === "svelte"
      ? readSvelteRoutes(routesAbs, modulesAbs, { operations, aliases }, context)
      : request.platform === "react"
        ? readReactRoutes(routesAbs, modulesAbs, { operations, aliases }, context)
        : [];
  const tests = readTests(request.testRoots.map(at), context);
  diagnostics.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return { name: request.name, modules: scan.modules, routes, tests, diagnostics };
}

function main(): void {
  let text: string;
  try {
    text = readStdin();
  } catch {
    fail("reader error: cannot read standard input");
  }
  let request;
  try {
    request = parseRequest(text);
  } catch (error) {
    fail(error instanceof Error ? error.message : "reader error: invalid request");
  }
  if (request.cache !== null && request.cache !== "") ensureDir(absolute(request.cache));
  const frontends: FrontendFacts[] = [];
  for (const frontend of request.frontends) {
    try {
      frontends.push(readFrontend(frontend, request.cache));
    } catch (error) {
      fail(error instanceof Error ? `reader error: ${error.message}` : "reader error: cannot read frontend");
    }
  }
  const response: ReaderResponse = {
    protocol: PROTOCOL_VERSION,
    reader: READER_VERSION,
    typescript: ts.version,
    svelte: SVELTE_VERSION,
    frontends,
  };
  try {
    writeFileSync(1, `${JSON.stringify(response)}\n`, "utf8");
  } catch {
    fail("reader error: cannot write standard output");
  }
}

try {
  main();
} catch (error) {
  fail(error instanceof Error ? error.message : "reader error");
}
