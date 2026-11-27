<script lang="ts">
  import { book, cn, hoverType } from "$lib/frame";
  import Hover from "./Hover.svelte";
  import { kindCode, typeBadgeClass } from "./record-codes";

  // The kind badge: the record type's short code, colored by what the type is.
  // Its accessible name is the type name, and hovering or focusing it explains
  // the type from LiteSpec's glossary. A click still opens the full
  // explanation through ExplainLayer.
  type Props = { type: string; size?: "sm" | "lg"; class?: string };
  let { type, size = "sm", class: className = "" }: Props = $props();
  const name = $derived(book.typeName(type));
</script>

<Hover hover={hoverType(book, type)}>
  <span
    role="img"
    aria-label={name}
    data-explain={"type:" + type}
    class={cn(
      "inline-grid shrink-0 cursor-default place-items-center rounded-[2px] font-semibold tracking-normal select-none",
      size === "lg" ? "size-6 text-xs" : "size-5 text-[11px] leading-none",
      typeBadgeClass(type, book.typeLayer(type)),
      className,
    )}>{kindCode(type)}</span
  >
</Hover>
