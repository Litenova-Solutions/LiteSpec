<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import RowMarks from "./RowMarks.svelte";
  import { indentClass } from "./outline-rows";

  // One record row in the outline tree: the toggle, the icon, the title, the
  // child count and the row marks.
  type Props = {
    id: string;
    depth?: number;
    level?: number;
    kids?: number;
    open?: boolean;
    dim?: boolean;
  };
  let { id, depth = 1, level = 1, kids = 0, open = false, dim = false }: Props = $props();

  const selected = $derived(book.selected === id);
  const label = $derived.by(() => {
    return (
      book.typeName(book.typeOf(id)) + ", " + book.title(id) +
      (book.hasFailingProof(id) ? ", a test fails" : "") +
      (id !== book.selected && book.related.has(id) ? ", linked" : "")
    );
  });

  function choose(): void { void book.select(id); }
  function toggle(e: MouseEvent): void {
    e.stopPropagation();
    if (book.expanded[id]) book.expanded[id] = false;
    else void book.select(id);
  }
  function key(e: KeyboardEvent): void {
    if (e.key === "Enter" || e.key === " ") { e.preventDefault(); void book.select(id); }
  }
</script>

<div
  role="treeitem"
  tabindex={selected ? 0 : -1}
  aria-selected={selected}
  aria-label={label}
  aria-level={level}
  aria-expanded={kids ? open : undefined}
  data-rk={id}
  onclick={choose}
  onkeydown={key}
  class={cn(
    "relative flex h-7 cursor-pointer items-center gap-[7px] rounded-sm pr-2 text-[0.84375rem] leading-[1.35] select-none",
    indentClass(depth),
    selected ? "bg-highlight-subtle font-medium text-foreground" : "text-subtle-foreground hover:bg-muted hover:text-foreground",
    dim && "opacity-[0.55]",
  )}
>
  {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-highlight"></span>{/if}
  <button type="button" tabindex={-1} aria-hidden="true" onclick={toggle} class={cn("grid size-6 flex-none cursor-pointer place-items-center text-muted-foreground", !kids && "invisible")}>
    <RecordIcon name="chev" class={cn("size-4 motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
  </button>
  <span class="grid flex-none text-muted-foreground"><RecordIcon name={book.typeOf(id)} class="size-4" /></span>
  <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap" title={book.title(id)}>{book.title(id)}</span>
  {#if !open && kids > 0}<span class="font-mono text-[0.6875rem] text-muted-foreground">{kids}</span>{/if}
  <span class="flex flex-none items-center gap-[7px]"><RowMarks {id} /></span>
</div>
