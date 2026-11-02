// Builds the single-file book mockup from a real generated .book data folder.
// Usage: BOOK_DATA=<path to .book/data> OUT=<file.html> node .agents/book-mockup/build.js
const fs = require('fs');
const path = require('path');

const dataDir = process.env.BOOK_DATA;
if (!dataDir) throw new Error('set BOOK_DATA to the .book/data folder');
const outFile = process.env.OUT;
if (!outFile) throw new Error('set OUT to the html file to write');
const shellPath = path.join(__dirname, 'shell.html');
const stylePath = path.join(__dirname, 'style.css');
const appPath = path.join(__dirname, 'app.js');

/* ---------- load the generated shards ---------- */
const store = {};
global.BookData = { put: (k, v) => (store[k] = v) };
for (const f of fs.readdirSync(dataDir).filter((f) => f.endsWith('.js'))) {
  new Function(fs.readFileSync(path.join(dataDir, f), 'utf8'))();
}

const manifest = store.manifest;
const recs = {};
for (const k of Object.keys(store)) if (k.startsWith('records-')) Object.assign(recs, store[k]);
for (const [id, r] of Object.entries(recs)) if (r && !r.id) r.id = id;
const all = Object.values(recs).filter((r) => r && r.id);
const byId = (id) => recs[id];

/* ---------- helpers ---------- */
const decode = (s) =>
  s
    .replace(/<[^>]+>/g, '')
    .replace(/&#39;/g, "'")
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&nbsp;/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();

function blocksFromHtml(html) {
  if (!html) return [];
  const out = [];
  const re = /<(h[1-6]|p|li)[^>]*>([\s\S]*?)<\/\1>/gi;
  let m;
  while ((m = re.exec(html))) {
    const tag = m[1].toLowerCase();
    const text = decode(m[2]);
    if (!text) continue;
    out.push([tag === 'li' ? 'li' : tag.startsWith('h') ? 'h' : 'p', text]);
  }
  return out;
}

const clip = (v, n) => (typeof v === 'string' && v.length > n ? v.slice(0, n).trim() + '...' : v);
function trimValue(v, depth = 0) {
  if (typeof v === 'string') return clip(v, 1800);
  if (Array.isArray(v)) return v.slice(0, 40).map((x) => trimValue(x, depth + 1));
  if (v && typeof v === 'object') {
    const o = {};
    for (const [k, val] of Object.entries(v)) {
      if (k === 'approvedHash') continue;
      o[k] = trimValue(val, depth + 1);
    }
    return o;
  }
  return v;
}

const moduleOf = (id) => {
  const tail = String(id).split('/')[1] || '';
  return tail.includes('.') ? tail.split('.')[0] : tail;
};

// A record is settled once a person settled it: approved, reviewed or retired.
// Still a draft is the only state that is not settled.
const settledStatus = (s) => s !== 'draft';

/* ---------- pages the read mode walks ---------- */
const PAGES = [
  { part: 'Intent', title: 'NightHall', rec: 'product/nighthall' },
  { part: 'Intent', title: 'Churn Under 3 Percent', rec: 'goal/churn-under-3-percent' },
  { part: 'Intent', title: 'Use a Buyer Account', rec: 'journey/use-a-buyer-account' },
  { part: 'Behavior', title: 'The Accounts Module', rec: 'module/accounts' },
  { part: 'Behavior', title: 'Add an External Ticket Card', rec: 'use-case/accounts.add-external-ticket-card' },
  { part: 'Behavior', title: 'Fifty Cards at Most', rec: 'rule/external-ticket-card.account-holds-at-most-fifty-cards' },
  { part: 'Behavior', title: 'Change the Account Profile', rec: 'use-case/accounts.change-user-account-profile' },
  { part: 'Decisions', title: 'Buyer Message Locale Falls Back to the Address Market', rec: 'decision/0086-buyer-locale-fallback' },
  { part: 'Decisions', title: 'Is the Organizer Webshop a Hosting Service?', rec: 'constraint/platform-hosting-duties', ballot: true },
].filter((p) => byId(p.rec));

/* ---------- which records carry prose in the mockup ---------- */
const keep = new Set(PAGES.map((p) => p.rec));
for (const b of store.queue.ballots || []) keep.add(b.id);
for (const r of all) {
  const id = r.id;
  if (id === 'module/accounts') keep.add(id);
  if (/^use-case\/accounts\./.test(id)) keep.add(id);
  if (/^rule\/external-ticket-card\./.test(id)) keep.add(id);
  if (r.type === 'decision') keep.add(id);
  if (id === 'constraint/platform-hosting-duties') keep.add(id);
}
// one hop of references, so links in the sampled pages resolve to real titles
for (const id of Array.from(keep)) {
  const r = byId(id);
  if (!r) continue;
  const refs = [];
  for (const e of r.out || []) refs.push(e.id);
  for (const e of r.in || []) refs.push(e.id);
  const walk = (v) => {
    if (typeof v === 'string') {
      if (/^[a-z][a-z-]*\/[a-z0-9][a-z0-9.-]*(#[\w-]+)?$/.test(v)) refs.push(v);
    } else if (Array.isArray(v)) v.forEach(walk);
    else if (v && typeof v === 'object') Object.values(v).forEach(walk);
  };
  walk(r.fields || {});
  for (const ref of refs) if (byId(ref) && keep.size < 700) keep.add(ref);
}

const embedded = {};
const labels = {};
for (const id of keep) {
  const r = byId(id);
  if (!r) continue;
  labels[id] = { t: r.title, y: r.type };
  const finds = { A: 0, S: 0, G: 0 };
  for (const f of r.findings || []) if (finds[f.tier] !== undefined) finds[f.tier] += 1;
  embedded[id] = {
    id,
    type: r.type,
    title: r.title,
    status: r.status,
    effective: r.effective,
    owner: r.owner,
    decider: r.decider,
    decideBy: r.decideBy,
    layer: r.layer,
    src: r.src ? [r.src.path, r.src.line] : null,
    fields: trimValue(r.fields || {}),
    body: blocksFromHtml(r.bodyHtml),
    impl: (r.impl || []).slice(0, 12).map((i) => [i.kind, i.path, i.line]),
    tests: (r.tests || []).length,
    expect: r.expect || null,
    finds,
    out: (r.out || []).slice(0, 40).map((e) => [e.id, e.key]),
    in: (r.in || []).slice(0, 40).map((e) => [e.id, e.key]),
  };
}

/* ---------- the full index, for search ---------- */
const statusChar = { approved: 'a', reviewed: 'r', draft: 'd', retired: 'x' };
const sorted = all.slice().sort((a, b) => (a.id < b.id ? -1 : 1));
const index = {
  ids: sorted.map((r) => r.id).join('\n'),
  titles: sorted.map((r) => String(r.title || '').replace(/\n/g, ' ')).join('\n'),
  status: sorted.map((r) => statusChar[r.status] || '?').join(''),
};

/* ---------- aggregates ---------- */
const layerLabels = {};
for (const l of manifest.layers) layerLabels[l.key] = l.label;

const layerRows = [];
for (const l of manifest.layers) {
  const rows = all.filter((r) => r.layer === l.key);
  const types = {};
  let settled = 0;
  for (const r of rows) {
    types[r.type] = (types[r.type] || 0) + 1;
    if (settledStatus(r.effective)) settled += 1;
  }
  layerRows.push({
    key: l.key,
    label: l.label,
    total: rows.length,
    settled,
    types: Object.entries(types).sort((a, b) => b[1] - a[1]),
  });
}

const moduleNames = {};
for (const r of all) if (r.type === 'module') moduleNames[moduleOf(r.id)] = r.title;
const moduleRows = Object.keys(moduleNames)
  .map((m) => {
    const rows = all.filter((r) => r.layer === 'behavior' && moduleOf(r.id) === m);
    const types = {};
    let settled = 0;
    let findings = 0;
    let findingsGap = 0;
    let unimplemented = 0;
    for (const r of rows) {
      types[r.type] = (types[r.type] || 0) + 1;
      if (settledStatus(r.effective)) settled += 1;
      for (const f of r.findings || []) {
        findings += 1;
        if (f.tier !== 'A') findingsGap += 1;
        if (f.code === 'effect-unimplemented') unimplemented += 1;
      }
    }
    return { id: m, name: moduleNames[m], total: rows.length, settled, findings, findingsGap, unimplemented, types: Object.entries(types).sort((a, b) => b[1] - a[1]) };
  })
  .sort((a, b) => b.total - a.total);

const q = store.queue;
const batches = Object.values(q.batches);
const decisionsQueued = batches.filter((b) => b.kind === 'decision' || b.kind === 'use-case');
const decisionRows = decisionsQueued
  .map((b) => {
    const r = byId(b.records && b.records[0]);
    return {
      title: r ? r.title : b.selector,
      rec: r ? r.id : b.selector,
      due: b.decideBy,
      tier: b.tier,
      kind: b.kind,
      n: b.n,
      awaiting: b.awaiting || 0,
    };
  })
  .sort((a, b) => String(a.due).localeCompare(String(b.due)));

const ballots = (q.ballots || []).map((b) => {
  const r = byId(b.id);
  return {
    id: b.id,
    title: r ? r.title : b.id,
    question: b.q && b.q.question,
    recommendation: b.q && b.q.recommendation,
    tier: b.q && b.q.tier,
    decider: b.q && b.q.decider,
    due: b.q && b.q.decideBy,
    ballot: b.q && b.q.ballot,
  };
});

const secKinds = {};
for (const s of q.security || []) {
  if (!secKinds[s.kind]) secKinds[s.kind] = { kind: s.kind, n: 0, samples: [] };
  secKinds[s.kind].n += 1;
  if (secKinds[s.kind].samples.length < 4) {
    const r = byId(s.id);
    secKinds[s.kind].samples.push({ id: s.id, title: r ? r.title : s.id, text: s.text });
  }
}

const findingMeaning = {
  'value-invalid': 'A field or statement word does not follow the shape the schema asks for.',
  'result-scenario-missing': 'A use-case result has no scenario that exercises it.',
  'use-case-unverified': 'A use case reached by a user has no end-to-end proof.',
  'refusal-sentence-missing': 'A refusal rule has no sentence stating what the user is told.',
  'effect-unimplemented': 'A promised effect has no code behind it.',
  'route-invalid': 'A screen route does not match the route grammar.',
  'proves-unreached': 'A test claims to prove something no record claims.',
  'proves-duplicate': 'Two tests claim to prove the same thing.',
  'use-case-unreached': 'A use case is owned by no entry point.',
  'requirement-measure-missing': 'A requirement states no measure, so nobody can check it.',
  'step-screen-missing': 'A journey step happens on no screen.',
  'use-case-act-missing': 'A use case is called by no journey step.',
  'requirement-fulfillment-missing': 'A requirement is fulfilled by no use case.',
  'use-case-method-drift': 'A use-case handler does something its record does not state.',
  'format-drift': 'A source file drifted from the format the schema asks for.',
};
const codeCounts = {};
for (const r of all) {
  for (const f of r.findings || []) {
    if (!codeCounts[f.code]) {
      codeCounts[f.code] = { code: f.code, tier: f.tier, tiers: { S: 0, G: 0, A: 0 }, n: 0, records: 0, sample: f.message, rec: r.id };
    }
    const c = codeCounts[f.code];
    c.n += 1;
    c.records += 1;
    if (c.tiers[f.tier] !== undefined) c.tiers[f.tier] += 1;
    // A note is shown at its most serious tier when one code produces more than one.
    if (f.tier === 'S' || (f.tier === 'G' && c.tier !== 'S')) c.tier = f.tier;
  }
}
const findingRows = Object.values(codeCounts)
  .sort((a, b) => b.n - a.n)
  .map((c) => ({ ...c, meaning: findingMeaning[c.code] || 'A check the validator writes on this record.' }));

const drafts = all.filter((r) => r.effective === 'draft');
const totals = manifest.totals;
const tierTotals = { A: 0, S: 0, G: 0 };
for (const r of all) for (const f of r.findings || []) if (tierTotals[f.tier] !== undefined) tierTotals[f.tier] += 1;
const totalOf = (tier) => tierTotals[tier] || 0;
const codeOf = (code) => (findingRows.filter((f) => f.code === code)[0] || { n: 0 }).n;
const notImplemented = codeOf('effect-unimplemented');
const unproved = codeOf('use-case-unverified');

const data = {
  project: { title: manifest.project.title, id: manifest.project.id },
  generated: new Date().toISOString().slice(0, 10),
  edition: 'LiteSpec 3.0.0',
  totals,
  metrics: {
    records: all.length,
    settled: all.filter((r) => settledStatus(r.effective)).length,
    draft: drafts.length,
    decisionsQueued: decisionsQueued.length,
    batches: batches.length,
    ballots: ballots.length,
    security: (q.security || []).length,
    findings: findingRows.reduce((n, f) => n + f.n, 0),
    styleFindings: totalOf('A'),
    securityFindings: totalOf('S'),
    governanceFindings: totalOf('G'),
    proofRecords: all.filter((r) => r.layer === 'proof').length,
    notImplemented,
    unproved,
  },
  layers: layerRows,
  modules: moduleRows,
  pages: PAGES,
  index,
  records: embedded,
  labels,
  decisions: decisionRows.slice(0, 40),
  ballots,
  security: { groups: Object.values(secKinds), total: (q.security || []).length },
  findings: findingRows,
  lexicon: store.lexicon,
};

/* ---------- write ---------- */
const payload = JSON.stringify(data).replace(/</g, '\\u003c');
const html = fs
  .readFileSync(shellPath, 'utf8')
  .replace('/*__STYLE__*/', () => fs.readFileSync(stylePath, 'utf8'))
  .replace('/*__BOOK_DATA__*/', () => payload)
  .replace('/*__APP__*/', () => fs.readFileSync(appPath, 'utf8'));
fs.writeFileSync(outFile, html, 'utf8');
console.log(
  JSON.stringify(
    {
      wrote: path.resolve(outFile),
      kilobytes: Math.round(Buffer.byteLength(html) / 1024),
      records: all.length,
      embedded: Object.keys(embedded).length,
      pages: PAGES.length,
      modules: moduleRows.length,
      findingsRows: findingRows.length,
      decisions: data.decisions.length,
      ballots: ballots.length,
    },
    null,
    2,
  ),
);
