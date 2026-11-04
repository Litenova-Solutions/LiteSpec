// The navigation's row models. The outline and list views flatten their trees
// into one descriptor array each, which the sidebar virtualizes; the queue,
// security and ballot views render from small group models. Every builder
// mirrors its book.js function, and every word comes from the book or the lexicon.
import { base, cap, plural, type Ballot, type Batch, type Book, type Question, type SecurityItem } from "$lib/book";

export interface LayerRow { kind: "layer"; key: string; layer: string }
export interface DescRow { kind: "desc"; key: string; layer: string }
export interface NoteRow { kind: "note"; key: string; text: string }
export interface GroupRow { kind: "group"; key: string; gk: string; label: string; count: number; completion: string; open: boolean; depth: number }
export interface RecordRow { kind: "record"; key: string; id: string; depth: number; level: number; kids: number; open: boolean; dim: boolean }
export interface ListGroupRow { kind: "listgroup"; key: string; label: string; icon: string | null; count: number; completion: string }
export interface ListRecordRow { kind: "listrecord"; key: string; id: string }
export interface ShowAllRow { kind: "showall"; key: string; total: number }
export type Row = LayerRow | DescRow | NoteRow | GroupRow | RecordRow | ListGroupRow | ListRecordRow | ShowAllRow;

/** The fixed height of each row kind; descriptions measure themselves. */
export const ROW_HEIGHT: Record<Row["kind"], number> = {
  layer: 34, desc: 56, note: 28, group: 26, record: 28, listgroup: 30, listrecord: 46, showall: 50,
};

// One entry per depth, as literal classes so Tailwind emits them.
const INDENT = [
  "pl-0", "pl-[22px]", "pl-[44px]", "pl-[66px]", "pl-[88px]", "pl-[110px]", "pl-[132px]",
  "pl-[154px]", "pl-[176px]", "pl-[198px]", "pl-[220px]", "pl-[242px]", "pl-[264px]",
];
export function indentClass(depth: number) { return INDENT[Math.min(Math.max(depth, 0), INDENT.length - 1)]; }

/** The open questions as ballots: the queue shard, else the loaded records. */
export function ballotItems(b: Book): Ballot[] {
  if (b.queue) return b.ballots();
  const items: Ballot[] = [];
  for (const id of b.order) {
    if (!b.isReal(id)) continue;
    ((b.fields(id).questions ?? []) as Question[]).forEach((q, i) => {
      if (q?.question) items.push({ id, i, q });
    });
  }
  return items;
}

/** The security items: the queue shard, else the loaded records. */
export function securityItems(b: Book): SecurityItem[] {
  if (b.queue) return b.security();
  const items: SecurityItem[] = [];
  for (const id of b.order) {
    if (!b.isReal(id)) continue;
    const f = b.fields(id);
    if (f.access && f.access.tenant === "cross")
      items.push({ kind: "cross", id, text: "Acts across tenants" + (f.access.grant ? ", through " + b.title(f.access.grant) : "") });
    for (const e of f.effects ?? [])
      if (e.calls) items.push({ kind: "calls", id, text: "Calls " + b.title(base(String(e.calls))) + (String(e.calls).indexOf("#") > 0 ? ", " + String(e.calls).split("#")[1].replace(/-/g, " ") : "") });
    for (const c of f.dataClasses ?? [])
      if (c.classification === "personal" || c.classification === "sensitive")
        items.push({ kind: "data", id, text: cap(String(c.classification)) + " data: " + String(c.name).replace(/-/g, " ") + (c.onErasure ? ", on erasure " + c.onErasure : "") });
  }
  return items;
}

export function ballotRisk(b: Book, x: Ballot): string[] {
  return x.q?.riskClasses?.length ? x.q.riskClasses.map(String) : b.riskClassesOf(x.id);
}

export function ballotName(b: Book, key: string) {
  if (b.has(key)) return b.title(key);
  return key.replace(/^decision\//, "").replace(/-/g, " ");
}

// A batch named by what it holds, as release.md describes the four kinds.
export function batchName(b: Book, batch: Batch) {
  const p = String(batch.selector || "").split("/");
  if (batch.kind === "use-case") return "This use case with its results and scenarios";
  if (batch.kind === "decision") {
    const dn = base(String(batch.selector || "").replace(/^decision\//, ""));
    return "Decision " + (b.has(dn) ? '"' + b.title(dn) + '"' : batch.selector) + " with what it decides";
  }
  if (batch.kind === "record") return "This record on its own";
  return "All draft " + (p[1] ? b.pluralName(p[1]) : "").toLowerCase() + (p[0] === "intent" || p[0] === "governance" ? "" : " in " + p[0].replace(/-/g, " "));
}

function treeInto(b: Book, id: string, depth: number, out: Row[]) {
  if (!b.isVisible(id)) return;
  const kids = b.kids(id).filter((k) => b.isVisible(k));
  const open = kids.length > 0 && (b.anyFilter() ? true : !!b.expanded[id]);
  out.push({ kind: "record", key: "row/" + id, id, depth, level: b.depthOf[id] ?? depth, kids: kids.length, open, dim: b.anyFilter() && !b.matches(id) });
  if (open) groupedInto(b, kids, id, depth + 1, out);
}

function groupedInto(b: Book, ids: string[], pkey: string, depth: number, out: Row[]) {
  const groups: { key: string; label: string; list: string[] }[] = [];
  const seen: Record<string, { key: string; label: string; list: string[] }> = {};
  for (const id of ids) {
    const fg = b.fieldGroup(id);
    let key: string, label: string;
    if (fg && fg[0]) {
      key = "field:" + fg.join("/");
      label = fg.filter(Boolean).join(": ");
    } else {
      const t = b.typeOf(id);
      key = "type:" + t;
      const q = b.lex.types?.[t];
      label = q ? q.plural || q.label || t : cap(t) + "s";
    }
    if (!seen[key]) {
      seen[key] = { key, label, list: [] };
      groups.push(seen[key]);
    }
    seen[key].list.push(id);
  }
  const anyFilter = b.anyFilter();
  for (const g of groups) {
    const list = g.list.slice();
    if (b.sortKey !== "outline") list.sort(b.sorts[b.sortKey]?.cmp ?? b.sorts.outline.cmp);
    const rendered = list.filter((id) => b.isVisible(id));
    if (!rendered.length) continue;
    const gk = pkey + "|" + g.key;
    const refused = /^refused\b/i.test(g.label);
    const shut = !anyFilter && (b.closedGroups[gk] !== undefined ? b.closedGroups[gk] : refused);
    out.push({ kind: "group", key: "group/" + gk, gk, label: g.label, count: rendered.length, completion: b.completionCounts(rendered), open: !shut, depth });
    if (!shut) for (const id of list) treeInto(b, id, depth, out);
  }
}

export function buildOutlineRows(b: Book): Row[] {
  const out: Row[] = [];
  const anyFilter = b.anyFilter();
  for (const l of b.man?.layers ?? []) {
    if (!b.isLayerLoaded(l.key)) {
      out.push({ kind: "layer", key: "layer/" + l.key, layer: l.key });
      if (b.openLayers[l.key]) out.push({ kind: "note", key: "layer/" + l.key + "/loading", text: "Loading." });
      continue;
    }
    const ids = b.order.filter((id) => b.layerOf[id] === l.key && b.isReal(id));
    const shown = anyFilter ? ids.filter((id) => b.matches(id)).length : 0;
    if (anyFilter && !shown) continue;
    out.push({ kind: "layer", key: "layer/" + l.key, layer: l.key });
    if (!(anyFilter || b.openLayers[l.key])) continue;
    if (!anyFilter) out.push({ kind: "desc", key: "layer/" + l.key + "/desc", layer: l.key });
    const roots = b.rootsOf[l.key] ?? [];
    if (roots.length > 1) groupedInto(b, roots, l.key, 1, out);
    else for (const id of roots) treeInto(b, id, 1, out);
  }
  return out;
}

export function buildListRows(b: Book): Row[] {
  const out: Row[] = [];
  const cmp = b.sorts[b.sortKey]?.cmp ?? b.sorts.outline.cmp;
  const ids = b.order.filter((id) => b.passes(id)).sort(cmp);
  const count: Record<string, number> = {};
  const members: Record<string, string[]> = {};
  if (b.groupKey !== "none") {
    for (const id of ids) {
      const g = b.groupVal(id)[0];
      count[g] = (count[g] ?? 0) + 1;
      (members[g] ??= []).push(id);
    }
    ids.sort((a, c) => {
      const x = b.groupVal(a)[0], y = b.groupVal(c)[0];
      return x < y ? -1 : x > y ? 1 : 0;
    });
    if (b.groupKey === "type") {
      const order = Object.keys(b.lex.types ?? {});
      ids.sort((a, c) => order.indexOf(b.typeOf(a)) - order.indexOf(b.typeOf(c)));
    }
  }
  const limit = b.listShowAll ? ids.length : 400;
  const shownIds = ids.slice(0, limit);
  const closed: Record<string, boolean> = {};
  let lastG: string | null = null;
  for (const id of shownIds) {
    const g = b.groupVal(id);
    if (b.groupKey !== "none" && g[0] !== lastG) {
      const groupIds = members[g[0]] ?? [];
      const refused = b.groupKey === "field" && /^refused\b/i.test(g[1]);
      closed[g[0]] = refused && !b.listShowAll;
      out.push({ kind: "listgroup", key: "listgroup/" + g[0], label: g[1], icon: b.groupKey === "type" ? g[0] : null, count: count[g[0]] ?? 0, completion: b.completionCounts(groupIds) });
      if (refused && !b.listShowAll) out.push({ kind: "note", key: "listgroup/" + g[0] + "/note", text: "Refused records are collapsed. Use Show all to see them." });
      lastG = g[0];
    }
    if (closed[g[0]]) continue;
    out.push({ kind: "listrecord", key: "list/" + id, id });
  }
  if (ids.length > limit) out.push({ kind: "showall", key: "list/showall", total: ids.length });
  else if (b.listShowAll && ids.length > 400) out.push({ kind: "note", key: "list/shown", text: "Showing all " + ids.length + " records." });
  if (!ids.length) out.push({ kind: "note", key: "list/empty", text: "No record matches these filters." });
  return out;
}

export interface QueueBatchRow { key: string; id: string; title: string; sub: string; icon: string }
export interface QueueRiskGroup { key: string; approver: string; batches: QueueBatchRow[] }

export function queueGroups(b: Book): QueueRiskGroup[] {
  const batches = b.queue?.batches ?? {};
  const first: Record<string, string> = {};
  for (const id of b.order) {
    const bat = b.get(id)?.batch;
    if (bat && batches[bat] && !first[bat]) first[bat] = id;
  }
  for (const k of Object.keys(batches)) {
    if (first[k]) continue;
    const recs = batches[k].records ?? batches[k].ids ?? [];
    if (recs.length) first[k] = base(recs[0]);
  }
  const list = (b.queue?.order ?? Object.keys(batches)).filter((k) => batches[k] && first[k]);
  const groups: Record<string, string[]> = {};
  for (const k of list) {
    const batch = batches[k];
    const risk = batch.riskClasses?.length ? batch.riskClasses : first[k] ? b.riskClassesOf(first[k]) : [];
    const key = risk.length ? risk.map((v) => cap(String(v).replace(/-/g, " "))).join(", ") : "No risk";
    (groups[key] ??= []).push(k);
  }
  const riskFirst = (a: string, c: string) => (a === "No risk" ? 1 : 0) - (c === "No risk" ? 1 : 0) || a.localeCompare(c);
  return Object.keys(groups).sort(riskFirst).map((riskKey) => {
    const ks = groups[riskKey];
    let approver = "";
    for (const k of ks) {
      const ap = batches[k].approvers;
      if (!approver && ap?.length) approver = ap[0];
    }
    if (!approver && first[ks[0]]) {
      const rc = b.riskClassesOf(first[ks[0]]);
      if (rc.length) approver = b.approverFor(rc[0], first[ks[0]]);
    }
    return {
      key: riskKey,
      approver,
      batches: ks.slice(0, 200).map((k) => {
        const batch = batches[k];
        const id = first[k];
        const nm = batchName(b, batch);
        const riskText = batch.riskClasses?.length ? ", risk: " + batch.riskClasses.map((v) => cap(String(v).replace(/-/g, " "))).join(", ") : "";
        const approverText = batch.approvers?.length ? ", approved by " + batch.approvers.join(", ") : "";
        return {
          key: k,
          id,
          title: nm === "This use case with its results and scenarios" || nm === "This record on its own" ? b.title(id) : nm,
          sub: plural(batch.n ?? 0, "record") + riskText + approverText + (batch.decideBy ? ", decide by " + b.fmtDate(batch.decideBy) : ""),
          icon: batch.kind === "decision" ? "decision" : batch.kind === "use-case" ? "use-case" : "ballot",
        };
      }),
    };
  });
}

export interface SecurityGroup { kind: string; label: string; text: string; icon: string; items: SecurityItem[] }

export function securityGroups(b: Book): SecurityGroup[] {
  const all = securityItems(b).filter((x) => b.passes(x.id));
  return [
    { kind: "cross", label: "Access", text: "Use cases that act outside the caller's own tenant.", icon: "shield", items: all.filter((x) => x.kind === "cross") },
    { kind: "calls", label: "Calls", text: "Every call that leaves the system for an external system.", icon: "ext", items: all.filter((x) => x.kind === "calls") },
    { kind: "data", label: "Data classes", text: "Data classes that hold personal or sensitive fields, with their erasure position.", icon: "lock", items: all.filter((x) => x.kind === "data") },
  ].filter((g) => g.items.length);
}

export interface BallotGroup { key: string; approver: string; total: number; ballots: { key: string; label: string; items: Ballot[] }[] }

export function ballotGroups(b: Book): BallotGroup[] {
  const items = ballotItems(b).filter((x) => b.passes(x.id, "questions"));
  const byRisk: Record<string, Ballot[]> = {};
  for (const x of items) {
    const risk = ballotRisk(b, x);
    const key = risk.length ? risk.join(", ") : "No risk";
    (byRisk[key] ??= []).push(x);
  }
  const byDate = (a: Ballot, c: Ballot) => String(a.q.decideBy ?? "").localeCompare(String(c.q.decideBy ?? ""));
  const riskFirst = (a: string, c: string) => (a === "No risk" ? 1 : 0) - (c === "No risk" ? 1 : 0) || a.localeCompare(c);
  return Object.keys(byRisk).sort(riskFirst).map((riskKey) => {
    const list = byRisk[riskKey].slice().sort(byDate);
    const firstRisk = riskKey === "No risk" ? "" : riskKey.split(", ")[0].toLowerCase().replace(/ /g, "-");
    const approver = firstRisk ? b.approverFor(firstRisk, list[0].id) : "";
    const by: Record<string, Ballot[]> = {};
    for (const x of list) {
      const k = x.q.ballot || "unsorted";
      (by[k] ??= []).push(x);
    }
    return {
      key: riskKey,
      approver,
      total: list.length,
      ballots: Object.keys(by)
        .sort((a, c) => by[c].length - by[a].length || a.localeCompare(c))
        .map((k) => ({ key: k, label: cap(ballotName(b, k)), items: by[k].slice().sort(byDate) })),
    };
  });
}
