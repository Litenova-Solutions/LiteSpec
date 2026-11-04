<script lang="ts">
  import { cn } from "$lib/utils";
  import { iconFor } from "./icons";
  let { name, class: className = "" }: { name: string; class?: string } = $props();
  const Glyph = $derived(iconFor(name));
</script>

<Glyph class={cn("size-4 shrink-0", className)} aria-hidden="true" />
