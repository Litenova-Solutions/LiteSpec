(function () {
"use strict";
// The book client: outline, detail, rail and review from sharded data.
// The shell loads data/manifest.js, then this script loads the lexicon,
// the outline of each opened layer, record packs on navigation, search
// packs on first search, and the queue pack on first queue view.
var MAN = null, LEX = null, QUEUE = null;
var REC = {};
var NODE = {};
var SEARCH = {};
var searchLoaded = false, queueLoaded = false, queueLoading = false, outlineLoading = false, treeLoading = false;
var outlineLoaded = {}, shardLoaded = {};
var layerOf = {}, parentOf = {}, depthOf = {}, order = [], kidsOf = {}, rootsOf = {};
var layerByKey = {};
var orderIdx = {};
var TYPE = {}, PLURAL = {}, GL = {}, SX = {};
var EXPLAIN = {};
var LAYER_ICON = { intent: "core", behavior: "folder", implementation: "code", proof: "flask", governance: "deviation", glossary: "term" };
var TODAY = new Date();
var VETO = 7;

var P = {
  product: '<path d="M21 8 12 3 3 8v8l9 5 9-5z"/><path d="m3 8 9 5 9-5"/><path d="M12 13v8"/>',
  problem: '<path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><path d="M12 9v4"/><path d="M12 17h.01"/>',
  goal: '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1"/>',
  feature: '<path d="m12 3 9 4.5-9 4.5-9-4.5z"/><path d="m3 12 9 4.5 9-4.5"/><path d="m3 16.5 9 4.5 9-4.5"/>',
  requirement: '<path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/><path d="m9 14.5 2 2 4-4"/>',
  decision: '<circle cx="6" cy="6" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="12" cy="18" r="2.5"/><path d="M6 8.5v1a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2v-1"/><path d="M12 11.5v4"/>',
  constraint: '<path d="M3 21h18"/><path d="M6 17v-6"/><path d="M10 17v-6"/><path d="M14 17v-6"/><path d="M18 17v-6"/><path d="m12 3 8 5H4z"/>',
  research: '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>',
  journey: '<circle cx="6" cy="18.5" r="2.5"/><path d="M8.5 18.5H17a3.5 3.5 0 0 0 0-7H7a3.5 3.5 0 0 1 0-7h8.5"/><circle cx="18" cy="4.5" r="2.5"/>',
  module: '<path d="M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.7-.9l-.8-1.2A2 2 0 0 0 7.9 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/>',
  aggregate: '<rect x="3" y="3" width="18" height="18" rx="3"/><path d="M3 9h18"/><path d="M9 9v12"/>',
  rule: '<path d="M12 21s7.5-3.5 7.5-9.5V5.5L12 3 4.5 5.5v6C4.5 17.5 12 21 12 21z"/>',
  event: '<path d="M4 12h3l3-8 4 16 3-8h3"/>',
  "use-case": '<path d="M13 2.5 4 13.5h7.5l-1 8 9-11H12z"/>',
  scenario: '<path d="M9 6h11"/><path d="M9 12h11"/><path d="M9 18h11"/><path d="m3 6 1 1 2-2"/><path d="m3 12 1 1 2-2"/><path d="m3 18 1 1 2-2"/>',
  actor: '<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
  role: '<rect x="3" y="5" width="18" height="14" rx="2"/><circle cx="9" cy="12" r="2.5"/><path d="M14 10h4"/><path d="M14 14h4"/>',
  permission: '<circle cx="8" cy="15" r="4"/><path d="m11 12 9-9"/><path d="m17 6 3 3"/>',
  "entry-point": '<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><path d="m10 17 5-5-5-5"/><path d="M15 12H3"/>',
  "external-system": '<rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/><path d="M6 6h.01M6 18h.01"/>',
  limit: '<path d="M12 14l4-4"/><path d="M3.3 19a10 10 0 1 1 17.4 0"/>',
  runbook: '<path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/>',
  "known-gap": '<circle cx="12" cy="12" r="9" stroke-dasharray="3 3"/>',
  deviation: '<path d="M6 3v12"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/>',
  term: '<path d="M4 7V5h16v2"/><path d="M9 19h6"/><path d="M12 5v14"/>',
  core: '<path d="M12 2 2 7l10 5 10-5-10-5Z"/><path d="m2 17 10 5 10-5"/><path d="m2 12 10 5 10-5"/>',
  folder: '<path d="M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.7-.9l-.8-1.2A2 2 0 0 0 7.9 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/>',
  shield: '<path d="M20 13c0 5-3.5 7.5-7.7 9a1 1 0 0 1-.6 0C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.2-2.7a1.2 1.2 0 0 1 1.6 0C14.5 3.8 17 5 19 5a1 1 0 0 1 1 1z"/><path d="m9 12 2 2 4-4"/>',
  server: '<rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/><path d="M6 6h.01M6 18h.01"/>',
  book: '<path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/>',
  chev: '<path d="m9 6 6 6-6 6"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
  moon: '<path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/>',
  download: '<path d="M12 4v11"/><path d="m7 10 5 5 5-5"/><path d="M5 20h14"/>',
  approved: '<circle cx="12" cy="12" r="9"/><path d="m8.5 12 2.5 2.5 4.5-5"/>',
  mine: '<circle cx="12" cy="12" r="9"/><path d="m8.5 12 2.5 2.5 4.5-5"/>',
  draft: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  inherited: '<circle cx="12" cy="12" r="9" stroke-dasharray="2.5 2.5"/><path d="M12 8v4l2.5 1.5"/>',
  question: '<circle cx="12" cy="12" r="9"/><path d="M9.6 9.3a2.5 2.5 0 0 1 4.8.9c0 1.7-2.4 2.3-2.4 2.3"/><path d="M12 16.5h.01"/>',
  changes: '<circle cx="12" cy="12" r="9"/><path d="M12 7.5v5"/><path d="M12 16.5h.01"/>',
  link: '<path d="M9 17H7A5 5 0 0 1 7 7h2"/><path d="M15 7h2a5 5 0 1 1 0 10h-2"/><path d="M8 12h8"/>',
  flask: '<path d="M9 3h6"/><path d="M10 3v6l-5.5 9.5A2 2 0 0 0 6.2 21h11.6a2 2 0 0 0 1.7-2.5L14 9V3"/><path d="M7.5 15h9"/>',
  passing: '<circle cx="12" cy="12" r="9"/><path d="m8.5 12 2.5 2.5 4.5-5"/>',
  failing: '<circle cx="12" cy="12" r="9"/><path d="m9 9 6 6M15 9l-6 6"/>',
  awaiting: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  unconfirmed: '<circle cx="12" cy="12" r="9"/><path d="M8 12h8"/>',
  unproved: '<circle cx="12" cy="12" r="9" stroke-dasharray="2.5 2.5"/>',
  x: '<circle cx="12" cy="12" r="9"/><path d="m9 9 6 6M15 9l-6 6"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  minus: '<path d="M5 12h14"/>',
  arrow: '<path d="M5 12h14"/><path d="m13 6 6 6-6 6"/>',
  ext: '<path d="M15 3h6v6"/><path d="M10 14 21 3"/><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/>',
  alert: '<path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><path d="M12 9v4"/><path d="M12 17h.01"/>',
  info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v5"/><path d="M12 8h.01"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  quote: '<path d="M7 11H4V7h4v5a4 4 0 0 1-4 4"/><path d="M17 11h-3V7h4v5a4 4 0 0 1-4 4"/>',
  lock: '<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
  globe: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18"/><path d="M12 3a14 14 0 0 1 0 18a14 14 0 0 1 0-18"/>'
};

P.grid = '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>';
P.list = '<path d="M8 6h13"/><path d="M8 12h13"/><path d="M8 18h13"/><path d="M3 6h.01"/><path d="M3 12h.01"/><path d="M3 18h.01"/>';
P.tree = '<path d="M5 3v14a2 2 0 0 0 2 2h4"/><path d="M5 9h6"/><rect x="13" y="6" width="8" height="6" rx="1"/><rect x="13" y="16" width="8" height="6" rx="1"/>';
P.ballot = '<path d="M9 11l3 3 8-8"/><path d="M20 12v7a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9"/>';
P.filter = '<path d="M3 5h18l-7 8v6l-4 2v-8z"/>';
P.sort = '<path d="m3 16 4 4 4-4"/><path d="M7 20V4"/><path d="m21 8-4-4-4 4"/><path d="M17 4v16"/>';
P.up = '<path d="m18 15-6-6-6 6"/>';
P.left = '<path d="m15 18-6-6 6-6"/>';
P.right = '<path d="m9 18 6-6-6-6"/>';
P.expand = '<path d="m7 15 5 5 5-5"/><path d="m7 9 5-5 5 5"/>';
P.collapse = '<path d="m7 20 5-5 5 5"/><path d="m7 4 5 5 5-5"/>';
P.target = '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="3"/>';
P.close = '<path d="M18 6 6 18"/><path d="m6 6 12 12"/>';
P.automation = '<path d="M4 12h4l3-8 4 16 3-8h2"/><circle cx="20" cy="12" r="1.5"/>';
P.notification = '<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10.3 21a1.9 1.9 0 0 0 3.4 0"/>';
P.setting = '<path d="M4 6h10"/><path d="M18 6h2"/><circle cx="16" cy="6" r="2"/><path d="M4 12h4"/><path d="M12 12h8"/><circle cx="10" cy="12" r="2"/><path d="M4 18h12"/><path d="M20 18h0"/><circle cx="18" cy="18" r="2"/>';
P["integration-event"] = '<path d="M4 12h3l3-8 4 16 3-8h3"/><path d="M2 20h20"/>';
P.result = P.passing;
P.reviewed = P.approved;
P.retired = '<circle cx="12" cy="12" r="9"/><path d="M8 12h8"/>';
P.shieldq = P.shield;
P.screen = '<rect x="3" y="4" width="18" height="13" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/>';
P.topic = P.book; P["architecture-layer"] = P.core; P.frontend = P.screen; P.route = P.link; P.code = '<path d="m8 8-4 4 4 4"/><path d="m16 8 4 4-4 4"/>';
P.test = P.flask; P["test-project"] = P.folder; P["test-class"] = P.flask; P.finding = P.alert; P["finding-code"] = P.alert; P["check-tier"] = P.shield; P.layer = P.core;
P.next = '<path d="M5 12h12"/><path d="m13 6 6 6-6 6"/><path d="M3 5v14"/>';
P.missing = '<circle cx="12" cy="12" r="9" stroke-dasharray="3 3"/><path d="M12 8v5"/><path d="M12 16h.01"/>';
P.stale = '<circle cx="12" cy="12" r="9"/><path d="M12 7v5"/><path d="M9 15h6"/>';
function ic(name, cls) { return '<svg class="' + (cls || "icon") + '" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + (P[name] || P.term) + "</svg>"; }
function esc(s) { return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }

// ---------- shard loading ----------
// Every shard arrives as data/<key>.js calling BookData.put; the client
// inserts script tags through BookData.load and never uses fetch.
function bookData() { return window.BookData; }
function loadShards(keys) { return Promise.all(keys.map(function (k) { return bookData().load(k); })); }
function walkOutline(layerKey, node, parent, depth) {
  if (!node || !node.id) return [];
  if (node.kind === "group") {
    var flat = [];
    (node.children || []).forEach(function (c) { flat = flat.concat(walkOutline(layerKey, c, parent, depth)); });
    return flat;
  }
  var id = node.id;
  layerOf[id] = layerKey; parentOf[id] = parent; depthOf[id] = depth; order.push(id);
  NODE[id] = { label: node.label || id, type: node.type || id.split("/")[0], status: node.status || null };
  var kids = [];
  (node.children || []).forEach(function (c) { kids = kids.concat(walkOutline(layerKey, c, id, depth + 1)); });
  kidsOf[id] = kids;
  return [id];
}
function addLayerOutline(layerKey, shardArrays) {
  var roots = [];
  (shardArrays || []).forEach(function (arr) {
    (arr || []).forEach(function (n) { roots = roots.concat(walkOutline(layerKey, n, null, 1)); });
  });
  rootsOf[layerKey] = (rootsOf[layerKey] || []).concat(roots);
  orderIdx = {}; order.forEach(function (id, i) { orderIdx[id] = i; });
  kidsOf["layer/" + layerKey] = rootsOf[layerKey];
  // Each layer is also a page, so its whole tree can be read from the top.
  var meta = (LEX.layers || {})[layerKey] || {}, manifestLayer = layerByKey[layerKey] || {};
  REC["layer/" + layerKey] = { type: "layer", title: manifestLayer.label || meta.label || layerKey, fields: { question: meta.question || "", writes: meta.writes || "", reviews: meta.reviews || "" }, bodyHtml: "", sections: null, src: null };
}
function ensureLayer(key) {
  if (outlineLoaded[key]) return Promise.resolve();
  var layer = layerByKey[key];
  if (!layer) return Promise.resolve();
  var keys = layer.outline || [];
  if (!keys.length) { outlineLoaded[key] = true; rootsOf[key] = rootsOf[key] || []; kidsOf["layer/" + key] = rootsOf[key]; return Promise.resolve(); }
  return loadShards(keys).then(function (values) {
    addLayerOutline(key, values);
    outlineLoaded[key] = true;
  }, function () { outlineLoaded[key] = true; });
}
function loadLayers(keys) { return Promise.all((keys || []).map(ensureLayer)); }
function allOutlinesLoaded() { return (MAN.layers || []).every(function (l) { return outlineLoaded[l.key]; }); }
function ensureAllOutlines() {
  var keys = (MAN.layers || []).map(function (l) { return l.key; }).filter(function (k) { return !outlineLoaded[k]; });
  return loadLayers(keys);
}
function storeRecordPack(value) { if (value) Object.keys(value).forEach(function (k) { REC[k] = value[k]; }); }
function ensureRecords(ids) {
  var shards = {};
  (ids || []).forEach(function (id) {
    id = base(id);
    if (!REC[id] && MAN.shardOf && MAN.shardOf[id] && !shardLoaded[MAN.shardOf[id]]) shards[MAN.shardOf[id]] = 1;
  });
  var keys = Object.keys(shards);
  if (!keys.length) return Promise.resolve();
  return loadShards(keys).then(function (values) {
    keys.forEach(function (k, i) { shardLoaded[k] = true; storeRecordPack(values[i]); });
  }, function () { keys.forEach(function (k) { shardLoaded[k] = true; }); });
}
function ensureSearch() {
  if (searchLoaded) return Promise.resolve();
  var keys = MAN.search || [];
  if (!keys.length) { searchLoaded = true; return Promise.resolve(); }
  return loadShards(keys).then(function (values) {
    (values || []).forEach(function (rows) {
      (rows || []).forEach(function (r) { if (r && r[0]) SEARCH[r[0]] = r[3] || ""; });
    });
    searchLoaded = true;
  }, function () { searchLoaded = true; });
}
function ensureQueue() {
  if (queueLoaded) return Promise.resolve();
  if (queueLoading) return new Promise(function (resolve) { var t = setInterval(function () { if (queueLoaded) { clearInterval(t); resolve(); } }, 50); });
  queueLoading = true;
  return bookData().load("queue").then(function (value) {
    QUEUE = value || { batches: {}, ballots: [], security: [] };
    if (QUEUE.review && QUEUE.review.vetoWindowDays) VETO = QUEUE.review.vetoWindowDays;
    queueLoaded = true; queueLoading = false;
  }, function () { QUEUE = { batches: {}, ballots: [], security: [] }; queueLoaded = true; queueLoading = false; });
}

// ---------- lexicon ----------
// Type labels, field labels, link phrases, glossary definitions, model
// section texts and finding meanings all come from the lexicon shard.
function lexSection(title) { var s = (LEX.sections || {})[title]; return s ? s.text || "" : ""; }
function lexSectionSource(title, fallback) { var s = (LEX.sections || {})[title]; return (s && s.source) || fallback; }
function gdef(term) { var g = (LEX.glossary || {})[String(term).toLowerCase()]; return g ? g.definition || "" : ""; }
function findingMeaning(code) { var f = (LEX.findings || {})[code]; return f ? f.meaning || "" : ""; }
function normCarriers(link) {
  var c = link.carriers;
  if (Array.isArray(c)) return c;
  if (typeof c === "string") return c.split(",").map(function (s) { return s.trim(); }).filter(Boolean);
  return [];
}
function resolveField(field) {
  var candidate = field, dot = candidate.indexOf(".");
  if (dot > 0 && (LEX.types || {})[candidate.slice(0, dot)]) candidate = candidate.slice(dot + 1);
  var links = LEX.links || [];
  while (true) {
    if (links.some(function (l) { return l.field === candidate; })) return candidate;
    var nested = candidate.lastIndexOf(".");
    if (nested < 0) break;
    candidate = candidate.slice(0, nested);
  }
  // A leaf link field reads from its own row: journey steps name their actor,
  // and the Links table carries that row as actor.
  var leaf = field.slice(field.lastIndexOf(".") + 1);
  if (leaf !== candidate && links.some(function (l) { return l.field === leaf; })) return leaf;
  return null;
}
function linkRow(field, carrierType) {
  var links = LEX.links || [];
  var candidate = resolveField(field);
  if (candidate === null) return null;
  var cands = links.filter(function (l) { return l.field === candidate; });
  if (!cands.length) return null;
  if (carrierType) {
    for (var i = 0; i < cands.length; i++) {
      var carriers = normCarriers(cands[i]);
      if (carriers.indexOf(carrierType) >= 0) return { forward: cands[i].forward, inverse: cands[i].inverse, carriers: carriers };
    }
  }
  return { forward: cands[0].forward, inverse: cands[0].inverse, carriers: normCarriers(cands[0]) };
}
function phraseFor(field, carrierType, dir) {
  var row = linkRow(field, carrierType);
  if (!row || !row[dir]) return field;
  var text = row[dir];
  if (text.indexOf(" / ") >= 0) {
    var parts = text.split(" / "), idx = row.carriers.indexOf(carrierType);
    return (idx >= 0 && parts[idx] ? parts[idx] : parts[0]).trim();
  }
  return text;
}
// A field reads per carrier when its rows carry different inverse phrases.
function linkVaries(field) {
  var candidate = resolveField(field);
  if (candidate === null) return false;
  var rows = (LEX.links || []).filter(function (l) { return l.field === candidate; });
  if (rows.some(function (l) { return (l.forward || "").indexOf(" / ") >= 0 || (l.inverse || "").indexOf(" / ") >= 0; })) return true;
  return uniq(rows.map(function (l) { return l.inverse || ""; })).length > 1;
}
function buildLexiconCaches() {
  TYPE = {}; PLURAL = {};
  Object.keys(LEX.types || {}).forEach(function (t) {
    TYPE[t] = LEX.types[t].label || t;
    PLURAL[t] = LEX.types[t].plural || LEX.types[t].label || t;
  });
  GL = {};
  Object.keys(LEX.glossary || {}).forEach(function (k) { GL[k] = LEX.glossary[k].definition || ""; });
  SX = { statuses: lexSection("Statuses"), riskTiers: lexSection("Risk Tiers"), checkLevels: lexSection("Check Levels"), batch: lexSection("Approval as Data") || lexSection("Questions and Ballots") };
  EXPLAIN = {
    draft: ["Draft", SX.statuses, lexSectionSource("Statuses", "model.md, Statuses")], approved: ["Approved", SX.statuses, lexSectionSource("Statuses", "model.md, Statuses")], reviewed: ["Reviewed", SX.statuses, lexSectionSource("Statuses", "model.md, Statuses")], retired: ["Retired", SX.statuses, lexSectionSource("Statuses", "model.md, Statuses")],
    inherited: ["Effective status", GL["effective status"], "Glossary"], tier1: ["Risk tiers", SX.riskTiers, lexSectionSource("Risk Tiers", "model.md, Risk Tiers")], transcribed: ["Transcription", GL.transcription, "Glossary"],
    awaiting: ["Verified", GL.verified, "Glossary"], unconfirmed: ["Verified", GL.verified, "Glossary"], unproved: ["Verified", GL.verified, "Glossary"], passing: ["Verified", GL.verified, "Glossary"], failing: ["Verified", GL.verified, "Glossary"], stale: ["Stale", GL.stale, "Glossary"],
    norun: ["Verified", GL.verified, "Glossary"], settled: ["Statuses", SX.statuses, lexSectionSource("Statuses", "model.md, Statuses")], implemented: ["Implemented", GL.implemented, "Glossary"], reachable: ["Reachable", GL.reachable, "Glossary"],
    G: ["Check level", SX.checkLevels, lexSectionSource("Check Levels", "model.md, Check Levels")], S: ["Check level", SX.checkLevels, lexSectionSource("Check Levels", "model.md, Check Levels")], A: ["Check level", SX.checkLevels, lexSectionSource("Check Levels", "model.md, Check Levels")],
    known: ["Known gap", GL["known gap"], "Glossary"], batch: ["Batch review", SX.batch, "release.md, Batch Review"], finding: ["Finding", GL.finding, "Glossary"], ballot: ["Ballot", GL.ballot, "Glossary"]
  };
}

// The record layer a type belongs to, from LiteSpec's model page.
function kindOf(id) {
  var t = typeOf(id), q = (LEX.types || {})[t];
  if (!q || !q.layer) return "entry";
  var layer = String(q.layer).split(",")[0].trim();
  return layer.indexOf("intent") === 0 ? "intent" : layer.indexOf("behavior") === 0 ? "behavior" : layer.indexOf("governance") === 0 ? "governance" : "entry";
}
function isBeh(id) { return kindOf(id) === "behavior"; }
// A LiteSpec record has a type in the model page's record type table; everything else is code, a test, a finding or a grouping.
function isReal(id) { return !!(LEX.types || {})[typeOf(id)]; }
function typeName(t) { return TYPE[t] || t; }
function tag(t) { return (TYPE[t] || t).toLowerCase(); }
function base(s) { return String(s || "").split("#")[0]; }
function title(id) { id = base(id); return REC[id] ? REC[id].title : NODE[id] ? NODE[id].label : id; }
function typeOf(id) { id = base(id); return REC[id] ? REC[id].type : NODE[id] ? NODE[id].type : id.split("/")[0]; }
function cap(s) { s = String(s == null ? "" : s); return s.charAt(0).toUpperCase() + s.slice(1); }
function plural(n, one, many) { return n + " " + (n === 1 ? one : (many || one + "s")); }
function uniq(a) { return a.filter(function (x, i) { return a.indexOf(x) === i; }); }
function siblingsOf(id) { var p = parentOf[id]; return p ? kidsOf[p] : rootsOf[layerOf[id]]; }

// ---------- relations ----------
// Each record carries its outgoing and incoming edges with field keys;
// every word about a link comes from the lexicon shard.
function edgeList(r, key, targetKey) {
  var raw = r ? r[key] : null;
  if (!raw) return [];
  if (Array.isArray(raw)) return raw.map(function (e) { return { field: e.key || e.field || "", id: base(e[targetKey] || e.id || e.to || e.from || "") }; }).filter(function (e) { return e.field && e.id; });
  var list = [];
  Object.keys(raw).forEach(function (f) { (raw[f] || []).forEach(function (t) { list.push({ field: f, id: base(t) }); }); });
  return list;
}
function outEdges(id) { return edgeList(REC[base(id)], "out", "to"); }
function inEdges(id) { return edgeList(REC[base(id)], "in", "from"); }
function fieldMatches(edgeField, field) { return edgeField === field || edgeField.slice(-field.length - 1) === "." + field; }
function outs(id, field) { return outEdges(id).filter(function (e) { return fieldMatches(e.field, field); }).map(function (e) { return e.id; }); }
function ins(id, field) { return inEdges(id).filter(function (e) { return fieldMatches(e.field, field); }).map(function (e) { return e.id; }); }
function performers(id) {
  var r = REC[base(id)];
  var g = (r && r.fields && r.fields.access ? r.fields.access.grant : "") || "", res = [];
  if (g.indexOf("permission/") === 0) ins(g, "allows").forEach(function (role) { ins(role, "holds").forEach(function (a) { res.push({ id: a, via: "via " + title(g) + ", role " + title(role) }); }); });
  return res;
}
function upOne(id) {
  var t = typeOf(id);
  if (t === "scenario" || t === "result") return REC[parentOf[id]] && isReal(parentOf[id]) ? parentOf[id] : null;
  if (t === "screen") return outs(id, "serves").map(base).filter(function (x) { return REC[x] || NODE[x]; })[0] || outs(id, "offers").filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (["use-case", "rule", "automation", "notification", "setting"].indexOf(t) >= 0) return outs(id, "fulfills").filter(function (x) { return REC[x] || NODE[x]; })[0] || outs(id, "serves").map(base).filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (t === "journey") return outs(id, "cites").filter(function (x) { return REC[x] || NODE[x]; })[0] || outs(id, "serves").map(base).filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (t === "decision") return outs(id, "decides").filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (t === "requirement") return outs(id, "cites").filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (t === "feature") return outs(id, "advances").filter(function (x) { return REC[x] || NODE[x]; })[0] || outs(id, "solves").filter(function (x) { return REC[x] || NODE[x]; })[0] || null;
  if (t === "event") return ins(id, "raises")[0] || null;
  return null;
}
function traceOf(id) { var c = [id], cur = id, g = 0, u; while (g++ < 8 && (u = upOne(cur)) && c.indexOf(u) < 0) { c.unshift(u); cur = u; } return c.length > 1 ? c : []; }
var traceShown = {};
function connectionsOf(id) {
  var rows = [], seen = {};
  // Labels are the link field; a record pointing here reads "Points at this", from the Links table.
  // Nested paths share their Links row label, so one sentence covers every edge that reads the same.
  function push(label, tid, via) { if (tid === id) return; var k = label + "|" + tid; if (seen[k]) return; seen[k] = 1; rows.push({ label: label, id: tid, via: via || "" }); }
  function shortLabel(field) { return resolveField(field) || field; }
  outEdges(id).forEach(function (e) { if (e.id !== parentOf[id]) push(shortLabel(e.field), e.id); });
  inEdges(id).forEach(function (e) { if (e.id !== parentOf[id]) push("Points at this: " + shortLabel(e.field), e.id); });
  return rows;
}

// ---------- review state ----------
var verdict = {}, notes = {}, answered = {};
function layerWord(id) { return isBeh(id) ? "reviewed" : "approved"; }
function human(id) {
  if (verdict[id]) return verdict[id];
  var r = REC[base(id)];
  if (r) {
    if (r.status === "retired") return "retired";
    if (r.status === "draft") return "draft";
    if (r.effective === "draft") return "inherited";
    return r.status === "reviewed" ? "reviewed" : "approved";
  }
  var n = NODE[base(id)];
  if (n && n.status) return n.status === "draft" ? "draft" : n.status === "retired" ? "retired" : n.status === "reviewed" ? "reviewed" : "approved";
  return "approved";
}
var HLABEL = { approved: "approved", reviewed: "reviewed", mine: "approved by you", mineR: "reviewed by you", draft: "draft", inherited: "effective status: draft", question: "question asked", changes: "changes requested", retired: "retired" };
var HICON = { approved: "approved", reviewed: "approved", mine: "approved", mineR: "approved", draft: "draft", inherited: "inherited", question: "question", changes: "changes", retired: "retired" };
function needs(id) { if (!isReal(id)) return false; var h = human(id); return !(h === "approved" || h === "reviewed" || h === "mine" || h === "mineR" || h === "retired"); }
function tierOf(id) { var r = REC[base(id)], risk = (r && r.fields ? r.fields.risk : null) || []; return risk.length ? 1 : 3; }
function openQs(id) { var r = REC[base(id)]; return r ? ((r.fields || {}).questions || []).filter(function (q, i) { return !answered[base(id) + "#" + i]; }) : []; }
function proofState(id) {
  var r = REC[base(id)]; if (!r) return null;
  if (r.proof) return r.proof.state;
  if (r.type === "use-case" || r.type === "automation") {
    var sc = (scenariosOf(base(id)) || []).filter(function (s) { return REC[s] && REC[s].proof; }).map(function (s) { return REC[s].proof.state; });
    if (sc.indexOf("failing") >= 0) return "failing";
    if (r.machine && r.machine.verified) return "passing";
    if (r.machine && !r.machine.implemented) return "unproved";
    return sc.length ? "awaiting" : "unproved";
  }
  if (r.type === "requirement" && r.machine) return r.machine.verified ? "passing" : "awaiting";
  return null;
}
var PLABEL = { passing: "passing", failing: "failing", awaiting: "test written, not run", unconfirmed: "test not traced", unproved: "no test", stale: "stale" };
function pmark(state, text) { if (!state) return ""; return '<span class="pm pm-' + state + '" data-explain="' + state + '" tabindex="0">' + ic(state, "icon-sm") + esc(text || PLABEL[state]) + "</span>"; }
function days(d) { var x = new Date(d + "T00:00:00Z"); return isNaN(x) ? null : Math.round((x - TODAY) / 86400000); }
function addDays(d, n) { var x = new Date(d + "T00:00:00Z"); x.setUTCDate(x.getUTCDate() + n); return x.toISOString().slice(0, 10); }
function fmtDate(s) { if (!s) return ""; var d = new Date(s + "T00:00:00Z"); return isNaN(d) ? esc(s) : d.toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "UTC" }); }
function dateHint(s) { var n = days(s); if (n === null) return ""; if (n === 0) return "today"; return n > 0 ? "in " + plural(n, "day") : plural(-n, "day") + " ago"; }
function dueOf(id) { var r = REC[base(id)]; if (!r) return null; var ds = []; if (r.status === "draft" && r.decideBy) ds.push(r.decideBy); openQs(id).forEach(function (q) { if (q.decideBy) ds.push(q.decideBy); }); ds.sort(); return ds[0] || null; }
function duration(iso) {
  var m = /^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?)?$/.exec(String(iso || "")); if (!m) return esc(iso);
  var p = []; if (m[1]) p.push(plural(+m[1], "day")); if (m[2]) p.push(plural(+m[2], "hour")); if (m[3]) p.push(plural(+m[3], "minute")); if (m[4]) p.push(plural(+m[4], "second"));
  return p.join(" ") || esc(iso);
}

// ---------- explanations ----------
function n0(x) { return Number(x || 0).toLocaleString("en-GB"); }
// Each key names the LiteSpec text that explains it: a glossary term or a section of the model page.
function explainHtml(key) {
  var e = EXPLAIN[key];
  if (!e && key.indexOf("type:") === 0) { var t = key.slice(5), q = (LEX.types || {})[t], g = GL[(TYPE[t] || t).toLowerCase()]; if (q) { var lr = (LEX.layers || {})[String(q.layer || "").split(",")[0].trim()] || {}; e = [TYPE[t] || t, (q.question || "") + (g ? " " + g : "") + (lr.writes ? " Written by " + lr.writes + "; reviewed by " + lr.reviews + "." : ""), "model.md, Record Types"]; } else if (g) e = [TYPE[t] || t, g, "Glossary"]; }
  if (!e && key.indexOf("g:") === 0) e = [cap(key.slice(2)), GL[key.slice(2)], "Glossary"];
  if (!e || !e[1]) return "";
  return "<b>" + esc(e[0]) + "</b><p>" + inline(cap(String(e[1]))) + '</p><p class="mv">' + ic("book", "icon-sm") + "From LiteSpec: " + esc(e[2]) + "</p>";
}
function xp(key, html, cls) { return '<button type="button" class="xbtn ' + (cls || "") + '" data-explain="' + esc(key) + '">' + html + "</button>"; }

// ---------- facets ----------
var STATUS_KEYS = ["draft", "inherited", "approved", "reviewed", "retired", "mine", "changes", "question"];
var FACETS = [
  { key: "layer", label: "Layer", get: function (id) { return layerOf[id] ? [layerOf[id]] : []; }, name: function (v) { return (layerByKey[v] || {}).label || v; } },
  { key: "type", label: "Type", get: function (id) { return [typeOf(id)]; }, name: function (v) { return typeName(v); } },
  { key: "status", label: "status", get: function (id) { var h = human(id); return [h === "mineR" ? "mine" : h]; }, name: function (v) { return HLABEL[v]; } },
  { key: "tier", label: "risk", get: function (id) { var r = REC[base(id)]; if (!r) return []; var risk = (r.fields || {}).risk || []; return risk.length ? risk.map(function (x) { return "risk:" + x; }).concat(["tier1"]) : ["tier3"]; }, name: function (v) { return v === "tier1" ? "Tier 1" : v === "tier3" ? "Tier 3" : cap(v.slice(5).replace("-", " ")); } },
  { key: "questions", label: "questions", get: function (id) { return openQs(id).length ? ["open"] : []; }, name: function () { return "Has open questions"; } },
  { key: "findings", label: "Findings", get: function (id) { var r = REC[base(id)]; if (!r) return []; var f = r.findings || []; return f.length ? (f.some(function (x) { return !x.known; }) ? ["finding"] : ["known"]) : []; }, name: function (v) { return v === "finding" ? "Finding" : "Known gap"; } },
  { key: "proof", label: "proves", get: function (id) { var p = proofState(id); return p ? [p] : []; }, name: function (v) { return PLABEL[v]; } },
  { key: "due", label: "decideBy", get: function (id) { var d = dueOf(id); if (!d) return []; var n = days(d); return [n < 0 ? "over" : n <= 7 ? "week" : n <= 31 ? "month" : "later"]; }, name: function (v) { return { over: "Overdue", week: "Within 7 days", month: "Within a month", later: "Later" }[v]; } },
  { key: "decider", label: "decider", get: function (id) { var r = REC[base(id)]; return r && r.decider ? [r.decider] : []; }, name: function (v) { return v; } },
  { key: "provenance", label: "provenance", get: function (id) { var r = REC[base(id)]; if (!r) return []; return (r.fields || {}).provenance === "transcribed" ? ["transcribed"] : ["written"]; }, name: function (v) { return v === "transcribed" ? "transcribed" : "written"; } }
];
var active = {};
function passes(id, skip) {
  for (var i = 0; i < FACETS.length; i++) {
    var f = FACETS[i], sel = active[f.key]; if (!sel || !sel.length || f.key === skip) continue;
    var vals = f.get(id);
    if (!vals.length) continue;
    if (!sel.some(function (v) { return vals.indexOf(v) >= 0; })) return false;
  }
  if (query && !textHit(id)) return false;
  return true;
}
var textCache = {};
function textHit(id) {
  var t = textCache[id];
  if (t === undefined) {
    var r = REC[base(id)], label = title(id);
    if (r) { var f = r.fields || {}; t = ((r.title || label) + " " + (f.statement || "") + " " + (f.meaning || "") + " " + (f.summary || "") + " " + (f.given || "") + " " + (f.then || "")).toLowerCase(); }
    else t = (label + " " + (SEARCH[base(id)] || "")).toLowerCase();
    textCache[id] = t;
  }
  return query.toLowerCase().split(/\s+/).every(function (w) { return t.indexOf(w) >= 0; });
}
function anyFilter() { return !!query || Object.keys(active).some(function (k) { return active[k] && active[k].length; }); }

// ---------- state ----------
var view = "outline", sortKey = "outline", groupKey = "type", query = "", selected = null, expanded = {}, related = {}, noteFor = null, noteKind = null;
var showAllScen = {}, connAll = {}, facetsOpen = false, recent = [], focusQ = null;

// ---------- navigation ----------
var SORTS = {
  outline: { label: "Outline order", cmp: function (a, b) { return (orderIdx[a] || 0) - (orderIdx[b] || 0); } },
  title: { label: "Title", cmp: function (a, b) { return title(a).localeCompare(title(b)); } },
  due: { label: "Decide by, soonest", cmp: function (a, b) { var x = dueOf(a) || "9999", y = dueOf(b) || "9999"; return x < y ? -1 : x > y ? 1 : (orderIdx[a] || 0) - (orderIdx[b] || 0); } },
  tier: { label: "Risk tier first", cmp: function (a, b) { return tierOf(a) - tierOf(b) || (orderIdx[a] || 0) - (orderIdx[b] || 0); } },
  questions: { label: "Most open questions", cmp: function (a, b) { return openQs(b).length - openQs(a).length || (orderIdx[a] || 0) - (orderIdx[b] || 0); } },
  proof: { label: "Proof, failing first", cmp: function (a, b) { var o = { failing: 0, unconfirmed: 1, unproved: 2, awaiting: 3, passing: 4 }; var x = o[proofState(a)], y = o[proofState(b)]; x = x === undefined ? 5 : x; y = y === undefined ? 5 : y; return x - y || (orderIdx[a] || 0) - (orderIdx[b] || 0); } },
  status: { label: "Review status", cmp: function (a, b) { return STATUS_KEYS.indexOf(human(a)) - STATUS_KEYS.indexOf(human(b)) || (orderIdx[a] || 0) - (orderIdx[b] || 0); } }
};
var GROUPS = { none: "No grouping", type: "Type", layer: "Layer", status: "Review status", decider: "Decider", tier: "Risk tier" };
function groupVal(id) {
  if (groupKey === "type") return [typeOf(id), PLURAL[typeOf(id)] || typeOf(id)];
  if (groupKey === "layer") return [layerOf[id], (layerByKey[layerOf[id]] || {}).label || layerOf[id]];
  if (groupKey === "status") return [human(id), HLABEL[human(id)]];
  if (groupKey === "decider") { var r = REC[base(id)]; return [r && r.decider ? r.decider : "~", r && r.decider ? r.decider : "No decider"]; }
  if (groupKey === "tier") return [String(tierOf(id)), "Tier " + tierOf(id)];
  return ["", ""];
}
var visCache = null;
function computeVisible() {
  visCache = {};
  if (!anyFilter()) return;
  order.forEach(function (id) { if (passes(id)) { visCache[id] = 2; var p = parentOf[id]; while (p && !visCache[p]) { visCache[p] = 1; p = parentOf[p]; } } });
}
function isVisible(id) { return !anyFilter() || !!visCache[id]; }
function matches(id) { return !anyFilter() || visCache[id] === 2; }

function statusIcon(id) { if (!isReal(id)) return ""; var h = human(id); return '<span class="st s-' + (h === "mineR" ? "mine" : h) + '" title="' + esc(HLABEL[h]) + '">' + ic(HICON[h], "icon-sm") + "</span>"; }
function rowAttrs(id, extra) {
  var t = typeOf(id), h = human(id), ps = proofState(id), lk = id !== selected && !!related[id];
  var risk = tierOf(id) === 1 ? ", tier 1" : "";
  var name = typeName(t) + ", " + title(id) + ", " + HLABEL[h] + risk + (openQs(id).length ? ", " + plural(openQs(id).length, "open question") : "") + (ps === "failing" ? ", a test fails" : "") + (lk ? ", linked" : "");
  return ' role="treeitem" tabindex="' + (id === selected ? 0 : -1) + '" aria-selected="' + (id === selected) + '" aria-label="' + esc(name) + '" data-row="' + esc(id) + '"' + (extra || "");
}
function rowTail(id) {
  var s = "", q = openQs(id).length, ps = proofState(id), lk2 = lacks(id), r = REC[base(id)];
  if (lk2) s += '<span class="lackd" title="' + esc(cap(lk2)) + '">' + ic("missing", "icon-sm") + "</span>";
  if (related[id] && id !== selected) s += '<span class="lk" title="Linked to the selected record">' + ic("link", "icon-sm") + "</span>";
  if (q) s += '<span class="qd" title="' + plural(q, "open question") + '">' + ic("question", "icon-sm") + q + "</span>";
  if (r && tierOf(id) === 1) s += '<span class="tierb" title="Tier 1: ' + esc(((r.fields || {}).risk || []).join(", ")) + '">T1</span>';
  if (ps === "failing") s += '<span class="mfail" title="A test fails">' + ic("flask", "icon-sm") + "</span>";
  return s + statusIcon(id);
}
var closedGroups = {};
function grouped(ids, render, pkey) {
  var types = []; ids.forEach(function (id) { var t = typeOf(id); if (types.indexOf(t) < 0) types.push(t); });
  return types.map(function (t) {
    var list = ids.filter(function (id) { return typeOf(id) === t; });
    if (sortKey !== "outline") list.sort(SORTS[sortKey].cmp);
    var rows = list.map(render).join("");
    var gk = (pkey || "") + "|" + t, shut = closedGroups[gk] && !anyFilter();
    return rows ? '<li class="tg" role="presentation"><button type="button" class="tgb" data-tg="' + esc(gk) + '" aria-expanded="' + !shut + '">' + ic("chev", "icon-sm chev") + ic(t, "icon-sm") + "<span>" + esc(PLURAL[t] || t) + '</span><span class="n">' + list.filter(isVisible).length + "</span></button></li>" + (shut ? "" : rows) : "";
  }).join("");
}
function treeRow(id) {
  if (!isVisible(id)) return "";
  var kids = (kidsOf[id] || []).filter(isVisible);
  var open = kids.length && (anyFilter() ? true : !!expanded[id]);
  var dim = anyFilter() && !matches(id);
  var s = '<li role="none"><div class="trow' + (id === selected ? " sel" : "") + (related[id] && id !== selected ? " linked" : "") + '"' + rowAttrs(id, ' aria-level="' + (depthOf[id] || 1) + '"' + (kids.length ? ' aria-expanded="' + !!open + '"' : "") + (dim ? ' style="opacity:.55"' : "")) + ">";
  s += '<button type="button" class="tw' + (kids.length ? "" : " leaf") + '" tabindex="-1" aria-hidden="true" data-tog="' + esc(id) + '">' + ic("chev", "icon-sm") + "</button>";
  s += '<span class="ti">' + ic(typeOf(id)) + '</span><span class="tt" title="' + esc(title(id)) + '">' + esc(title(id)) + "</span>";
  if (!open && kids.length) s += '<span class="tn">' + kids.length + "</span>";
  s += rowTail(id) + "</div>";
  if (open) s += '<ul role="group">' + grouped(kids, treeRow, id) + "</ul>";
  return s + "</li>";
}
var openLayers = { intent: true };
function outlineHtml() {
  return (MAN.layers || []).map(function (l) {
    var meta = layerByKey[l.key] || l;
    if (!outlineLoaded[l.key]) {
      var open0 = openLayers[l.key];
      return '<section class="sec' + (open0 ? " open" : "") + '"><button type="button" class="sec-h" data-layer="' + l.key + '" aria-expanded="' + !!open0 + '">' + ic(LAYER_ICON[l.key] || "core") + "<span>" + esc(meta.label || l.key) + "</span>" + ic("chev", "icon-sm chev") + "</button>" + (open0 ? '<p class="relnote">Loading.</p>' : "") + "</section>";
    }
    var ids = order.filter(function (id) { return layerOf[id] === l.key && isReal(id); });
    var shown = ids.filter(matches).length;
    if (anyFilter() && !shown) return "";
    var open = anyFilter() || openLayers[l.key];
    var here = selected && layerOf[selected] === l.key;
    var appr = ids.filter(function (id) { return !needs(id); }).length;
    var s = '<section class="sec' + (open ? " open" : "") + '"><button type="button" class="sec-h' + (here ? " here" : "") + '" data-layer="' + l.key + '" aria-expanded="' + !!open + '"' + (here ? ' aria-current="page"' : "") + ">" + ic(LAYER_ICON[l.key] || "core") + "<span>" + esc(meta.label || l.key) + "</span>";
    s += '<span class="pillc" title="' + appr + " of " + ids.length + ' approved or reviewed">' + (anyFilter() ? shown + " shown" : (ids.length || order.filter(function (id) { return layerOf[id] === l.key; }).length).toLocaleString("en-GB")) + "</span>" + ic("chev", "icon-sm chev") + "</button>";
    if (open) {
      if (!anyFilter()) s += '<p class="sec-desc">' + inline(meta.question || "") + (meta.writes ? " Written by " + esc(meta.writes) + "; reviewed by " + esc(meta.reviews) + "." : "") + ' <button type="button" class="iref" data-go="layer/' + l.key + '">Open</button></p>';
      var roots = rootsOf[l.key] || [];
      s += '<div class="tree"><ul role="tree" aria-label="' + esc(meta.label || l.key) + ' outline">' + (roots.length > 1 ? grouped(roots, treeRow, l.key) : roots.map(treeRow).join("")) + "</ul></div>";
    }
    return s + "</section>";
  }).join("");
}
function listHtml() {
  var ids = order.filter(function (id) { return passes(id); }).sort(SORTS[sortKey].cmp);
  var s = '<div role="tree" aria-label="Records">', lastG = null, count = {};
  if (groupKey !== "none") { ids.forEach(function (id) { var g = groupVal(id)[0]; count[g] = (count[g] || 0) + 1; }); ids.sort(function (a, b) { var x = groupVal(a)[0], y = groupVal(b)[0]; return x < y ? -1 : x > y ? 1 : 0; }); if (groupKey === "type") { var to = Object.keys(PLURAL); ids.sort(function (a, b) { return to.indexOf(typeOf(a)) - to.indexOf(typeOf(b)); }); } }
  var limit = 400;
  ids.slice(0, limit).forEach(function (id) {
    var g = groupVal(id);
    if (groupKey !== "none" && g[0] !== lastG) { s += '<div class="grp-h" role="presentation">' + (groupKey === "type" ? ic(g[0], "icon-sm") : "") + "<span>" + esc(g[1]) + '</span><span class="n">' + count[g[0]] + "</span></div>"; lastG = g[0]; }
    var d = dueOf(id), sub = parentOf[id] ? title(parentOf[id]) : ((layerByKey[layerOf[id]] || {}).label || "");
    s += '<div class="trow lrow' + (id === selected ? " sel" : "") + '"' + rowAttrs(id, ' aria-level="1"') + '><span class="ti">' + ic(typeOf(id)) + '</span><span class="tt">' + esc(title(id)) + '<span class="sub2">' + (groupKey !== "type" ? esc(typeName(typeOf(id))) + " &middot; " : "") + esc(sub) + "</span></span>" + (d ? '<span class="due' + (days(d) < 0 ? " over" : "") + '" title="Decide by ' + fmtDate(d) + '">' + fmtDate(d).replace(/ \d{4}$/, "") + "</span>" : "") + rowTail(id) + "</div>";
  });
  if (ids.length > limit) s += '<p class="relnote">Showing the first ' + limit + " of " + ids.length + ". Narrow the filter to see the rest.</p>";
  if (!ids.length) s += '<p class="relnote">No record matches these filters.</p>';
  return s + "</div>";
}
function ballotName(key) {
  if (REC[key] || NODE[key]) return title(key);
  return key.replace(/^decision\//, "").replace(/-/g, " ");
}
function ballotItems() {
  if (QUEUE && QUEUE.ballots) {
    return (QUEUE.ballots || []).map(function (x) {
      if (x.q) return x;
      return { id: base(x.id || x.record || ""), i: x.i !== undefined ? x.i : (x.index || 0), q: { question: x.question || "", tier: x.tier || 3, decider: x.decider || "", decideBy: x.decideBy || null, ballot: x.ballot || null, recommendation: x.recommendation || "" } };
    }).filter(function (x) { return x.id && x.q.question; });
  }
  var items = [];
  Object.keys(REC).forEach(function (id) {
    if (!isReal(id)) return;
    (((REC[id] || {}).fields || {}).questions || []).forEach(function (q, i) { if (q.question) items.push({ id: id, i: i, q: q }); });
  });
  return items;
}
function ballotsHtml() {
  if (!queueLoaded) return '<p class="relnote">Loading the ballots.</p>';
  var items = ballotItems().filter(function (x) { return passes(x.id, "questions"); });
  if (!items.length) return '<p class="relnote">No open question matches.</p>';
  var by = {}; items.forEach(function (x) { var b = x.q.ballot || "unsorted"; (by[b] = by[b] || []).push(x); });
  var keys = Object.keys(by).sort(function (a, b) { var ta = Math.min.apply(null, by[a].map(function (x) { return x.q.tier || 3; })), tb = Math.min.apply(null, by[b].map(function (x) { return x.q.tier || 3; })); return ta - tb || by[b].length - by[a].length || a.localeCompare(b); });
  return keys.map(function (k) {
    var list = by[k].sort(function (a, b) { return (a.q.tier || 3) - (b.q.tier || 3) || String(a.q.decideBy).localeCompare(String(b.q.decideBy)); });
    return '<section class="ballot"><div class="ballot-h">' + ic("ballot", "icon-sm") + "<span>" + esc(cap(ballotName(k))) + '</span><span class="n">' + list.length + "</span></div>" + list.map(function (x) {
      var done = answered[x.id + "#" + x.i], t = x.q.tier || 3, auto = t === 3 && x.q.recommendation && x.q.decideBy ? addDays(x.q.decideBy, VETO) : null;
      return '<button type="button" class="qrow' + (selected === x.id && focusQ === x.i ? " sel" : "") + '" data-q="' + esc(x.id) + "|" + x.i + '"><span class="q">' + esc(x.q.question) + '</span><span class="qm">' + (t === 1 ? '<span class="tierb">T1</span>' : '<span class="tn">Tier 3</span>') + "<span>" + esc(x.q.decider || "") + "</span>" + (x.q.decideBy ? '<span class="due' + (days(x.q.decideBy) < 0 ? " over" : "") + '">by ' + fmtDate(x.q.decideBy) + "</span>" : "") + (auto ? "<span>auto " + fmtDate(auto) + "</span>" : "") + (done ? '<span class="answered">' + ic("approved", "icon-sm") + "Answered</span>" : "") + "<span>" + ic(typeOf(x.id), "icon-sm") + " " + esc(title(x.id)) + "</span></span></button>";
    }).join("") + "</section>";
  }).join("");
}
function securityReviewer() { return (QUEUE && QUEUE.review && QUEUE.review.securityReviewer) || "named in the project file"; }
function securityItems() {
  if (QUEUE && QUEUE.security) return QUEUE.security || [];
  var items = [];
  Object.keys(REC).forEach(function (id) {
    if (!isReal(id)) return;
    var f = (REC[id] || {}).fields || {};
    if (f.access && f.access.tenant === "cross") items.push({ kind: "cross", id: id, text: "Acts across tenants" + (f.access.grant ? ", through " + title(f.access.grant) : "") });
    (f.effects || []).forEach(function (e) { if (e.calls) items.push({ kind: "calls", id: id, text: "Calls " + title(base(e.calls)) + (e.calls.indexOf("#") > 0 ? ", " + e.calls.split("#")[1].replace(/-/g, " ") : "") }); });
    (f.dataClasses || []).forEach(function (c) { if (c.classification === "personal" || c.classification === "sensitive") items.push({ kind: "data", id: id, text: cap(c.classification) + " data: " + c.name.replace(/-/g, " ") + (c.onErasure ? ", on erasure " + c.onErasure : "") }); });
  });
  return items;
}
function securityHtml() {
  if (!queueLoaded) return '<p class="relnote">Loading the security items.</p>';
  var all = securityItems().filter(function (x) { return passes(x.id); });
  var G = [["cross", "Cross-tenant access", "Use cases that act outside the caller's own tenant."], ["calls", "Outside calls", "Every call that leaves the system for an external system."], ["data", "Personal and sensitive data", "Data classes that hold personal or sensitive fields, with their erasure position."]];
  return '<p class="relnote">For the security reviewer, ' + esc(securityReviewer()) + ". Security review stands apart from reviewed status.</p>" + G.map(function (g) {
    var list = all.filter(function (x) { return x.kind === g[0]; }); if (!list.length) return "";
    return '<div class="secq-h">' + ic(g[0] === "cross" ? "shield" : g[0] === "calls" ? "ext" : "lock", "icon-sm") + "<span>" + g[1] + '</span><span class="n">' + list.length + "</span></div><p>" + g[2] + "</p>" + list.map(function (x) {
      return '<div class="trow lrow' + (x.id === selected ? " sel" : "") + '"' + rowAttrs(x.id, ' aria-level="1"') + '><span class="ti">' + ic(typeOf(x.id)) + '</span><span class="tt">' + esc(title(x.id)) + '<span class="sub2">' + esc(x.text) + "</span></span>" + rowTail(x.id) + "</div>";
    }).join("");
  }).join("");
}
// A batch named by what it holds, as release.md describes the four kinds.
function batchName(b) { var p = String(b.selector || "").split("/"); if (b.kind === "use-case") return "This use case with its results and scenarios"; if (b.kind === "decision") { var dn = base(String(b.selector || "").replace(/^decision\//, "")); return "Decision " + (REC[dn] || NODE[dn] ? '"' + title(dn) + '"' : b.selector) + " with what it decides"; } if (b.kind === "record") return "This record on its own"; return "All draft " + (PLURAL[p[1]] || p[1] || "").toLowerCase() + (p[0] === "intent" || p[0] === "governance" ? "" : " in " + p[0].replace(/-/g, " ")); }
function batchOf(id) {
  var batches = (QUEUE && QUEUE.batches) || {};
  var r = REC[base(id)];
  if (r && r.batch && batches[r.batch]) return { key: r.batch, batch: batches[r.batch] };
  var keys = Object.keys(batches);
  for (var i = 0; i < keys.length; i++) { var b = batches[keys[i]], recs = b.records || b.ids || []; if (recs.indexOf(base(id)) >= 0) return { key: keys[i], batch: b }; }
  return null;
}
function queueHtml() {
  if (!queueLoaded) return '<p class="relnote">Loading the review queue.</p>';
  var batches = (QUEUE && QUEUE.batches) || {};
  var first = {};
  Object.keys(REC).forEach(function (id) { var b = REC[id].batch; if (b && batches[b] && !first[b]) first[b] = id; });
  Object.keys(batches).forEach(function (k) { if (first[k]) return; var recs = batches[k].records || batches[k].ids || []; if (recs.length) first[k] = base(recs[0]); });
  var list = ((QUEUE && (QUEUE.order || QUEUE.queueOrder)) || Object.keys(batches)).filter(function (k) { return batches[k] && first[k]; });
  return '<p class="relnote">Batches in the order <span class="code">litespec model review</span> prints them. ' + xp("batch", "What is a batch?") + "</p>" + list.slice(0, 200).map(function (k) {
    var b = batches[k], id = first[k], nm = batchName(b);
    return '<div class="trow lrow' + (id === selected ? " sel" : "") + '"' + rowAttrs(id, ' aria-level="1"') + '><span class="ti">' + ic(b.kind === "decision" ? "decision" : b.kind === "use-case" ? "use-case" : "ballot") + '</span><span class="tt">' + esc(nm === "This use case with its results and scenarios" || nm === "This record on its own" ? title(id) : nm) + '<span class="sub2">' + plural(b.n || 0, "record") + (b.tier === 1 ? ", tier 1" : "") + (b.decideBy ? ", decide by " + fmtDate(b.decideBy) : "") + "</span></span>" + (b.tier === 1 ? '<span class="tierb">T1</span>' : "") + "</div>";
  }).join("") + (list.length > 200 ? '<p class="relnote">and ' + (list.length - 200) + " more batches</p>" : "");
}
function navHtml() {
  if (view === "queue") { computeVisible(); return queueHtml(); }
  computeVisible();
  if (view === "outline") return outlineHtml();
  if (view === "list") return listHtml();
  if (view === "ballots") return ballotsHtml();
  return securityHtml();
}
function navList() { return Array.prototype.map.call(document.querySelectorAll("#nav [data-row], #nav [data-q]"), function (x) { return x.getAttribute("data-row") || x.getAttribute("data-q"); }); }

// ---------- sidebar chrome ----------
function vtabsHtml() {
  var nb = ballotItems().filter(function (x) { return !answered[x.id + "#" + x.i]; }).length, ns = securityItems().length;
  return [["outline", "Outline", "tree", ""], ["queue", "Review queue", "next", ""], ["security", "Security", "shield", ns], ["ballots", "Ballots", "ballot", nb]].map(function (v, i) {
    return '<button type="button" role="tab" aria-selected="' + (view === v[0]) + '" data-view="' + v[0] + '" title="' + v[1] + " (" + (i + 1) + ')">' + ic(v[2], "icon-sm") + v[1] + (v[3] !== "" ? ' <span class="n">' + v[3] + "</span>" : "") + "</button>";
  }).join("");
}
function toolsHtml() {
  var nActive = Object.keys(active).reduce(function (n, k) { return n + (active[k] ? active[k].length : 0); }, 0);
  var s = '<button type="button" class="tbtn' + (nActive ? " on" : "") + '" id="facetBtn" aria-expanded="' + facetsOpen + '" aria-controls="facetPanel">' + ic("filter", "icon-sm") + "Filter" + (nActive ? ' <span class="badge-n">' + nActive + "</span>" : "") + "</button>";
  if (view === "outline" || view === "list") {
    s += '<label class="vh" for="sortSel">Sort</label><select class="sel" id="sortSel" title="Sort">' + Object.keys(SORTS).map(function (k) { return '<option value="' + k + '"' + (k === sortKey ? " selected" : "") + ">" + (k === "outline" ? "Sort: outline" : SORTS[k].label) + "</option>"; }).join("") + "</select>";
  }
  if (view === "list") s += '<label class="vh" for="grpSel">Group</label><select class="sel" id="grpSel" title="Group">' + Object.keys(GROUPS).map(function (k) { return '<option value="' + k + '"' + (k === groupKey ? " selected" : "") + ">" + (k === "none" ? GROUPS[k] : "By " + GROUPS[k].toLowerCase()) + "</option>"; }).join("") + "</select>";
  s += '<span class="grow"></span>';
  if (view === "outline") s += '<button type="button" class="tbtn ic" data-act="expand" title="Expand all (e)" aria-label="Expand all">' + ic("expand", "icon-sm") + '</button><button type="button" class="tbtn ic" data-act="collapse" title="Collapse all (c)" aria-label="Collapse all">' + ic("collapse", "icon-sm") + "</button>";
  s += '<button type="button" class="tbtn ic" data-act="reveal" title="Show the selected record" aria-label="Show the selected record">' + ic("target", "icon-sm") + "</button>";
  return s;
}
function facetPanelHtml() {
  if (!facetsOpen) return "";
  var base0 = order;
  return '<div class="facets" role="group" aria-label="Filters">' + FACETS.map(function (f) {
    if (f.key === "layer" && view === "outline") return "";
    var counts = {}; base0.forEach(function (id) { if (!passes(id, f.key)) return; f.get(id).forEach(function (v) { counts[v] = (counts[v] || 0) + 1; }); });
    var sel = active[f.key] || [];
    var vals = Object.keys(counts); sel.forEach(function (v) { if (vals.indexOf(v) < 0) vals.push(v); });
    if (!vals.length) return "";
    if (f.key === "type") { var to = Object.keys(PLURAL); vals.sort(function (a, b) { return to.indexOf(a) - to.indexOf(b); }); }
    else if (f.key === "status") vals.sort(function (a, b) { return STATUS_KEYS.indexOf(a) - STATUS_KEYS.indexOf(b); });
    else vals.sort(function (a, b) { return (counts[b] || 0) - (counts[a] || 0); });
    return '<div class="facet"><h4>' + esc(f.label) + '</h4><div class="fopts">' + vals.map(function (v) {
      var on = sel.indexOf(v) >= 0;
      return '<button type="button" class="fopt" aria-pressed="' + on + '" data-facet="' + f.key + '" data-val="' + esc(v) + '"' + (!counts[v] && !on ? " disabled" : "") + ">" + esc(f.name(v)) + ' <span class="c">' + (counts[v] || 0) + "</span></button>";
    }).join("") + "</div></div>";
  }).join("") + "</div>";
}
function chipsHtml() {
  var s = "";
  FACETS.forEach(function (f) { (active[f.key] || []).forEach(function (v) { s += '<span class="chip">' + esc(f.label) + ": " + esc(f.name(v)) + '<button type="button" aria-label="Remove ' + esc(f.name(v)) + '" data-unf="' + f.key + "|" + esc(v) + '">' + ic("close", "icon-sm") + "</button></span>"; }); });
  if (query) s += '<span class="chip">Text: ' + esc(query) + '<button type="button" aria-label="Clear the text filter" data-unq>' + ic("close", "icon-sm") + "</button></span>";
  if (s) s += '<button type="button" class="chip clear" data-clear>Clear all</button>';
  return s;
}
var LEGEND = '<div class="legend"><span><i class="st s-draft">' + ic("draft", "icon-sm") + "</i>Draft</span><span><i class=\"st s-approved\">" + ic("approved", "icon-sm") + "</i>Approved or reviewed</span><span><i class=\"st s-inherited\">" + ic("inherited", "icon-sm") + "</i>Held by a draft</span><span><i class=\"lackd\">" + ic("missing", "icon-sm") + "</i>No code or no test</span></div>";
function shownHtml() {
  if (view === "ballots") { var b = ballotItems(), o = b.filter(function (x) { return !answered[x.id + "#" + x.i]; }); return "<b>" + o.length + "</b> open of " + b.length + " questions, tier 1 first"; }
  if (view === "outline") return shownOutline() + LEGEND;
  if (view === "queue") return "The review queue: every draft and reopened record, in approval batches";
  if (view === "security") return "<b>" + securityItems().filter(function (x) { return passes(x.id); }).length + "</b> items in the queue";
  return shownOutline();
}
function shownOutline() {
  var n = order.filter(function (id) { return isReal(id) && passes(id); }).length;
  var total = 0; Object.keys((MAN || {}).totals || {}).forEach(function (k) { total += MAN.totals[k]; });
  if (!total) total = order.filter(isReal).length;
  return anyFilter() ? "<b>" + n.toLocaleString("en-GB") + "</b> of " + total.toLocaleString("en-GB") + " records match" : "<b>" + total.toLocaleString("en-GB") + "</b> records in this book.";
}

// ---------- detail building blocks ----------
// Every entry links to the file and line it comes from, opened in the editor.
function editorTemplate() { return MAN ? MAN.editor : null; }
function consumerRoot() { return (MAN && MAN.roots && MAN.roots.consumer) || ""; }
function litespecRoot() { return (MAN && MAN.roots && MAN.roots.litespec) || ""; }
function editorHref(root, path, line) {
  var t = editorTemplate();
  if (!t || t === "none" || !path) return null;
  var href = String(t).split("{root}").join(root || "").split("{path}").join(path);
  if (line) href = href.split("{line}").join(String(line));
  else href = href.replace(/:{line}/g, "").split("{line}").join("");
  return href;
}
function lsHref(path) { return editorHref(litespecRoot(), path, null); }
function provisionLink(p) { var m = /^litespec\/(.+)#(.+)$/.exec(String(p || "")); if (!m) return '<span class="code">' + esc(p) + "</span>"; var href = lsHref("docs/" + m[1] + ".md"); return href ? '<a class="code" href="' + esc(href) + '" title="Open the provision in LiteSpec">' + esc(p) + "</a>" : '<span class="code">' + esc(p) + "</span>"; }
function srcHref(src) { if (!src || !src.path) return null; return editorHref(src.litespec ? litespecRoot() : consumerRoot(), src.path, src.line || null); }
function srcLink(src, cls) {
  if (!src || !src.path) return "";
  var href = srcHref(src);
  if (src.litespec) { var label = "LiteSpec " + src.path; return href ? '<a class="srcl ' + (cls || "") + '" href="' + esc(href) + '" title="Open in LiteSpec">' + ic("book", "icon-sm") + "<span>" + esc(label) + "</span></a>" : '<span class="srcl ' + (cls || "") + '">' + ic("book", "icon-sm") + "<span>" + esc(label) + "</span></span>"; }
  var text = src.path + (src.line ? ":" + src.line : "");
  return href ? '<a class="srcl ' + (cls || "") + '" href="' + esc(href) + '" title="Open ' + esc(src.path) + ' in the editor">' + ic("ext", "icon-sm") + "<span>" + esc(text) + "</span></a>" : '<span class="srcl ' + (cls || "") + '">' + ic("ext", "icon-sm") + "<span>" + esc(text) + "</span></span>";
}
function srcIcon(id) { var r = REC[base(id)], href = r && r.src && r.src.path && !r.src.litespec ? srcHref(r.src) : null; return href ? '<a class="srci" href="' + esc(href) + '" title="Open ' + esc(r.src.path) + ' in the editor" aria-label="Open the source">' + ic("ext", "icon-sm") + "</a>" : ""; }
function missingRef(id) { return '<span class="outref" title="No record with this identifier is in the book">' + esc(id) + "</span>"; }
function ref(id, opts) {
  opts = opts || {}; var b = base(id), t = typeOf(b), known = !!(REC[b] || NODE[b]);
  var label = esc(known ? title(b) : b) + (opts.sub ? '<span class="tn" style="font-family:var(--sans)"> ' + esc(opts.sub) + "</span>" : "");
  if (!known) return '<span class="ref out" title="No record with this identifier is in the book">' + ic(t, "icon-sm") + "<span>" + label + '</span><span class="ttag">' + esc(tag(t)) + "</span></span>";
  return '<span class="refw"><button type="button" class="ref" data-go="' + esc(b) + '">' + ic(t, "icon-sm") + "<span>" + label + '</span><span class="ttag" data-explain="type:' + esc(t) + '">' + esc(tag(t)) + "</span>" + (REC[b] ? statusIcon(b) : "") + "</button>" + (REC[b] ? srcIcon(b) : "") + "</span>";
}
function refs(ids) {
  ids = uniq((ids || []).filter(Boolean).map(base));
  return ids.length ? '<ol class="rlist">' + ids.map(function (i) {
    var r = REC[i], t = typeOf(i), stmt = r && (r.fields.statement || r.fields.meaning || r.fields.summary);
    var head = r ? '<button type="button" class="iref" data-go="' + esc(i) + '">' + esc(r.title) + "</button>" : NODE[i] ? '<button type="button" class="iref" data-go="' + esc(i) + '">' + esc(NODE[i].label) + "</button>" : missingRef(i);
    return "<li>" + head + '<span class="rmeta"><span class="ttag" data-explain="type:' + esc(t) + '">' + esc(tag(t)) + "</span>" + (r ? statusIcon(i) : "") + "</span>" + (stmt ? '<div class="rstmt">' + esc(String(stmt).slice(0, 220)) + "</div>" : "") + "</li>";
  }).join("") + "</ol>" : "";
}
function inlineRef(id) { var b = base(id); return (REC[b] || NODE[b]) ? '<button type="button" class="iref" data-go="' + esc(b) + '">' + esc(title(b)) + "</button>" : missingRef(b); }
// Blocks that only list linked records move to the rail, so the page reads as content.
var LINKS_IN_RAIL = { "based on": 1, "depends on": 1, "governs": 1, "raised by": 1, "fulfilled by": 1, "governed by": 1, "fulfills": 1, "fulfills and serves": 1, "solves and advances": 1, "basedOn": 1, "addresses": 1, "actor": 1, "dependsOn": 1, "decides": 1, "raises": 1, "subject": 1, "affects": 1, "replaces": 1, "implements": 1, "calls": 1, "proves": 1, "serves": 1, "advances": 1, "cites": 1 };
// A field reads with its LiteSpec label; an unknown field renders raw, never invented.
function fieldLabel(s) { s = String(s); return (LEX.fields || {})[s] || s; }
function blk(key, heading, inner, count, aside) { if (!inner) return ""; if (count === 0) return ""; var hk = fieldLabel(heading).toLowerCase(); if (heading.indexOf("Points at this: ") === 0 || LINKS_IN_RAIL[heading] || LINKS_IN_RAIL[hk] || LINKS_IN_RAIL[heading.toLowerCase()]) return ""; return '<section class="blk" id="b-' + key + '"><h2>' + esc(fieldLabel(heading)) + (count != null ? ' <span class="n">' + count + "</span>" : "") + (aside ? '<span class="aside">' + aside + "</span>" : "") + "</h2>" + inner + "</section>"; }
function kv(pairs) { pairs = pairs.filter(function (p) { return p[1] !== undefined && p[1] !== null && p[1] !== ""; }); return pairs.length ? '<dl class="kv">' + pairs.map(function (p) { return "<dt>" + esc(fieldLabel(p[0])) + "</dt><dd>" + p[1] + "</dd>"; }).join("") + "</dl>" : ""; }
function tile(k, v, hint, big) { return '<div class="tile"><div class="k">' + esc(fieldLabel(k)) + '</div><div class="v' + (big ? " big" : "") + '">' + v + "</div>" + (hint ? '<div class="h">' + hint + "</div>" : "") + "</div>"; }
function callout(kind, iconName, html) { return '<div class="callout c-' + kind + '">' + ic(iconName, "icon-sm") + "<div>" + html + "</div></div>"; }
function badge(text, kind) { return '<span class="badge' + (kind ? " k-" + kind : "") + '">' + esc(text) + "</span>"; }
function table(head, rows) { return '<table class="tbl"><thead><tr>' + head.map(function (h) { return "<th>" + esc(h) + "</th>"; }).join("") + "</tr></thead><tbody>" + rows.map(function (r) { return "<tr>" + r.join("") + "</tr>"; }).join("") + "</tbody></table>"; }
function td(html, cls) { return "<td" + (cls ? ' class="' + cls + '"' : "") + ">" + html + "</td>"; }
function bullets(list) { if (typeof list === "string" && list) list = [list]; return list && list.length ? "<ul>" + list.map(function (x) { return "<li>" + esc(x) + "</li>"; }).join("") + "</ul>" : '<span class="tn">&ndash;</span>'; }
var CLS = { public: ["public", ""], internal: ["internal", "info"], personal: ["personal", "warn"], sensitive: ["sensitive", "bad"], confidential: ["confidential", "bad"] };
function clsBadge(c) { var x = CLS[c] || [cap(c), ""]; return badge(x[0], x[1]); }

function inline(s) {
  var out = esc(s);
  out = out.replace(/(^|[\s(])(https?:\/\/[^\s<)]+)/g, function (m, pre, url) { return pre + "[" + url.replace(/^https?:\/\/(www\.)?/, "") + "](" + url + ")"; });
  out = out.replace(/`([^`]+)`/g, function (m, c) { var id = base(c.replace(/&amp;/g, "&")); if (REC[id] || NODE[id]) return inlineRef(id); return "<code>" + c + "</code>"; });
  out = out.replace(/\*\*([^*]+)\*\*/g, "<b>$1</b>").replace(/(^|[^*])\*([^*]+)\*/g, "$1<em>$2</em>");
  return out.replace(/\[([^\]]+)\]\((https?:[^)\s]+)\)/g, '<a href="$2" target="_blank" rel="noopener">$1</a>').replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, function (m, text, href) {
    if (!curSrc || !curSrc.path) return text;
    var target = srcHref({ path: resolvePath(curSrc.path, href.split("#")[0]), line: null, litespec: curSrc.litespec });
    return target ? '<a href="' + esc(target) + '" title="Open ' + esc(href) + ' in the editor">' + text + "</a>" : text;
  });
}
function resolvePath(from, rel) { var parts = from.split("/").slice(0, -1); rel.split("/").forEach(function (p) { if (p === "..") parts.pop(); else if (p && p !== ".") parts.push(p); }); return parts.join("/"); }
// Record bodies arrive rendered; the client only places the Why sections
// and skips the sections each type renders from fields.
function secList(r) { return (r.sections && r.sections.list) || []; }
function secIntro(r) { return (r.sections && r.sections.intro) || ""; }
function secHtml(r, name) { var s = secList(r).filter(function (x) { return x.h === name; })[0]; return s ? s.html || "" : ""; }
function stripTags(html) { var tmp = document.createElement("div"); tmp.innerHTML = html; return (tmp.textContent || "").replace(/\s+/g, " "); }
var SKIP = { aggregate: ["Transitions", "Data", "Data Classes", "States"], "use-case": ["Goal"], feature: ["Offering"], topic: ["Summary"], actor: ["Summary"], role: ["Summary"], constraint: ["Summary"], "external-system": ["Summary", "Operations"], product: ["Summary", "The Boundary"], runbook: ["Trigger"], journey: ["Steps"] };
function leadText(r) { var f = r.fields || {}; return String(f.statement || f.summary || f.meaning || f.boundary || f.question || f.reason || ""); }
function bodyBlocks(r) {
  var out = "", skip = SKIP[r.type] || [], lt = leadText(r).slice(0, 40);
  var intro = secIntro(r).trim();
  if (intro && !(lt && stripTags(intro).indexOf(lt) === 0)) out += '<div class="prose" style="margin-bottom:24px">' + intro + "</div>";
  secList(r).forEach(function (s, i) {
    if (skip.indexOf(s.h) >= 0 || !(s.html || "").trim() || WHY_SECTIONS.test(s.h)) return;
    var h = s.h, html = s.html, key = "s" + i;
    if (/^(Risk|Known Limits|Impact)$/.test(h)) out += blk(key, h, '<div class="risk"><div class="prose">' + html + "</div></div>");
    else if (/^Example$/.test(h)) out += blk(key, h, '<div class="example"><div class="prose">' + html + "</div></div>");
    else if (/^(Procedure|Verification|Rollback or Recovery)$/.test(h)) out += blk(key, h, '<div class="prose procedure">' + html + "</div>");
    else if (/Open (Modeling )?Questions/.test(h)) out += blk(key, h, callout("info", "question", '<div class="prose">' + html + "</div>"));
    else out += blk(key, h, '<div class="prose">' + html + "</div>");
  });
  return out;
}
function leadHtml(html) { return html ? '<div class="lead prose">' + html + "</div>" : ""; }
var WHY_SECTIONS = /^(Why.*|Rationale)$/;
function whyHtml(r) {
  var f = r.fields || {}, parts = [];
  [["reason", "Reason"], ["rationale", "Rationale"], ["appliesBecause", "Applies because"], ["refusedBecause", "Refused because"]].forEach(function (k) { if (f[k[0]]) parts.push([k[1], inline(String(f[k[0]]))]); });
  secList(r).forEach(function (x) { if (WHY_SECTIONS.test(x.h) && (x.html || "").trim()) parts.push([x.h, x.html]); });
  return parts.length ? '<div class="whyb">' + parts.map(function (p) { return '<div class="whyr"><span class="whyk">' + esc(p[0]) + '</span><div class="whyv prose">' + p[1] + "</div></div>"; }).join("") + "</div>" : "";
}
function lead(text) { return text ? '<p class="lead">' + inline(String(text)) + "</p>" : ""; }
function jump(items) { items = items.filter(function (x) { return x[2]; }); return items.length > 2 ? '<nav class="jump" aria-label="On this record">' + items.map(function (x) { return '<a href="#b-' + x[0] + '">' + esc(fieldLabel(x[1])) + (typeof x[2] === "number" ? " <span>" + x[2] + "</span>" : "") + "</a>"; }).join("") + "</nav>" : ""; }
// Children of the outline stand in where the mockup read embedded lists.
function kidsByType(id, t) { return (kidsOf[base(id)] || []).filter(function (x) { return typeOf(x) === t; }); }
function descByType(id, t, maxDepth) {
  var out = [], seen = {};
  (function walk(x, d) {
    if (d > maxDepth) return;
    (kidsOf[x] || []).forEach(function (k) { if (seen[k]) return; seen[k] = 1; if (typeOf(k) === t) out.push(k); walk(k, d + 1); });
  })(base(id), 1);
  return out;
}
function resultsOf(id) { var r = REC[base(id)]; return (r && r.results) || kidsByType(id, "result"); }
function scenariosOf(id) { var r = REC[base(id)]; if (r && r.scenarios) return r.scenarios; var out = descByType(id, "scenario", 2); return out.length ? out : kidsByType(id, "scenario"); }
function rulesOf(id) { var r = REC[base(id)]; return (r && r.rules) || kidsByType(id, "rule"); }
function eventsOf(id) { var r = REC[base(id)]; return (r && r.events) || kidsByType(id, "event"); }
function screensOf(id) { var r = REC[base(id)]; if (r && r.screens) return r.screens; return ins(base(id), "offers"); }
function shownByOf(id) { var r = REC[base(id)]; return (r && r.shownBy) || kidsByType(id, "scenario"); }
function resultInfoOf(scenarioId) {
  var r = REC[base(scenarioId)];
  if (r && r.resultInfo) return r.resultInfo;
  var rid = outs(base(scenarioId), "result")[0];
  var q = rid ? REC[rid] : null;
  return q ? { title: q.title, outcome: (q.fields || {}).outcome, code: (q.fields || {}).code, meaning: (q.fields || {}).meaning } : null;
}
function refusalOf(ruleId) {
  var r = REC[base(ruleId)];
  if (r && r.refusalInfo) return { info: r.refusalInfo, id: null };
  var rid = outs(base(ruleId), "refusal")[0];
  var q = rid ? REC[rid] : null;
  return { info: q ? { title: q.title, code: (q.fields || {}).code, meaning: (q.fields || {}).meaning } : null, id: rid || null };
}
function resultCards(list) {
  if (!list || !list.length) return "";
  return '<div class="resgrid">' + list.map(function (x) { var ok = x.outcome === "success"; return '<div class="res ' + (ok ? "ok" : "no") + '"><div class="rt">' + ic(ok ? "passing" : "x", "icon-sm") + (x.id && (REC[x.id] || NODE[x.id]) ? '<button type="button" class="iref" data-go="' + esc(x.id) + '">' + esc(x.title) + "</button>" : "<span>" + esc(x.title) + "</span>") + (x.kind === "generic" ? " " + badge("generic refusal") : "") + "</div><p>" + esc(x.meaning) + "</p>" + (x.code ? '<div class="rc"><span class="code">' + esc(x.code) + "</span></div>" : "") + "</div>"; }).join("") + "</div>";
}
function scenarioCard(sid, open) {
  var s = REC[base(sid)]; if (!s) return "";
  var f = s.fields || {}, ri = resultInfoOf(sid);
  var b = '<dl class="gwt"><dt>Given</dt><dd>' + esc(f.given) + "</dd><dt>When</dt><dd>" + esc(f.when) + "</dd><dt>Then</dt><dd>" + esc(f.then) + "</dd></dl>";
  b += '<div class="sc-f">' + (ri ? badge(ri.title, ri.outcome === "success" ? "good" : "bad") + (ri.code ? '<span class="code">' + esc(ri.code) + "</span>" : "") : "") + '<span style="flex:1"></span><button type="button" class="iref" data-go="' + esc(sid) + '">Open scenario</button></div>';
  return '<div class="sc' + (open ? " open" : "") + '"><button type="button" class="sc-h" data-sc aria-expanded="' + !!open + '">' + ic("chev", "icon-sm chev") + '<span class="t">' + esc(s.title) + "</span>" + pmark(s.proof ? s.proof.state : null) + '</button><div class="sc-b">' + b + "</div></div>";
}
function scenarioBlock(id, scs) {
  scs = (scs || []).filter(function (s) { return REC[base(s)]; }); if (!scs.length) return "";
  var lim = showAllScen[id] ? scs.length : 6, o = { failing: 0, unconfirmed: 1, unproved: 2, awaiting: 3, passing: 4, norun: 3 };
  var ord = scs.slice().sort(function (a, b) { var pa = (REC[base(a)].proof || {}).state, pb = (REC[base(b)].proof || {}).state; return (o[pa] === undefined ? 5 : o[pa]) - (o[pb] === undefined ? 5 : o[pb]); });
  var states = {}; scs.forEach(function (s) { var p = (REC[base(s)].proof || {}).state || "unproved"; states[p] = (states[p] || 0) + 1; });
  var aside = Object.keys(states).map(function (k) { return states[k] + " " + (PLABEL[k] || k).toLowerCase(); }).join(", ");
  return blk("sc", "Scenarios", '<div class="scs">' + ord.slice(0, lim).map(function (s, i) { return scenarioCard(s, i === 0 && (REC[base(s)].proof || {}).state === "failing"); }).join("") + "</div>" + (scs.length > lim ? '<button type="button" class="btn ol more" data-allsc="' + esc(id) + '">Show all ' + scs.length + " scenarios</button>" : ""), scs.length, aside);
}
function effectsBlock(f) {
  var list = f.effects || []; if (!list.length) return "";
  var VERB = { runs: ["Runs", "use-case"], sends: ["Sends", "notification"], emits: ["Emits", "integration-event"], calls: ["Calls", "ext"] };
  return blk("eff", "effects", '<div class="effects">' + list.map(function (e) {
    var k = Object.keys(VERB).filter(function (x) { return e[x]; })[0]; if (!k) return "";
    var tgt = e[k], op = String(tgt).split("#")[1];
    var cond = [e.forEach ? "For " + e.forEach : "", e.onFailure ? edgeFailureText(e.onFailure) : "", e.sharesTransaction !== undefined ? (String(e.sharesTransaction) === "true" ? "In the same transaction" : "In its own transaction") : ""].filter(Boolean).join(" &middot; ");
    return '<div class="eff' + (k === "calls" ? " calls" : "") + '"><span class="verb">' + ic(VERB[k][1], "icon-sm") + VERB[k][0] + "</span><span>" + inlineRef(tgt) + (op ? ' <span class="tn" style="font-family:var(--sans)">' + esc(op.replace(/-/g, " ")) + "</span>" : "") + (cond ? '<span class="cond">' + cond + "</span>" : "") + "</span>" + (e.timing ? badge(cap(e.timing), e.timing === "inline" ? "info" : "") : "") + "</div>";
  }).join("") + "</div>", list.length);
}
// One wave-5 value in LiteSpec words: the mode names the case, the reason completes "because ...".
function modeWord(m) { return String(m || "").replace(/-/g, " "); }
function opRef(op) { var b = base(op), tail = String(op || "").split("#")[1]; return inlineRef(b) + (tail ? ' <span class="tn" style="font-family:var(--sans)">' + esc(tail.replace(/-/g, " ")) + "</span>" : ""); }
function freshnessNote(g) {
  var f = g.freshness; if (!f) return "";
  if (f.mode === "bounded") return "fresh within " + duration(f.within);
  if (f.mode === "advisory") return "advisory, authoritative value in " + inlineRef(f.authority);
  return esc(cap(modeWord(f.mode)));
}
function pagingHtml(p) {
  if (!p) return "";
  if (p.mode === "none") return kv([["Paging", "None" + (p.reason ? ", because " + esc(p.reason) : "")]]);
  return kv([["Order", esc(p.order)], ["Paging", esc(p.offset || p.cursor)], ["Page size", p.defaultLimit + " by default, at most " + p.maxLimit]]);
}
function compensationHtml(c) {
  if (!c) return "";
  if (c.mode === "use-case") return inlineRef(c.useCase);
  return "None" + (c.reason ? ", because " + esc(c.reason) : "");
}
function periodSuffix(p) {
  if (!p) return "";
  if (typeof p === "string") return " after " + duration(p);
  return ", None" + (p.reason ? ", because " + esc(p.reason) : "");
}
function releasedByHtml(r) {
  if (!r) return "";
  if (typeof r === "string") return inlineRef(r);
  return "Never" + (r.reason ? ", because " + esc(r.reason) : "");
}
function erasureHtml(e) {
  if (!e) return "";
  if (e.mode === "call") return "Calls " + opRef(e.operation);
  if (e.mode === "event") return "Forwards through " + inlineRef(e.event);
  return "Impossible" + (e.reason ? ", because " + esc(e.reason) : "");
}
function edgeFailureText(o) {
  if (!o) return "";
  if (o.mode === "retry") { var r = o.retry || {}; return "On failure: " + plural(r.maxAttempts || 0, "attempt") + (r.initialDelay ? ", from " + duration(r.initialDelay) : "") + (r.maxDelay ? " up to " + duration(r.maxDelay) : "") + (r.backoff ? ", " + esc(modeWord(r.backoff)) + " backoff" : "") + (o.then ? ", then " + esc(modeWord(o.then)) : ""); }
  if (o.mode === "skip-and-log") return "On failure: skips and logs";
  if (o.mode === "discard") return "On failure: discards";
  return "On failure: " + esc(modeWord(o.mode));
}
function idemBlock(f) {
  var i = f.idempotency; if (!i) return "";
  var MODE = { natural: "Naturally idempotent", key: "Idempotency key", none: "Not idempotent", "read-only": "Reads only" };
  return tile("idempotency", esc(MODE[i.mode] || cap(i.mode)), [i.key ? '<span class="code">' + esc(i.key) + "</span>" : "", i.scope ? "scoped to " + esc(i.scope) : "", i.reason ? esc(i.reason) : ""].filter(Boolean).join(" &middot; "));
}
function failureBlock(f) {
  var o = f.onFailure; if (!o) return "";
  var r = o.retry || null;
  var retry = r ? plural(r.maxAttempts || 0, "attempt") + (r.initialDelay ? ", from " + duration(r.initialDelay) : "") + (r.maxDelay ? " up to " + duration(r.maxDelay) : "") + (r.backoff ? ", " + esc(modeWord(r.backoff)) + " backoff" : "") : "";
  return blk("fail", "onFailure", '<div class="tiles">' + tile("mode", esc(cap(modeWord(o.mode)))) + (retry ? tile("retry", retry) : "") + (o.then ? tile("then", esc(modeWord(o.then))) : "") + (o.deadLetter ? tile("deadLetter", "After " + duration(o.deadLetter.after), esc(o.deadLetter.action)) : "") + (o.surfacesIn ? tile("surfacesIn", inlineRef(o.surfacesIn)) : "") + (o.runbook ? tile("runbook", inlineRef(o.runbook)) : "") + "</div>");
}
function accessBlock(id, f) {
  var a = f.access; if (!a) return "";
  var TEN = { own: ["own", "good"], cross: ["cross", "bad"], public: ["public", "warn"], none: ["none", ""] };
  var g = a.grant || "", grant = g.indexOf("permission/") === 0 ? inlineRef(g) : g.indexOf("self") === 0 ? "self" + (g.indexOf("(") > 0 ? ", " + inline(g.slice(g.indexOf("(") + 1, -1)) : "") : esc(g);
  var who = performers(id), t = TEN[a.tenant] || [cap(a.tenant), ""];
  return blk("acc", "access", '<div class="tiles">' + tile("auth", (a.auth || []).length ? a.auth.map(function (x) { return badge(x); }).join(" ") : "none") + tile("grant", grant) + tile("tenant", badge(t[0], t[1])) + tile("reachedThrough", (f.reachedThrough || []).map(inlineRef).join(", ") || "&ndash;") + "</div>" + '<div class="sub">actor, through roles that allow the grant</div>' + (who.length ? refs(uniq(who.map(function (w) { return w.id; }))) : '<p class="prose" style="color:var(--fg-2)">' + (g === "anonymous" || g === "system" ? "grant: " + esc(g) : "No role allows this permission.") + "</p>"));
}
function dataTouched(f) {
  var s = "";
  if ((f.changes || []).length) s += kv([["Changes", refs(f.changes)]]);
  if ((f.reads || []).length) s += kv([["Reads", refs(f.reads)]]);
  if (f.consistency) s += kv([["Consistency", badge(cap(f.consistency.mode), "info") + ((f.consistency.cites || []).length ? '<div class="tn" style="font-family:var(--sans);margin-top:4px">because ' + f.consistency.cites.map(inlineRef).join(", ") + "</div>" : "")]]);
  return s;
}
function drivenByBlock(f) {
  var d = f.drivenBy; if (!d) return "";
  if (d.automation) return blk("drv", "drivenBy", refs([d.automation]));
  return blk("drv", "drivenBy", kv([["Person", d.person ? inlineRef(d.person) : ""], ["Runbook", d.runbook ? inlineRef(d.runbook) : ""], ["Due within", d.dueWithin ? duration(d.dueWithin) : ""]]));
}
function triggeredByHtml(t) {
  if (!t) return "";
  if (t.event) return '<div class="flowline">' + inlineRef(t.event) + "</div>";
  if (t.schedule) return '<div class="tiles">' + tile("schedule", t.schedule.every ? "Every " + duration(t.schedule.every) : t.schedule.cron ? '<span class="code">' + esc(t.schedule.cron) + "</span>" : "") + "</div>";
  if (t.manual) return '<div class="tiles">' + tile("manual", "Runs by hand") + "</div>";
  return "";
}
function typedFieldHtml(x) {
  var bits = [];
  if (x.type) bits.push('<span class="code">' + esc(x.type) + "</span>");
  if (x.required) bits.push(badge("required"));
  if (x.of) bits.push(Array.isArray(x.of) ? esc(x.of.join(", ")) : inlineRef(x.of));
  return bits.join(" ");
}

// ---------- header, questions, findings ----------
function header(id) {
  var r = REC[base(id)], l = layerByKey[layerOf[id]], h = human(id);
  if (!l) l = { key: id.split("/")[1], label: "LiteSpec" };
  var s = '<div class="eyebrow" hidden>' + ic(LAYER_ICON[l.key] || "core", "icon-sm") + "<span>" + esc(l.label) + "</span><span>&middot;</span>" + (isReal(id) ? xp("type:" + r.type, esc(typeName(r.type))) : "<span>" + esc(typeName(r.type)) + "</span>") + "</div>";
  s += "<h1>" + esc(r.title) + "</h1>";
  if ((LEX.types || {})[r.type] || GL[(TYPE[r.type] || "").toLowerCase()]) s += '<details class="about"><summary>' + ic("info", "icon-sm") + "What is " + (/^([aeio]|u(?!se))/i.test(typeName(r.type)) ? "an " : "a ") + esc(typeName(r.type).toLowerCase()) + "?</summary><div>" + explainHtml("type:" + r.type) + "</div></details>";
  var chain = [], p = parentOf[id]; while (p) { chain.unshift(p); p = parentOf[p]; }
  var lk = layerOf[id] || (id.indexOf("layer/") === 0 ? id.slice(6) : null), ly = lk && layerByKey[lk];
  s = '<nav class="crumb" aria-label="Location">' + (ly && id.indexOf("layer/") !== 0 ? '<button type="button" data-go="layer/' + lk + '">' + esc(ly.label) + '</button><span class="s">/</span>' : "") + chain.map(function (c) { return '<button type="button" data-go="' + esc(c) + '">' + esc(title(c)) + '</button><span class="s">/</span>'; }).join("") + (ly ? '<span class="cty">' + esc(PLURAL[r.type] || typeName(r.type)) + '</span><span class="s">/</span>' : "") + "<b>" + esc(r.title) + "</b></nav>" + s;
  s += '<div class="srcrow">' + (r.src ? srcLink(r.src) : "") + "</div>";
  return s + (layerOf[id] ? travHtml(id) : "");
}
function travHtml(id) {
  var sib = (siblingsOf(id) || []).filter(function (x) { return typeOf(x) === typeOf(id); }), i = sib.indexOf(id), p = parentOf[id];
  var s = '<div class="trav">';
  s += '<button type="button" class="tbtn" data-go="' + esc(p || "") + '"' + (p ? "" : " disabled") + ' title="Up to the parent (u)">' + ic("up", "icon-sm") + "Up" + "</button>";
  s += '<button type="button" class="tbtn ic" aria-label="Previous sibling" title="Previous sibling ([)" data-go="' + esc(sib[i - 1] || "") + '"' + (i > 0 ? "" : " disabled") + ">" + ic("left", "icon-sm") + "</button>";
  s += '<span class="pos">' + (i + 1) + " of " + sib.length + " " + esc((PLURAL[typeOf(id)] || "").toLowerCase()) + (p ? " in " + esc(title(p)) : "") + "</span>";
  s += '<button type="button" class="tbtn ic" aria-label="Next sibling" title="Next sibling (])" data-go="' + esc(sib[i + 1] || "") + '"' + (i < sib.length - 1 ? "" : " disabled") + ">" + ic("right", "icon-sm") + "</button>";
  return s + '<span class="grow"></span></div>';
}
// Every page opens with the same facts, in the same order and the same shape.
function factKey(text) {
  if (/^(test written, not run|test not traced|no test|passing|failing|stale)$/i.test(text)) return "Proof";
  if (/steps served$/.test(text)) return "Steps served";
  if (/^(implemented|not implemented)$/.test(text)) return "Implemented";
  if (/^outcome: /.test(text)) return "Outcome";
  if (/^Ends |^Expired /.test(text)) return "Ends";
  if (/^Review by /.test(text)) return "Review by";
  if (/state(s)? never reached|Every state is reached/.test(text)) return "States";
  if (/^applies: /.test(text)) return "Applies";
  if (/^Tier [GSA]$/.test(text)) return "Check level";
  if (/^(Arrives from outside|Leaves the system)$/.test(text)) return "Direction";
  if (/^Verified|^Not yet verified/.test(text)) return "Verified";
  if (/^(planned|shipped|later|refused)$/i.test(text)) return "State";
  return "Kind";
}
function metaRow(id, extra) {
  var r = REC[base(id)], f = r.fields || {}, facts = [], h = human(id);
  function add(k, v, ex) { if (v) facts.push('<div class="fact"><dt>' + esc(k) + "</dt><dd" + (ex ? ' data-explain="' + ex + '" tabindex="0"' : "") + ">" + v + "</dd></div>"); }
  if (isReal(id)) add("Status", esc(HLABEL[h === "mineR" ? "mine" : h] || h), h);
  if (isReal(id)) {
    var why = r.status === "draft" ? "Waiting for " + esc(r.decider || "a decider") + (r.decideBy ? " to decide by " + fmtDate(r.decideBy) : "") + (openQs(id).length ? ", with " + plural(openQs(id).length, "open question") : "")
      : r.effective === "draft" ? "Counts as draft while " + ((r.heldBy || []).length ? r.heldBy.map(inlineRef).join(", ") + ((r.heldBy || []).length === 1 ? " is" : " are") + " draft" : "a record above it is draft")
      : r.status === "retired" ? "Replaced" : (f.approvedBy ? (isBeh(id) ? "Reviewed by " : "Approved by ") + esc(f.approvedBy) + (f.approvedOn ? " on " + fmtDate(f.approvedOn) : "") : "Settled");
    add("Status reason", why);
  }
  if (r.owner) add("Owner", esc(r.owner));
  if (isReal(id)) add("Risk tier", (f.risk || []).length ? "Tier 1: " + esc(f.risk.join(", ").replace(/-/g, " ")) : "Tier 3", "tier1");
  var bo = batchOf(id);
  if (bo) add("Approval batch", esc(batchName(bo.batch)) + ' <span class="tn">(' + plural(bo.batch.n || 0, "record") + ")</span>", "batch");
  if (f.provenance === "transcribed") add("Provenance", "Transcribed from code", "transcribed");
  (extra || []).forEach(function (x) { if (!x) return; var tmp = document.createElement("div"); tmp.innerHTML = x; var text = tmp.textContent.trim(); if (!text) return; var ex = (tmp.querySelector("[data-explain]") || {}).getAttribute ? tmp.querySelector("[data-explain]").getAttribute("data-explain") : ""; var key = factKey(text); text = text.replace(new RegExp("^" + key + "\\s*:?\\s*", "i"), "").replace(/^[a-z]+:\s*/i, function (m) { return /^(outcome|applies|kind):/i.test(m) ? "" : m; }).replace(/ steps served$/, ""); if (text === "true") text = "Yes"; if (text === "false") text = "No"; add(key, esc(cap(text)), ex); });
  return facts.length ? '<dl class="facts">' + facts.join("") + "</dl>" : "";
}
function machineChip(state, text) { var k = state === "passing" ? "good" : state === "failing" ? "bad" : state ? "wait" : ""; return '<span class="mchip ' + k + '"' + (EXPLAIN[state] ? ' data-explain="' + state + '" tabindex="0"' : "") + ">" + ic(state || "flask", "icon-sm") + esc(text || PLABEL[state]) + "</span>"; }
function questionsBlock(id) {
  var qs = ((REC[base(id)] || {}).fields || {}).questions || []; if (!qs.length) return "";
  return blk("qs", "questions", qs.map(function (q, i) {
    var t = tierOf(id), done = answered[base(id) + "#" + i], auto = t === 3 && q.recommendation && q.decideBy ? addDays(q.decideBy, VETO) : null;
    var nm = q.ballot ? ballotName(q.ballot) : "";
    return '<div class="qcard' + (t === 1 ? " t1" : "") + (focusQ === i ? " hl" : "") + '" id="q-' + i + '"><div class="qcard-h">' + ic(t === 1 ? "alert" : "question", "icon-sm") + "<b>Tier " + t + "</b>" + (nm ? "<span>&middot; Ballot: " + esc(cap(nm)) + "</span>" : "") + '<span class="grow"></span>' + (q.decideBy ? "<span>" + esc(q.decider || "") + " by " + fmtDate(q.decideBy) + " (" + dateHint(q.decideBy) + ")</span>" : "") + "</div>" +
      '<div class="qcard-b"><p class="q">' + inline(q.question) + "</p>" + (q.recommendation ? '<div class="rec">' + ic("approved", "icon-sm") + "<div><b>Recommended.</b> " + inline(q.recommendation) + "</div></div>" : "") + "</div>" +
      '<div class="qcard-f">' + (done ? '<span class="answered">' + ic("approved", "icon-sm") + (done === "accept" ? "You accepted the recommendation" : "You answered: " + esc(done)) + "</span>" : (q.recommendation ? '<button type="button" class="btn" data-ans="' + i + '|accept">Accept recommendation</button>' : "") + '<button type="button" class="btn ol" data-ans="' + i + '|other">Answer differently</button>') + '<span class="grow"></span>' + (auto && !done ? "<span>Auto-approves on " + fmtDate(auto) + "</span>" : t === 1 && !done ? "<span>Needs an explicit answer</span>" : "") + "</div></div>";
  }).join(""), qs.length);
}
function findingsBlock(id) {
  var f = (REC[base(id)] || {}).findings || []; if (!f.length) return "";
  var TL = { G: ["Blocks a release", "bad"], S: ["Blocks once approved", "warn"], A: ["Advice", ""] };
  return blk("checks", "Findings", '<div class="finds">' + f.map(function (x) { var tl = x.known ? ["Accepted gap", "info"] : TL[x.tier] || ["", ""]; return '<div class="find' + (x.known ? " known" : "") + '">' + ic(x.known ? "known-gap" : "alert", "icon-sm") + '<div><div class="fp">' + esc(findingMeaning(x.code)) + "</div>" + '<div class="fm">' + inline(String(x.message || "").replace(/'([a-z-]+\/[^']+)'/g, "`$1`")) + "</div>" + '<div class="fx"><span class="fc">' + esc(x.code) + "</span>" + (x.path ? '<span class="code">' + esc(x.path) + (x.line ? ":" + x.line : "") + "</span>" : "") + "</div></div>" + xp(x.known ? "known" : x.tier, badge(tl[0], tl[1])) + "</div>"; }).join("") + "</div>", f.length, "what the checks found on this record");
}
function kidsStrip(id) {
  var k = kidsOf[id] || []; if (k.length < 2 || k.length > 60) return "";
  var types = uniq(k.map(typeOf));
  return '<div class="kids-strip"><span class="lab">Contains</span>' + types.map(function (t) { var n = k.filter(function (x) { return typeOf(x) === t; }); return '<button type="button" class="tbtn" data-go="' + esc(n[0]) + '">' + ic(t, "icon-sm") + n.length + " " + esc((n.length === 1 ? TYPE[t] : PLURAL[t] || t).toLowerCase()) + "</button>"; }).join("") + "</div>";
}

// ---------- per-type detail ----------
var DETAIL = {
  product: function (id, r) { var f = r.fields || {}; return (introClosed() ? "" : '<div class="intro">' + ic("book") + '<div><b>New here?</b> The outline follows the four record layers. The rail on the right traces each record from its goal down to the code and tests. Hover any mark, type or link for its LiteSpec definition. Every source path opens in the editor.</div><button type="button" class="btn gh ic" data-intro aria-label="Close this">' + ic("close", "icon-sm") + "</button></div>") + metaRow(id) + lead(f.boundary) + blk("exc", "exclusions", '<ul class="exclusions">' + (f.exclusions || []).map(function (x) { return "<li>" + ic("x", "icon-sm") + "<span>" + esc(x) + "</span></li>"; }).join("") + "</ul>", (f.exclusions || []).length) + blk("aud", "addresses", refs(f.addresses)); },
  goal: function (id, r) { var f = r.fields || {}; return metaRow(id) + lead(f.statement) + blk("m", "measure", '<div class="tiles">' + tile("Measure", esc(f.measure)) + tile("target", esc(f.target), "", true) + tile("dueBy", fmtDate(f.dueBy), dateHint(f.dueBy)) + "</div>") + blk("served", "Points at this: advances", refs(ins(id, "advances").concat(ins(id, "serves")))); },
  problem: function (id, r) { var f = r.fields || {}; return metaRow(id) + lead(f.statement) + blk("aff", "affects", refs(f.affects)); },
  feature: function (id, r) { var f = r.fields || {}, st = f.lifecycle; return metaRow(id, [badge(cap(st), st === "shipped" ? "good" : st === "refused" ? "bad" : "info")]) + leadHtml(secHtml(r, "Offering")) + "" + blk("links", "solves and advances", kv([["Solves", refs(f.solves)], ["Advances", refs(f.advances)]])) + blk("reqs", "Points at this: cites", refs(ins(id, "cites")), ins(id, "cites").length); },
  topic: function (id, r) { var reqs = kidsByType(id, "requirement"); return metaRow(id) + leadHtml(secHtml(r, "Summary")) + blk("reqs", "Requirements", refs(reqs), reqs.length); },
  requirement: function (id, r) {
    var f = r.fields || {}, ful = uniq(ins(id, "fulfills")), ver = ful.filter(function (x) { return proofState(x) === "passing"; }).length;
    var testRun = (MAN || {}).testRun || {};
    var cov = ful.length ? '<div class="tiles">' + tile("fulfills", ful.length, uniq(ful.map(typeOf)).map(function (t) { return ful.filter(function (x) { return typeOf(x) === t; }).length + " " + (PLURAL[t] || t).toLowerCase(); }).join(", "), true) + (testRun.date ? tile("verified", ver + " of " + ful.length, '<span class="meter" style="display:flex"><i style="width:' + Math.round(ver / ful.length * 100) + '%;background:var(--conv-fg)"></i></span>', true) : tile("verified", xp("norun", "Not known yet"), "no test run is in the model", true)) + "</div>" : callout("bad", "alert", "Nothing in Behavior fulfills this requirement yet.");
    var exc = (f.except || []).length ? (f.except.map(function (x) { var fb = base(x.fallback), fx = REC[fb]; return '<div class="reqline"><div class="t">' + ic("alert", "icon-sm") + "Except when " + esc(x.condition) + '</div><p>Then the fallback holds: ' + inlineRef(x.fallback) + (fx ? ", " + esc(fx.fields.statement || "") : "") + "</p></div>"; }).join("")) : "";
    var meas = f.measure ? '<div class="tiles">' + tile("Measure", esc(f.measure)) + (f.target ? tile("target", esc(f.target), "", true) : "") + (f.dueBy ? tile("dueBy", fmtDate(f.dueBy), dateHint(f.dueBy)) : "") + "</div>" + kv([["Check", f.check ? esc(f.check) : ""], ["Runs on", f.runsOn ? esc(f.runsOn) : ""], ["Evidence", f.evidence ? esc(f.evidence) : ""]]) : "";
    var vals = (f.values || []).length ? table(["Value", "Effective from"], f.values.map(function (v) { return [td(esc(v.value)), td(fmtDate(v.effectiveFrom) + " &middot; " + dateHint(v.effectiveFrom), "mut")]; })) : "";
    return metaRow(id, [badge(f.kind, "info"), machineChip(r.machine && r.machine.verified ? "passing" : "awaiting", r.machine && r.machine.verified ? "Verified" : "Not yet verified")]) + lead(f.statement) + "" +
      blk("exc", "except", exc, (f.except || []).length) + blk("cov", "Points at this: fulfills", cov) + blk("meas", "measure", meas) + blk("vals", "values", vals) + blk("cites", "cites", refs(f.cites)) + blk("ful", "Points at this: fulfills", refs(ful), ful.length);
  },
  decision: function (id, r) {
    var f = r.fields || {}, choice = f.choice;
    var head = choice ? callout("info", "passing", "<b>Chosen:</b> " + esc(choice) + (f.approvedOn ? " on " + fmtDate(f.approvedOn) : "")) : callout("warn", "clock", "<b>Not decided yet.</b> " + esc(r.decider || "The decider") + (r.decideBy ? " decides by " + fmtDate(r.decideBy) + " (" + dateHint(r.decideBy) + ")." : "."));
    var opts = '<div class="opts">' + (f.options || []).map(function (o) { var c = choice && o.name === choice; return '<div class="opt' + (c ? " chosen" : "") + '"><h3>' + esc(o.name) + (c ? " " + badge("Chosen", "good") : "") + '</h3><div class="pc"><span class="plus">' + ic("plus", "icon-sm") + "</span><span>" + esc(o.pros) + '</span><span class="minus">' + ic("minus", "icon-sm") + "</span><span>" + esc(o.cons) + "</span></div></div>"; }).join("") + "</div>";
    return metaRow(id) + head + blk("opts", "options", opts, (f.options || []).length) + (f.reopenedWhen ? blk("rev", "reopenedWhen", callout("quiet", "clock", esc(f.reopenedWhen))) : "") + blk("dec", "decides", refs(f.decides), (f.decides || []).length) + blk("basis", "basedOn", refs(f.basedOn)) + (f.replaces ? blk("rep", "replaces", refs([f.replaces])) : "");
  },
  constraint: function (id, r) { var f = r.fields || {}; return metaRow(id, [badge(f.kind, "info"), badge("applies: " + !!f.applies, f.applies ? "warn" : "")]) + leadHtml(secHtml(r, "Summary")) + blk("src", "authority", callout("quiet", "quote", esc(f.authority))) + ""; },
  research: function (id, r) { var f = r.fields || {}; return metaRow(id) + lead(f.question) + (f.conclusion ? blk("con", "conclusion", '<div class="prose">' + inline(String(f.conclusion)) + "</div>") : "") + blk("srcs", "Sources", '<ol class="rlist">' + (f.sources || []).map(function (x) { if (x.path) return '<li><span class="code">' + esc(x.path) + "</span>" + (x.title ? " " + esc(x.title) : "") + '<span class="rsub">' + (x.readOn ? "read " + fmtDate(x.readOn) : "") + "</span></li>"; var host = ""; try { host = new URL(x.url).host.replace(/^www\./, ""); } catch (e) { host = ""; } return '<li><a href="' + esc(x.url) + '" target="_blank" rel="noopener">' + esc(x.title || x.url.replace(/^https?:\/\/(www\.)?/, "")) + '</a><span class="rsub">' + esc(host) + (x.readOn ? ", read " + fmtDate(x.readOn) : "") + "</span></li>"; }).join("") + "</ol>", (f.sources || []).length); },
  journey: function (id, r) {
    var f = r.fields || {}, steps = f.steps || [], srv = r.stepServers || {};
    var served = steps.filter(function (s) { return (srv[s.id] || []).length; }).length;
    var stepsH = steps.map(function (s, i) {
      var who = s.actor ? title(s.actor) : "", ini = who.split(/\s+/).map(function (w) { return w[0]; }).join("").slice(0, 2).toUpperCase();
      var by = srv[s.id] || [], scr = (r.stepScreens || {})[s.id] || [], sys = /(automation)$/.test(s.actor || "");
      return '<div class="chapter-step"><span class="num">' + (i + 1) + '</span><div><div class="statement">' + esc(s.statement) + "</div>" + (who ? '<div class="who"><span class="avatar">' + esc(ini) + "</span>" + inlineRef(s.actor) + "</div>" : "") + (s.failure ? '<div class="wrong">' + ic("alert", "icon-sm") + "<span><b>If it goes wrong</b> " + esc(s.failure) + "</span></div>" : "") + '<div class="srv">' + (by.length ? by.map(serverLine).join("") : '<span class="unserved">' + ic("alert", "icon-sm") + "Nothing serves this step yet</span>") + (scr.length ? '<div class="stepscr">' + ic("screen", "icon-sm") + "<span>screen: " + scr.map(inlineRef).join(", ") + "</span></div>" : sys || !by.length ? "" : '<div class="stepscr none">' + ic("alert", "icon-sm") + "<span>No screen carries this step</span></div>") + "</div></div></div>";
    }).join("");
    var reqs = (f.cites || []).map(function (q) { var x = REC[base(q)]; return '<div class="reqline"><div class="t">' + ic("requirement", "icon-sm") + inlineRef(q) + "</div>" + (x ? "<p>" + esc(x.fields.statement || "") + "</p>" + (((x.fields || {}).except || []).length ? '<div class="exc">' + x.fields.except.map(function (e) { return "<b>Except when</b> " + esc(e.condition) + ", " + inlineRef(e.fallback); }).join("<br>") + "</div>" : "") : "") + "</div>"; }).join("");
    return metaRow(id, ['<span class="mchip' + (served === steps.length ? " good" : " wait") + '">' + ic("journey", "icon-sm") + served + " of " + steps.length + " steps served</span>"]) + (f.finishedWhen ? '<div class="finished">' + ic("passing", "icon-sm") + "<div><b>Finished when</b> " + esc(f.finishedWhen) + "</div></div>" : "") +
      blk("actor", "actor", refs([f.actor])) + blk("steps", "steps", stepsH, steps.length) + blk("cites", "cites", reqs, (f.cites || []).length);
  },
  actor: function (id, r) { var f = r.fields || {}, h = f.holds || []; return metaRow(id, [badge(f.kind, "info")]) + leadHtml(secHtml(r, "Summary")) + blk("holds", "holds", h.length ? table(["Role", "Scope"], h.map(function (x) { return [td(inlineRef(x.role)), td(badge(cap(x.scope)))]; })) : callout("quiet", "info", "This actor holds no role."), h.length) + blk("walks", "Points at this: actor", refs(ins(id, "actor"))); },
  module: function (id, r) {
    var f = r.fields || {}, all = descByType(id, "aggregate", 6).concat(descByType(id, "use-case", 6), descByType(id, "rule", 6), descByType(id, "event", 6), descByType(id, "automation", 6), descByType(id, "notification", 6), descByType(id, "setting", 6)), cnt = function (t) { return all.filter(function (x) { return typeOf(x) === t; }).length; };
    return metaRow(id) + lead(f.summary) + (all.length ? blk("size", "Contains", '<div class="tiles">' + ["aggregate", "use-case", "rule", "event", "automation", "notification", "setting"].map(function (t) { return cnt(t) ? tile(PLURAL[t], cnt(t), "", true) : ""; }).join("") + "</div>") : "") + blk("dep", "dependsOn", refs(f.dependsOn), (f.dependsOn || []).length);
  },
  aggregate: function (id, r) {
    var f = r.fields || {}, reach = r.reachable || [], tr = f.transitions || [], starts = {};
    tr.forEach(function (t) { if (t.from === "-") starts[t.to] = 1; });
    var states = (f.states || []).length ? '<div class="states">' + f.states.map(function (s) { var b = starts[s.name] ? badge("created as", "info") : reach.length && reach.indexOf(s.name) < 0 ? badge("not reachable", "bad") : ""; return '<div class="stc"><div class="n">' + esc(s.name) + b + "</div><p>" + esc(s.meaning) + "</p></div>"; }).join("") + "</div>" : "";
    var froms = []; tr.forEach(function (t) { if (froms.indexOf(t.from) < 0) froms.push(t.from); });
    var flow = tr.length ? '<div class="flow">' + froms.map(function (fr) { var tos = uniq(tr.filter(function (t) { return t.from === fr; }).map(function (t) { return t.to; })); return '<div class="flow-g"><div class="from' + (fr === "-" ? " start" : "") + '">' + (fr === "-" ? "Created as" : esc(fr)) + "</div><div>" + tos.map(function (to) { return '<div class="tr"><span class="arrow">' + ic("arrow", "icon-sm") + '</span><span class="to">' + esc(to) + '</span><span class="by">' + tr.filter(function (t) { return t.from === fr && t.to === to; }).map(function (t) { return t.by ? inlineRef(t.by) : '<span class="tn">no use case named</span>'; }).join("") + "</span></div>"; }).join("") + "</div></div>"; }).join("") + "</div>" : "";
    var steps = (f.steps || []).length ? '<ol class="steps">' + f.steps.map(function (s) { var label = s.id || s.name || ""; return "<li><div><b>" + esc(cap(String(label).replace(/-/g, " "))) + "</b>" + (s.runs ? '<div class="who">Runs ' + inlineRef(s.runs) + "</div>" : "") + (s.guard ? '<div class="who">when ' + esc(s.guard) + "</div>" : "") + '<div class="who">Compensation: ' + compensationHtml(s.compensation) + "</div></div><span></span></li>"; }).join("") + "</ol>" + ((f.exits || []).length ? table(["Exit"], f.exits.map(function (x) { var name = typeof x === "string" ? x : (x.name || ""); return [td(esc(cap(String(name))), "nm")]; })) : "") : "";
    var classes = (f.dataClasses || []).length ? table(["Class", "Fields", "Purpose", "On erasure"], f.dataClasses.map(function (c) { return [td("<b>" + esc(cap(c.name.replace(/-/g, " "))) + "</b><div style=\"margin-top:4px\">" + clsBadge(c.classification) + "</div>"), td('<div class="fieldchips">' + (c.fields || []).map(function (x) { return '<span class="code">' + esc(x) + "</span>"; }).join("") + "</div>"), td(esc(c.purpose || "")), td('<div class="erase"><b>' + esc(cap(c.onErasure)) + "</b>" + periodSuffix(c.period) + "</div>" + (c.basis || c.reason ? '<div class="tn" style="font-family:var(--sans);margin-top:3px">' + esc(c.basis || c.reason) + "</div>" : "") + (c.reviewBy ? '<div class="tn" style="font-family:var(--sans)">review by ' + fmtDate(c.reviewBy) + "</div>" : ""))]; })) : "";
    var data = (f.data || []).length ? table(["Field", "Meaning", "Class"], f.data.map(function (d) { return [td(esc(d.name), "nm"), td(esc(d.meaning)), td(clsBadge(d.classification))]; })) : "";
    var rules = rulesOf(id).filter(function (x) { return REC[base(x)]; });
    var rl = rules.length ? table(["Rule", "Kind", "Refusal"], rules.map(function (x) { var q = REC[base(x)], rf = refusalOf(x); return [td(inlineRef(x) + '<div class="tn" style="font-family:var(--sans);font-size:13px;margin-top:2px">' + esc((q.fields || {}).statement) + "</div>"), td(badge(cap((q.fields || {}).kind))), td(rf.info && rf.info.code ? '<span class="code">' + esc(rf.info.code) + "</span>" : rf.id ? inlineRef(rf.id) : '<span class="tn">none</span>')]; })) : "";
    var evs = eventsOf(id);
    var unreached = (f.states || []).filter(function (s) { return reach.length && reach.indexOf(s.name) < 0 && !starts[s.name]; }).length;
    return metaRow(id, [badge(cap(f.kind || "entity"), "info"), (f.states || []).length ? machineChip(unreached ? "failing" : "passing", unreached ? plural(unreached, "state") + " never reached" : "Every state is reached") : ""]) + lead(f.summary) +
      (f.finishedWhen ? '<div class="finished">' + ic("passing", "icon-sm") + "<div><b>Finished when</b> " + esc(f.finishedWhen) + "</div></div>" : "") +
      jump([["states", "states", (f.states || []).length], ["flow", "transitions", tr.length], ["steps", "steps", (f.steps || []).length], ["classes", "dataClasses", (f.dataClasses || []).length], ["data", "data", (f.data || []).length], ["rules", "Rules", rules.length], ["events", "Events", evs.length]]) +
      blk("states", "states", states, (f.states || []).length) + blk("flow", "transitions", flow, tr.length, "what moves the record from state to state") + blk("steps", "steps", steps, (f.steps || []).length) + drivenByBlock(f) +
      blk("classes", "dataClasses", classes, (f.dataClasses || []).length, "what is kept, and what happens on erasure") + blk("data", "data", data, (f.data || []).length) + blk("rules", "Rules", rl, rules.length) + blk("events", "Events", refs(evs), evs.length);
  },
  rule: function (id, r) {
    var f = r.fields || {}, rf = refusalOf(id);
    return metaRow(id, [badge(f.kind, "info"), machineChip(r.proof ? r.proof.state : null)]) + lead(f.statement) +
      blk("ref", "refusal", rf.info ? resultCards([{ title: rf.info.title, outcome: "refusal", code: rf.info.code, meaning: rf.info.meaning }]) : rf.id ? refs([rf.id]) : callout("quiet", "info", "This rule names no refusal; it holds by construction.")) +
      blk("gov", "Governs", refs(f.governs), (f.governs || []).length) + blk("binds", "binds", refs(f.binds), (f.binds || []).length) + blk("ful", "Fulfills", refs(f.fulfills)) + blk("basis", "basedOn", refs(f.basedOn)) +
      blk("more", "reads, releasedBy, frozenWhen", kv([["Reads", refs(f.reads)], ["Released by", releasedByHtml(f.releasedBy)], ["Frozen when", f.frozenWhen ? esc(f.frozenWhen) : ""]]));
  },
  event: function (id, r) { var f = r.fields || {}; return metaRow(id) + lead(f.meaning) + blk("carry", "carries", '<div class="fieldchips">' + (f.carries || []).map(function (c) { return '<span class="code">' + esc(c) + "</span>"; }).join("") + "</div>", (f.carries || []).length) + blk("by", "Points at this: raises", refs(ins(id, "raises"))) + blk("trig", "Points at this: triggeredBy.event", refs(ins(id, "triggeredBy.event"))) + blk("tr", "Points at this: translates", refs(ins(id, "translates"))); },
  "use-case": function (id, r) {
    var f = r.fields || {}, res = resultsOf(id).filter(function (x) { return REC[base(x)]; }).map(function (x) { var q = REC[base(x)], qf = q.fields || {}; return { id: x, title: q.title, outcome: qf.outcome, meaning: qf.meaning, code: qf.code, kind: /\.(forbidden|invalid-input)$/.test(x) ? "generic" : "" }; }), scs = scenariosOf(id).filter(function (s) { return REC[base(s)]; });
    var okR = res.filter(function (x) { return x.outcome === "success"; }), noR = res.filter(function (x) { return x.outcome !== "success"; });
    var inputT = (f.input || []).length ? table(["Field", "Type", "Meaning", "Rules"], f.input.map(function (x) { return [td(esc(x.name) + (x.classification ? '<div style="margin-top:4px">' + clsBadge(x.classification) + "</div>" : ""), "nm"), td(typedFieldHtml(x)), td(esc(x.meaning) + (x.origin ? '<div class="tn" style="font-family:var(--sans);margin-top:3px">from ' + esc(x.origin) + "</div>" : "")), td(bullets(x.rules))]; })) : "";
    var out = "";
    if (f.output && f.output.include) out = Object.keys(f.output.include).map(function (k) { var g = f.output.include[k]; return '<div class="sub">' + esc(cap(k)) + " " + clsBadge(g.classification || "internal") + (g.freshness ? ' <span class="tn" style="font-family:var(--sans)">' + freshnessNote(g) + "</span>" : "") + "</div>" + table(["Field", "Meaning"], (g.fields || []).map(function (x) { return [td(esc(x.name), "nm"), td(esc(x.meaning))]; })); }).join("");
    else if (Array.isArray(f.output) && f.output.length) out = table(["Field", "Meaning"], f.output.map(function (x) { return [td(esc(x.name), "nm"), td(esc(x.meaning))]; }));
    var paging = pagingHtml(f.paging);
    var audit = f.audit ? (f.audit.exempt ? callout("quiet", "info", "<b>Not audited.</b> " + esc(f.audit.exempt)) : callout("info", "book", "<b>Audited</b> against " + esc(f.audit.target || "") + ". " + esc(f.audit.level || ""))) : "";
    var resH = (okR.length ? '<div class="sub">Outcome: Success</div>' + resultCards(okR) : "") + (noR.length ? '<div class="sub">Outcome: Refusal</div>' + resultCards(noR) : "");
    var ps = proofState(id);
    return metaRow(id, [badge(f.kind, "info"), machineChip(ps)]) + leadHtml(secHtml(r, "Goal")) + (f.summary && !secHtml(r, "Goal") ? lead(f.summary) : "") +
      jump([["acc", "access", true], ["where", "offers", screensOf(id).length || (browserReached(r) ? true : 0)], ["built", "implements", (r.impl || []).length ? true : 0], ["data", "changes and reads", (f.changes || []).length + (f.reads || []).length], ["in", "input", (f.input || []).length], ["out", "output", out ? true : 0], ["eff", "effects", (f.effects || []).length], ["res", "Results", res.length], ["sc", "Scenarios", scs.length]]) +
      accessBlock(id, f) + whereBlock(id, r) + builtBlock(r) + blk("data", "changes and reads", dataTouched(f)) + blk("guar", "idempotency", '<div class="tiles">' + idemBlock(f) + "</div>" + (audit ? '<div style="margin-top:10px">' + audit + "</div>" : "")) +
      (f.background ? blk("bg", "background", '<div class="prose">' + inline(String(f.background)) + "</div>") : "") +
      blk("in", "input", inputT, (f.input || []).length) + blk("out", "output", out + paging) + effectsBlock(f) + blk("res", "Results", resH, res.length) + scenarioBlock(id, scs) +
      blk("rules", "Points at this: governs", refs(ins(id, "governs")), ins(id, "governs").length) + blk("raises", "Raises", refs(f.raises), (f.raises || []).length) + blk("ful", "fulfills and serves", kv([["Fulfills", refs(f.fulfills)], ["Serves", refs(f.serves)]]));
  },
  automation: function (id, r) {
    var f = r.fields || {}, t = f.triggeredBy || {};
    var kind = t.event ? "event" : t.schedule ? "schedule" : t.manual ? "manual" : "automation";
    var run = (kind === "schedule" || kind === "manual") ? blk("run", "schedule", '<div class="tiles">' + (f.guard ? tile("guard", esc(f.guard)) : "") + (f.batchSize ? tile("batchSize", f.batchSize, "", true) : "") + (f.lease ? tile("lease", typeof f.lease === "string" ? esc(f.lease) : esc(f.lease.name) + " for " + duration(f.lease.duration)) : "") + (f.runsWhere ? tile("runsWhere", esc(f.runsWhere)) : "") + (f.cursor ? tile("cursor", esc(typeof f.cursor === "string" ? f.cursor : (f.cursor.name || ""))) : "") + idemBlock(f) + "</div>" + kv([["Selection", f.selection ? esc(f.selection) : ""], ["Service level", f.serviceLevel ? esc(f.serviceLevel) : ""], ["Evidence", f.evidence ? esc(f.evidence) : ""], ["Runbook", f.runbook ? inlineRef(f.runbook) : ""]])) : blk("guar", "idempotency", '<div class="tiles">' + idemBlock(f) + "</div>");
    return metaRow(id, [badge(cap(kind), "info")]) + (secHtml(r, "Goal") ? leadHtml(secHtml(r, "Goal")) : lead(f.summary)) +
      blk("trig", "triggeredBy", triggeredByHtml(t) + (f.skippedWhen ? '<div class="cond">Skipped when ' + esc(f.skippedWhen) + "</div>" : "")) +
      effectsBlock(f) + run + failureBlock(f) + blk("data", "changes and reads", dataTouched(f)) + blk("ful", "fulfills and serves", kv([["Fulfills", refs(f.fulfills)], ["Serves", refs(f.serves)]]));
  },
  notification: function (id, r) {
    var f = r.fields || {}, arr = function (v) { return Array.isArray(v) ? v : v ? [v] : []; };
    var params = arr(f.params).length ? (typeof arr(f.params)[0] === "string" ? '<div class="fieldchips">' + f.params.map(function (p) { return '<span class="code">' + esc(p) + "</span>"; }).join("") + "</div>" : table(["Parameter", "Meaning", "From"], f.params.map(function (p) { return [td(esc(p.name), "nm"), td(esc(p.meaning)), td(esc(p.source || ""), "mut")]; }))) : "";
    var claims = arr(f.claims).length ? f.claims.map(function (c) { return '<div class="reqline"><div class="t">' + ic("quote", "icon-sm") + esc(c.claim) + "</div>" + (c.requirement ? "<p>Backed by " + inlineRef(c.requirement) + "</p>" : "<p class=\"unserved\">No requirement backs this claim</p>") + "</div>"; }).join("") : "";
    return metaRow(id) + lead(f.meaning) +
      blk("who", "addresses", '<div class="tiles">' + tile("addresses", arr(f.addresses).map(function (a) { return a.indexOf("/") > 0 ? inlineRef(a) : esc(a); }).join(", ") || esc(f.recipient || "")) + tile("channel", arr(f.channel).map(function (c) { return badge(cap(c)); }).join(" ")) + tile("consent", esc(f.consent || "")) + tile("frequency", esc(f.frequency || "")) + "</div>" + kv([["Template", f.template ? '<span class="code">' + esc(f.template) + "</span>" : ""], ["Language", f.locale ? esc(f.locale) : ""], ["Deduplicated by", f.dedupKey ? '<span class="code">' + esc(f.dedupKey) + "</span>" : ""], ["Suppressed when", f.suppressedWhen ? esc(f.suppressedWhen) : ""], ["Personal data", arr(f.personal).map(esc).join(", ")]])) +
      blk("params", "params", params, arr(f.params).length) + blk("claims", "claims", claims, arr(f.claims).length) + blk("by", "Points at this: effects.sends", refs(ins(id, "sends"))) + blk("ful", "fulfills and serves", kv([["Fulfills", refs(f.fulfills)], ["Serves", refs(f.serves)]]));
  },
  setting: function (id, r) {
    var f = r.fields || {};
    return metaRow(id, [f.secret ? badge("Secret", "bad") : "", f.restart ? badge("Needs a restart", "warn") : ""]) + lead(f.meaning || f.statement) +
      blk("val", "default", '<div class="tiles">' + tile("default", f.secret ? "Hidden" : f["default"] !== undefined ? esc(f["default"]) + (f.unit ? " " + esc(f.unit) : "") : f.value !== undefined ? esc(f.value) : "None", "", true) + tile("valueType", esc(cap(f.valueType || ""))) + (f.range ? tile("range", esc(f.range)) : "") + tile("setBy", esc(cap(f.setBy || ""))) + "</div>" + kv([["At the bound", f.atBound ? esc(f.atBound) : ""], ["Read by", refs(ins(id, "reads"))], ["Runbook", f.runbook ? inlineRef(f.runbook) : ""]])) +
      blk("ful", "Fulfills", refs(f.fulfills));
  },
  "integration-event": function (id, r) {
    var f = r.fields || {};
    return metaRow(id, [badge(f.direction === "in" ? "Arrives from outside" : "Leaves the system", f.direction === "in" ? "info" : "warn")]) + lead("About " + (f.subject || "")) +
      blk("id", "eventType", kv([["Event type", '<span class="code">' + esc(f.eventType) + "</span>"], ["Translates", f.translates ? inlineRef(f.translates) : ""], ["Trust", f.trust ? esc(f.trust) : ""], ["Correlation", f.correlation ? esc(f.correlation) : ""], ["Dataschema", f.dataschema ? '<a href="' + esc(f.dataschema) + '">' + esc(f.dataschema) + "</a>" : ""]])) +
      blk("pay", "payload", table(["Field", "Type", "Meaning", "Class"], (f.payload || []).map(function (p) { return [td(esc(p.name), "nm"), td(typedFieldHtml(p)), td(esc(p.meaning)), td(clsBadge(p.classification))]; })), (f.payload || []).length) +
      blk("pub", "Points at this: effects.emits", refs(ins(id, "emits"))) + blk("trig", "Points at this: triggeredBy.event", refs(ins(id, "triggeredBy.event")));
  },
  "external-system": function (id, r) {
    var f = r.fields || {};
    var ops = (f.operations || []).length ? table(["Operation", "Timeout", "When it times out"], f.operations.map(function (o) { return [td(esc(o.name.replace(/-/g, " ")) + '<div style="margin-top:4px">' + badge(o.direction === "in" ? "Inbound" : "Outbound", o.direction === "in" ? "info" : "warn") + "</div>" + (o.response ? '<div class="tn" style="font-family:var(--sans);margin-top:4px;white-space:normal">' + esc(o.response) + "</div>" : "")), td(duration(o.timeout)), td(esc(o.onTimeout))]; })) : "";
    return metaRow(id) + leadHtml(secHtml(r, "Summary")) + blk("as", "actor", refs([f.actor])) + blk("ops", "operations", ops, (f.operations || []).length) + (f.erasure ? blk("er", "erasure", callout("quiet", "lock", erasureHtml(f.erasure))) : "") + blk("by", "Points at this: effects.calls", refs(ins(id, "calls")));
  },
  role: function (id, r) { var f = r.fields || {}, al = f.allows || []; return metaRow(id, [badge("Scope: " + cap(f.scope), "info")]) + leadHtml(secHtml(r, "Summary")) + blk("allows", "allows", al.length ? table(["Permission", "Meaning"], al.map(function (p) { return [td(inlineRef(p)), td(esc(REC[base(p)] ? (REC[base(p)].fields || {}).meaning : ""))]; })) : "", al.length) + blk("held", "Points at this: holds", refs(ins(id, "holds"))); },
  permission: function (id, r) { var u = ins(id, "grant"); return metaRow(id) + lead((r.fields || {}).meaning) + blk("by", "Points at this: allows", refs(ins(id, "allows"))) + blk("need", "Points at this: access.grant", refs(u), u.length); },
  "entry-point": function (id, r) { var f = r.fields || {}, u = ins(id, "reachedThrough"); return metaRow(id, [badge(f.kind, "info")]) + (f.path ? blk("path", "path", '<span class="code" style="font-size:14px;padding:4px 10px">' + esc(f.path) + "</span>") : "") + blk("ent", "Points at this: reachedThrough", refs(u), u.length); },
  runbook: function (id, r) { var f = r.fields || {}; return metaRow(id) + (f.trigger ? callout("warn", "alert", "<b>Trigger.</b> " + esc(f.trigger)) : "") + blk("lim", "Points at this: runbook", refs(ins(id, "runbook"))); },
  "known-gap": function (id, r) { var f = r.fields || {}, n = days(f.endsBy), subj = f.subject || [], href = lsHref("docs/tools/model.md"); return metaRow(id, [machineChip(n < 0 ? "failing" : "awaiting", n < 0 ? "Expired " + plural(-n, "day") + " ago" : "Ends " + fmtDate(f.endsBy) + ", " + dateHint(f.endsBy))]) + "" + blk("check", "check", kv([["Check", href ? '<a class="code" href="' + esc(href) + '" title="Open the finding codes in LiteSpec">' + esc(f.check) + "</a>" : '<span class="code">' + esc(f.check) + "</span>"], ["Ends by", fmtDate(f.endsBy)]])) + blk("subj", "subject", refs(subj.slice(0, 30)) + (subj.length > 30 ? '<p class="cnote" style="margin-top:8px">and ' + (subj.length - 30) + " more</p>" : ""), subj.length); },
  deviation: function (id, r) { var f = r.fields || {}, n = days(f.reviewBy); return metaRow(id, [machineChip(n < 0 ? "failing" : "awaiting", "Review by " + fmtDate(f.reviewBy) + " (" + dateHint(f.reviewBy) + ")")]) + "" + blk("prov", "provision", provisionLink(f.provision)) + blk("rm", "Remove when", callout("quiet", "clock", esc(f.removeWhen))); },
  scenario: function (id, r) { var f = r.fields || {}, ri = resultInfoOf(id), pr = r.proof || {}, testRun = (MAN || {}).testRun || {}, t0 = pr.test || {}; return metaRow(id, [machineChip(pr.state)]) + blk("gwt", "given, when, then", '<div class="panel"><dl class="gwt"><dt>Given</dt><dd>' + esc(f.given) + "</dd><dt>When</dt><dd>" + esc(f.when) + "</dd><dt>Then</dt><dd>" + esc(f.then) + "</dd></dl></div>") + blk("exer", "exercises", refs(f.exercises), (f.exercises || []).length) + blk("res", "result", ri ? resultCards([ri]) : "") + blk("ex", "Examples", (f.examples || []).length ? table(Object.keys(f.examples[0]), f.examples.map(function (row) { return Object.keys(row).map(function (k) { return td(esc(row[k])); }); })) : "") + blk("proof", "proves", '<div class="tiles">' + tile("Proof", pmark(pr.state), t0.file || t0.path ? fileLink(t0.path || t0.file, t0.line, t0.name) : pr.tests ? plural(pr.tests, "test") + " claim this example" : "No test claims it") + tile("test run", testRun.date ? fmtDate(testRun.date) : "None recorded", testRun.date ? dateHint(testRun.date) : "the model holds no test run") + "</div>"); },
  term: function (id, r) {
    var f = r.fields || {}, LANG = { nl: "Dutch", de: "German", fr: "French" };
    var labels = f.labels ? table(["Language", "Word"], [[td("English", "mut"), td("<b>" + esc(f.term) + "</b>")]].concat(Object.keys(f.labels).map(function (k) { return [td(esc(LANG[k] || k), "mut"), td(esc(f.labels[k]))]; }))) + '<p class="cnote" style="margin-top:8px">Translations sit outside the approval hash, so adding a language reopens no approval.</p>' : "";
    return metaRow(id) + lead(f.meaning) + blk("lab", "labels", labels) + blk("vals", "values", (f.values || []).length ? table(["Code", "Label"], f.values.map(function (v) { return [td(esc(v.code), "nm"), td(esc(v.label))]; })) : "", (f.values || []).length) +
      blk("rej", "rejected", (f.rejected || []).length ? '<div class="rej">' + f.rejected.map(function (x) { return '<div class="rej-i"><s>' + esc(x.word) + "</s><div><p>" + esc(x.reason) + "</p></div></div>"; }).join("") + "</div>" : "", (f.rejected || []).length) + blk("rel", "broader and narrower", kv([["Broader", refs(f.broader)], ["Narrower", refs(f.narrower)]])) + blk("used", "Points at this: terms", refs(ins(id, "terms").slice(0, 30)), ins(id, "terms").length);
  }
};
function serverLine(b) {
  var r = REC[base(b)], info = null;
  if (r && r.machine) { var sc = scenariosOf(base(b)).filter(function (x) { return REC[base(x)] && REC[base(x)].proof; }); info = { built: r.machine.implemented, scenarios: sc.length, proven: sc.filter(function (x) { return REC[base(x)].proof.state === "awaiting" || REC[base(x)].proof.state === "passing"; }).length }; }
  var m = info ? (info.built ? badge("implemented", "good") : badge("not implemented", "bad")) + (info.scenarios ? badge(info.proven + " of " + info.scenarios + " scenarios confirmed", info.proven === info.scenarios ? "good" : "warn") : badge("no scenarios", "bad")) : "";
  return '<div class="srvl">' + ref(b) + (m ? '<span class="srvm">' + m + "</span>" : "") + "</div>";
}
function whereBlock(id, r) {
  var sc = screensOf(id);
  if (!sc.length) return browserReached(r) ? blk("where", "Points at this: offers", callout("bad", "screen", "<b>No screen offers this action.</b> It is reached through " + ((r.fields || {}).reachedThrough || []).map(inlineRef).join(", ") + ", but no page lets a person start it.")) : "";
  return blk("where", "Points at this: offers", sc.map(function (x) { var q = REC[base(x)]; return '<div class="scrline">' + ref(x) + (q ? '<span class="code">' + esc((q.fields || {}).address || "") + "</span>" + (q.route ? badge("implemented", "good") : badge("not implemented", "bad")) + (((q.calls || []).indexOf(id) >= 0) ? badge("calls", "good") : q.route ? badge("no calls link", "warn") : "") : "") + "</div>"; }).join(""), sc.length);
}
function builtBlock(r) {
  var impl = r.impl || [];
  var endpoint = impl.filter(function (x) { return x.kind === "endpoint"; })[0], handler = impl.filter(function (x) { return x.kind === "handler"; })[0];
  if (!handler && !endpoint) return r.machine && !r.machine.implemented ? blk("built", "implements", callout("bad", "alert", "No code carries this action's mark yet.")) : "";
  var rows = [];
  if (endpoint) rows.push([td("endpoint"), td(((endpoint.route || "").trim() ? '<span class="code">' + esc(endpoint.route) + "</span><br>" : "") + '<span class="tn">' + esc(endpoint.path) + ":" + endpoint.line + "</span>")]);
  if (handler) rows.push([td("handler"), td('<span class="code">' + esc(handler.name) + '</span><br><span class="tn">' + esc(handler.path) + ":" + handler.line + "</span>")]);
  var more = impl.filter(function (c) { return c.kind !== "handler" && c.kind !== "endpoint"; });
  return blk("built", "implements", table(["implements", "source"], rows) + (more.length ? '<p class="cnote" style="margin-top:8px">and ' + plural(more.length, "more part") + " marked with this action: " + uniq(more.map(function (c) { return c.kind.replace(/-/g, " "); })).join(", ") + "</p>" : ""), null, "found by the marks in the code");
}
function listOf(id) { var k = kidsOf[id] || []; return blk("in", "Contains", refs(k.slice(0, 150)) + (k.length > 150 ? '<p class="cnote">and ' + (k.length - 150) + " more in the outline</p>" : ""), k.length); }
// A layer page: what the layer answers, who writes and reviews it, and its record types with the question each answers.
DETAIL.layer = function (id, r) {
  var f = r.fields || {}, key = id.slice(6), ids = order.filter(function (x) { return layerOf[x] === key && isReal(x); }), types = uniq(ids.map(typeOf));
  var rows = types.map(function (t) { var first = ids.filter(function (x) { return typeOf(x) === t; }); return [td('<button type="button" class="iref" data-go="' + esc(first[0]) + '">' + ic(t, "icon-sm") + " " + esc(PLURAL[t] || t) + "</button>"), td(String(first.length), "num"), td(esc((((LEX.types || {})[t]) || {}).question || ""))]; });
  return lead(f.question) + (f.writes ? kv([["Written by", esc(f.writes)], ["Reviewed by", esc(f.reviews)]]) : "") + blk("types", "Record types", table(["Type", "Records", "Answers"], rows), types.length);
};
DETAIL["architecture-layer"] = function (id, r) { return lead(GL["architecture layer"]) + listOf(id); };
DETAIL.folder = listOf; DETAIL.frontend = listOf; DETAIL["test-project"] = listOf; DETAIL["test-class"] = listOf;
DETAIL["check-tier"] = function (id, r) { return lead(SX.checkLevels) + listOf(id); };
DETAIL["finding-code"] = function (id, r) { var f = r.fields || {}; return metaRow(id, [xp(f.tier, badge("Tier " + f.tier, f.tier === "G" ? "bad" : f.tier === "S" ? "warn" : ""))]) + lead(f.meaning) + listOf(id); };
DETAIL.finding = function (id, r) {
  var f = r.fields || {}, subj = f.subject ? base(f.subject) : null;
  return metaRow(id, [xp(f.tier, badge("Tier " + f.tier, f.tier === "G" ? "bad" : f.tier === "S" ? "warn" : "")), f.known ? xp("known", badge("known", "info")) : "", f.outcome ? badge(f.outcome) : "", f.reason ? badge(f.reason) : ""]) + lead(f.message) +
    blk("about", "Finding", kv([["code", inlineRef("finding-code/" + f.code)], ["subject", subj ? inlineRef(subj) : '<span class="tn">none</span>'], ["path", r.src ? srcLink(r.src) : ""]]));
};
DETAIL.code = function (id, r) { var f = r.fields || {}; return metaRow(id, (f.kinds || []).map(function (k) { return badge(k); })) + blk("impl", "implements", refs(f.implements), (f.implements || []).length); };
DETAIL.route = function (id, r) { var f = r.fields || {}; return metaRow(id, [badge(f.frontend, "info")]) + blk("scr", "implements", refs([f.screen])) + blk("calls", "calls", refs(f.calls), (f.calls || []).length); };
DETAIL.test = function (id, r) { var f = r.fields || {}; return metaRow(id, [f.browser ? badge("browser test", "info") : "", badge(f.confirmed ? "confirmed" : "not confirmed", f.confirmed ? "good" : "warn"), xp("norun", badge(f.passing === null || f.passing === undefined ? "passing: not run" : f.passing ? "passing" : "failing", f.passing ? "good" : f.passing === false ? "bad" : ""))]) + blk("proves", "proves", refs(f.claims), (f.claims || []).length); };
// A screen: where it lives, and each use case it offers with whether its page calls it.
DETAIL.screen = function (id, r) {
  var f = r.fields || {}, off = (f.offers || []).map(base), calls = r.calls || [];
  var offT = off.length ? table(["Use case", "Called by the page", "Implemented", "Scenarios confirmed"], off.map(function (u) { var q = REC[base(u)], b = q && q.machine ? { built: q.machine.implemented, scenarios: scenariosOf(u).length, proven: scenariosOf(u).filter(function (x) { return REC[base(x)] && REC[base(x)].proof && REC[base(x)].proof.state === "awaiting"; }).length } : null; return [td(inlineRef(u)), td(!r.route ? '<span class="tn">no page</span>' : calls.indexOf(u) >= 0 ? "Yes" : '<span class="warnt">No</span>'), td(b ? (b.built ? "Yes" : '<span class="warnt">No</span>') : ""), td(b ? (b.scenarios ? b.proven + " of " + b.scenarios : '<span class="warnt">None</span>') : "")]; })) : "";
  return metaRow(id) + lead(f.summary || f.meaning || "") +
    blk("where", "Where it is", kv([["Address", '<span class="code">' + esc(f.address || f.host || "") + "</span>"], ["Entry point", f.entryPoint ? inlineRef(f.entryPoint) : ""], ["Floorplan", esc(cap(String(f.floorplan || "").replace(/-/g, " ")))], ["Page in the code", r.route ? srcLink({ path: r.route.file || r.route.path, line: 1 }) : '<span class="warnt">No page carries this screen yet</span>']])) +
    blk("offers", "offers", offT, off.length);
};
// A result: what it means, its code, and the scenarios that show it.
DETAIL.result = function (id, r) {
  var f = r.fields || {}, sc = shownByOf(id).filter(function (x) { return REC[base(x)]; }), generic = /.(forbidden|invalid-input)$/.test(id);
  return metaRow(id, [badge("outcome: " + f.outcome)]) + lead(f.meaning) + (f.code ? blk("code", "Refusal code", '<span class="code" style="font-size:13px;padding:4px 10px">' + esc(f.code) + "</span>") : "") +
    blk("ex", "Scenarios", sc.length ? '<div class="scs">' + sc.map(function (x) { return scenarioCard(x); }).join("") + "</div>" : '<p class="cnote">' + (generic ? esc(GL["generic refusal"] || "") : "No scenario shows this result.") + "</p>", sc.length);
};
function detailHtml(id) {
  var r = REC[base(id)];
  if (!r) return '<nav class="crumb" aria-label="Location"><b>' + esc(base(id)) + "</b></nav><h1>" + esc(base(id)) + '</h1><p class="relnote">No record with this identifier is in the book.</p>';
  curSrc = r.src || null;
  if (r.type === "layer") return header(id) + DETAIL.layer(id, r);
  var body = DETAIL[r.type] ? DETAIL[r.type](id, r) : metaRow(id), w = whyHtml(r);
  // the Why block goes right after the first lead paragraph, or right after the facts when there is none
  if (w) { var at = body.indexOf('<p class="lead">'), atMd = body.indexOf('<div class="lead prose">'), i = at >= 0 ? body.indexOf("</p>", at) + 4 : atMd >= 0 ? body.indexOf("</div>", atMd) + 6 : body.indexOf("</dl>") + 5; body = i > 4 ? body.slice(0, i) + w + body.slice(i) : w + body; }
  return header(id) + body + questionsBlock(id) + bodyBlocks(r);
}
var curSrc = null;

function introClosed() { try { return localStorage.getItem("book-intro") === "closed"; } catch (e) { return false; } }
// ---------- verdict bar ----------
function vbarHtml() {
  if (!selected || !REC[base(selected)] || !isReal(selected)) return "";
  var id = selected, h = human(id), beh = isBeh(id), next = nextNeeding(id), qn = openQs(id).length;
  var bo = batchOf(id), bt = bo ? bo.batch : null, approveLabel = beh ? "Review" : "Approve", mine = beh ? "mineR" : "mine";
  if (!needs(id) && h !== mine) return '<div class="vbar-in"><span class="settled">' + ic("approved", "icon-sm") + esc(cap(HLABEL[h] || h)) + (REC[base(id)].fields.approvedBy ? " by " + esc(REC[base(id)].fields.approvedBy) + (REC[base(id)].fields.approvedOn ? " on " + fmtDate(REC[base(id)].fields.approvedOn) : "") : "") + '</span><span class="grow"></span><button type="button" class="btn ol" data-v="changes">Request changes</button>' + (next ? '<button type="button" class="btn gh" data-next title="' + esc(title(next)) + '">Next draft' + ic("arrow", "icon-sm") + "</button>" : "") + "</div>";
  var block = qn ? "Answer " + plural(qn, "open question") + " first" : "";
  var s = '<div class="vbar-in"><button type="button" class="btn" data-v="' + mine + '"' + (h === mine || qn ? " disabled" : "") + (block ? ' title="' + block + '"' : "") + ">" + ic("approved", "icon-sm") + approveLabel + "</button>" +
    '<button type="button" class="btn ol" data-v="changes">Request changes</button><button type="button" class="btn ol" data-v="question">Ask a question</button>' +
    '<span class="hint">' + (block ? esc(block) : h === mine ? esc(cap(HLABEL[mine])) : bt && bt.n > 1 ? "Approves the whole batch: " + plural(bt.n, "record") + (tierOf(id) === 1 ? ", with a second reviewer" : "") : tierOf(id) === 1 ? "Tier 1: needs a second reviewer" : "") + '</span><span class="grow"></span>' +
    '<button type="button" class="btn gh" data-next' + (next ? ' title="' + esc(title(next)) + '"' : " disabled") + '><span class="nx">Next draft</span>' + ic("arrow", "icon-sm") + "</button>";
  if (noteFor === id) s += '<div class="note-in"><textarea id="note" aria-label="' + (noteKind === "question" ? "Your question" : "What should change") + '" placeholder="' + (noteKind === "question" ? "Your question" : noteKind === "answer" ? "Your answer" : "What should change") + '">' + esc(notes[id] || "") + '</textarea><div style="display:flex;flex-direction:column;gap:6px"><button type="button" class="btn" data-note="save">Save</button><button type="button" class="btn gh" data-note="cancel">Cancel</button></div></div>';
  return s + "</div>";
}
function nextNeeding(from) {
  var list = view === "list" ? order.filter(function (x) { return passes(x); }).sort(SORTS[sortKey].cmp) : order.filter(function (x) { return !anyFilter() || passes(x); });
  var i = list.indexOf(from); for (var k = 1; k <= list.length; k++) { var id = list[(i + k) % list.length]; if (needs(id) && typeOf(id) !== "scenario") return id; } return null;
}

// ---------- link sentences ----------
// Each link field read in both directions, in the Links table phrases.
function linkSentence(id, label, items) {
  var inbound = label.indexOf("Points at this: ") === 0, field = inbound ? label.slice(16) : label, me = typeName(typeOf(id)).toLowerCase();
  if (!inbound) return "This " + me + " " + phraseFor(field, typeOf(id), "forward");
  var carrier = items.length ? typeOf(items[0].id) : null;
  return "This " + me + " " + phraseFor(field, carrier, "inverse");
}

// ---------- from goal to code ----------
// Down the authority order: what each record type leads to in the layer below, with the words for that step.
function downOf(id) {
  var r = REC[base(id)], t = typeOf(id), out = [];
  function add(list, phrase) { (list || []).forEach(function (x) { x = base(x); if ((REC[x] || NODE[x]) && x !== base(id)) out.push({ id: x, phrase: phrase }); }); }
  if (t === "product") add(order.filter(function (x) { return typeOf(x) === "goal"; }), "a goal of the product");
  if (t === "goal") add(ins(id, "advances").filter(function (x) { return typeOf(x) === "feature"; }), "serves this goal");
  if (t === "problem") add(ins(id, "solves"), "solves this problem");
  if (t === "feature") add(ins(id, "cites"), "cites this feature");
  if (t === "requirement") add(ins(id, "fulfills"), "fulfills this requirement");
  if (t === "journey") { add(ins(id, "serves"), "serves a step"); }
  if (t === "use-case" || t === "automation") { add(ins(id, "offers"), "offers it"); add(resultsOf(id), "how it ends"); }
  if (t === "result") add(shownByOf(id), "shows this result");
  if (t === "scenario" || t === "rule") add(r ? r.provedBy : null, "proves a scenario of it");
  if (t === "screen") add(r ? r.implementedBy : null, "implements it");
  if (t === "aggregate") add(ins(id, "changes"), "changes it");
  return out;
}
var GAP = { goal: "No feature serves this goal", feature: "No requirement cites this feature" };
var BAND = { intent: "Intent", behavior: "Behavior", entry: "", governance: "Governance" };
function bandOf(id) { var t = typeOf(id); if (t === "code" || t === "route") return "Implementation"; if (t === "test") return "Proof"; return BAND[kindOf(id)] || "Behavior"; }
function traceHtml(id) {
  var up = traceOf(id).filter(function (x) { return x !== id; }), seen = {}, rows = [], gaps = [];
  // Above a use case, results and scenarios are walked through but not listed, so the trace reaches code and tests without the detail.
  var quiet = ["use-case", "automation", "result", "scenario", "rule"].indexOf(typeOf(id)) < 0;
  up.forEach(function (x) { seen[x] = 1; rows.push({ id: x, depth: 0, phrase: "" }); });
  seen[id] = 1; rows.push({ id: id, depth: 0, phrase: "", here: true });
  (function walk(x, depth) {
    if (depth > 5) return;
    var kids = downOf(x);
    if (typeOf(x) === "requirement" && REC[base(x)]) { var kb = REC[base(x)].keptBy || {}, parts = Object.keys(kb).map(function (t) { return plural(kb[t], (TYPE[t] || t).toLowerCase()); }); rows.push(parts.length ? { note: title(x) + ": kept by " + parts.join(", "), depth: depth + 1, band: "Behavior" } : { gapnote: "Nothing keeps this requirement", depth: depth + 1, band: "Intent" }); }
    if (!kids.length && GAP[typeOf(x)]) rows.push({ gapnote: GAP[typeOf(x)], depth: depth + 1, band: bandOf(x) });
    var by = {}; kids.forEach(function (k) { if (seen[k.id]) return; (by[typeOf(k.id)] = by[typeOf(k.id)] || []).push(k); });
    Object.keys(by).forEach(function (t) { var hide = quiet && (t === "result" || t === "scenario"); by[t].forEach(function (k, i) { seen[k.id] = 1; if (hide) { walk(k.id, depth); return; } if (i < 3) { rows.push({ id: k.id, depth: depth + 1, phrase: k.phrase }); walk(k.id, depth + 1); } else if (i === 3) rows.push({ more: by[t].length - 3, depth: depth + 1, t: t }); }); });
  })(id, 0);
  var bands = ["Intent", "Behavior"], s = '<section class="trace2"><h3>' + ic("journey", "icon-sm") + "From goal to behavior</h3>";
  bands.forEach(function (b) {
    var here = rows.filter(function (x) { if (x.note || x.gapnote) return x.band === b; return x.id ? bandOf(x.id) === b : false; });
    var g = gaps.filter(function (x) { return bandOf(x.after) === b || (b === "Implementation" && x.text.indexOf("code") > 0) || (b === "Proof" && x.text.indexOf("test") > 0); });
    if (!here.length && !g.length) return;
    var lastPh = "";
    s += '<div class="band ' + b.toLowerCase() + '"><span class="band-h" data-explain="g:layer">' + b + "</span>" + here.map(function (x) {
      var showPh = x.phrase && (x.phrase + x.depth) !== lastPh; lastPh = x.id ? (x.phrase || "") + x.depth : lastPh;
      if (x.note) return '<div class="tr2 note" style="padding-left:' + (Math.min(x.depth, 3) * 10) + 'px">' + ic("use-case", "icon-sm") + "<span>" + esc(x.note) + "</span></div>";
      if (x.gapnote) return '<div class="tr2 gap" style="padding-left:' + (Math.min(x.depth, 3) * 10) + 'px">' + ic("missing", "icon-sm") + "<span>" + esc(x.gapnote) + "</span></div>";
      if (x.more) return '<div class="tr2 more" style="padding-left:' + (Math.min(x.depth, 3) * 10) + 'px">and ' + x.more + " more " + esc((x.more === 1 ? TYPE[x.t] : PLURAL[x.t] || x.t).toLowerCase()) + "</div>";
      return '<div class="tr2' + (x.here ? " here" : "") + '" style="padding-left:' + (Math.min(x.depth, 3) * 10) + 'px">' + (showPh ? '<span class="ph">' + esc(x.phrase) + "</span>" : "") + '<span class="row"><button type="button" class="ci" data-go="' + esc(x.id) + '" data-hl="' + esc(x.id) + '">' + ic(typeOf(x.id)) + '<span class="tt">' + esc(title(x.id)) + '</span></button><span class="ttag" data-explain="type:' + esc(typeOf(x.id)) + '">' + esc(tag(typeOf(x.id))) + "</span>" + (REC[base(x.id)] ? statusIcon(x.id) : "") + "</span></div>";
    }).join("") + g.map(function (x) { return '<div class="tr2 gap" style="padding-left:' + (Math.min(x.depth, 3) * 10 + 10) + 'px">' + ic("missing", "icon-sm") + "<span>" + esc(x.text) + " (" + esc(title(x.after)) + ")</span></div>"; }).join("") + "</div>";
  });
  return s + "</section>";
}

// ---------- the Intent tree: traverse from the product down, one level per click ----------
var tvOpen = {};
function productId() { return (MAN.project && MAN.project.id) || order.filter(function (x) { return typeOf(x) === "product"; })[0]; }
function tvKids(id) {
  var t = id.indexOf("tv/") === 0 ? id : typeOf(id);
  if (t === "product") return order.filter(function (x) { return typeOf(x) === "goal"; }).concat(["tv/no-goal"]);
  if (t === "tv/no-goal") return Object.keys(REC).filter(function (x) { return REC[x].type === "feature" && !(((REC[x].fields || {}).advances || []).length); });
  if (t === "goal") return ins(id, "advances").filter(function (x) { return typeOf(x) === "feature"; });
  if (t === "feature") return ins(id, "cites");
  if (t === "requirement") return ins(id, "cites").filter(function (x) { return typeOf(x) === "journey"; });
  return [];
}
function tvMeta(id, kids) {
  if (id === "tv/no-goal") return plural(kids.length, "feature");
  var r = REC[base(id)], t = typeOf(id);
  if (t === "requirement") { var kb = (r || {}).keptBy || {}, parts = Object.keys(kb).map(function (k) { return plural(kb[k], (TYPE[k] || k).toLowerCase()); }); return r ? (parts.length ? "Kept by " + parts.join(", ") : '<span class="gapw">Nothing keeps it</span>') : ""; }
  if (t === "goal") return kids.length ? plural(kids.length, "feature") + (kids.length === 1 ? " serves it" : " serve it") : '<span class="gapw">No feature serves it</span>';
  if (t === "feature") return kids.length ? plural(kids.length, "requirement") : '<span class="gapw">No requirement cites it</span>';
  if (t === "product") return plural(Math.max(0, kids.length - 1), "goal");
  return "";
}
function tvNode(id, path) {
  var group = id.indexOf("tv/") === 0, r = REC[base(id)], kids = path.indexOf(id) >= 0 ? [] : tvKids(id), open = !!tvOpen[id] && kids.length;
  var gap = !group && r && ((r.type === "goal" || r.type === "feature") && !kids.length || (r.type === "requirement" && !Object.keys(r.keptBy || {}).length));
  var s = '<li><div class="tvn' + (open ? " open" : "") + (gap ? " gap" : "") + '" role="button" tabindex="0" data-tv="' + esc(id) + '" aria-expanded="' + !!open + '">' +
    ic(group ? "folder" : typeOf(id), "icon-sm") + '<span class="tvt">' + esc(group ? "Features that serve no goal" : title(id)) + "</span>" + (group ? "<span></span>" : '<a class="tvo" data-go="' + esc(id) + '" data-wide="0" title="Open the page" aria-label="Open the page">' + ic("ext", "icon-sm") + "</a>") +
    '<span class="tvk">' + esc(group ? "group" : tag(typeOf(id))) + "</span>" + '<span class="tvm">' + tvMeta(id, kids) + "</span></div>";
  if (open) s += "<ul>" + kids.slice(0, 40).map(function (k) { return tvNode(k, path.concat([id])); }).join("") + (kids.length > 40 ? '<li><span class="tvmore">and ' + (kids.length - 40) + " more</span></li>" : "") + "</ul>";
  return s + "</li>";
}
function intentTreeHtml() {
  var g = layerByKey.intent || {};
  return '<div class="itree"><div class="itree-h"><div><h1>Intent, top down</h1>' + (g.question ? '<p class="lead" style="margin-bottom:6px">' + esc(g.question) + "</p>" : "") + '<div class="tvlegend"><span>Click a box to open the level below it.</span><span>A dashed red box is where the chain breaks.</span><span>' + ic("ext", "icon-sm") + " opens the page.</span></div></div>" + '<div class="itree-a"><button type="button" class="btn ol" data-tvall="1">Open two levels</button><button type="button" class="btn ol" data-tvall="0">Collapse</button><button type="button" class="btn" data-wide="0">Close</button></div></div><div class="tv"><ul>' + tvNode(productId(), []) + "</ul></div></div>";
}

// ---------- implementation and proof: links to the mark and the test, or a plain statement that none was found ----------
var KIND_WORD = { handler: "Handler", endpoint: "Endpoint", "aggregate-root": "Aggregate root", "aggregate-method": "Aggregate method", automation: "Automation", port: "Port", adapter: "Adapter", event: "Event class", "rule-exception": "Refusal exception", result: "Result code", "domain-service": "Domain service", route: "Route" };
function fileLink(path, line, label) {
  var href = editorHref(consumerRoot(), path, line || null);
  var body = '<span class="fn">' + esc(label || String(path).split("/").pop()) + "</span>" + '<span class="fp">' + esc(String(path).split("/").slice(0, -1).join("/")).replace(/\//g, "/<wbr>") + (line ? ":" + line : "") + "</span>";
  return href ? '<a class="flink" href="' + esc(href) + '" title="Open ' + esc(path) + (line ? " at line " + line : "") + '">' + body + "</a>" : '<span class="flink">' + body + "</span>";
}
function implHtml(id) {
  var r = REC[base(id)]; if (!r || !r.expect || (!r.expect.impl && !(r.impl || []).length)) return "";
  var list = r.impl || [];
  var s = '<section class="ip"><h3>' + ic("code", "icon-sm") + '<span data-explain="implemented">Implementation</span><span class="n">' + list.length + "</span></h3>";
  if (!list.length) return s + '<p class="ipnone">' + ic("missing", "icon-sm") + "No code carries the mark of this " + esc(typeName(r.type).toLowerCase()) + ".</p></section>";
  return s + list.map(function (x) { return '<div class="ipi"><span class="ipk">' + esc(KIND_WORD[x.kind] || cap(x.kind)) + "</span>" + fileLink(x.path, x.line) + "</div>"; }).join("") + "</section>";
}
function proofHtml(id) {
  var r = REC[base(id)]; if (!r || !r.expect || !r.expect.proof) return "";
  var hasRun = !!(((MAN || {}).testRun || {}).date);
  var s = '<section class="ip"><h3>' + ic("flask", "icon-sm") + '<span data-explain="awaiting">Proof</span>';
  if (r.type === "use-case" || r.type === "automation") {
    var sc = scenariosOf(id).filter(function (x) { return REC[base(x)]; }), none = sc.filter(function (x) { return !((REC[base(x)] || {}).tests || []).length; });
    s += '<span class="n">' + (sc.length - none.length) + " of " + sc.length + "</span></h3>";
    if (!sc.length) return s + '<p class="ipnone">' + ic("missing", "icon-sm") + "This use case has no scenarios, so nothing can prove it.</p></section>";
    s += '<p class="ipsum">' + plural(sc.length - none.length, "scenario") + " proved by a test" + (none.length ? ", " + plural(none.length, "scenario") + " with no test" : "") + "." + (hasRun ? "" : " No test run is in the model.") + "</p>";
    none.forEach(function (x) { s += '<p class="ipnone">' + ic("missing", "icon-sm") + '<span>No test proves <button type="button" class="iref" data-go="' + esc(x) + '">' + esc(title(x)) + "</button></span></p>"; });
    sc.filter(function (x) { return ((REC[base(x)] || {}).tests || []).length; }).slice(0, 6).forEach(function (x) { var t = REC[base(x)].tests[0]; s += '<div class="ipi"><span class="ipk">' + esc(title(x)) + "</span>" + fileLink(t.path || t.file, t.line, t.name) + "</div>"; });
    return s + "</section>";
  }
  var ts = r.tests || [];
  s += '<span class="n">' + ts.length + "</span></h3>";
  if (!ts.length) return s + '<p class="ipnone">' + ic("missing", "icon-sm") + "No test proves this " + esc(typeName(r.type).toLowerCase()) + ".</p></section>";
  return s + ts.map(function (t) { return '<div class="ipi"><span class="ipk">' + (t.browser ? "Browser test" : "Test") + (t.confirmed ? "" : ", not confirmed") + "</span>" + fileLink(t.path || t.file, t.line, t.name) + "</div>"; }).join("") + (hasRun ? "" : '<p class="ipsum">No test run is in the model.</p>') + "</section>";
}
function lacks(id) { var r = REC[base(id)]; if (!r) return ""; if (typeof r.lacks === "string") return r.lacks; if (!r.expect) return ""; var miss = []; if (r.expect.impl && !(r.impl || []).length) miss.push("no code"); if (r.expect.proof) { if (r.type === "use-case" || r.type === "automation") { var sc = scenariosOf(id).filter(function (x) { return REC[base(x)]; }); if (!sc.length || sc.some(function (x) { return !((REC[base(x)] || {}).tests || []).length; })) miss.push("scenarios without a test"); } else if (!(r.tests || []).length) miss.push("no test"); } return miss.join(", "); }

// ---------- findings on the record ----------
function browserReached(r) { return ((r.fields || {}).reachedThrough || []).some(function (x) { return (kidsOf[base(x)] || []).some(function (k) { return typeOf(k) === "screen"; }); }); }
function findingsRail(id) {
  var f = ((REC[base(id)] || {}).findings || []).filter(function (x) { return !x.known; }); if (!f.length) return "";
  var byCode = {}; f.forEach(function (x) { (byCode[x.code] = byCode[x.code] || []).push(x); });
  return '<section class="missing"><h3>' + ic("alert", "icon-sm") + xp("finding", "Findings") + '<span class="n">' + f.length + "</span></h3>" + Object.keys(byCode).map(function (c) { var x = byCode[c][0]; return '<div class="mi"><button type="button" class="iref" data-go="finding-code/' + esc(c) + '"><span class="code">' + esc(c) + "</span></button> " + xp(x.tier, badge("Tier " + x.tier, x.tier === "G" ? "bad" : x.tier === "S" ? "warn" : "")) + (byCode[c].length > 1 ? ' <span class="tn">' + byCode[c].length + "</span>" : "") + "</div>"; }).join("") + "</section>";
}

// ---------- connections ----------
function connHtml(id) {
  if (!REC[base(id)]) return "";
  var rows = connectionsOf(id), tr = traceOf(id);
  var tr2 = traceHtml(id), showTrace = tr2.split('class="tr2').length > 2; traceShown = {}; if (showTrace) (tr2.match(/data-go="([^"]+)"/g) || []).forEach(function (m) { traceShown[m.slice(9, -1)] = 1; }); rows = rows.filter(function (x) { return !traceShown[x.id]; }); var s = (showTrace ? tr2 : "") + implHtml(id) + proofHtml(id) + '<h2>Connections <span class="n">' + rows.length + "</span></h2>";
  if (!rows.length) return isReal(id) ? s + '<p class="cnote">No other record links to this one.</p>' : "";
  var layerKeyOf = function (x) { return layerOf[x] || "behavior"; };
  var groups = (MAN.layers || []).map(function (l) { return { key: l.key, label: (layerByKey[l.key] || {}).label || l.key, rows: rows.filter(function (x) { return layerKeyOf(x.id) === l.key; }) }; });
  var stray = rows.filter(function (x) { return !layerOf[x.id]; });
  if (stray.length) groups.push({ key: "behavior", label: (layerByKey.behavior || {}).label || "Behavior", rows: [] });
  groups.forEach(function (g) {
    if (g.key === "behavior") g.rows = g.rows.concat(stray.filter(function (x) { return true; }));
    if (!g.rows.length) return;
    s += '<section class="cg"><h3 class="cg-h">' + ic(LAYER_ICON[g.key] || "globe") + esc(g.label) + '<span class="n">' + g.rows.length + "</span></h3>";
    uniq(g.rows.map(function (x) { return x.label; })).forEach(function (l) {
      var items = g.rows.filter(function (x) { return x.label === l; });
      var parts = [items];
      if (l.indexOf("Points at this: ") === 0 && linkVaries(l.slice(16))) {
        var byType = {};
        items.forEach(function (x) { var t = typeOf(x.id); (byType[t] = byType[t] || []).push(x); });
        parts = Object.keys(byType).map(function (t) { return byType[t]; });
      }
      parts.forEach(function (part) {
        var key = id + "|" + g.key + "|" + l + "|" + typeOf(part[0].id), lim = connAll[key] ? part.length : 8;
        s += '<span class="cl">' + esc(linkSentence(id, l, part)) + (part.length > 1 ? " &middot; " + part.length : "") + "</span>";
        part.slice(0, lim).forEach(function (x) {
          var t = typeOf(x.id);
          if (!REC[base(x.id)] && !NODE[base(x.id)]) { s += '<div class="ci out">' + ic(t) + '<span class="tt">' + esc(x.id) + '</span><span class="ttag">' + esc(tag(t)) + "</span></div>"; return; }
          s += '<button type="button" class="ci" data-go="' + esc(x.id) + '" data-hl="' + esc(x.id) + '">' + ic(t) + '<span class="tt">' + esc(title(x.id)) + (x.via ? '<span class="via">' + esc(x.via) + "</span>" : "") + '</span><span class="ttag" data-explain="type:' + esc(t) + '">' + esc(tag(t)) + "</span>" + (REC[base(x.id)] ? statusIcon(x.id) : "") + "</button>";
        });
        if (part.length > lim) s += '<button type="button" class="cmore" data-cmore="' + esc(key) + '">Show ' + (part.length - lim) + " more</button>";
      });
    });
    s += "</section>";
  });
  return s;
}

// ---------- render ----------
var navEl = document.getElementById("nav"), detailEl = document.getElementById("detail"), connEl = document.getElementById("conn"), vbarEl = document.getElementById("vbar");
function cssId(id) { return window.CSS && CSS.escape ? CSS.escape(id) : id; }
function headerWidgets() {
  var t = (MAN || {}).testRun || {};
  document.getElementById("run").innerHTML = '<button type="button" class="xbtn" data-explain="norun">' + ic("flask", "icon-sm") + (t.date ? "Test run " + fmtDate(t.date) + ": <b>" + (t.passed || 0).toLocaleString("en-GB") + "</b> passed" + (t.failed ? ', <b class="bad">' + t.failed + "</b> failed" : "") : "No test run in the model") + "</button>";
  var real = order.filter(isReal), a = real.filter(function (id) { return !needs(id); }).length;
  document.getElementById("prog").innerHTML = '<button type="button" class="xbtn" data-explain="settled"><span><b>' + a.toLocaleString("en-GB") + "</b> of " + real.length.toLocaleString("en-GB") + ' settled</span><span class="bar"><i style="width:' + Math.max(1, Math.round(a / Math.max(1, real.length) * 100)) + '%"></i></span></button>';
}
function renderNav() {
  if (anyFilter() && !allOutlinesLoaded() && !outlineLoading) {
    outlineLoading = true;
    document.getElementById("vtabs").innerHTML = vtabsHtml();
    document.getElementById("tools").innerHTML = toolsHtml();
    document.getElementById("facetPanel").innerHTML = facetPanelHtml();
    document.getElementById("chips").innerHTML = chipsHtml();
    document.getElementById("shown").innerHTML = shownHtml();
    navEl.innerHTML = '<p class="relnote">Loading the outline.</p>';
    ensureAllOutlines().then(function () { outlineLoading = false; renderNav(); });
    return;
  }
  document.getElementById("vtabs").innerHTML = vtabsHtml();
  document.getElementById("tools").innerHTML = toolsHtml();
  document.getElementById("facetPanel").innerHTML = facetPanelHtml();
  document.getElementById("chips").innerHTML = chipsHtml();
  document.getElementById("shown").innerHTML = shownHtml();
  navEl.innerHTML = navHtml();
}
function render(opts) {
  opts = opts || {};
  related = {};
  if (REC[base(selected)]) { connectionsOf(selected).forEach(function (x) { if (REC[base(x.id)] || NODE[base(x.id)]) related[x.id] = 1; }); traceOf(selected).forEach(function (x) { if (x !== selected) related[x] = 1; }); }
  headerWidgets(); renderNav();
  if (!opts.keepDetail) { detailEl.innerHTML = detailHtml(selected); connEl.innerHTML = connHtml(selected); }
  vbarEl.innerHTML = vbarHtml();
  if (opts.reveal || opts.focusRow) { var r = navEl.querySelector('[data-row="' + cssId(selected) + '"]'); if (r) { var nb = r.getBoundingClientRect(), side = r.closest(".side") || navEl, pb = side.getBoundingClientRect(), top = Math.max(pb.top, 0) + 160, bottom = Math.min(pb.bottom, window.innerHeight) - 20; if (nb.top < top || nb.bottom > bottom) r.scrollIntoView({ block: "center" }); if (opts.focusRow) r.focus({ preventScroll: true }); } }
}
function expandTo(id) { expanded = {}; if ((kidsOf[id] || []).length) expanded[id] = true; var p = parentOf[id]; delete closedGroups[(p || layerOf[id]) + "|" + typeOf(id)]; while (p) { expanded[p] = true; delete closedGroups[(parentOf[p] || layerOf[p]) + "|" + typeOf(p)]; p = parentOf[p]; } if (layerOf[id]) openLayers[layerOf[id]] = true; if (view !== "outline" && view !== "queue" && view !== "security" && view !== "ballots") view = "outline"; }
function layerFor(id) {
  if (layerOf[id]) return layerOf[id];
  if (id.indexOf("layer/") === 0) return id.slice(6);
  var t = id.split("/")[0], q = (LEX.types || {})[t];
  return q && q.layer ? String(q.layer).split(",")[0].trim() : null;
}
function preloadUpChain(id) {
  var guard = 0, cur = base(id);
  function step() {
    if (guard++ >= 8) return Promise.resolve();
    var u = null;
    try { u = upOne(cur); } catch (e) { u = null; }
    if (!u || u === cur) return Promise.resolve();
    cur = base(u);
    return ensureRecords([cur]).then(step);
  }
  return ensureRecords([cur]).then(step);
}
function preloadFor(id) {
  id = base(id);
  var layer = layerFor(id);
  var needLayer = layer && layerByKey[layer] && !outlineLoaded[layer] ? loadLayers([layer]) : Promise.resolve();
  return needLayer.then(function () { return preloadUpChain(id); }).then(function () {
    var r = REC[id];
    if (!r) return;
    var t = r.type, ids = [];
    var depth = t === "use-case" ? 2 : (t === "automation" || t === "aggregate" || t === "result" ? 1 : 0);
    if (depth) {
      var seen = {};
      (function walk(x, d) {
        if (d > depth) return;
        (kidsOf[x] || []).forEach(function (k) { if (seen[k]) return; seen[k] = 1; ids.push(k); walk(k, d + 1); });
      })(id, 1);
    }
    outEdges(id).concat(inEdges(id)).slice(0, 200).forEach(function (e) { ids.push(e.id); });
    Object.keys(r.stepServers || {}).forEach(function (k) { (r.stepServers[k] || []).forEach(function (x) { ids.push(x); }); });
    Object.keys(r.stepScreens || {}).forEach(function (k) { (r.stepScreens[k] || []).forEach(function (x) { ids.push(x); }); });
    return ensureRecords(ids.slice(0, 500));
  });
}
function select(id, opts) {
  id = base(id); opts = opts || {}; if (typeof pop !== "undefined" && pop) pop.hidden = true;
  detailEl.innerHTML = '<p class="relnote">Loading.</p>';
  preloadFor(id).then(function () {
    selected = id; noteFor = null; focusQ = opts.q === undefined ? null : opts.q; expandTo(id);
    recent = [id].concat(recent.filter(function (x) { return x !== id; })).slice(0, 12);
    if (!opts.fromHistory) history.pushState({ id: id }, "", "#" + encodeURIComponent(id));
    render({ focusRow: opts.focusRow, reveal: true });
    window.scrollTo(0, 0); connEl.scrollTop = 0;
    if (focusQ !== null) { var q = document.getElementById("q-" + focusQ); if (q) q.scrollIntoView({ block: "center" }); }
  });
}
function setVerdict(v) {
  if (!selected || !REC[base(selected)]) return;
  if (v === "mine" || v === "mineR") { verdict[selected] = v; noteFor = null; var n = nextNeeding(selected); if (n) select(n, { focusRow: view !== "ballots" }); else render(); return; }
  verdict[selected] = v; noteFor = selected; noteKind = v; render(); var t = document.getElementById("note"); if (t) t.focus();
}
function setView(v) {
  view = v;
  if ((v === "queue" || v === "security" || v === "ballots") && !queueLoaded) {
    render({ keepDetail: true, reveal: true });
    ensureQueue().then(function () { render({ keepDetail: true, reveal: true }); });
    return;
  }
  render({ keepDetail: true, reveal: true });
  if (v === "queue" || v === "security") { var first = navEl.querySelector("[data-row]"); if (first && first.getAttribute("data-row") !== selected) select(first.getAttribute("data-row")); }
  if (v === "ballots") { var q = navEl.querySelector("[data-q]"); if (q) { var p = q.getAttribute("data-q").split("|"); if (p[0] !== selected) select(p[0], { q: +p[1] }); } }
}

// ---------- events ----------
var pop = document.getElementById("xpop");
function showExplain(el) {
  var h = explainHtml(el.getAttribute("data-explain")); if (!h) return;
  pop.innerHTML = h; pop.hidden = false;
  var b = el.getBoundingClientRect(), w = Math.min(340, window.innerWidth - 24);
  pop.style.width = w + "px"; pop.style.left = Math.max(12, Math.min(b.left, window.innerWidth - w - 12)) + "px"; pop.style.top = (b.bottom + 8) + "px";
  if (b.bottom + 8 + pop.offsetHeight > window.innerHeight - 12) pop.style.top = Math.max(12, b.top - pop.offsetHeight - 8) + "px";
}
document.addEventListener("keydown", function (e) { if ((e.key === "Enter" || e.key === " ") && e.target.getAttribute && e.target.getAttribute("data-explain") && e.target.tagName !== "BUTTON") { e.preventDefault(); showExplain(e.target); } });
window.addEventListener("scroll", function () { pop.hidden = true; }, true);
var hoverT = null, hoverEl = null;
document.addEventListener("mouseover", function (e) { var t = e.target.closest && e.target.closest("[data-explain]"); if (!t || t === hoverEl) return; hoverEl = t; clearTimeout(hoverT); hoverT = setTimeout(function () { showExplain(t); }, 300); });
document.addEventListener("mouseout", function (e) { var t = e.target.closest && e.target.closest("[data-explain]"); if (!t) return; if (e.relatedTarget && (t.contains(e.relatedTarget) || (e.relatedTarget.closest && e.relatedTarget.closest("#xpop")))) return; clearTimeout(hoverT); hoverEl = null; pop.hidden = true; });
document.addEventListener("focusin", function (e) { var t = e.target.closest && e.target.closest("[data-explain]"); if (t) showExplain(t); });
function preloadTreeTypes() {
  if (treeLoading) return Promise.resolve();
  treeLoading = true;
  var ids = order.filter(function (x) { return typeOf(x) === "goal" || typeOf(x) === "feature"; });
  return ensureRecords(ids).then(function () { treeLoading = false; }, function () { treeLoading = false; });
}
document.body.addEventListener("click", function (e) {
  var t;
  if ((t = e.target.closest("[data-explain]"))) { e.stopPropagation(); showExplain(t); return; }
  if (!e.target.closest("#xpop")) pop.hidden = true;
  if ((t = e.target.closest("[data-tg]"))) { var gk = t.getAttribute("data-tg"); closedGroups[gk] = !closedGroups[gk]; renderNav(); return; }
  if ((t = e.target.closest(".tvo"))) { document.body.classList.remove("wide"); select(t.getAttribute("data-go")); return; }
  if ((t = e.target.closest("[data-tv]"))) { var tid = t.getAttribute("data-tv"); tvOpen[tid] = !tvOpen[tid]; if (tid.indexOf("tv/") !== 0 && !REC[base(tid)]) { ensureRecords([tid]).then(function () { document.getElementById("itreeBody").innerHTML = intentTreeHtml(); }); return; } document.getElementById("itreeBody").innerHTML = intentTreeHtml(); return; }
  if ((t = e.target.closest("[data-tvall]"))) { var pid = productId(); tvOpen = {}; tvOpen[pid] = true; if (t.getAttribute("data-tvall") === "1") tvKids(pid).forEach(function (g) { tvOpen[g] = true; }); else tvOpen = {}; document.getElementById("itreeBody").innerHTML = intentTreeHtml(); return; }
  if ((t = e.target.closest("[data-wide]"))) { var on = t.getAttribute("data-wide") === "1"; document.body.classList.toggle("wide", on); if (on) { document.getElementById("itreeBody").innerHTML = '<div class="itree"><p class="relnote">Loading the Intent tree.</p></div>'; window.scrollTo(0, 0); ensureLayer("intent").then(preloadTreeTypes).then(function () { var p = productId(); if (!tvOpen[p]) tvOpen[p] = true; document.getElementById("itreeBody").innerHTML = intentTreeHtml(); }); } if (!on && t.getAttribute("data-go")) select(t.getAttribute("data-go")); return; }
  if ((t = e.target.closest("[data-intro]"))) { try { localStorage.setItem("book-intro", "closed"); } catch (x) { } detailEl.innerHTML = detailHtml(selected); return; }
  if ((t = e.target.closest("[data-tog]"))) { var id = t.getAttribute("data-tog"); if (expanded[id]) { expanded[id] = false; renderNav(); } else select(id); e.stopPropagation(); return; }
  if ((t = e.target.closest("[data-q]"))) { var p = t.getAttribute("data-q").split("|"); select(p[0], { q: +p[1] }); return; }
  if ((t = e.target.closest("[data-row]"))) { select(t.getAttribute("data-row"), { focusRow: e.detail === 0 }); return; }
  if ((t = e.target.closest("[data-layer]"))) { var k = t.getAttribute("data-layer"); openLayers[k] = !openLayers[k]; if (openLayers[k] && !outlineLoaded[k]) { renderNav(); ensureLayer(k).then(function () { renderNav(); }); return; } renderNav(); return; }
  if ((t = e.target.closest("[data-view]"))) { setView(t.getAttribute("data-view")); return; }
  if ((t = e.target.closest("#facetBtn"))) { facetsOpen = !facetsOpen; renderNav(); return; }
  if ((t = e.target.closest("[data-facet]"))) { var fk = t.getAttribute("data-facet"), fv = t.getAttribute("data-val"), a = active[fk] = active[fk] || []; var i = a.indexOf(fv); if (i >= 0) a.splice(i, 1); else a.push(fv); render({ keepDetail: true }); return; }
  if ((t = e.target.closest("[data-unf]"))) { var u = t.getAttribute("data-unf").split("|"); active[u[0]] = (active[u[0]] || []).filter(function (x) { return x !== u.slice(1).join("|"); }); render({ keepDetail: true }); return; }
  if ((t = e.target.closest("[data-unq]"))) { query = ""; qEl.value = ""; render({ keepDetail: true }); return; }
  if ((t = e.target.closest("[data-clear]"))) { active = {}; query = ""; qEl.value = ""; render({ keepDetail: true }); return; }
  if ((t = e.target.closest("[data-act]"))) { var act = t.getAttribute("data-act"); if (act === "expand") order.forEach(function (x) { if ((kidsOf[x] || []).length && typeOf(x) !== "use-case") expanded[x] = true; }); if (act === "collapse") expanded = {}; if (act === "reveal") { expandTo(selected); } render({ keepDetail: true, reveal: true }); return; }
  if ((t = e.target.closest("[data-go]"))) { e.preventDefault(); if (t.getAttribute("data-go")) select(t.getAttribute("data-go")); return; }
  if ((t = e.target.closest("[data-v]"))) { setVerdict(t.getAttribute("data-v")); return; }
  if ((t = e.target.closest("[data-ans]"))) { var pa = t.getAttribute("data-ans").split("|"); if (pa[1] === "accept") { answered[selected + "#" + pa[0]] = "accept"; render(); } else { noteFor = selected; noteKind = "answer"; focusQ = +pa[0]; vbarEl.innerHTML = vbarHtml(); document.getElementById("note").focus(); } return; }
  if ((t = e.target.closest("[data-note]"))) { var val = document.getElementById("note").value; if (t.getAttribute("data-note") === "save") { if (noteKind === "answer") answered[selected + "#" + focusQ] = val || "answered"; else notes[selected] = val; } else if (noteKind !== "answer") delete verdict[selected]; noteFor = null; render(); return; }
  if ((t = e.target.closest("[data-next]"))) { var n = nextNeeding(selected); if (n) select(n); return; }
  if ((t = e.target.closest("[data-sc]"))) { var c = t.closest(".sc"); c.classList.toggle("open"); t.setAttribute("aria-expanded", c.classList.contains("open")); return; }
  if ((t = e.target.closest("[data-allsc]"))) { showAllScen[t.getAttribute("data-allsc")] = true; detailEl.innerHTML = detailHtml(selected); return; }
  if ((t = e.target.closest("[data-cmore]"))) { connAll[t.getAttribute("data-cmore")] = true; connEl.innerHTML = connHtml(selected); return; }
  if ((t = e.target.closest("#recentBtn"))) { var m = document.getElementById("recentMenu"), open = m.hidden; m.hidden = !open; t.setAttribute("aria-expanded", open); if (open) m.innerHTML = "<h4>Recently opened</h4>" + (recent.length ? recent.map(function (x) { return '<div class="trow" role="menuitem" tabindex="0" data-row="' + esc(x) + '"><span class="ti">' + ic(typeOf(x)) + '</span><span class="tt">' + esc(title(x)) + '</span><span class="ttag">' + esc(tag(typeOf(x))) + "</span></div>"; }).join("") : '<p class="relnote">Nothing yet.</p>'); return; }
  if ((t = e.target.closest("#keysBtn"))) { document.getElementById("keys").hidden = false; return; }
  if (e.target.id === "keys") { e.target.hidden = true; return; }
  if (!e.target.closest(".recent")) document.getElementById("recentMenu").hidden = true;
});
document.body.addEventListener("change", function (e) {
  if (e.target.id === "sortSel") { sortKey = e.target.value; render({ keepDetail: true }); }
  if (e.target.id === "grpSel") { groupKey = e.target.value; render({ keepDetail: true }); }
});
connEl.addEventListener("mouseover", function (e) { var t = e.target.closest("[data-hl]"); if (!t) return; var r = navEl.querySelector('[data-row="' + cssId(t.getAttribute("data-hl")) + '"]'); if (r) r.classList.add("hl"); });
connEl.addEventListener("mouseout", function (e) { var t = e.target.closest("[data-hl]"); if (!t) return; var r = navEl.querySelector('[data-row="' + cssId(t.getAttribute("data-hl")) + '"]'); if (r) r.classList.remove("hl"); });
var qEl = document.getElementById("q"), qTimer = null;
qEl.addEventListener("focus", function () { ensureSearch().then(function () { if (query) render({ keepDetail: true }); }); });
qEl.addEventListener("input", function () { clearTimeout(qTimer); qTimer = setTimeout(function () { query = qEl.value.trim(); ensureSearch().then(function () { render({ keepDetail: true }); }); }, 120); });
qEl.addEventListener("keydown", function (e) {
  if (e.key === "Escape") { qEl.value = ""; query = ""; render({ keepDetail: true }); qEl.blur(); }
  if (e.key === "Enter") {
    clearTimeout(qTimer); query = qEl.value.trim();
    ensureSearch().then(function () {
      computeVisible();
      var hits = order.filter(function (id) { return passes(id); }), words = query.toLowerCase().split(/\s+/);
      var byTitle = hits.filter(function (id) { var t = title(id).toLowerCase(); return words.every(function (w) { return t.indexOf(w) >= 0; }); });
      var f = byTitle[0] || hits[0]; if (f) select(f, { focusRow: true }); else render({ keepDetail: true });
    });
  }
  if (e.key === "ArrowDown") { var g = navEl.querySelector("[data-row]"); if (g) { g.focus(); e.preventDefault(); } }
});
function move(delta) { var list = navList(), cur = view === "ballots" ? selected + "|" + focusQ : selected, i = list.indexOf(cur); var n = list[i + delta] || (i < 0 ? list[0] : null); if (!n) return; if (n.indexOf("|") > 0) { var p = n.split("|"); select(p[0], { q: +p[1] }); } else select(n, { focusRow: true }); }
document.addEventListener("keydown", function (e) {
  var k = e.key, typing = /^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName);
  if ((e.ctrlKey || e.metaKey) && (k === "k" || k === "K")) { e.preventDefault(); qEl.focus(); qEl.select(); return; }
  if (k === "Escape") { pop.hidden = true; document.getElementById("keys").hidden = true; document.getElementById("recentMenu").hidden = true; if (anyFilter() && !typing) { active = {}; query = ""; qEl.value = ""; render({ keepDetail: true }); } return; }
  if (typing || e.ctrlKey || e.metaKey || e.altKey) return;
  var inTree = e.target.closest && e.target.closest("[role=treeitem]");
  if (k === "/") { e.preventDefault(); qEl.focus(); return; }
  if (k === "?") { document.getElementById("keys").hidden = false; return; }
  if (k === "j" || (inTree && k === "ArrowDown")) { move(1); e.preventDefault(); return; }
  if (k === "k" || (inTree && k === "ArrowUp")) { move(-1); e.preventDefault(); return; }
  if (k === "u" || (inTree && k === "ArrowLeft" && !(expanded[selected] && (kidsOf[selected] || []).length))) { if (parentOf[selected]) select(parentOf[selected], { focusRow: true }); e.preventDefault(); return; }
  if (inTree && k === "ArrowLeft") { expanded[selected] = false; render({ keepDetail: true, focusRow: true }); e.preventDefault(); return; }
  if (inTree && k === "ArrowRight") { if ((kidsOf[selected] || []).length) { expanded[selected] = true; render({ keepDetail: true, focusRow: true }); } e.preventDefault(); return; }
  if (k === "[" || k === "]") { var sib = (siblingsOf(selected) || []).filter(function (x) { return typeOf(x) === typeOf(selected); }), i = sib.indexOf(selected), n = sib[i + (k === "]" ? 1 : -1)]; if (n) select(n, { focusRow: true }); return; }
  if (k === "n") { var nx = nextNeeding(selected); if (nx) select(nx, { focusRow: true }); return; }
  if (k === "a") { setVerdict(isBeh(selected) ? "mineR" : "mine"); return; }
  if (k === "e") { order.forEach(function (x) { if ((kidsOf[x] || []).length && typeOf(x) !== "use-case") expanded[x] = true; }); render({ keepDetail: true, reveal: true }); return; }
  if (k === "c") { expanded = {}; render({ keepDetail: true }); return; }
  if (k >= "1" && k <= "4") { setView(["outline", "queue", "security", "ballots"][+k - 1]); return; }
});
window.addEventListener("popstate", function (e) { var id = (e.state && e.state.id) || decodeURIComponent(location.hash.slice(1)); if (id) select(base(id), { fromHistory: true }); });

var themeBtn = document.getElementById("theme");
function paintTheme() { var d = document.documentElement.getAttribute("data-theme") === "dark"; themeBtn.innerHTML = ic(d ? "sun" : "moon"); themeBtn.setAttribute("aria-label", d ? "Switch to light theme" : "Switch to dark theme"); }
themeBtn.addEventListener("click", function () { var el = document.documentElement; el.setAttribute("data-theme", el.getAttribute("data-theme") === "dark" ? "light" : "dark"); paintTheme(); });
paintTheme();
var dl = document.getElementById("dl");
view = "outline";
dl.innerHTML = ic("download", "icon-sm") + "Download review";
dl.addEventListener("click", function () {
  var items = Object.keys(verdict).map(function (id) { var v = verdict[id]; return { record: id, verdict: v === "mine" ? "approve" : v === "mineR" ? "review" : v === "changes" ? "request-changes" : "question", note: notes[id] || null }; });
  var ans = Object.keys(answered).map(function (k) { var p = k.split("#"), q = (((REC[p[0]] || {}).fields || {}).questions || [])[+p[1]] || {}; return { record: p[0], question: q.question || "", ballot: q.ballot || null, answer: answered[k] === "accept" ? "accept recommendation" : answered[k] }; });
  var blob = new Blob([JSON.stringify({ project: MAN.project, generatedOn: MAN.generatedOn, verdicts: items, answers: ans }, null, 2)], { type: "application/json" });
  var a = document.createElement("a"); a.href = URL.createObjectURL(blob); a.download = "review.json"; document.body.appendChild(a); a.click(); setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
});

function preloadAll() {
  var layers = (MAN.layers || []).map(function (l) { return l.key; }).filter(function (k) { return !outlineLoaded[k]; });
  return loadLayers(layers).then(function () { return ensureRecords(Object.keys(MAN.shardOf || {})); });
}
function boot() {
  detailEl.innerHTML = '<p class="relnote">Loading the book.</p>';
  bookData().load("manifest").then(function (m) {
    MAN = m;
    TODAY = new Date(((MAN.generatedOn || "").slice(0, 10) || new Date().toISOString().slice(0, 10)) + "T00:00:00Z");
    if (isNaN(TODAY)) TODAY = new Date();
    return bookData().load("lexicon");
  }).then(function (lx) {
    LEX = lx;
    buildLexiconCaches();
    layerByKey = {};
    (MAN.layers || []).forEach(function (l) { var meta = (LEX.layers || {})[l.key] || {}; layerByKey[l.key] = { key: l.key, label: l.label || meta.label || l.key, outline: l.outline || [], writes: meta.writes || "", reviews: meta.reviews || "", question: meta.question || "" }; });
    var hash = decodeURIComponent(location.hash.slice(1));
    var keys = ["intent"];
    var startType = hash ? hash.split("/")[0] : "";
    var startLayer = (LEX.types || {})[startType] ? String(LEX.types[startType].layer || "").split(",")[0].trim() : null;
    if (startLayer && keys.indexOf(startLayer) < 0 && layerByKey[startLayer]) keys.push(startLayer);
    if (hash.indexOf("layer/") === 0) { var lk = hash.slice(6); if (layerByKey[lk] && keys.indexOf(lk) < 0) keys.push(lk); }
    return loadLayers(keys).then(function () { return hash; });
  }).then(function (hash) {
    document.getElementById("projName").textContent = (MAN.project && (MAN.project.title || MAN.project.id)) || "Book";
    document.title = (((MAN.project && (MAN.project.title || MAN.project.id)) || "Book") + " Book");
    if (MAN.unplaced && MAN.unplaced.length && window.console) console.info("Unplaced records: " + MAN.unplaced.join(", "));
    var total = 0; Object.keys(MAN.totals || {}).forEach(function (k) { total += MAN.totals[k]; });
    var start = base(hash);
    function finish() {
      var first = (NODE[start] || REC[start]) ? start : productId() || order[0];
      if (!first) { detailEl.innerHTML = '<p class="relnote">This book holds no records.</p>'; renderNav(); return; }
      if (start && !NODE[start] && !REC[start]) {
        var t = start.split("/")[0], layer = (LEX.types || {})[t] ? String(LEX.types[t].layer || "").split(",")[0].trim() : null;
        if (layer && layerByKey[layer] && !outlineLoaded[layer]) { loadLayers([layer]).then(finish); return; }
      }
      selected = first;
      history.replaceState({ id: first }, "", "#" + encodeURIComponent(first));
      select(first, { fromHistory: true });
    }
    if (total && total <= 500) preloadAll().then(finish);
    else finish();
  }).catch(function (err) {
    detailEl.innerHTML = '<p class="relnote">The book could not be loaded (' + esc(err && err.message ? err.message : "missing data") + ").</p>";
  });
}
boot();
})();
