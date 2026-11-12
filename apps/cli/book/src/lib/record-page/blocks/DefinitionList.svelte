<script lang="ts">
  import { book } from "$lib/frame";
  import type { Snippet } from "svelte";

  // A definition list of facts. Pairs carry rendered HTML and empty pairs drop
  // out; or compose DefinitionRow children for live content such as record lists.
  type Props = {
    pairs?: { label: string; html: string }[] | null;
    children?: Snippet;
  };
  let { pairs = null, children }: Props = $props();

  const rows = $derived((pairs ?? []).filter((p) => p.html !== undefined && p.html !== null && p.html !== ""));
</script>

{#if pairs ? rows.length > 0 : true}
  <dl class="grid grid-cols-[160px_minmax(0,1fr)] gap-x-5 gap-y-2.5 text-[0.90625rem]">
    {#if pairs}
      {#each rows as p (p.label)}
        <dt class="text-muted-foreground">{book.fieldLabel(p.label)}</dt>
        <dd class="m-0 min-w-0">{@html p.html}</dd>
      {/each}
    {:else}
      {@render children?.()}
    {/if}
  </dl>
{/if}
