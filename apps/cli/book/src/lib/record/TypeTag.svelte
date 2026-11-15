<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { kindCode, layerBadgeClass } from "./record-codes";

  // The kind badge: the record type's short code, colored by its record layer.
  // Its accessible name is the type name, and hovering or focusing it explains
  // the type from LiteSpec's model page.
  type Props = { type: string; size?: "sm" | "lg"; class?: string };
  let { type, size = "sm", class: className = "" }: Props = $props();
  const name = $derived(book.typeName(type));
</script>

<span
  role="img"
  aria-label={name}
  data-explain={"type:" + type}
  class={cn(
    "inline-grid shrink-0 cursor-default place-items-center rounded-[2px] font-semibold tracking-normal select-none",
    size === "lg" ? "size-6 text-xs" : "size-5 text-[11px] leading-none",
    layerBadgeClass(book.typeLayer(type)),
    className,
  )}>{kindCode(type)}</span
>
