<script lang="ts">
  import { outlineState } from "./outline-state.svelte";

  // The navigation filter's match inside a row title: the first occurrence of
  // the filter text reads on a wash, so the reader sees why the row stayed.
  // Without a filter, or without a match, the title reads plain.
  type Props = { text: string };
  let { text }: Props = $props();
  const q = $derived(outlineState.navFilter.trim().toLowerCase());
  const at = $derived(q ? text.toLowerCase().indexOf(q) : -1);
</script>

{#if at >= 0}{text.slice(0, at)}<mark class="bg-tag-yellow text-inherit">{text.slice(at, at + q.length)}</mark>{text.slice(at + q.length)}{:else}{text}{/if}
