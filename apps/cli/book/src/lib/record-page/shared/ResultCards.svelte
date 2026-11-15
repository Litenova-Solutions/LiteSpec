<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { codeCls } from "../page-html";
  import Fact from "../blocks/Fact.svelte";

  // Results as aligned rows: the outcome icon, the title, the meaning and the code.
  type Props = {
    items: { id?: string | null; title: string; outcome: string; meaning: string; code?: string | null; kind?: string }[];
  };
  let { items }: Props = $props();
</script>

{#if items?.length}
  <div>
    {#each items as x, i (x.id ?? i)}
      {@const ok = x.outcome === "success"}
      <div class="grid grid-cols-[180px_minmax(0,1fr)] gap-x-4 gap-y-1 border-b border-slate-200 py-2.5">
        <div class="flex items-start gap-2 text-sm font-medium">
          <RecordIcon name={ok ? "passing" : "x"} class="mt-0.5 size-3.5 shrink-0" />
          {#if x.id && book.has(x.id)}
            <button type="button" class="text-left underline decoration-slate-300 underline-offset-2 hover:decoration-slate-900" data-go={x.id}>
              {x.title}
            </button>
          {:else}
            <span>{x.title}</span>
          {/if}
          {#if x.kind === "generic"}{" "}<Fact text="Generic refusal" />{/if}
        </div>
        <p class="m-0 text-sm leading-snug text-slate-600">{x.meaning}</p>
        {#if x.code}<div class="col-start-2 mt-0.5"><span class={codeCls}>{x.code}</span></div>{/if}
      </div>
    {/each}
  </div>
{/if}
