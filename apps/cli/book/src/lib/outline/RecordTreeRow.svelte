<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { outlineState } from "./outline-state.svelte";
  import { RecordIcon } from "$lib/record";
  import RowMarks from "./RowMarks.svelte";
  import { indentClass } from "./outline-rows";

  // One record row in the outline tree: the toggle, the icon, the title, the
  // child count and the row marks. The chevron only expands or collapses; the
  // row body selects. The best search hit carries a marker while a query is active.
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
    "relative flex h-8 cursor-pointer items-center gap-2 pr-2 text-sm leading-[1.35] select-none focus-visible:outline-2 focus-visible:outline-solid focus-visible:-outline-offset-2 focus-visible:outline-blue-600",
    indentClass(depth),
    selected ? "bg-blue-50 font-medium text-slate-900" : "text-slate-900 hover:bg-slate-100",
    dim && "opacity-[0.55]",
    best && !selected && "bg-blue-50/50",
  )}
>
  {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-blue-700"></span>{/if}
  <button type="button" tabindex={-1} aria-hidden="true" onclick={toggle} class={cn("grid size-6 flex-none cursor-pointer place-items-center text-slate-600", !kids && "invisible")}>
    <RecordIcon name="chev" class={cn("size-4 motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
  </button>
  <span class="grid flex-none text-slate-600"><RecordIcon name={book.typeOf(id)} class="size-4" /></span>
  <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap" title={book.title(id)}>{book.title(id)}</span>
  {#if !open && kids > 0}<span class="text-xs text-slate-600 tabular-nums">{kids}</span>{/if}
  <span class="flex flex-none items-center gap-2"><RowMarks {id} /></span>
</div>
