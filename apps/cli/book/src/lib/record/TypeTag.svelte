<script lang="ts">
  import { book } from "$lib/frame";

  // The record type in lower case; hovering explains it from LiteSpec's model page.
  let { type }: { type: string } = $props();
</script>

<span class="font-mono text-[11px] text-muted-foreground" data-explain={"type:" + type}>{book.typeTag(type)}</span>
