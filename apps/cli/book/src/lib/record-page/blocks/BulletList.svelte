<script lang="ts">
  // A bullet list, or a dash when there is nothing to list.
  let { items }: { items: (string | null | undefined)[] | string } = $props();
  const list = $derived(typeof items === "string" ? (items ? [items] : []) : (items ?? []).filter((x): x is string => !!x));
</script>

{#if list.length}
  <ul class="m-0 list-disc pl-4 text-[13.5px] text-subtle-foreground">
    {#each list as x}<li class="mt-0.5 first:mt-0">{x}</li>{/each}
  </ul>
{:else}
  <span class="font-mono text-[11px] text-muted-foreground">&ndash;</span>
{/if}
