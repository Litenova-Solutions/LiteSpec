<script lang="ts">
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import StatusIcon from "$lib/components/book/StatusIcon.svelte";
  import TypeTag from "$lib/components/book/TypeTag.svelte";
  import { Separator } from "$lib/components/ui/separator";
  import { buildTrace } from "./rail";

  // The "From goal to behavior" trace: the chain above the record and what it
  // leads to below, grouped into the Intent and Behavior bands.
  let { id }: { id: string | null } = $props();
  const model = $derived.by(() => (id ? buildTrace(id) : null));
  const INDENT = ["pl-0", "pl-2.5", "pl-5", "pl-7.5"];
  const pad = (depth: number) => INDENT[Math.min(depth, 3)] ?? "pl-0";
</script>

{#if model && model.show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="From goal to behavior" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <Icon name="journey" class="size-3.5" />From goal to behavior
    </h3>
    {#each model.bands as band}
      {#if band.rows.length}
        <div class="mb-3.5 border-l-[3px] border-highlight py-1 pl-3 last:mb-0">
          <button
            type="button"
            data-explain="g:layer"
            class="mb-1 inline-block text-[10.5px] font-semibold tracking-[0.08em] text-highlight-foreground uppercase"
          >{band.name}</button>
          {#each band.rows as row}
            {#if row.kind === "rec"}
              <div class="{pad(row.depth)} py-0.5">
                {#if row.showPh}
                  <span class="ml-6 block text-[11.5px] text-muted-foreground">{row.phrase}</span>
                {/if}
                <span class="flex items-center gap-1">
                  <button
                    type="button"
                    data-go={row.id}
                    onmouseenter={() => (book.hover = row.id)}
                    onmouseleave={() => (book.hover = null)}
                    class="flex min-w-0 flex-1 items-center gap-1.5 rounded-sm px-1.5 py-1 text-left text-[13.5px] leading-snug hover:bg-muted {row.here
                      ? 'bg-muted font-semibold'
                      : ''}"
                  >
                    <Icon name={book.typeOf(row.id)} class="size-4 shrink-0 text-muted-foreground" />
                    <span class="min-w-0 flex-1 break-words">{book.title(row.id)}</span>
                  </button>
                  <TypeTag type={book.typeOf(row.id)} />
                  {#if book.get(row.id)}
                    <StatusIcon id={row.id} />
                  {/if}
                </span>
              </div>
            {:else if row.kind === "note"}
              <p class="{pad(row.depth)} flex items-center gap-1.5 py-1 text-[12.5px]">
                <Icon name="use-case" class="size-3.5 shrink-0 text-muted-foreground" /><span>{row.note}</span>
              </p>
            {:else if row.kind === "gap"}
              <p class="{pad(row.depth)} my-1 flex items-start gap-1.5 text-[12.5px]">
                <Icon name="missing" class="mt-0.5 size-3.5 shrink-0 text-warn-foreground" /><span>{row.gap}</span>
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
