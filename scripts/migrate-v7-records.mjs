#!/usr/bin/env bun
// migrate-v7-records.mjs
//
// Migrates LiteSpec v6 record files to the v7 field vocabulary in one pass.
// Standalone ESM with no dependencies beyond node builtins and Bun.YAML.
// Parses with Bun.YAML.parse and writes deterministic YAML with 2-space indent.
// Preserves a leading yaml-language-server comment line. Other comments are
// dropped as part of the clean formatting.
//
// Usage:
//   bun scripts/migrate-v7-records.mjs [--check|--dry-run] <dir> [<dir> ...]
//
// Each directory is walked recursively for .yaml files, including hidden
// directories such as .litespec. Skipped: project.yaml, glossary.yaml and
// ui-source-lock.yaml files, and model/ and book/ subtrees. Files that fail
// to parse are reported as skipped and left untouched.
//
// With --check or --dry-run nothing is written. The exit code is 1 when at
// least one file would change and 0 when the tree is already migrated.
// Without a flag every changed file is rewritten and a summary is printed.
//
// The migration applies the 28 v7 renames: records splits into results and
// scenarios (refusals, permissions, entryPoints on set pages) with redundant
// entry type fields removed; domain-event carries and integration-event
// payload become fields; input/output become inputs/outputs; channel becomes
// channels; coverage becomes scope; screen displayStates becomes states while
// pageFrame is removed, task becomes job and archetype is inferred from the
// old pageFrame pattern or the route; idempotency keyScope becomes scope;
// setting valueType plus default/choices/bounds/range/unit/outOfRange move
// into a value object; schedule timezone becomes timeZone; many plus rowShape
// become type list plus items; result sentence/sentences become message plus
// translations; dataClass basis becomes basedOn; rule binds merges into
// governs; requirement and rule cites merge into basedOn; flow answers splits
// into solves (problem links) and basedOn (constraint links); triggered
// effect commands become runs; through, arrivesThrough, reachedThrough and
// deliveredThrough become via; external operation and integration-event action
// become direction; reaction and job settles become tracksState; access
// recentSignIn becomes recentSignInWindow; constraint openPoints becomes
// openQuestions; entry-point reauthenticationTriggers becomes
// reauthenticateWhen; reference-data variation becomes variesBy; cast person
// partyOf becomes party; decision admits failureBehavior becomes onFailure;
// goal observation date becomes observedOn and occasion date becomes occursOn;
// use-case exposure and background are removed. Idempotent: a migrated tree
// is left byte-identical by a second run.

import { readdir, readFile, writeFile, stat } from "node:fs/promises";
import path from "node:path";

const SCHEMA_VERSION = "7.0.0";
const SKIP_FILES = new Set(["project.yaml", "glossary.yaml", "ui-source-lock.yaml"]);
const SKIP_DIRS = new Set(["model", "book", "node_modules", ".git"]);
const ARCHETYPES = new Set([
  "HomePage",
  "ListPage",
  "WorkPage",
  "RecordPage",
  "SettingsPage",
  "FormPage",
  "AuthPage",
  "OutcomePage",
  "ReportPage",
]);
const PAGE_FRAME_ARCHETYPES = {
  "home-page": "HomePage",
  "list-page": "ListPage",
  "work-page": "WorkPage",
  "record-page": "RecordPage",
  "settings-page": "SettingsPage",
  "form-page": "FormPage",
  "auth-page": "AuthPage",
  "outcome-page": "OutcomePage",
  "report-page": "ReportPage",
};
const SETTING_VALUE_KEYS = ["default", "choices", "bounds", "range", "unit", "outOfRange"];
const FIELD_ITEM_KEYS = ["identifies", "choices", "contentTypes", "maxSize"];

// ---------------------------------------------------------------------------
// Minimal deterministic YAML writer (block style, 2-space indent).
// ---------------------------------------------------------------------------

function isPlainMap(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function needsQuotes(text) {
  if (text.length === 0) return true;
  if (/^\s|\s$/.test(text)) return true;
  if (text.includes("\t")) return true;
  if (/^(~|null|Null|NULL|true|True|TRUE|false|False|FALSE|yes|Yes|YES|no|No|NO|on|On|ON|off|Off|OFF)$/.test(text)) return true;
  if (/^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/.test(text)) return true;
  if (/^[-?:,[\]{}&*!|>'"%@`#]/.test(text)) return true;
  if (/:($|[\s])/.test(text)) return true;
  if (text.includes(" #")) return true;
  if (text.startsWith("- ") || text.startsWith("? ")) return true;
  return false;
}

function emitScalar(value) {
  if (value === null || value === undefined) return "null";
  if (value instanceof Date) {
    const iso = value.toISOString();
    if (iso.endsWith("T00:00:00.000Z")) return iso.slice(0, 10);
    return iso.endsWith(".000Z") ? `${iso.slice(0, -5)}Z` : iso;
  }
  if (typeof value === "boolean") return value ? "true" : "false";
  if (typeof value === "number") {
    if (Number.isFinite(value)) return String(value);
    return JSON.stringify(String(value));
  }
  const text = String(value);
  if (text.includes("\n")) return null; // caller renders a literal block
  if (needsQuotes(text)) return JSON.stringify(text);
  return text;
}

function emitLines(value, indent, lines) {
  const pad = " ".repeat(indent);
  if (Array.isArray(value)) {
    if (value.length === 0) {
      lines.push(`${pad}[]`);
      return;
    }
    for (const item of value) {
      if (isPlainMap(item)) {
        const keys = Object.keys(item);
        if (keys.length === 0) {
          lines.push(`${pad}- {}`);
          continue;
        }
        const first = keys[0];
        appendMappingValue(lines, indent, first, item[first], true);
        for (let i = 1; i < keys.length; i += 1) {
          appendMappingValue(lines, indent + 2, keys[i], item[keys[i]], false);
        }
      } else if (Array.isArray(item)) {
        lines.push(`${pad}-`);
        emitLines(item, indent + 2, lines);
      } else {
        const scalar = emitScalar(item);
        if (scalar === null) {
          lines.push(`${pad}- |`);
          for (const line of String(item).split("\n")) lines.push(`${" ".repeat(indent + 2)}${line}`);
        } else {
          lines.push(`${pad}- ${scalar}`);
        }
      }
    }
    return;
  }
  if (isPlainMap(value)) {
    const keys = Object.keys(value);
    if (keys.length === 0) {
      lines.push(`${pad}{}`);
      return;
    }
    for (const key of keys) appendMappingValue(lines, indent, key, value[key], false);
    return;
  }
  const scalar = emitScalar(value);
  if (scalar === null) {
    lines.push(`${pad}|`);
    for (const line of String(value).split("\n")) lines.push(`${" ".repeat(indent + 2)}${line}`);
  } else {
    lines.push(`${pad}${scalar}`);
  }
}

function appendMappingValue(lines, indent, key, value, dash) {
  const pad = " ".repeat(indent);
  const prefix = dash ? `${pad}- ${key}:` : `${pad}${key}:`;
  if (isPlainMap(value)) {
    if (Object.keys(value).length === 0) {
      lines.push(`${prefix} {}`);
      return;
    }
    lines.push(prefix);
    emitLines(value, indent + 2, lines);
    return;
  }
  if (Array.isArray(value)) {
    if (value.length === 0) {
      lines.push(`${prefix} []`);
      return;
    }
    lines.push(prefix);
    emitLines(value, indent + 2, lines);
    return;
  }
  const scalar = emitScalar(value);
  if (scalar === null) {
    lines.push(`${prefix} |`);
    for (const line of String(value).split("\n")) lines.push(`${" ".repeat(indent + 2)}${line}`);
  } else if (scalar === "null" && value === null) {
    lines.push(prefix);
  } else {
    lines.push(`${prefix} ${scalar}`);
  }
}

function dumpDocument(value) {
  const lines = [];
  emitLines(value, 0, lines);
  return `${lines.join("\n")}\n`;
}

// ---------------------------------------------------------------------------
// Migration helpers. Every helper returns true when it changed the mapping.
// ---------------------------------------------------------------------------

function sameValue(a, b) {
  return JSON.stringify(a) === JSON.stringify(b);
}

function unionLists(first, second) {
  const out = Array.isArray(first) ? [...first] : [first];
  const extra = Array.isArray(second) ? second : [second];
  for (const item of extra) {
    if (!out.some((seen) => sameValue(seen, item))) out.push(item);
  }
  return out;
}

// Replaces oldKey with newKey at the same position. Used when newKey is absent.
function replaceKey(obj, oldKey, newKey, newValue) {
  const out = {};
  for (const key of Object.keys(obj)) {
    if (key === oldKey) out[newKey] = newValue;
    else out[key] = obj[key];
  }
  for (const key of Object.keys(obj)) delete obj[key];
  for (const key of Object.keys(out)) obj[key] = out[key];
}

// Renames oldKey to newKey. Array values merge with an existing newKey value,
// scalar values keep the existing newKey value. Returns true when changed.
function renameKey(obj, oldKey, newKey, file, stats, tag) {
  if (!(oldKey in obj)) return false;
  const oldValue = obj[oldKey];
  if (newKey in obj) {
    if (Array.isArray(obj[newKey]) || Array.isArray(oldValue)) {
      const merged = unionLists(obj[newKey], oldValue);
      if (!sameValue(merged, obj[newKey])) obj[newKey] = merged;
      delete obj[oldKey];
      count(file, stats, tag);
      return true;
    }
    if (!sameValue(obj[newKey], oldValue)) warn(file, stats, `${tag}: kept ${newKey}, dropped conflicting ${oldKey}`);
    delete obj[oldKey];
    count(file, stats, tag);
    return true;
  }
  replaceKey(obj, oldKey, newKey, oldValue);
  count(file, stats, tag);
  return true;
}

function count(file, stats, tag) {
  file.tags.add(tag);
  stats.renames[tag] = (stats.renames[tag] ?? 0) + 1;
  file.changed = true;
}

function warn(file, stats, message) {
  file.warnings.push(message);
  stats.warnings.push(`${file.rel}: ${message}`);
  file.changed = true;
}

function entryKind(entry) {
  if (isPlainMap(entry) && typeof entry.type === "string") return entry.type;
  if (isPlainMap(entry) && typeof entry.id === "string") {
    const slash = entry.id.indexOf("/");
    if (slash > 0) {
      const prefix = entry.id.slice(0, slash);
      if (prefix === "result" || prefix === "scenario" || prefix === "refusal" || prefix === "permission") return prefix;
      if (prefix === "entry-point") return "entry-point";
    }
  }
  return null;
}

// Splits a legacy records array into typed child arrays. Returns true changed.
function migrateRecords(obj, file, stats) {
  if (!Array.isArray(obj.records)) return false;
  const entries = obj.records;
  const rootType = typeof obj.type === "string" ? obj.type : null;
  const buckets = { results: [], scenarios: [], refusals: [], permissions: [], entryPoints: [] };
  const kinds = { results: "result", scenarios: "scenario", refusals: "refusal", permissions: "permission", entryPoints: "entry-point" };
  let leftover = [];
  if (rootType === "use-case" || rootType === "reaction" || rootType === "job" || rootType === "refusal-set" || rootType === "permission-set" || rootType === "entry-point-set") {
    for (const entry of entries) {
      const kind = entryKind(entry);
      if (rootType === "use-case" && kind === "result") buckets.results.push(entry);
      else if (rootType === "use-case" && kind === "scenario") buckets.scenarios.push(entry);
      else if ((rootType === "reaction" || rootType === "job") && kind === "scenario") buckets.scenarios.push(entry);
      else if (rootType === "refusal-set" && (kind === "refusal" || kind === null)) buckets.refusals.push(entry);
      else if (rootType === "permission-set" && kind === "permission") buckets.permissions.push(entry);
      else if (rootType === "entry-point-set" && (kind === "entry-point" || kind === null)) buckets.entryPoints.push(entry);
      else leftover.push(entry);
    }
  } else {
    for (const entry of entries) {
      const kind = entryKind(entry);
      if (kind === "result") buckets.results.push(entry);
      else if (kind === "scenario") buckets.scenarios.push(entry);
      else if (kind === "refusal") buckets.refusals.push(entry);
      else if (kind === "permission") buckets.permissions.push(entry);
      else if (kind === "entry-point") buckets.entryPoints.push(entry);
      else leftover.push(entry);
    }
  }
  const out = {};
  let changed = false;
  for (const key of Object.keys(obj)) {
    if (key === "records") {
      for (const [bucket, singular] of Object.entries(kinds)) {
        if (buckets[bucket].length === 0) continue;
        out[bucket] = buckets[bucket].map((entry) => {
          if (isPlainMap(entry) && entry.type === singular) {
            const copy = { ...entry };
            delete copy.type;
            return copy;
          }
          return entry;
        });
      }
      if (leftover.length > 0) {
        out.records = leftover;
        warn(file, stats, `records.split: kept ${leftover.length} unclassified entr${leftover.length === 1 ? "y" : "ies"} under records`);
      }
      changed = true;
      count(file, stats, "records.split");
    } else {
      out[key] = obj[key];
    }
  }
  for (const key of Object.keys(obj)) delete obj[key];
  for (const key of Object.keys(out)) obj[key] = out[key];
  return changed;
}

function archetypeFromRoute(route) {
  if (typeof route !== "string" || !route.startsWith("/") || route.includes("<")) return null;
  const lower = route.toLowerCase();
  if (route === "/") return "HomePage";
  if (lower.includes("setting")) return "SettingsPage";
  if (lower.includes("login") || lower.includes("sign-in") || lower.includes("signin") || lower.includes("auth")) return "AuthPage";
  if (lower.includes("/new") || lower.includes("create") || lower.includes("edit") || lower.includes("add") || lower.includes("form")) return "FormPage";
  if (route.includes("{")) return "RecordPage";
  if (lower.includes("report")) return "ReportPage";
  if (lower.includes("outcome") || lower.includes("result") || lower.includes("success")) return "OutcomePage";
  return "ListPage";
}

function inferArchetype(doc) {
  const pattern = isPlainMap(doc.pageFrame) && typeof doc.pageFrame.pattern === "string" ? doc.pageFrame.pattern : null;
  if (pattern !== null && pattern in PAGE_FRAME_ARCHETYPES) {
    return { value: PAGE_FRAME_ARCHETYPES[pattern], from: "pageFrame" };
  }
  if (pattern !== null) {
    const generic = `${pattern.replace(/-page$/, "").replace(/(^|-)([a-z])/g, (m, dash, ch) => ch.toUpperCase())}Page`;
    if (ARCHETYPES.has(generic)) return { value: generic, from: "pageFrame" };
  }
  const fromRoute = archetypeFromRoute(doc.route);
  if (fromRoute !== null) return { value: fromRoute, from: "route" };
  return { value: "WorkPage", from: "fallback" };
}

// Screen transforms: drop pageFrame, task becomes job, infer archetype.
function migrateScreen(doc, file, stats) {
  let changed = false;
  if ("pageFrame" in doc) {
    if (!("archetype" in doc)) {
      const inferred = inferArchetype(doc);
      const out = {};
      for (const key of Object.keys(doc)) {
        if (key === "pageFrame") {
          out.archetype = inferred.value;
          count(file, stats, `screen.archetype=${inferred.value}(${inferred.from})`);
        } else {
          out[key] = doc[key];
        }
      }
      for (const key of Object.keys(doc)) delete doc[key];
      for (const key of Object.keys(out)) doc[key] = out[key];
      changed = true;
    } else {
      delete doc.pageFrame;
      changed = true;
    }
    count(file, stats, "pageFrame>removed");
  } else if (!("archetype" in doc)) {
    const inferred = inferArchetype(doc);
    doc.archetype = inferred.value;
    count(file, stats, `screen.archetype=${inferred.value}(${inferred.from})`);
    changed = true;
  }
  if ("task" in doc) {
    if (!("job" in doc)) replaceKey(doc, "task", "job", doc.task);
    else delete doc.task;
    count(file, stats, "task>job");
    changed = true;
  }
  return changed;
}

// Wraps a legacy setting valueType plus sibling value keys into a value object.
function migrateSettingValue(obj, file, stats) {
  if (!("valueType" in obj)) return false;
  const valueType = obj.valueType;
  const value = isPlainMap(obj.value) ? { ...obj.value } : {};
  if (!("type" in value)) value.type = valueType;
  for (const key of SETTING_VALUE_KEYS) {
    if (key in obj && !(key in value)) value[key] = obj[key];
  }
  const out = {};
  for (const key of Object.keys(obj)) {
    if (key === "valueType") {
      out.value = value;
      count(file, stats, "valueType>value");
    } else if (key === "value" || SETTING_VALUE_KEYS.includes(key)) {
      if (key === "value") out.value = value;
      // moved sibling keys are dropped; the value object holds them now
    } else {
      out[key] = obj[key];
    }
  }
  for (const key of Object.keys(obj)) delete obj[key];
  for (const key of Object.keys(out)) obj[key] = out[key];
  return true;
}

// Converts many plus rowShape on a field definition to type list plus items.
function migrateFieldShape(obj, file, stats) {
  let changed = false;
  const hasRowShape = isPlainMap(obj) && "rowShape" in obj;
  const many = isPlainMap(obj) ? obj.many : undefined;
  const manyTrue = many === true || (typeof many === "string" && many.trim().toLowerCase() === "true");
  if (hasRowShape || manyTrue) {
    const items = {};
    if (manyTrue && obj.type !== undefined && obj.type !== "list") items.type = obj.type;
    for (const key of Object.keys(obj)) {
      if (FIELD_ITEM_KEYS.includes(key)) items[key] = obj[key];
    }
    if (hasRowShape && isPlainMap(obj.rowShape)) {
      if ("items" in obj && isPlainMap(obj.items)) {
        warn(file, stats, "rowShape>items: kept existing items, dropped rowShape");
      } else {
        for (const key of Object.keys(obj.rowShape)) items[key] = obj.rowShape[key];
      }
    }
    const out = {};
    let inserted = false;
    for (const key of Object.keys(obj)) {
      if (key === "many" || key === "rowShape" || FIELD_ITEM_KEYS.includes(key)) continue;
      if (key === "type") {
        out.type = "list";
        out.items = items;
        inserted = true;
        continue;
      }
      out[key] = obj[key];
    }
    if (!inserted) {
      out.type = "list";
      out.items = items;
    }
    for (const key of Object.keys(obj)) delete obj[key];
    for (const key of Object.keys(out)) obj[key] = out[key];
    count(file, stats, hasRowShape ? "rowShape>items" : "many>list+items");
    changed = true;
  } else if (isPlainMap(obj) && "many" in obj) {
    delete obj.many;
    count(file, stats, "many>removed");
    changed = true;
  }
  return changed;
}

// Splits a legacy flow answers array into solves and basedOn.
function migrateAnswers(obj, file, stats) {
  if (!Array.isArray(obj.answers)) return false;
  const solves = [];
  const basedOn = [];
  const unknown = [];
  for (const id of obj.answers) {
    if (typeof id === "string" && id.startsWith("problem/")) solves.push(id);
    else if (typeof id === "string" && id.startsWith("constraint/")) basedOn.push(id);
    else {
      basedOn.push(id);
      unknown.push(id);
    }
  }
  if (unknown.length > 0) warn(file, stats, `answers>solves+basedOn: routed ${unknown.length} non-problem non-constraint link${unknown.length === 1 ? "" : "s"} to basedOn`);
  const out = {};
  for (const key of Object.keys(obj)) {
    if (key === "answers") {
      if (solves.length > 0) {
        out.solves = "solves" in obj && Array.isArray(obj.solves) ? unionLists(obj.solves, solves) : solves;
      }
      if (basedOn.length > 0) {
        if ("basedOn" in obj && Array.isArray(obj.basedOn)) out.basedOn = unionLists(obj.basedOn, basedOn);
        else if (!("basedOn" in out)) out.basedOn = basedOn;
      }
      count(file, stats, "answers>solves+basedOn");
    } else if (key === "solves" || key === "basedOn") {
      if (!(key in out)) out[key] = obj[key];
    } else {
      out[key] = obj[key];
    }
  }
  for (const key of Object.keys(obj)) delete obj[key];
  for (const key of Object.keys(out)) obj[key] = out[key];
  return true;
}

// Merges top-level requirement and rule cites into basedOn. Step cites stay.
function migrateTopCites(doc, file, stats) {
  if ((doc.type !== "requirement" && doc.type !== "rule") || !("cites" in doc)) return false;
  if (!Array.isArray(doc.cites)) {
    renameKey(doc, "cites", "basedOn", file, stats, "cites>basedOn");
    return true;
  }
  if ("basedOn" in doc && Array.isArray(doc.basedOn)) {
    const merged = unionLists(doc.basedOn, doc.cites);
    const out = {};
    for (const key of Object.keys(doc)) {
      if (key === "cites") continue;
      if (key === "basedOn") out.basedOn = merged;
      else out[key] = doc[key];
    }
    for (const key of Object.keys(doc)) delete doc[key];
    for (const key of Object.keys(out)) doc[key] = out[key];
  } else {
    replaceKey(doc, "cites", "basedOn", doc.cites);
  }
  count(file, stats, "cites>basedOn");
  return true;
}

// Renames date on goal observations and cast occasions. Figure values stay.
function migrateScopedDates(doc, file, stats) {
  let changed = false;
  if (doc.type === "goal" && Array.isArray(doc.observations)) {
    for (const item of doc.observations) {
      if (isPlainMap(item) && "date" in item) {
        if (!("observedOn" in item)) replaceKey(item, "date", "observedOn", item.date);
        else delete item.date;
        count(file, stats, "date>observedOn");
        changed = true;
      }
    }
  }
  if (doc.type === "cast" && Array.isArray(doc.occasions)) {
    for (const item of doc.occasions) {
      if (isPlainMap(item) && "date" in item) {
        if (!("occursOn" in item)) replaceKey(item, "date", "occursOn", item.date);
        else delete item.date;
        count(file, stats, "date>occursOn");
        changed = true;
      }
    }
  }
  return changed;
}

// Global recursive pass for context-free renames. Guards: action stays inside
// deadLetter blocks, and cites/carries/date keep their allowed scopes because
// they are handled by scoped passes instead of this walk.
function migrateDeep(node, file, stats, guarded) {
  if (Array.isArray(node)) {
    for (const item of node) migrateDeep(item, file, stats, guarded);
    return;
  }
  if (!isPlainMap(node)) return;
  renameKey(node, "coverage", "scope", file, stats, "coverage>scope");
  renameKey(node, "displayStates", "states", file, stats, "displayStates>states");
  renameKey(node, "keyScope", "scope", file, stats, "keyScope>scope");
  renameKey(node, "basis", "basedOn", file, stats, "basis>basedOn");
  renameKey(node, "payload", "fields", file, stats, "payload>fields");
  renameKey(node, "sentence", "message", file, stats, "sentence>message");
  renameKey(node, "sentences", "translations", file, stats, "sentences>translations");
  renameKey(node, "binds", "governs", file, stats, "binds>governs");
  renameKey(node, "commands", "runs", file, stats, "commands>runs");
  renameKey(node, "through", "via", file, stats, "through>via");
  renameKey(node, "arrivesThrough", "via", file, stats, "arrivesThrough>via");
  renameKey(node, "reachedThrough", "via", file, stats, "reachedThrough>via");
  renameKey(node, "deliveredThrough", "via", file, stats, "deliveredThrough>via");
  renameKey(node, "settles", "tracksState", file, stats, "settles>tracksState");
  renameKey(node, "recentSignIn", "recentSignInWindow", file, stats, "recentSignIn>recentSignInWindow");
  renameKey(node, "recentSignInLimit", "recentSignInWindow", file, stats, "recentSignInLimit>recentSignInWindow");
  renameKey(node, "openPoints", "openQuestions", file, stats, "openPoints>openQuestions");
  renameKey(node, "reauthenticationTriggers", "reauthenticateWhen", file, stats, "reauthenticationTriggers>reauthenticateWhen");
  renameKey(node, "variation", "variesBy", file, stats, "variation>variesBy");
  renameKey(node, "partyOf", "party", file, stats, "partyOf>party");
  renameKey(node, "failureBehavior", "onFailure", file, stats, "failureBehavior>onFailure");
  renameKey(node, "timezone", "timeZone", file, stats, "timezone>timeZone");
  renameKey(node, "channel", "channels", file, stats, "channel>channels");
  renameKey(node, "input", "inputs", file, stats, "input>inputs");
  renameKey(node, "output", "outputs", file, stats, "output>outputs");
  if ("target" in node && isPlainMap(node.target) && node.target.from === "output") {
    node.target.from = "outputs";
    count(file, stats, "audit.target.from:output>outputs");
  }
  if (!guarded) renameKey(node, "action", "direction", file, stats, "action>direction");
  if ("exposure" in node) {
    delete node.exposure;
    count(file, stats, "exposure>removed");
  }
  if ("background" in node) {
    delete node.background;
    count(file, stats, "background>removed");
  }
  if ("pageFrame" in node) {
    delete node.pageFrame;
    count(file, stats, "pageFrame>removed");
  }
  migrateSettingValue(node, file, stats);
  migrateFieldShape(node, file, stats);
  migrateAnswers(node, file, stats);
  const childGuarded = guarded || "deadLetter" in node;
  for (const key of Object.keys(node)) migrateDeep(node[key], file, stats, key === "deadLetter" ? true : childGuarded);
}

function migrateDocument(doc, file, stats) {
  if (!isPlainMap(doc)) return;
  migrateRecords(doc, file, stats);
  if (doc.type === "screen") migrateScreen(doc, file, stats);
  if (doc.type === "domain-event" && "carries" in doc) {
    renameKey(doc, "carries", "fields", file, stats, "carries>fields");
  }
  migrateTopCites(doc, file, stats);
  migrateScopedDates(doc, file, stats);
  migrateDeep(doc, file, stats, false);
}

// ---------------------------------------------------------------------------
// File walk, parse, write, summary.
// ---------------------------------------------------------------------------

function parseArgs(argv) {
  const dirs = [];
  let dryRun = false;
  for (const arg of argv) {
    if (arg === "--check" || arg === "--dry-run") dryRun = true;
    else if (arg === "--help" || arg === "-h") {
      console.log("Usage: bun scripts/migrate-v7-records.mjs [--check|--dry-run] <dir> [<dir> ...]");
      console.log("Migrates LiteSpec v6 record files to the v7 field vocabulary.");
      process.exit(0);
    } else if (arg.startsWith("-")) {
      console.error(`Unknown flag: ${arg}`);
      process.exit(2);
    } else {
      dirs.push(arg);
    }
  }
  return { dirs, dryRun };
}

async function collectYamlFiles(roots) {
  const found = [];
  async function walk(dir) {
    let entries;
    try {
      entries = await readdir(dir, { withFileTypes: true });
    } catch {
      return;
    }
    entries.sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0));
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (SKIP_DIRS.has(entry.name)) continue;
        await walk(full);
      } else if (entry.isFile() && entry.name.endsWith(".yaml") && !SKIP_FILES.has(entry.name)) {
        found.push(full);
      }
    }
  }
  for (const root of roots) {
    const abs = path.resolve(root);
    let info;
    try {
      info = await stat(abs);
    } catch {
      console.error(`Directory not found: ${root}`);
      process.exit(2);
    }
    if (info.isFile() && abs.endsWith(".yaml") && !SKIP_FILES.has(path.basename(abs))) found.push(abs);
    else if (info.isDirectory()) await walk(abs);
    else {
      console.error(`Not a directory: ${root}`);
      process.exit(2);
    }
  }
  return found;
}

function splitHeader(text) {
  const lines = text.split("\n");
  const header = [];
  let i = 0;
  while (i < lines.length && lines[i].startsWith("#") && lines[i].includes("yaml-language-server")) {
    header.push(lines[i]);
    i += 1;
  }
  return { header: header.join("\n"), body: lines.slice(i).join("\n") };
}

function yamlParse(text) {
  const engine = globalThis.Bun?.YAML;
  if (!engine) {
    console.error("Bun.YAML is unavailable. Run this script with bun: bun scripts/migrate-v7-records.mjs <dir>");
    process.exit(2);
  }
  return engine.parse(text);
}

async function main() {
  const { dirs, dryRun } = parseArgs(process.argv.slice(2));
  if (dirs.length === 0) {
    console.error("Usage: bun scripts/migrate-v7-records.mjs [--check|--dry-run] <dir> [<dir> ...]");
    process.exit(2);
  }
  const files = await collectYamlFiles(dirs);
  const stats = { scanned: files.length, changed: 0, renames: {}, warnings: [] };
  const changedFiles = [];
  for (const full of files) {
    const rel = path.relative(process.cwd(), full).split(path.sep).join("/");
    const file = { rel, tags: new Set(), warnings: [], changed: false };
    let text;
    try {
      text = await readFile(full, "utf8");
    } catch {
      stats.warnings.push(`${rel}: unreadable, skipped`);
      continue;
    }
    const { header, body } = splitHeader(text);
    let doc;
    try {
      doc = yamlParse(body);
    } catch (error) {
      stats.warnings.push(`${rel}: parse failed, skipped (${String(error?.message ?? error).split("\n")[0]})`);
      continue;
    }
    if (!isPlainMap(doc)) continue;
    migrateDocument(doc, file, stats);
    if (!file.changed) continue;
    stats.changed += 1;
    changedFiles.push({ rel, tags: [...file.tags].sort(), warnings: file.warnings });
    if (!dryRun) {
      const out = header.length > 0 ? `${header}\n${dumpDocument(doc)}` : dumpDocument(doc);
      await writeFile(full, out, "utf8");
    }
  }
  changedFiles.sort((a, b) => (a.rel < b.rel ? -1 : a.rel > b.rel ? 1 : 0));
  const mode = dryRun ? "dry run: no writes" : "wrote changes";
  console.log(`migrate-v7-records ${SCHEMA_VERSION}: scanned ${stats.scanned} files, ${dryRun ? "would change" : "changed"} ${stats.changed} (${mode})`);
  for (const entry of changedFiles) {
    console.log(`  ${entry.rel}: ${entry.tags.join(", ")}`);
  }
  if (stats.warnings.length > 0) {
    console.log("warnings:");
    for (const warning of stats.warnings) console.log(`  ${warning}`);
  }
  console.log(JSON.stringify({ scanned: stats.scanned, changed: stats.changed, dryRun, renames: stats.renames, files: changedFiles.map((e) => e.rel) }));
  if (dryRun && stats.changed > 0) process.exit(1);
}

await main();
