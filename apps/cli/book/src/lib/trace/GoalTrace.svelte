<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";
  import { Separator } from "$lib/ui/separator";
  import { buildTrace, traceRowKey } from "./trace-model";
  import type { TraceModel } from "./trace-model";

  // The "From goal to behavior" trace: the chain above the record and what it
  // leads to below, grouped into the Intent and Behavior bands. The rail passes
  // the model it already built; a standalone use builds it from the id.
  type Props = { id: string | null; trace?: TraceModel | null; topRule?: boolean };
  let { id, trace = undefined, topRule = true }: Props = $props();
  const model = $derived(trace !== undefined ? trace : id ? buildTrace(book, id) : null);
  const INDENT = ["pl-0", "pl-2", "pl-4", "pl-6"];
  const pad = (depth: number) => INDENT[Math.min(depth, 3)] ?? "pl-0";
</script>

{#snippet rowBody(rid: string)}
  <RecordIcon name={book.typeOf(rid)} class="size-4 shrink-0 text-slate-600" />
  <span class="min-w-0 flex-1 break-words">{book.title(rid)}</span>
{/snippet}

{#if model && model.show}
  {#if topRule}<Separator class="h-0.5 bg-slate-900" />{/if}
  <section aria-label="From goal to behavior" class={topRule ? "mt-3 mb-8" : "mb-8"}>
    <h3 class="mb-2 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <RecordIcon name="flow" class="size-3.5" />From goal to behavior
    </h3>
    {#each model.bands as band (band.name)}
      {#if band.rows.length}
        <div class="mb-4 border-l-[3px] border-slate-300 py-1 pl-3.5 last:mb-0">
          <button
            type="button"
            data-explain="g:layer"
            class="mb-1 inline-block text-xs font-semibold tracking-[0.08em] text-slate-600 uppercase"
          >{band.name}</button>
          {#each band.rows as row, i (traceRowKey(row) + "#" + i)}
            {#if row.kind === "rec"}
              <div class="{pad(row.depth)} py-1">
                {#if row.showPh}
                  <span class="ml-6 block text-xs text-slate-600">{row.phrase}</span>
                {/if}
                <span class="flex items-center gap-1">
                  {#if row.here}
                    <span
                      aria-current="true"
                      class="flex min-w-0 flex-1 items-center gap-2 rounded-sm border-l-2 border-blue-700 bg-blue-50 px-2 py-1 text-left text-sm leading-snug font-semibold"
                    >
                      {@render rowBody(row.id)}
                    </span>
                  {:else}
                    <a
                      href={"#" + encodeURIComponent(row.id)}
                      data-go={row.id}
                      class="flex min-w-0 flex-1 items-center gap-2 rounded-sm border-l-2 border-transparent px-2 py-1 text-left text-sm leading-snug hover:bg-slate-100"
                    >
                      {@render rowBody(row.id)}
                    </a>
                  {/if}
                  <TypeTag type={book.typeOf(row.id)} />
                </span>
              </div>
            {:else if row.kind === "note"}
              <p class="{pad(row.depth)} flex items-center gap-2 py-1 text-xs">
                <RecordIcon name="use-case" class="size-3.5 shrink-0 text-slate-600" /><span>{row.note}</span>
              </p>
            {:else if row.kind === "gap"}
              <p class="{pad(row.depth)} my-1 flex items-start gap-2 text-xs">
                <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0 text-warn-foreground" /><span>{row.gap}</span>
              </p>
            {:else}
              <p class="{pad(row.depth)} ml-6 py-1 text-xs text-slate-600">
                and {row.more} more {(row.more === 1 ? book.typeName(row.t) : book.pluralName(row.t) || row.t).toLowerCase()}
              </p>
            {/if}
          {/each}
        </div>
      {/if}
    {/each}
  </section>
{/if}
