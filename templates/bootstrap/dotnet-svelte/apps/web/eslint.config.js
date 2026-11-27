import js from "@eslint/js";
import svelte from "eslint-plugin-svelte";
import globals from "globals";
import tseslint from "typescript-eslint";

// The route layout ban: page geometry comes from the floorplans and the
// shared primitives, so a route file never writes raw layout or spacing
// utilities. The rule reads the route text because the ban is about the
// class names the route writes, not about the script it runs.

/**
 * The class heads a route file must never use.
 * @type {Set<string>}
 */
const bannedUtilityHeads = new Set([
  "m",
  "mt",
  "mb",
  "mx",
  "my",
  "p",
  "px",
  "py",
  "gap",
  "space",
  "flex",
  "grid",
  "w",
  "h",
]);

/**
 * Whether a class token rebuilds geometry the primitives own.
 * @param {string} token the class token as written in the route file.
 * @returns {boolean} true when the token is a banned layout utility.
 */
function isBannedUtility(token) {
  if (token === "flex" || token === "grid") {
    return true;
  }
  const head = token.split("-")[0] ?? "";
  return bannedUtilityHeads.has(head);
}

/**
 * The banned utilities in one class attribute value.
 * @param {string} value the class attribute value as written.
 * @returns {string[]} the banned tokens in the value, in written order.
 */
function bannedInClassValue(value) {
  return value
    .split(/\s+/)
    .map((token) => token.trim())
    .filter((token) => token.length > 0 && isBannedUtility(token));
}

/**
 * @typedef {object} RouteRuleContext
 * @property {string} [filename] the physical file name as ESLint reports it.
 * @property {{ text: string }} sourceCode the file text under lint.
 * @property {(finding: { node: unknown, message: string }) => void} report the finding sink.
 */

/**
 * The file names a finding was already reported for, so the shared
 * processor blocks of one route file report each banned token once.
 * @type {Set<string>}
 */
const reportedTokens = new Set();

const routeLayoutPlugin = {
  rules: {
    "no-layout-utilities": {
      meta: {
        type: "problem",
        docs: {
          description: "Refuse raw layout and spacing utilities in route files.",
        },
      },
      /**
       * @param {RouteRuleContext} context the rule context of the file under lint.
       * @returns {{ Program: (node: unknown) => void }} the route text visitor.
       */
      create(context) {
        return {
          /**
           * @param {unknown} _node the program node the finding points at.
           */
          Program(_node) {
            const filename = context.filename;
            if (typeof filename !== "string" || !filename.endsWith("+page.svelte")) {
              return;
            }
            const text = context.sourceCode.text;
            /** @type {Set<string>} */
            const found = new Set();
            for (const match of text.matchAll(/class\s*=\s*"([^"]*)"/g)) {
              for (const token of bannedInClassValue(match[1] ?? "")) {
                found.add(token);
              }
            }
            for (const match of text.matchAll(/class:([A-Za-z0-9_-]+)/g)) {
              const token = match[1] ?? "";
              if (token.length > 0 && isBannedUtility(token)) {
                found.add(token);
              }
            }
            for (const token of [...found].sort()) {
              const key = `${filename}:${token}`;
              if (reportedTokens.has(key)) {
                continue;
              }
              reportedTokens.add(key);
              context.report({
                node: _node,
                message: `Route file uses the banned layout utility "${token}"; compose a floorplan or a shared primitive instead.`,
              });
            }
          },
        };
      },
    },
  },
};

export default [
  { ignores: ["build/", ".svelte-kit/", "dist/"] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...svelte.configs["flat/recommended"],
  {
    // Svelte components carry <script lang="ts">: the Svelte parser hands
    // those blocks to the TypeScript parser.
    files: ["**/*.svelte", "**/*.svelte.ts"],
    languageOptions: {
      parserOptions: { parser: tseslint.parser, extraFileExtensions: [".svelte"] },
    },
  },
  {
    languageOptions: {
      globals: { ...globals.browser, ...globals.node },
    },
  },
  {
    files: ["src/routes/**/+page.svelte"],
    plugins: { "route-layout": routeLayoutPlugin },
    rules: { "route-layout/no-layout-utilities": "error" },
  },
];
