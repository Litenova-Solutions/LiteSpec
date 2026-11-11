<script lang="ts">
  import { book, capitalize, esc, formatLabel, stripStep } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { SourceLink } from "$lib/record";
  import type { Fact } from "./type-facts";
  import { codeCls, inlineRefHtml } from "./page-html";
  import { teamValue } from "./page-values";

  // The field table: the type's relations and facts, then the plain fields.
  let { id, r, facts }: { id: string; r: BookRecord; facts: Fact[] } = $props();

  interface FieldRow {
    label: string;
    html: string;
    route?: boolean;
  }

  const routePath = $derived.by(() => {
    if (r.type !== "screen") return null;
    const route = r.route as RecordFields | undefined;
    if (!route) return null;
    const p = route.file ?? route.path;
    return p ? String(p) : null;
  });

  const rows = $derived.by(() => {
    const out: FieldRow[] = [];
    const add = (label: string, value: unknown) => {
      if (value !== undefined && value !== null && value !== "") out.push({ label, html: String(value) });
    };
    const linkList = (list: unknown[]) => list.map((x) => inlineRefHtml(stripStep(String(x)))).join(", ");
    const f = r.fields ?? {};
    const flat = (v: unknown) => (Array.isArray(v) ? v : [v]) as unknown[];
    if (f.affects) add("Affects", linkList(flat(f.affects)));
    if (f.affectsSegment) add("Affects segment", esc(f.affectsSegment));
    if (f.actor) add("Actor", inlineRefHtml(stripStep(String(f.actor))));
    if (r.type === "actor") {
      if (f.kind) add("Kind", esc(capitalize(String(f.kind))));
      const team = teamValue(f);
      if (team !== undefined) add("Team", team ? "Yes" : "No");
    }
    if (f.solves) add("Solves", linkList(flat(f.solves)));
    if (f.advances) add("Advances", linkList(flat(f.advances)));
    if (f.serves) add("Serves", linkList(flat(f.serves).map((x) => String(x).split("#")[0])));
    const parent = book.parent(id);
    if (parent) add("Parent", inlineRefHtml(parent));
    if (r.type === "permission" && Array.isArray(f.risk) && f.risk.length)
      add("Risk", f.risk.map((x: unknown) => esc(capitalize(formatLabel(x)))).join("; "));
    if (r.type === "problem") {
      if (f.costToday) add("Cost today", esc(f.costToday));
      if (f.workaroundToday) add("Workaround today", esc(f.workaroundToday));
      if (r.evidenceStrength) add("Evidence strength", esc(capitalize(String(r.evidenceStrength))));
    }
    if (r.type === "journey" && f.finishedWhen) add("Finished when", esc(f.finishedWhen));
    if (r.type === "screen") {
      if (f.address || f.host) add("Address", `<span class="${codeCls}">${esc(String(f.address || f.host))}</span>`);
      if (f.entryPoint) add("Entry point", inlineRefHtml(stripStep(String(f.entryPoint))));
      if (f.floorplan) add("Floorplan", esc(capitalize(formatLabel(f.floorplan))));
      if (r.route) {
        if (routePath) out.push({ label: "Route", html: "", route: true });
        else add("Route", "No page carries this screen");
      } else if (r.captureState) add("Capture state", esc(String(r.captureState)));
    }
    for (const x of facts) if (x.text && !out.some((row) => row.label === x.key)) add(x.key, esc(x.text));
    const skip: Record<string, number> = {
      statement: 1, summary: 1, description: 1, offering: 1, meaning: 1, situation: 1, whyItMatters: 1,
      purpose: 1, whatHappens: 1, affects: 1, affectsSegment: 1, actor: 1, solves: 1, advances: 1,
      serves: 1, risk: 1, costToday: 1, workaroundToday: 1, finishedWhen: 1,
      address: 1, host: 1, entryPoint: 1, floorplan: 1, kind: r.type === "actor" ? 1 : 0,
      questions: 1, evidence: 1, lifecycle: 1, plannedFor: 1, refusedBecause: 1, provenance: 1,
      decider: 1, decideBy: 1, owner: 1, approvedBy: 1, approvedOn: 1, approvedHash: 1, reviewBy: 1,
    };
    for (const key of Object.keys(f)) {
      if (skip[key]) continue;
      const v = f[key];
      if (typeof v === "string" && v) add(book.fieldLabel(key), esc(v));
      else if (typeof v === "number") add(book.fieldLabel(key), esc(v));
      else if (typeof v === "boolean") add(book.fieldLabel(key), v ? "Yes" : "No");
    }
    return out;
  });
</script>

{#if rows.length}
  <table class="mb-7 w-full border-collapse text-[14.5px]">
    <tbody class="[&_tr:last-child_th]:border-b-0 [&_tr:last-child_td]:border-b-0">
      {#each rows as row}
        <tr>
          <th class="w-[180px] border-b border-border py-2 pr-4 text-left align-top font-medium text-muted-foreground">
            {row.label}
          </th>
          <td class="border-b border-border py-2 align-top break-words text-foreground">
            {#if row.route && routePath}
              <SourceLink src={{ path: routePath, line: 1 }} />
            {:else}
              {@html row.html}
            {/if}
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
{/if}
