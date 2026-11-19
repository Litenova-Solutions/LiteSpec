// Syntax-only parsing for TypeScript sources and Svelte components, plus the
// cached per-file fact extraction the route, module and call readers share.
// Svelte script blocks are fed to the TypeScript parser with their line offset
// kept. Facts hold lines only, never file paths, so cache entries stay valid
// for identical content in any file.

import { parse as parseSvelteComponent } from "svelte/compiler";
import * as ts from "typescript";
import { withCache } from "./cache";
import { readText } from "./files";
import { ReaderContext } from "./protocol";

const parseSvelte = parseSvelteComponent as unknown as (source: string, options: { modern: boolean }) => unknown;

export interface MarkSite {
  values: string[];
  line: number;
}

export interface CallSite {
  name: string;
  line: number;
}

export interface NavigationSite {
  path: string;
  line: number;
}

export interface FileFacts {
  marks: MarkSite[];
  calls: CallSite[];
  imports: string[];
  functions: string[];
  route: string | null;
  navigations: NavigationSite[];
}

export type FileKind = "ts" | "svelte";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

export function walk(node: ts.Node, visit: (node: ts.Node) => void): void {
  visit(node);
  ts.forEachChild(node, child => walk(child, visit));
}

export function lineOf(sourceFile: ts.SourceFile, node: ts.Node): number {
  return ts.getLineAndCharacterOfPosition(sourceFile, node.getStart(sourceFile)).line + 1;
}

export function plainText(node: ts.Node): string | null {
  if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) return node.text;
  return null;
}

function unwrapExpression(node: ts.Expression): ts.Expression {
  let current = node;
  while (
    ts.isAsExpression(current) ||
    ts.isSatisfiesExpression(current) ||
    ts.isParenthesizedExpression(current) ||
    ts.isNonNullExpression(current) ||
    ts.isTypeAssertionExpression(current)
  ) {
    current = current.expression;
  }
  return current;
}

function hasExportModifier(node: ts.Node): boolean {
  const modifiers = ts.canHaveModifiers(node) ? ts.getModifiers(node) : undefined;
  return modifiers !== undefined && modifiers.some(mod => mod.kind === ts.SyntaxKind.ExportKeyword);
}

function hasDefaultModifier(node: ts.Node): boolean {
  const modifiers = ts.canHaveModifiers(node) ? ts.getModifiers(node) : undefined;
  return modifiers !== undefined && modifiers.some(mod => mod.kind === ts.SyntaxKind.DefaultKeyword);
}

// Every static string the implements property of an object literal holds, from
// either a single string or an array of strings.
function implementsValues(initializer: ts.Expression): string[] {
  const unwrapped = unwrapExpression(initializer);
  if (!ts.isObjectLiteralExpression(unwrapped)) return [];
  const values: string[] = [];
  for (const prop of unwrapped.properties) {
    if (!ts.isPropertyAssignment(prop)) continue;
    const key = ts.isIdentifier(prop.name) ? prop.name.text : plainText(prop.name);
    if (key !== "implements") continue;
    const direct = plainText(prop.initializer);
    if (direct !== null) {
      values.push(direct);
      continue;
    }
    const list = unwrapExpression(prop.initializer);
    if (ts.isArrayLiteralExpression(list)) {
      for (const entry of list.elements) {
        const text = plainText(entry);
        if (text !== null) values.push(text);
      }
    }
  }
  return values;
}

// Every exported litespec constant of a file in source order: directly
// exported declarations plus local declarations named through an export list.
export function collectMarks(sourceFile: ts.SourceFile): MarkSite[] {
  const exported = new Set<string>();
  for (const statement of sourceFile.statements) {
    if (ts.isExportDeclaration(statement) && statement.moduleSpecifier === undefined) {
      const clause = statement.exportClause;
      if (clause !== undefined && ts.isNamedExports(clause)) {
        for (const element of clause.elements) {
          if (element.propertyName !== undefined && ts.isIdentifier(element.propertyName)) {
            exported.add(element.propertyName.text);
          } else if (ts.isIdentifier(element.name)) {
            exported.add(element.name.text);
          }
        }
      }
    }
  }
  const marks: MarkSite[] = [];
  for (const statement of sourceFile.statements) {
    if (!ts.isVariableStatement(statement)) continue;
    const direct = hasExportModifier(statement);
    for (const declaration of statement.declarationList.declarations) {
      if (!ts.isIdentifier(declaration.name) || declaration.name.text !== "litespec") continue;
      if (!direct && !exported.has("litespec")) continue;
      if (declaration.initializer === undefined) continue;
      marks.push({ values: implementsValues(declaration.initializer), line: lineOf(sourceFile, statement) });
    }
  }
  return marks;
}

// Every call of a source in source order: the identifier of a direct call or
// the property name of a method call.
export function collectCalls(sourceFile: ts.SourceFile, lineOffset: number): CallSite[] {
  const calls: CallSite[] = [];
  walk(sourceFile, node => {
    if (!ts.isCallExpression(node)) return;
    let target: ts.Expression = node.expression;
    while (ts.isParenthesizedExpression(target)) target = target.expression;
    if (ts.isIdentifier(target)) {
      calls.push({ name: target.text, line: lineOf(sourceFile, node) + lineOffset });
    } else if (ts.isPropertyAccessExpression(target) && ts.isIdentifier(target.name)) {
      calls.push({ name: target.name.text, line: lineOf(sourceFile, node) + lineOffset });
    }
  });
  return calls;
}

// Every module specifier of the import and re-export declarations, sorted with
// duplicates removed.
export function collectImports(sourceFile: ts.SourceFile): string[] {
  const specifiers = new Set<string>();
  for (const statement of sourceFile.statements) {
    if (ts.isImportDeclaration(statement) || ts.isExportDeclaration(statement)) {
      const specifier = statement.moduleSpecifier;
      if (specifier !== undefined) {
        const text = plainText(specifier);
        if (text !== null) specifiers.add(text);
      }
    }
  }
  return [...specifiers].sort();
}

function isFunctionValue(node: ts.Expression): boolean {
  const unwrapped = unwrapExpression(node);
  return ts.isArrowFunction(unwrapped) || ts.isFunctionExpression(unwrapped);
}

// Every named export of a file that is a function: exported function
// declarations and exported variables holding an arrow or function expression,
// including names carried through a local export list. Default exports and
// re-exports from other files are not named exports of this file.
export function collectExportedFunctions(sourceFile: ts.SourceFile): string[] {
  const names = new Set<string>();
  const locals = new Map<string, boolean>();
  for (const statement of sourceFile.statements) {
    if (ts.isFunctionDeclaration(statement) && statement.name !== undefined && ts.isIdentifier(statement.name)) {
      const name = statement.name.text;
      locals.set(name, true);
      if (hasExportModifier(statement) && !hasDefaultModifier(statement)) names.add(name);
    } else if (ts.isVariableStatement(statement)) {
      for (const declaration of statement.declarationList.declarations) {
        if (!ts.isIdentifier(declaration.name) || declaration.initializer === undefined) continue;
        const isFunction = isFunctionValue(declaration.initializer);
        locals.set(declaration.name.text, isFunction);
        if (isFunction && hasExportModifier(statement)) names.add(declaration.name.text);
      }
    }
  }
  for (const statement of sourceFile.statements) {
    if (!ts.isExportDeclaration(statement) || statement.moduleSpecifier !== undefined) continue;
    const clause = statement.exportClause;
    if (clause === undefined || !ts.isNamedExports(clause)) continue;
    for (const element of clause.elements) {
      const local = element.propertyName !== undefined && ts.isIdentifier(element.propertyName)
        ? element.propertyName.text
        : ts.isIdentifier(element.name)
          ? element.name.text
          : null;
      const exported = ts.isIdentifier(element.name) ? element.name.text : null;
      if (local !== null && exported !== null && exported !== "default" && locals.get(local) === true) {
        names.add(exported);
      }
    }
  }
  return [...names].sort();
}

// The literal path a navigation target holds: a string literal or
// no-substitution template as written, or the literal head of a template
// with substitutions. Anything else is dynamic and never a navigation.
function navigationPath(node: ts.Expression): string | null {
  const unwrapped = unwrapExpression(node);
  if (ts.isStringLiteral(unwrapped) || ts.isNoSubstitutionTemplateLiteral(unwrapped)) return unwrapped.text;
  if (ts.isTemplateExpression(unwrapped)) return unwrapped.head.text;
  return null;
}

// Whether the target is a navigation path: a literal path from the root.
function isNavigationPath(value: string | null): value is string {
  return value !== null && value.startsWith("/");
}

// Whether the expression names the document location: `location` or a
// member expression ending in `.location`, such as `window.location`.
function isLocationTarget(node: ts.Expression): boolean {
  const unwrapped = unwrapExpression(node);
  if (ts.isIdentifier(unwrapped)) return unwrapped.text === "location";
  if (ts.isPropertyAccessExpression(unwrapped)) return unwrapped.name.text === "location";
  return false;
}

// Every local name the file imports as `goto` from SvelteKit's navigation
// module, through a named import with or without an alias.
export function collectGotoNames(sourceFile: ts.SourceFile): Set<string> {
  const names = new Set<string>();
  for (const statement of sourceFile.statements) {
    if (!ts.isImportDeclaration(statement)) continue;
    if (!ts.isStringLiteral(statement.moduleSpecifier) || statement.moduleSpecifier.text !== "$app/navigation") continue;
    const bindings = statement.importClause?.namedBindings;
    if (bindings === undefined || !ts.isNamedImports(bindings)) continue;
    for (const element of bindings.elements) {
      const imported = element.propertyName !== undefined ? element.propertyName.text : element.name.text;
      if (imported === "goto") names.add(element.name.text);
    }
  }
  return names;
}

// The navigation target of a JSX anchor, link, button or form opening: the
// `href` or `action` attribute initializer, or null for any other tag or
// attribute. A `Link` or `Button` component counts like an anchor.
function jsxNavigationTarget(opening: ts.JsxOpeningLikeElement): ts.Expression | null {
  if (!ts.isIdentifier(opening.tagName)) return null;
  const tag = opening.tagName.text;
  const wanted = tag === "a" || tag === "Link" || tag === "Button" ? "href" : tag === "form" ? "action" : null;
  if (wanted === null) return null;
  for (const attribute of opening.attributes.properties) {
    if (!ts.isJsxAttribute(attribute) || !ts.isIdentifier(attribute.name) || attribute.name.text !== wanted) continue;
    const initializer = attribute.initializer;
    if (initializer === undefined) return null;
    if (ts.isStringLiteral(initializer)) return initializer;
    if (ts.isJsxExpression(initializer) && initializer.expression !== undefined) return initializer.expression;
    return null;
  }
  return null;
}

// Every full-page navigation of a source in source order: an assignment to the
// location itself or to a location `href`, a `location.assign` or
// `location.replace` call, a call to a `goto` imported from SvelteKit's
// navigation module, and a literal `href` on an anchor, `Link` or `Button`, or
// `action` on a form. Only literal paths or template literals with a literal
// path prefix count; anything dynamic is never a navigation.
export function collectNavigations(sourceFile: ts.SourceFile, lineOffset: number, gotoNames: Set<string>): NavigationSite[] {
  const navigations: NavigationSite[] = [];
  const push = (target: ts.Expression | null, node: ts.Node): void => {
    if (target === null) return;
    const path = navigationPath(target);
    if (isNavigationPath(path)) navigations.push({ path, line: lineOf(sourceFile, node) + lineOffset });
  };
  walk(sourceFile, node => {
    if (ts.isBinaryExpression(node) && node.operatorToken.kind === ts.SyntaxKind.EqualsToken) {
      const left = unwrapExpression(node.left);
      if (isLocationTarget(left)
        || (ts.isPropertyAccessExpression(left) && left.name.text === "href" && isLocationTarget(left.expression))) {
        push(node.right, node);
      }
      return;
    }
    if (ts.isCallExpression(node)) {
      const target = unwrapExpression(node.expression);
      if (ts.isPropertyAccessExpression(target)
        && (target.name.text === "assign" || target.name.text === "replace")
        && isLocationTarget(target.expression)) {
        const first = node.arguments[0];
        push(first ?? null, node);
        return;
      }
      if (ts.isIdentifier(target) && gotoNames.has(target.text)) {
        const first = node.arguments[0];
        push(first ?? null, node);
      }
      return;
    }
    if (ts.isJsxElement(node)) {
      push(jsxNavigationTarget(node.openingElement), node.openingElement);
      return;
    }
    if (ts.isJsxSelfClosingElement(node)) {
      push(jsxNavigationTarget(node), node);
    }
  });
  return navigations;
}

// The createFileRoute("...") path argument of a React route file, when present.
export function collectRoutePath(sourceFile: ts.SourceFile): string | null {
  let found: string | null = null;
  walk(sourceFile, node => {
    if (found !== null || !ts.isCallExpression(node)) return;
    if (!ts.isIdentifier(node.expression) || node.expression.text !== "createFileRoute") return;
    if (node.arguments.length === 0) return;
    const first = node.arguments[0];
    if (first !== undefined) {
      const text = plainText(first);
      if (text !== null) found = text;
    }
  });
  return found;
}

export function parseTypeScript(fileName: string, text: string): ts.SourceFile | null {
  try {
    const sourceFile = ts.createSourceFile(fileName, text, ts.ScriptTarget.Latest, true);
    return (sourceFile as unknown as { parseDiagnostics: readonly ts.Diagnostic[] }).parseDiagnostics.length === 0 ? sourceFile : null;
  } catch {
    return null;
  }
}

export interface SvelteScript {
  content: string;
  startLine: number;
  context: "module" | "instance";
}

// The module and instance scripts of a component with their content and the
// 1-based line each content starts on, plus the markup fragment for the
// anchor and form reader. A component that fails to parse yields ok false;
// the error text is left out so diagnostics stay deterministic.
export function parseSvelteScripts(text: string): { ok: boolean; scripts: SvelteScript[]; fragment: unknown } {
  let ast: unknown;
  try {
    ast = parseSvelte(text, { modern: true });
  } catch {
    return { ok: false, scripts: [], fragment: null };
  }
  if (!isRecord(ast)) return { ok: false, scripts: [], fragment: null };
  const scripts: SvelteScript[] = [];
  for (const key of ["module", "instance"] as const) {
    const node = ast[key];
    if (!isRecord(node)) continue;
    const content = node["content"];
    if (!isRecord(content)) continue;
    const start = content["start"];
    const end = content["end"];
    if (typeof start !== "number" || typeof end !== "number") continue;
    if (!(start >= 0 && end >= start && end <= text.length)) continue;
    let startLine = 1;
    for (let i = 0; i < start; i++) {
      if (text.charCodeAt(i) === 10) startLine++;
    }
    scripts.push({ content: text.slice(start, end), startLine, context: key });
  }
  return { ok: true, scripts, fragment: ast["fragment"] ?? null };
}

// The 1-based line of an offset in the text.
function lineOfOffset(text: string, offset: number): number {
  let line = 1;
  const end = Math.min(offset, text.length);
  for (let i = 0; i < end; i++) {
    if (text.charCodeAt(i) === 10) line++;
  }
  return line;
}

// The path an expression tag holds: a string literal or the literal head of
// a template literal. Anything else is dynamic and never a navigation.
function expressionTagPath(expression: unknown): string | null {
  if (!isRecord(expression)) return null;
  const value = expression["value"];
  if (expression["type"] === "Literal" && typeof value === "string") {
    return value;
  }
  const quasis = expression["quasis"];
  if (expression["type"] === "TemplateLiteral" && Array.isArray(quasis) && quasis.length > 0) {
    const first = quasis[0];
    if (isRecord(first) && isRecord(first["value"])) {
      const cooked = first["value"]["cooked"];
      if (typeof cooked === "string") return cooked;
    }
  }
  return null;
}

// The leading static text of a Svelte attribute value: the Text data before
// any expression, or the literal the single expression holds. A lone value in
// braces arrives unwrapped, so it reads like the first entry of a list. A
// value starting with a dynamic expression is never a navigation.
function svelteAttributePath(value: unknown): string | null {
  const first = Array.isArray(value) ? value[0] : value;
  if (first === undefined || !isRecord(first)) return null;
  if (first["type"] === "Text") {
    const data = first["data"];
    return typeof data === "string" ? data : null;
  }
  if (first["type"] === "ExpressionTag") return expressionTagPath(first["expression"]);
  return null;
}

// Every markup navigation of a Svelte fragment in document order: a literal
// `href` on an anchor or a `Button` component, or `action` on a form. Only
// literal paths or template literals with a literal path prefix count;
// anything dynamic is never a navigation. The walk visits every nested node
// once, whatever block holds it.
export function collectSvelteMarkupNavigations(fragment: unknown, text: string): NavigationSite[] {
  const navigations: NavigationSite[] = [];
  const seen = new Set<object>();
  const visit = (node: unknown): void => {
    if (Array.isArray(node)) {
      for (const entry of node) visit(entry);
      return;
    }
    if (!isRecord(node) || seen.has(node)) return;
    seen.add(node);
    const isLink = node["type"] === "RegularElement" && (node["name"] === "a" || node["name"] === "form");
    const isButton = node["type"] === "Component" && node["name"] === "Button";
    if (isLink || isButton) {
      const wanted = node["name"] === "form" ? "action" : "href";
      const attributes = node["attributes"];
      if (Array.isArray(attributes)) {
        for (const attribute of attributes) {
          if (!isRecord(attribute) || attribute["type"] !== "Attribute" || attribute["name"] !== wanted) continue;
          const path = svelteAttributePath(attribute["value"]);
          if (isNavigationPath(path)) {
            const start = attribute["start"];
            navigations.push({ path, line: typeof start === "number" ? lineOfOffset(text, start) : 1 });
          }
        }
      }
    }
    for (const key of Object.keys(node)) {
      if (key === "parent" || key === "metadata") continue;
      visit(node[key]);
    }
  };
  visit(fragment);
  return navigations;
}

function extractTypeScript(text: string, fileName: string): FileFacts | null {
  const sourceFile = parseTypeScript(fileName, text);
  if (sourceFile === null) return null;
  return {
    marks: collectMarks(sourceFile),
    calls: collectCalls(sourceFile, 0),
    imports: collectImports(sourceFile),
    functions: collectExportedFunctions(sourceFile),
    route: collectRoutePath(sourceFile),
    navigations: collectNavigations(sourceFile, 0, collectGotoNames(sourceFile)),
  };
}

function extractSvelte(text: string): FileFacts | null {
  const parsed = parseSvelteScripts(text);
  if (!parsed.ok) return null;
  const marks: MarkSite[] = [];
  const calls: CallSite[] = [];
  const navigations: NavigationSite[] = [];
  const imports = new Set<string>();
  const sources: { sourceFile: ts.SourceFile; script: SvelteScript }[] = [];
  for (const script of parsed.scripts) {
    const sourceFile = parseTypeScript("script.ts", script.content);
    if (sourceFile === null) return null;
    sources.push({ sourceFile, script });
  }
  const gotoNames = new Set<string>();
  for (const { sourceFile } of sources) {
    for (const name of collectGotoNames(sourceFile)) gotoNames.add(name);
  }
  for (const { sourceFile, script } of sources) {
    if (script.context === "module") {
      for (const site of collectMarks(sourceFile)) {
        marks.push({ values: site.values, line: site.line + script.startLine - 1 });
      }
    }
    for (const site of collectCalls(sourceFile, script.startLine - 1)) calls.push(site);
    for (const site of collectNavigations(sourceFile, script.startLine - 1, gotoNames)) navigations.push(site);
    for (const specifier of collectImports(sourceFile)) imports.add(specifier);
  }
  for (const site of collectSvelteMarkupNavigations(parsed.fragment, text)) navigations.push(site);
  calls.sort((a, b) => a.line - b.line);
  navigations.sort((a, b) => a.line - b.line);
  return { marks, calls, imports: [...imports].sort(), functions: [], route: null, navigations };
}

// The line of the first mark site of a file, or 1 when the file carries none.
export function firstMarkLine(marks: MarkSite[]): number {
  return marks.length > 0 && marks[0] !== undefined ? marks[0].line : 1;
}

// Every implements value of every mark site in source order.
export function markValues(marks: MarkSite[]): string[] {
  const values: string[] = [];
  for (const site of marks) {
    for (const value of site.values) values.push(value);
  }
  return values;
}

// The cached facts of one file, or null when the file cannot be read or parsed.
// An unparseable file is diagnosed once through the context.
export function readFacts(absPath: string, kind: FileKind, context: ReaderContext): FileFacts | null {
  const text = readText(absPath);
  if (text === null) return null;
  const facts = withCache<FileFacts | null>(context.cacheDir, kind, text, () =>
    kind === "svelte" ? extractSvelte(text) : extractTypeScript(text, absPath),
  );
  if (facts === null) context.diagnose(absPath, kind === "svelte" ? "svelte" : "typescript");
  return facts;
}
