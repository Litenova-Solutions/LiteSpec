<script lang="ts">
  import { cn } from "$lib/frame";
  import { recordIcon } from "./record-icons";

  // The icon for a record type or mark.
  type Props = { name: string; class?: string };
  let { name, class: className = "" }: Props = $props();
  const Glyph = $derived(recordIcon(name));
</script>

<Glyph class={cn("size-4 shrink-0", className)} aria-hidden="true" />
