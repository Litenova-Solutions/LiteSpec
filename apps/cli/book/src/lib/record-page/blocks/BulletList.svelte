<script lang="ts">
  // A bullet list, or a dash when there is nothing to list.
  type Props = { items: (string | null | undefined)[] | string };
  let { items }: Props = $props();
  const list = $derived(typeof items === "string" ? (items ? [items] : []) : (items ?? []).filter((x): x is string => !!x));
</script>

{#if list.length}
  <ul class="m-0 list-disc pl-4 text-sm text-slate-600">
    {#each list as x (x)}<li class="mt-1 first:mt-0">{x}</li>{/each}
  </ul>
{:else}
  <span class="font-mono text-xs text-slate-600">&ndash;</span>
{/if}
