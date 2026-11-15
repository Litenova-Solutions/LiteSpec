<script lang="ts">
  import type { Snippet } from "svelte";

  // A grid of fact tiles: each cell reads one labeled value on its own tile.
  type Props = { children: Snippet };
  let { children }: Props = $props();
</script>

<div class="mb-2 grid grid-cols-[repeat(auto-fit,minmax(12rem,1fr))] gap-px border border-border-subtle bg-border-subtle">{@render children()}</div>
