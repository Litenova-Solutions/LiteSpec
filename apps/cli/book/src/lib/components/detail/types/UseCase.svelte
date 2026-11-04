<script lang="ts">
  import { base, book, cap } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { esc, inline } from "$lib/book/html";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import Jump from "../blocks/Jump.svelte";
  import Kv from "../blocks/Kv.svelte";
  import KvRow from "../blocks/KvRow.svelte";
  import Table from "../blocks/Table.svelte";
  import Tile from "../blocks/Tile.svelte";
  import Tiles from "../blocks/Tiles.svelte";
  import AccessBlock from "../shared/AccessBlock.svelte";
  import BuiltBlock from "../shared/BuiltBlock.svelte";
  import DataTouched from "../shared/DataTouched.svelte";
  import EffectsBlock from "../shared/EffectsBlock.svelte";
  import ResultCards from "../shared/ResultCards.svelte";
  import ScenarioBlock from "../shared/ScenarioBlock.svelte";
  import WhereBlock from "../shared/WhereBlock.svelte";
  import {
    browserReached, bulletsHtml, clsBadgeHtml, freshnessNoteHtml, idemTileOf,
    pagingRows, proseOf, tdHtml, typedFieldHtml,
  } from "../helpers";

  // A use case: access, data, input and output, effects, results and scenarios.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const res = $derived(
    book.resultsOf(id).filter((x) => book.get(base(x))).map((x) => {
      const q = book.get(base(x));
      const qf = q?.fields ?? {};
      return {
        id: x, title: q?.title ?? x, outcome: String(qf.outcome ?? ""), meaning: String(qf.meaning ?? ""),
        code: qf.code ? String(qf.code) : null, kind: /\.(forbidden|invalid-input)$/.test(x) ? "generic" : "",
      };
    }),
  );
  const okR = $derived(res.filter((x) => x.outcome === "success"));
  const noR = $derived(res.filter((x) => x.outcome !== "success"));
  const scs = $derived(book.scenariosOf(id).filter((s) => book.get(base(s))));
  const input = $derived(((f.input as Fields[] | undefined) ?? []) as Fields[]);
  const output = $derived(f.output as Fields | Fields[] | undefined);
  const outputGroups = $derived(
    output && !Array.isArray(output) && (output as Fields).include
      ? Object.keys((output as Fields).include as Record<string, Fields>)
      : [],
  );
  const outputRows = $derived(Array.isArray(output) ? (output as Fields[]) : []);
  const hasOutput = $derived(outputGroups.length > 0 || outputRows.length > 0 || !!pagingRows(f));
  const paging = $derived(pagingRows(f));
  const idem = $derived(idemTileOf(f));
  const audit = $derived((f.audit as Fields | undefined) ?? null);
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const exposure = $derived(proseOf(r, "exposure"));
  const dataCount = $derived(
    (((f.changes as string[] | undefined) ?? []).length) + (((f.reads as string[] | undefined) ?? []).length),
  );
</script>

{#if whatHappens}
  <Block blockKey="wh" heading="whatHappens"><Prose html={whatHappens} /></Block>
{/if}
{#if exposure}
  <Block blockKey="exp" heading="exposure"><Prose html={exposure} /></Block>
{/if}
<Jump
  items={[
    { key: "acc", label: "access", count: true },
    { key: "where", label: "offers", count: book.screensOf(id).length || browserReached(r) },
    { key: "built", label: "implements", count: ((r.impl as Fields[] | undefined) ?? []).length > 0 },
    { key: "data", label: "data", count: dataCount },
    { key: "in", label: "input", count: input.length },
    { key: "out", label: "output", count: hasOutput },
    { key: "eff", label: "effects", count: ((f.effects as Fields[] | undefined) ?? []).length },
    { key: "res", label: "Results", count: res.length },
    { key: "sc", label: "Scenarios", count: scs.length },
  ]}
/>
<AccessBlock {id} {f} />
<WhereBlock {id} {r} />
<BuiltBlock {r} />
<DataTouched {f} />
<Block blockKey="guar" heading="idempotency">
  <Tiles>
    {#if idem}
      <Tile k="idempotency" html={idem.value} hint={idem.hint} />
    {/if}
  </Tiles>
  {#if audit}
    <div class="mt-2.5">
      {#if audit.exempt}
        <Callout kind="quiet" icon="info"><b>Not audited.</b> {String(audit.exempt)}</Callout>
      {:else}
        <Callout kind="info" icon="book">
          <b>Audited</b> against {String(audit.target ?? "")}. {String(audit.level ?? "")}
        </Callout>
      {/if}
    </div>
  {/if}
</Block>
{#if f.background}
  <Block blockKey="bg" heading="background">
    <Prose html={inline(book, String(f.background), r.src ?? null)} />
  </Block>
{/if}
{#if input.length}
  <Block blockKey="in" heading="input" count={input.length}>
    <Table
      head={["Name", "Type", "Meaning", "Rules"]}
      rows={input.map((x) => [
        tdHtml(esc(String(x.name ?? "")) + (x.classification ? `<div class="mt-1">${clsBadgeHtml(x.classification)}</div>` : ""), "nm"),
        tdHtml(typedFieldHtml(x)),
        tdHtml(
          esc(String(x.meaning ?? "")) +
            (x.origin ? `<div class="mt-0.5 text-[11px] text-muted-foreground">from ${esc(String(x.origin))}</div>` : ""),
        ),
        tdHtml(bulletsHtml(x.rules)),
      ])}
    />
  </Block>
{/if}
{#if hasOutput}
  <Block blockKey="out" heading="output">
    {#if outputGroups.length}
      {@const include = (output as Fields).include as Record<string, Fields>}
      {#each outputGroups as k}
        {@const g = include[k] ?? {}}
        <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">
          {cap(k)}{" "}
          <FactBadge text={cap(String(g.classification ?? "internal"))} />{" "}
          {#if g.freshness}<span class="font-mono text-[11px] font-normal text-muted-foreground">{@html freshnessNoteHtml(g)}</span>{/if}
        </div>
        <Table
          head={["Name", "Meaning"]}
          rows={((g.fields as Fields[] | undefined) ?? []).map((x) => [tdHtml(esc(String(x.name ?? "")), "nm"), tdHtml(esc(String(x.meaning ?? "")))])}
        />
      {/each}
    {:else if outputRows.length}
      <Table
        head={["Name", "Meaning"]}
        rows={outputRows.map((x) => [tdHtml(esc(String(x.name ?? "")), "nm"), tdHtml(esc(String(x.meaning ?? "")))])}
      />
    {/if}
    {#if paging}
      <Kv>
        {#each paging as p}
          <KvRow label={p.label} html={p.html} />
        {/each}
      </Kv>
    {/if}
  </Block>
{/if}
<EffectsBlock {f} />
{#if res.length}
  <Block blockKey="res" heading="Results" count={res.length}>
    {#if okR.length}
      <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">Outcome: Success</div>
      <ResultCards items={okR} />
    {/if}
    {#if noR.length}
      <div class="mt-[18px] mb-2 text-[13px] font-semibold text-subtle-foreground">Outcome: Refusal</div>
      <ResultCards items={noR} />
    {/if}
  </Block>
{/if}
<ScenarioBlock {id} {scs} />
