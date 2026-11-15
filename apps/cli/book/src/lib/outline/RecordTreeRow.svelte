<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { outlineState } from "./outline-state.svelte";
  import { RecordIcon, TypeTag } from "$lib/record";
  import RowMarks from "./RowMarks.svelte";
  import { indentClass } from "./outline-rows";

  // One record row in the outline tree: the toggle, the kind badge, the
  // title, the child count and the row marks. The chevron only expands or
  // collapses; the row body selects. The best search hit carries a marker
  // while a query is active. The row's accessible name holds the full title.
  type Props = {
    id: string;
    depth?: number;
    level?: number;
    kids?: number;
    open?: boolean;
    dim?: boolean;
    best?: boolean;
  };
  let { id, depth = 1, level = 1, kids = 0, open = false, dim = false, best = false }: Props = $props();

  const selected = $derived(book.selected === id);
  const label = $derived.by(() => {
    return (
      book.typeName(book.typeOf(id)) + ", " + book.title(id) +
      (book.hasFailingProof(id) ? ", a test fails" : "") +
      (id !== book.selected && book.related.has(id) ? ", linked" : "") +
      (best ? ", best match" : "")
    );
  });

  function choose(): void { outlineState.choose(id); }
  function toggle(e: MouseEvent): void {
    e.stopPropagation();
    book.expanded[id] = !open;
  }
  function key(e: KeyboardEvent): void {
    if (e.key === "Enter" || e.key === " ") { e.preventDefault(); outlineState.choose(id); }
  }
</script>

<div
  role="treeitem"
  tabindex={selected ? 0 : -1}
  aria-selected={selected}
  aria-current={best ? "true" : undefined}
  aria-label={label}
  aria-level={level}
  aria-expanded={kids ? open : undefined}
  data-rk={id}
  onclick={choose}
  onkeydown={key}
  class={cn(
    "relative flex h-8 cursor-pointer items-center gap-2 pr-4 text-sm leading-[1.35] select-none",
    indentClass(depth),
    selected ? "bg-layer-selected font-semibold text-foreground" : "text-foreground hover:bg-layer-hover",
    dim && "opacity-[0.55]",
    best && !selected && "bg-hover-wash",
  )}
>
  {#if selected}<span aria-hidden="true" class="absolute top-0 bottom-0 left-0 w-1 bg-interactive"></span>{/if}
  <button type="button" tabindex={-1} aria-hidden="true" onclick={toggle} class={cn("grid h-8 w-6 flex-none cursor-pointer place-items-center text-text-secondary", !kids && "invisible")}>
    <RecordIcon name="chev" class={cn("size-4 motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
  </button>
  <TypeTag type={book.typeOf(id)} />
  <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
  {#if !open && kids > 0}<span class={cn("text-xs tabular-nums", selected ? "text-text-secondary" : "text-text-helper")}>{kids}</span>{/if}
  <span class="flex flex-none items-center gap-2"><RowMarks {id} /></span>
</div>
