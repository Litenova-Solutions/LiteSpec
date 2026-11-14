<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { outlineState } from "./outline-state.svelte";
  import { RecordIcon } from "$lib/record";

  // One record row in the Security view: the icon, the title and the finding text.
  type Props = { id: string; suffix: string };
  let { id, suffix }: Props = $props();

  const selected = $derived(book.selected === id);
  const label = $derived.by(() => {
    return (
      book.typeName(book.typeOf(id)) + ", " + book.title(id) +
      (book.hasFailingProof(id) ? ", a test fails" : "") +
      (id !== book.selected && book.related.has(id) ? ", linked" : "")
    );
  });

  function choose(): void { outlineState.choose(id); }
  function key(e: KeyboardEvent): void {
    if (e.key === "Enter" || e.key === " ") { e.preventDefault(); outlineState.choose(id); }
  }
</script>

<div
  role="treeitem"
  tabindex={selected ? 0 : -1}
  aria-selected={selected}
  aria-label={label}
  aria-level={1}
  data-rk={id}
  onclick={choose}
  onkeydown={key}
  class={cn(
    "relative flex h-[46px] cursor-pointer items-start gap-[7px] rounded-sm px-2 py-[5px] text-[0.84375rem] select-none",
    selected ? "bg-highlight-subtle font-medium text-foreground" : "text-subtle-foreground hover:bg-muted hover:text-foreground",
  )}
>
  {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-highlight"></span>{/if}
  <span class="mt-0.5 grid flex-none text-muted-foreground"><RecordIcon name={book.typeOf(id)} class="size-4" /></span>
  <span class="min-w-0 flex-1">
    <span class="block overflow-hidden text-ellipsis whitespace-nowrap">{book.title(id)}</span>
    <span class="block overflow-hidden text-ellipsis whitespace-nowrap text-[0.71875rem] font-normal text-muted-foreground">{suffix}</span>
  </span>
</div>
