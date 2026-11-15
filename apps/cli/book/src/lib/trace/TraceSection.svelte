<script lang="ts">
  import { book, isScenarioType, pluralize, proofLabel, sourceLinkHref, stripStep } from "$lib/frame";
  import type { BookTest } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";
  import { buildTrace, implKey, kindWord, proofTestKey, railGroups, stepSuffixFor, traceRowKey } from "./trace-model";

  // The Trace section: the open record's trace where it crosses into another
  // record layer, then the code and tests that carry it. Same-layer records
  // stay in the outline and never read here.
  type Props = { id: string | null };
  let { id }: Props = $props();
  const model = $derived(id ? buildTrace(book, id) : null);
  const groups = $derived(id && model ? railGroups(book, model, id) : []);
  const traceIds = $derived(model ? model.rows.flatMap((row) => (row.kind === "rec" ? [row.id] : [])) : []);
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
  const show = $derived(groups.length > 0 || implShow || proofShow);

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

{#snippet recRow(rowId: string)}
  {@const step = id ? stepSuffixFor(book, rowId, id, traceIds) : ""}
  <a
    href={"#" + encodeURIComponent(rowId)}
    data-go={rowId}
    class="grid grid-cols-[20px_minmax(0,1fr)] gap-2 py-1.5 hover:bg-hover-wash"
  >
    <TypeTag type={book.typeOf(rowId)} />
    <span class="min-w-0 text-sm break-words">{book.title(rowId)}</span>
    {#if step}<span class="col-start-2 text-xs text-text-secondary">{step}</span>{/if}
  </a>
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
      <div class="mt-4 first-of-type:mt-0">
        <h3 class="mb-2 text-xs font-normal tracking-[0.32px] text-text-helper">{g.label}</h3>
        <ul class="m-0 list-none p-0">
          {#each g.rows as row, i (traceRowKey(row) + "#" + i)}
            <li>
              {#if row.kind === "rec"}
                {@render recRow(row.id)}
              {:else if row.kind === "gap"}
                {@render gapRow(row.gap)}
              {:else if row.kind === "more"}
                <p class="py-1 text-xs text-text-helper">
                  and {row.more} more {(row.more === 1 ? book.typeName(row.t) : book.pluralName(row.t) || row.t).toLowerCase()}
                </p>
              {/if}
            </li>
          {/each}
        </ul>
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
