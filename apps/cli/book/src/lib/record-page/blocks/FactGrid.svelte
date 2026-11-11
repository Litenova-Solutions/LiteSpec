<script lang="ts">
  import type { Snippet } from "svelte";

  // A grid of facts: each cell reads one labeled value in aligned rows.
  let { children }: { children: Snippet } = $props();
</script>

<div class="mb-2 grid grid-cols-[180px_minmax(0,1fr)] gap-x-4">{@render children()}</div>
