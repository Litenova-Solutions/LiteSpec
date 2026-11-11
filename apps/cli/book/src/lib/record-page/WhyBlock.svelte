<script lang="ts">
  import type { BookRecord } from "$lib/frame";
  import { glossary } from "$lib/record";
  import { whyPartsOf } from "./page-prose";

  // The Why sections: the reason fields and the Why explanation sections.
  let { r }: { r: BookRecord } = $props();
  const parts = $derived(whyPartsOf(r));
</script>

{#if parts.length}
  <div class="mb-[26px] border-l-[3px] border-highlight pl-4">
    {#each parts as p, i}
      <div class={i > 0 ? "mt-2.5" : ""}>
        <span class="mb-0.5 block text-xs font-semibold tracking-[0.05em] text-highlight-foreground uppercase">{p.label}</span>
        <div class="text-[15px] text-foreground" use:glossary={p.html}>{@html p.html}</div>
      </div>
    {/each}
  </div>
{/if}
