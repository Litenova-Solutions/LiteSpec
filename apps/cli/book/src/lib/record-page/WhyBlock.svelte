<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { glossaryLinks } from "$lib/record";
  import { whyPartsOf } from "./page-prose";

  // The Why sections: the reason fields and the Why explanation sections.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const parts = $derived(whyPartsOf(book, r));
</script>

{#if parts.length}
  <div class="mb-6 border-l-[3px] border-highlight py-1 pl-4">
    {#each parts as p, i (i)}
      <div class={i > 0 ? "mt-2" : ""}>
        <span class="mb-1 block text-xs font-semibold tracking-[0.05em] text-highlight-foreground uppercase">{p.label}</span>
        <div class="text-sm text-slate-900" use:glossaryLinks={p.html}>{@html p.html}</div>
      </div>
    {/each}
  </div>
{/if}
