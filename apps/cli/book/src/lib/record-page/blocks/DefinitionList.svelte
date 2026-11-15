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
  <dl class="grid grid-cols-[minmax(9rem,25%)_minmax(0,1fr)] border-t border-border-subtle text-sm [&_a]:text-link [&_a]:no-underline [&_a:hover]:underline">
    {#if pairs}
      {#each rows as p (p.label)}
        <dt class="border-b border-border-subtle py-3 pr-2 font-semibold">{book.fieldLabel(p.label)}</dt>
        <dd class="m-0 min-w-0 border-b border-border-subtle py-3">{@html p.html}</dd>
      {/each}
    {:else}
      {@render children?.()}
    {/if}
  </dl>
{/if}
