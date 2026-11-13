<script lang="ts">
  import type { Snippet } from "svelte";

  // A grid of facts: each cell reads one labeled value in aligned rows.
  type Props = { children: Snippet };
  let { children }: Props = $props();
</script>

<div class="mb-2 grid grid-cols-[minmax(0,1fr)] gap-x-4 sm:grid-cols-[180px_minmax(0,1fr)]">{@render children()}</div>
