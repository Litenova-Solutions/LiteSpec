<script lang="ts">
  import { book, isScenarioType, pluralize, proofLabel, sourceLinkHref, stripStep } from "$lib/frame";
  import type { BookTest } from "$lib/frame";
  import { hoverRecord, hoverRelation } from "$lib/frame/hover-text";
  import type { Hover as HoverData } from "$lib/frame/hover-text";
  import Hover from "$lib/record/Hover.svelte";
  import { RecordIcon, TypeTag } from "$lib/record";
  import RowGuides from "$lib/outline/RowGuides.svelte";
  import { guideFlags, indentClass } from "$lib/outline/outline-rows";
  import {
    buildTrace, groupChildrenByPhrase, implKey, kindWord, orphanBandCounts, orphanCarrierPhrase,
    orphanTypeCounts, proofTestKey, railGroups, relationTypeCounts, stepSuffixFor, traceRowKey, traceSectionVisible,
  } from "./trace-model";
  import type { RailGroup, RailNode, RailRelation, RailTypeCount } from "./trace-model";

  // The Trace section: the open record's trace where it crosses into another
  // record layer, then the code and tests that carry it. Same-layer records
  // stay in the outline and never read here. Each band opens with one
  // collapsed group row per relationship; each group opens to a tree of
  // records whose phrases read once as group rows above their records.
  type Props = { id: string | null };
  let { id }: Props = $props();

  // An opened list shows at most 6 children; the rest open in place.
  const CHILD_LIMIT = 6;

  const model = $derived(id ? buildTrace(book, id) : null);
  const groups = $derived(id && model ? railGroups(book, model, id) : []);
  const traceIds = $derived(model ? model.rows.flatMap((row) => (row.kind === "rec" ? [row.id] : [])) : []);
  // The trace walks links stored on each record, so a record the page has not
  // loaded yet would read as a gap. Load the walked records; the trace rebuilds
  // when they arrive and walks one step further.
  $effect(() => {
    const missing = traceIds.map((x) => stripStep(x) ?? x).filter((x) => x && book.get(x) === undefined);
    if (missing.length) void book.ensureRecords(missing);
    // A requirement whose keepers sit in an unloaded layer reads as a summary note; load
    // that layer so the trace lists the records that keep it.
    if (model?.rows.some((row) => row.kind === "note") && !book.isLayerLoaded("behavior")) void book.ensureLayer("behavior");
  });

  // Disclosure state, keyed by the open record so moving to another record starts fresh.
  let openRels = $state<Record<string, boolean>>({});
  let openNodes = $state<Record<string, boolean>>({});
  let openLists = $state<Record<string, boolean>>({});
  let openChild = $state<Record<string, boolean>>({});

  function relKey(g: RailGroup, r: RailRelation): string {
    return (id ?? "") + "|rel|" + g.label + "|" + r.key;
  }

  function orphanKey(g: RailGroup): string {
    return (id ?? "") + "|orph|" + g.label;
  }

  function nodeKey(node: RailNode): string {
    const rowId = node.row.kind === "rec" ? node.row.id : traceRowKey(node.row);
    return (id ?? "") + "|node|" + (node.carrierId ?? "") + ">" + rowId;
  }

  function listKey(kind: string, key: string): string {
    return (id ?? "") + "|list|" + kind + "|" + key;
  }

  function childKey(carrierId: string, phrase: string): string {
    return (id ?? "") + "|cgroup|" + carrierId + "|" + phrase;
  }

  function relOpen(g: RailGroup, r: RailRelation): boolean {
    return openRels[relKey(g, r)] ?? false;
  }

  function orphanOpen(g: RailGroup): boolean {
    return openRels[orphanKey(g)] ?? false;
  }

  function nodeOpen(node: RailNode): boolean {
    return openNodes[nodeKey(node)] ?? false;
  }

  function listOpen(kind: string, key: string): boolean {
    return openLists[listKey(kind, key)] ?? false;
  }

  function childOpen(carrierId: string, phrase: string): boolean {
    return openChild[childKey(carrierId, phrase)] ?? false;
  }

  // A link label's hover from LiteSpec's Links table, or nothing when the link
  // is containment, which the trace reads in its own words.
  function relationHover(field: string | null, dir: string, fromId: string) {
    if (!field || (dir !== "out" && dir !== "in")) return null;
    return hoverRelation(book, field, dir === "in" ? "in" : "out", fromId);
  }

  function countName(type: string, count: number): string {
    return (count === 1 ? book.typeName(type) : book.pluralName(type) || type).toLowerCase();
  }

  function countTotal(counts: RailTypeCount[]): number {
    return counts.reduce((n, c) => n + c.count, 0);
  }

  function breakdownText(counts: RailTypeCount[]): string {
    const total = countTotal(counts);
    if (!counts.length) return pluralize(total, "record");
    const parts = counts.map((c) => c.count + " " + countName(c.type, c.count));
    return pluralize(total, "record") + ": " + parts.join(", ");
  }

  function groupHover(label: string, field: string | null, dir: string, fromId: string, counts: RailTypeCount[]): HoverData {
    const base = relationHover(field, dir, fromId);
    const breakdown = breakdownText(counts);
    if (base) return { title: base.title, body: base.body ? base.body + " " + breakdown : breakdown };
    return { title: label, body: breakdown };
  }

  const rec = $derived(id ? book.get(id) : undefined);
  const impl = $derived(rec?.impl ?? []);
  const implShow = $derived(!!rec && !!rec.expect && (!!rec.expect.impl || impl.length > 0));
  const implLabel = $derived(book.layerByKey.implementation?.label ?? "Implementation");
  const proofShow = $derived(!!rec && !!rec.expect && !!rec.expect.proof);
  const proofGroup = $derived(book.layerByKey.proof?.label ?? "Proof");
  const scenarioKind = $derived(rec ? isScenarioType(rec.type) : false);
  const typeWord = $derived(rec ? book.typeName(rec.type).toLowerCase() : "");
  const sc = $derived(id ? book.scenariosOf(id).filter((x) => book.get(stripStep(x))) : []);
  const none = $derived(sc.filter((x) => !(book.get(stripStep(x))?.tests ?? []).length));
  const proved = $derived(sc.filter((x) => (book.get(stripStep(x))?.tests ?? []).length).slice(0, 6));
  const provedCount = $derived(sc.length - none.length);
  const summarySub = $derived(
    provedCount === sc.length
      ? sc.length === 1 ? "Proven" : "All proven"
      : provedCount === 0
        ? sc.length === 1 ? "Not proven" : "None proven"
        : provedCount + " of " + sc.length + " proven",
  );
  const tests = $derived(rec?.tests ?? []);
  const show = $derived(id ? traceSectionVisible(book, id) : false);

  function firstTest(x: string): BookTest | null {
    const list = book.get(stripStep(x))?.tests ?? [];
    return list[0] ?? null;
  }

  function testFile(t: BookTest): string {
    return String(t.path ?? t.file ?? "");
  }

  function fileName(path: string): string {
    return path.split("/").pop() || path;
  }

  function fileDir(path: string): string {
    return path.split("/").slice(0, -1).join("/");
  }

  function dirLine(path: string, line: number | null): string {
    const dir = fileDir(path);
    if (!dir) return "";
    return line === null ? dir : dir + ":" + line;
  }
</script>

{#snippet groupRow(open: boolean, label: string, count: number, hover: HoverData, depth: number, guides: boolean[], ontoggle: () => void)}
  <Hover {hover} side="left" block>
    <button
      type="button"
      aria-expanded={open}
      aria-label={label + ", " + pluralize(count, "record")}
      onclick={ontoggle}
      class="group relative flex h-7 w-full cursor-pointer items-center gap-2 pr-2 text-left text-xs text-text-secondary hover:bg-layer-hover {indentClass(depth)}"
    >
      <RowGuides {guides} />
      <span class="min-w-0 flex-1 truncate">{label}</span>
      <span class="shrink-0 tabular-nums">{count}</span>
      <span class="grid h-7 w-5 flex-none place-items-center">
        <RecordIcon name="chev" class="size-4 motion-safe:transition-transform motion-safe:duration-150 {open ? 'rotate-90' : ''}" />
      </span>
    </button>
  </Hover>
{/snippet}

{#snippet showMore(hidden: number, depth: number, onshow: () => void)}
  <div class={indentClass(depth)}>
    <button
      type="button"
      onclick={onshow}
      class="flex h-7 cursor-pointer items-center text-left text-sm text-link hover:underline"
    >
      Show {hidden} more
    </button>
  </div>
{/snippet}

{#snippet recordNode(node: RailNode, depth: number, guides: boolean[], lasts: readonly boolean[], isLast: boolean)}
  {#if node.row.kind === "rec"}
    {@const rowId = node.row.id}
    {@const step = id ? stepSuffixFor(book, rowId, id, traceIds) : ""}
    {@const kids = node.children}
    {@const isOpen = nodeOpen(node)}
    <Hover hover={hoverRecord(book, rowId)} side="left" block>
      <div class="group relative flex h-7 items-center gap-2 pr-2 {indentClass(depth)} hover:bg-layer-hover">
        <RowGuides {guides} />
        <TypeTag type={book.typeOf(rowId)} />
        <span class="flex min-w-0 flex-1 items-center gap-1">
          <a href={"#" + encodeURIComponent(rowId)} data-go={rowId} class="min-w-0 flex-1 truncate text-sm">{book.title(rowId)}</a>
          {#if step}<span class="shrink-0 text-xs text-text-secondary">{step}</span>{/if}
        </span>
        {#if kids.length}
          <button
            type="button"
            aria-expanded={isOpen}
            aria-label={(isOpen ? "Hide records under " : "Show records under ") + book.title(rowId)}
            onclick={() => (openNodes[nodeKey(node)] = !isOpen)}
            class="grid h-7 w-5 shrink-0 cursor-pointer place-items-center text-text-secondary"
          >
            <RecordIcon name="chev" class="size-4 motion-safe:transition-transform motion-safe:duration-150 {isOpen ? 'rotate-90' : ''}" />
          </button>
        {/if}
      </div>
    </Hover>
    {#if isOpen && kids.length}
      {@render childGroups(kids, rowId, depth, lasts, isLast)}
    {/if}
  {:else if node.row.kind === "more"}
    <div class="relative flex h-7 items-center {indentClass(depth)}">
      <RowGuides {guides} />
      <p class="m-0 text-xs text-text-helper">
        and {node.row.more} more {countName(node.row.t, node.row.more)}
      </p>
    </div>
  {/if}
{/snippet}

{#snippet nodeList(nodes: RailNode[], depth: number, lasts: readonly boolean[], groupLast: boolean, listKind: string, listBase: string)}
  {@const showAll = listOpen(listKind, listBase)}
  {@const shown = showAll ? nodes : nodes.slice(0, CHILD_LIMIT)}
  {@const hidden = nodes.length - shown.length}
  <ul class="m-0 list-none p-0">
    {#each shown as node, i (traceRowKey(node.row) + "#" + i)}
      {@const last = groupLast && hidden === 0 && i === shown.length - 1}
      {@const g = guideFlags(lasts, last, depth)}
      <li>{@render recordNode(node, depth, g, lasts, last)}</li>
    {/each}
  </ul>
  {#if hidden > 0}
    {@render showMore(hidden, depth, () => (openLists[listKey(listKind, listBase)] = true))}
  {/if}
{/snippet}

{#snippet childGroups(kids: RailNode[], carrierId: string, depth: number, lasts: readonly boolean[], isLast: boolean)}
  {@const lastsChild = [...lasts, isLast]}
  {@const cgroups = groupChildrenByPhrase(kids)}
  {#each cgroups as g, gi (g.phrase)}
    {@const groupLast = gi === cgroups.length - 1}
    {@const gGuides = guideFlags(lastsChild, false, depth + 1)}
    {@const counts = orphanTypeCounts(book, g.nodes)}
    {@const total = countTotal(counts)}
    {@const hover = groupHover(g.label, g.field, g.dir, carrierId, counts)}
    {@const open = childOpen(carrierId, g.phrase)}
    {@render groupRow(open, g.label, total, hover, depth + 1, gGuides, () => (openChild[childKey(carrierId, g.phrase)] = !open))}
    {#if open}
      {@render nodeList(g.nodes, depth + 1, lastsChild, groupLast, "group", carrierId + "|" + g.phrase)}
    {/if}
  {/each}
{/snippet}

{#snippet gapRow(text: string)}
  <p class="flex items-start gap-2 py-1.5 text-sm text-warning-ink">
    <RecordIcon name="alert" class="mt-0.5 size-4 shrink-0" />
    <span class="min-w-0 break-words">{text}</span>
  </p>
{/snippet}

{#snippet fileCells(badge: string, title: string, sub: string, loc: string)}
  <TypeTag type={badge} />
  <span class="min-w-0 text-sm break-words">{title}</span>
  <span class="col-start-2 text-xs text-text-secondary">{sub}</span>
  {#if loc}<span class="col-start-2 font-mono text-xs break-all text-text-secondary">{loc}</span>{/if}
{/snippet}

{#snippet fileRow(badge: string, title: string, href: string | null, sub: string, loc: string)}
  {#if href}
    <a {href} class="grid grid-cols-[20px_minmax(0,1fr)] gap-2 py-1.5 hover:bg-hover-wash">
      {@render fileCells(badge, title, sub, loc)}
    </a>
  {:else}
    <div class="grid grid-cols-[20px_minmax(0,1fr)] gap-2 py-1.5">
      {@render fileCells(badge, title, sub, loc)}
    </div>
  {/if}
{/snippet}

{#if show}
  <section aria-labelledby="rail-trace-h" class="mb-8 last:mb-0">
    <h2 id="rail-trace-h" class="mb-4 text-sm font-semibold">Trace</h2>
    {#each groups as g (g.label)}
      {@const lasts = [false]}
      {@const hasThrough = g.orphans.length > 0}
      <div class="mt-4 first-of-type:mt-0">
        <h3 class="flex h-8 items-center px-3 text-xs font-semibold text-text-secondary">{g.label}</h3>
        {#each g.relations as r, ri (r.key)}
          {@const counts = relationTypeCounts(book, r)}
          {@const total = countTotal(counts)}
          {@const hover = groupHover(r.label, r.field, r.dir, id ?? "", counts)}
          {@const open = relOpen(g, r)}
          {@const groupLast = ri === g.relations.length - 1 && !hasThrough}
          {@const gGuides = guideFlags(lasts, false, 1)}
          {@render groupRow(open, r.label, total, hover, 1, gGuides, () => (openRels[relKey(g, r)] = !open))}
          {#if open}
            {@render nodeList(r.nodes, 1, lasts, groupLast, "rel", g.label + "|" + r.key)}
          {/if}
        {/each}
        {#if hasThrough}
          {@const ocounts = orphanBandCounts(book, g.orphans, g.label)}
          {@const ototal = countTotal(ocounts)}
          {@const olabel = "Through " + orphanCarrierPhrase(book, g.orphans)}
          {@const ohover = { title: olabel, body: breakdownText(ocounts) }}
          {@const oopen = orphanOpen(g)}
          {@const oguides = guideFlags(lasts, false, 1)}
          {@render groupRow(oopen, olabel, ototal, ohover, 1, oguides, () => (openRels[orphanKey(g)] = !oopen))}
          {#if oopen}
            {@render nodeList(g.orphans, 1, lasts, true, "orph", g.label)}
          {/if}
        {/if}
        {#if g.gaps.length}
          <ul class="m-0 mt-2 list-none p-0">
            {#each g.gaps as gap, i (traceRowKey(gap) + "#" + i)}
              <li>{@render gapRow(gap.gap)}</li>
            {/each}
          </ul>
        {/if}
      </div>
    {/each}
    {#if implShow}
      <div class="mt-4 first-of-type:mt-0">
        <h3 class="mb-2 text-xs font-normal tracking-[0.32px] text-text-helper">{implLabel}</h3>
        {#if !impl.length}
          {@render gapRow("No code carries this mark.")}
        {:else}
          <ul class="m-0 list-none p-0">
            {#each impl as x (implKey(x))}
              {@const path = String(x.path ?? "")}
              {@const line = typeof x.line === "number" ? x.line : null}
              <li>
                {@render fileRow("code", fileName(path) || path, sourceLinkHref(book, path, line), kindWord(String(x.kind ?? "")), dirLine(path, line))}
              </li>
            {/each}
          </ul>
        {/if}
      </div>
    {/if}
    {#if proofShow}
      <div class="mt-4 first-of-type:mt-0">
        <h3 class="mb-2 text-xs font-normal tracking-[0.32px] text-text-helper">{proofGroup}</h3>
        {#if sc.length}
          <ul class="m-0 list-none p-0">
            <li>
              <div class="grid grid-cols-[20px_minmax(0,1fr)] gap-2 py-1.5">
                <RecordIcon name="minus" class="m-0.5 size-4 shrink-0" />
                <span class="min-w-0 text-sm break-words">{pluralize(sc.length, "scenario")}</span>
                <span class="col-start-2 text-xs text-text-secondary">{summarySub}</span>
              </div>
            </li>
            {#each proved as x (x)}
              {@const t = firstTest(x)}
              {#if t}
                {@const path = testFile(t)}
                {@const tline = typeof t.line === "number" ? t.line : null}
                {@const thref = sourceLinkHref(book, path, tline)}
                {@const state = book.get(stripStep(x))?.proof?.state ?? null}
                <li>
                  <a
                    href={"#" + encodeURIComponent(x)}
                    data-go={x}
                    class="grid grid-cols-[20px_minmax(0,1fr)] gap-2 py-1.5 hover:bg-hover-wash"
                  >
                    <TypeTag type="scenario" />
                    <span class="min-w-0 text-sm break-words">{book.title(x)}</span>
                    {#if state}<span class="col-start-2 text-xs text-text-secondary">{proofLabel(state)}</span>{/if}
                  </a>
                  {#if path}
                    <div class="pr-2 pb-1.5 pl-7">
                      {#if thref}
                        <a href={thref} class="font-mono text-xs break-all text-link hover:underline">{String(t.name || fileName(path))}</a>
                      {:else}
                        <span class="font-mono text-xs break-all text-text-secondary">{String(t.name || fileName(path))}</span>
                      {/if}
                    </div>
                  {/if}
                </li>
              {/if}
            {/each}
          </ul>
        {:else if scenarioKind}
          {@render gapRow("No scenarios.")}
        {:else if !tests.length}
          {@render gapRow("No test proves this " + typeWord + ".")}
        {:else}
          <ul class="m-0 list-none p-0">
            {#each tests as t (proofTestKey(t))}
              {@const path = testFile(t)}
              {@const tline = typeof t.line === "number" ? t.line : null}
              <li>
                {@render fileRow("test", String(t.name || fileName(path) || path), sourceLinkHref(book, path, tline), (t.browser ? "Browser test" : "Test") + (t.confirmed ? "" : ", not confirmed"), dirLine(path, tline))}
              </li>
            {/each}
          </ul>
        {/if}
      </div>
    {/if}
  </section>
{/if}
