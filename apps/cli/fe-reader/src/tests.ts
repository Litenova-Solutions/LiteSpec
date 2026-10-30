// Playwright test reading. Each test(...) call below a test root yields one
// entry with its title, proves and journey annotations, goto targets, and the
// refusal codes and texts its assertions hold. Annotations come from raw
// { type, description } objects and from proves("...") helper calls; goto
// templates map each hole to [param]; a string matching the refusal code
// pattern is a code, every other asserted string is a text.

import * as ts from "typescript";
import { withCache } from "./cache.js";
import { canonical, listFilesRecursive, readText, relativeTo, toPosix } from "./files.js";
import { lineOf, parseTypeScript, plainText, walk } from "./parse.js";
import { ReaderContext, TestFacts } from "./protocol.js";

const CODE_PATTERN = /^[a-z][a-z0-9-]*(\.[a-z0-9-]+)+$/;

interface FileTest {
  line: number;
  title: string;
  proves: string[];
  journey: string | null;
  gotos: string[];
  codes: string[];
  texts: string[];
}

function unwrap(node: ts.Expression): ts.Expression {
  let current = node;
  while (ts.isParenthesizedExpression(current)) current = current.expression;
  return current;
}

function isFunctionArg(node: ts.Expression): boolean {
  const unwrapped = unwrap(node);
  return ts.isArrowFunction(unwrapped) || ts.isFunctionExpression(unwrapped);
}

// The property chain below a root identifier, or null when the callee is not
// rooted at that identifier.
function chainBelow(callee: ts.Expression, root: string): string[] | null {
  const suffixes: string[] = [];
  let current = unwrap(callee);
  while (ts.isPropertyAccessExpression(current)) {
    if (!ts.isIdentifier(current.name)) return null;
    suffixes.unshift(current.name.text);
    current = unwrap(current.expression);
  }
  if (!ts.isIdentifier(current) || current.text !== root) return null;
  return suffixes;
}

const TEST_SUFFIXES = new Set(["only", "skip", "fixme", "fail"]);

// A Playwright test declaration: test(...) with the declaring suffixes only
// (suites, hooks and helpers are not tests) and a function body.
function isTestCall(node: ts.CallExpression): boolean {
  const suffixes = chainBelow(node.expression, "test");
  if (suffixes === null || !suffixes.every(suffix => TEST_SUFFIXES.has(suffix))) return false;
  return node.arguments.some(arg => isFunctionArg(arg));
}

// An assertion head: expect(...) with the assertion suffixes only. Asymmetric
// matcher factories such as expect.objectContaining are not heads; their
// strings are collected once through the outer assertion chain.
function isExpectCall(node: ts.CallExpression): boolean {
  const direct = unwrap(node.expression);
  if (ts.isIdentifier(direct) && direct.text === "expect") return true;
  const belowExpect = chainBelow(node.expression, "expect");
  if (belowExpect !== null && belowExpect.length > 0 && belowExpect.every(suffix => suffix === "soft" || suffix === "poll")) {
    return true;
  }
  const belowTest = chainBelow(node.expression, "test");
  if (belowTest !== null && belowTest.length > 0 && belowTest[0] === "expect") {
    return belowTest.slice(1).every(suffix => suffix === "soft" || suffix === "poll");
  }
  return false;
}

function templateRoute(node: ts.TemplateExpression): string {
  let route = node.head.text;
  for (const span of node.templateSpans) {
    route += "[param]" + span.literal.text;
  }
  return route;
}

function walkSkippingFunctions(node: ts.Node, visit: (node: ts.Node) => void): void {
  visit(node);
  if (ts.isArrowFunction(node) || ts.isFunctionExpression(node) || ts.isFunctionDeclaration(node)) return;
  ts.forEachChild(node, child => walkSkippingFunctions(child, visit));
}

function annotationOf(node: ts.ObjectLiteralExpression): { kind: string; description: string } | null {
  let kind: string | null = null;
  let description: string | null = null;
  for (const prop of node.properties) {
    if (!ts.isPropertyAssignment(prop)) continue;
    const key = ts.isIdentifier(prop.name) ? prop.name.text : plainText(prop.name);
    if (key === "type" && kind === null) kind = plainText(unwrap(prop.initializer));
    if (key === "description" && description === null) description = plainText(unwrap(prop.initializer));
  }
  if ((kind === "proves" || kind === "journey") && description !== null) return { kind, description };
  return null;
}

interface CollectedAnnotations {
  proves: string[];
  journey: string | null;
}

function collectAnnotations(args: readonly ts.Expression[]): CollectedAnnotations {
  const proves: string[] = [];
  let journey: string | null = null;
  for (const arg of args) {
    if (isFunctionArg(arg)) continue;
    walkSkippingFunctions(arg, node => {
      if (ts.isCallExpression(node)) {
        const callee = unwrap(node.expression);
        if (ts.isIdentifier(callee) && callee.text === "proves") {
          const first = node.arguments.length > 0 ? node.arguments[0] : undefined;
          if (first !== undefined) {
            const text = plainText(unwrap(first));
            if (text !== null) proves.push(text);
          }
        }
        return;
      }
      if (ts.isObjectLiteralExpression(node)) {
        const annotation = annotationOf(node);
        if (annotation !== null) {
          if (annotation.kind === "proves") proves.push(annotation.description);
          else if (journey === null) journey = annotation.description;
        }
      }
    });
  }
  return { proves, journey };
}

function passthroughParent(node: ts.Node): boolean {
  return (
    ts.isPropertyAccessExpression(node) ||
    ts.isCallExpression(node) ||
    ts.isNonNullExpression(node) ||
    ts.isParenthesizedExpression(node) ||
    ts.isAwaitExpression(node) ||
    ts.isAsExpression(node) ||
    ts.isSatisfiesExpression(node)
  );
}

// The argument lists of an assertion: the expect(...) arguments plus the
// arguments of every chained matcher call above it.
function assertionArgs(node: ts.CallExpression): ts.NodeArray<ts.Expression>[] {
  const lists: ts.NodeArray<ts.Expression>[] = [node.arguments];
  let current: ts.Node = node;
  for (;;) {
    const parent = current.parent;
    if (parent === undefined || !passthroughParent(parent)) break;
    if (ts.isCallExpression(parent)) lists.push(parent.arguments);
    current = parent;
  }
  return lists;
}

function collectStrings(lists: ts.NodeArray<ts.Expression>[], codes: string[], texts: string[]): void {
  for (const args of lists) {
    for (const arg of args) {
      walk(arg, node => {
        const text = plainText(node);
        if (text === null) return;
        if (CODE_PATTERN.test(text)) codes.push(text);
        else texts.push(text);
      });
    }
  }
}

function collectBody(fns: ts.Expression[], gotos: string[], codes: string[], texts: string[]): void {
  for (const fn of fns) {
    walk(fn, node => {
      if (!ts.isCallExpression(node)) return;
      const callee = unwrap(node.expression);
      if (ts.isPropertyAccessExpression(callee) && ts.isIdentifier(callee.name) && callee.name.text === "goto") {
        const first = node.arguments.length > 0 ? node.arguments[0] : undefined;
        if (first === undefined) return;
        const unwrapped = unwrap(first);
        const text = plainText(unwrapped);
        if (text !== null) gotos.push(text);
        else if (ts.isTemplateExpression(unwrapped)) gotos.push(templateRoute(unwrapped));
        return;
      }
      if (isExpectCall(node)) collectStrings(assertionArgs(node), codes, texts);
    });
  }
}

function extractTests(text: string, fileName: string): { ok: boolean; tests: FileTest[] } {
  const sourceFile = parseTypeScript(fileName, text);
  if (sourceFile === null) return { ok: false, tests: [] };
  const tests: FileTest[] = [];
  walk(sourceFile, node => {
    if (!ts.isCallExpression(node) || !isTestCall(node)) return;
    const first = node.arguments.length > 0 ? node.arguments[0] : undefined;
    const title = first === undefined ? "" : (plainText(unwrap(first)) ?? "");
    const annotations = collectAnnotations([...node.arguments]);
    const gotos: string[] = [];
    const codes: string[] = [];
    const texts: string[] = [];
    collectBody(node.arguments.filter(arg => isFunctionArg(arg)), gotos, codes, texts);
    tests.push({ line: lineOf(sourceFile, node), title, proves: annotations.proves, journey: annotations.journey, gotos, codes, texts });
  });
  tests.sort((a, b) => a.line - b.line);
  return { ok: true, tests };
}

// Every Playwright test below the test roots, sorted by file and line. A file
// that fails to parse is diagnosed and contributes no tests.
export function readTests(testRootsAbs: string[], context: ReaderContext): TestFacts[] {
  const tests: TestFacts[] = [];
  const seen = new Set<string>();
  for (const root of testRootsAbs) {
    for (const file of listFilesRecursive(root, [".ts", ".tsx"])) {
      if (seen.has(canonical(file))) continue;
      seen.add(canonical(file));
      const rel = relativeTo(context.root, file) ?? toPosix(file);
      const text = readText(file);
      if (text === null) continue;
      const extracted = withCache<{ ok: boolean; tests: FileTest[] }>(context.cacheDir, "tests", text, () => extractTests(text, file));
      if (!extracted.ok) {
        context.diagnose(file, "typescript");
        continue;
      }
      for (const test of extracted.tests) {
        tests.push({ file: rel, ...test });
      }
    }
  }
  tests.sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : a.line - b.line));
  return tests;
}
