<script lang="ts">
  import { cn } from "$lib/frame";
  import { iconFor } from "./record-icons";
  let { name, class: className = "" }: { name: string; class?: string } = $props();
  const Glyph = $derived(iconFor(name));
</script>

<Glyph class={cn("size-4 shrink-0", className)} aria-hidden="true" />
