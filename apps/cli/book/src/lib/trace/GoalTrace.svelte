<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";
  import { Separator } from "$lib/ui/separator";
  import { buildTrace, traceRowKey } from "./trace-model";

  // The "From goal to behavior" trace: the chain above the record and what it
  // leads to below, grouped into the Intent and Behavior bands.
  type Props = { id: string | null };
  let { id }: Props = $props();
  const model = $derived.by(() => (id ? buildTrace(book, id) : null));
  const INDENT = ["pl-0", "pl-2.5", "pl-5", "pl-7.5"];
  const pad = (depth: number) => INDENT[Math.min(depth, 3)] ?? "pl-0";
</script>

{#if model && model.show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="From goal to behavior" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <RecordIcon name="journey" class="size-3.5" />From goal to behavior
    </h3>
    {#each model.bands as band (band.name)}
      {#if band.rows.length}
        <div class="mb-3.5 border-l-[3px] border-highlight py-1 pl-3 last:mb-0">
          <button
            type="button"
            data-explain="g:layer"
            class="mb-1 inline-block text-[0.65625rem] font-semibold tracking-[0.08em] text-highlight-foreground uppercase"
          >{band.name}</button>
          {#each band.rows as row (traceRowKey(row))}
            {#if row.kind === "rec"}
              <div class="{pad(row.depth)} py-0.5">
                {#if row.showPh}
                  <span class="ml-6 block text-[0.71875rem] text-muted-foreground">{row.phrase}</span>
                {/if}
                <span class="flex items-center gap-1">
                  <button
                    type="button"
                    data-go={row.id}
                    class="flex min-w-0 flex-1 items-center gap-1.5 rounded-sm px-1.5 py-1 text-left text-[0.84375rem] leading-snug hover:bg-muted {row.here
                      ? 'bg-muted font-semibold'
                      : ''}"
                  >
                    <RecordIcon name={book.typeOf(row.id)} class="size-4 shrink-0 text-muted-foreground" />
                    <span class="min-w-0 flex-1 break-words">{book.title(row.id)}</span>
                  </button>
                  <TypeTag type={book.typeOf(row.id)} />
                </span>
              </div>
            {:else if row.kind === "note"}
              <p class="{pad(row.depth)} flex items-center gap-1.5 py-1 text-[0.78125rem]">
                <RecordIcon name="use-case" class="size-3.5 shrink-0 text-muted-foreground" /><span>{row.note}</span>
              </p>
            {:else if row.kind === "gap"}
              <p class="{pad(row.depth)} my-1 flex items-start gap-1.5 text-[0.78125rem]">
                <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0 text-warn-foreground" /><span>{row.gap}</span>
              </p>
            {:else}
              <p class="{pad(row.depth)} ml-6 py-0.5 text-xs text-muted-foreground">
                and {row.more} more {(row.more === 1 ? book.typeName(row.t) : book.pluralName(row.t) || row.t).toLowerCase()}
              </p>
            {/if}
          {/each}
        </div>
      {/if}
    {/each}
  </section>
{/if}
