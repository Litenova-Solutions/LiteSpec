<script lang="ts">
  import { book } from "$lib/frame";

  // The record type in lower case; hovering explains it from LiteSpec's model page.
  type Props = { type: string };
  let { type }: Props = $props();
</script>

<span class="font-mono text-[0.6875rem] text-muted-foreground" data-explain={"type:" + type}>{book.typeTag(type)}</span>
