<script lang="ts">
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { cn } from "$lib/utils";
  import RowTail from "./RowTail.svelte";
  import { indentClass } from "./rows";

  // One record row in the navigation. The outline tree, the record list and
  // the security list share it; the variant picks the shape.
  const DOT = " " + String.fromCharCode(0xb7) + " ";
  let {
    id,
    variant = "tree",
    depth = 1,
    level = 1,
    kids = 0,
    open = false,
    dim = false,
    sub = null,
    icon = null,
    tail = true,
    titleText = null,
  }: {
    id: string;
    variant?: "tree" | "list" | "security";
    depth?: number;
    level?: number;
    kids?: number;
    open?: boolean;
    dim?: boolean;
    sub?: string | null;
    icon?: string | null;
    tail?: boolean;
    titleText?: string | null;
  } = $props();

  const selected = $derived(book.selected === id);
  const hovered = $derived(book.hover === id);
  const label = $derived.by(() => {
    return (
      book.typeName(book.typeOf(id)) + ", " + book.title(id) +
      (book.proofState(id) === "failing" ? ", a test fails" : "") +
      (id !== book.selected && book.related.has(id) ? ", linked" : "")
    );
  });
  const subText = $derived.by(() => {
    if (variant !== "list" || sub !== null) return sub ?? "";
    const f = book.fields(id);
    const firstLine = String(f.statement || f.summary || f.description || f.meaning || f.offering || "");
    const parent = book.parent(id);
    const home = parent ? book.title(parent) : (book.layerByKey[book.layerOf[id]]?.label ?? "");
    return book.typeName(book.typeOf(id)) + (home ? DOT + home : "") + (firstLine ? DOT + firstLine : "");
  });

  function choose() { void book.select(id); }
  function toggle(e: MouseEvent) {
    e.stopPropagation();
    if (book.expanded[id]) book.expanded[id] = false;
    else void book.select(id);
  }
  function key(e: KeyboardEvent) {
    if (e.key === "Enter" || e.key === " ") { e.preventDefault(); void book.select(id); }
  }
</script>

{#if variant === "tree"}
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
      "relative flex h-7 cursor-pointer items-center gap-[7px] rounded-sm pr-2 text-[13.5px] leading-[1.35] select-none",
      indentClass(depth),
      selected ? "bg-highlight-subtle font-medium text-foreground" : "text-subtle-foreground hover:bg-muted hover:text-foreground",
      hovered && !selected && "bg-highlight-subtle",
      dim && "opacity-[0.55]",
    )}
  >
    {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-highlight"></span>{/if}
    <button type="button" tabindex={-1} aria-hidden="true" onclick={toggle} class={cn("grid size-4 flex-none cursor-pointer place-items-center text-muted-foreground", !kids && "invisible")}>
      <Icon name="chev" class={cn("size-3.5 transition-transform duration-150", open && "rotate-90")} />
    </button>
    <span class="grid flex-none text-muted-foreground"><Icon name={book.typeOf(id)} class="size-4" /></span>
    <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap" title={book.title(id)}>{book.title(id)}</span>
    {#if !open && kids > 0}<span class="font-mono text-[11px] text-muted-foreground">{kids}</span>{/if}
    {#if tail}<span class="flex flex-none items-center gap-[7px]"><RowTail {id} /></span>{/if}
  </div>
{:else}
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
      "relative flex h-[46px] cursor-pointer items-start gap-[7px] rounded-sm px-2 py-[5px] text-[13.5px] select-none",
      selected ? "bg-highlight-subtle font-medium text-foreground" : "text-subtle-foreground hover:bg-muted hover:text-foreground",
      hovered && !selected && "bg-highlight-subtle",
    )}
  >
    {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-highlight"></span>{/if}
    <span class="mt-0.5 grid flex-none text-muted-foreground"><Icon name={icon ?? book.typeOf(id)} class="size-4" /></span>
    <span class="min-w-0 flex-1">
      <span class="block overflow-hidden text-ellipsis whitespace-nowrap">{titleText ?? book.title(id)}</span>
      <span class="block overflow-hidden text-ellipsis whitespace-nowrap text-[11.5px] font-normal text-muted-foreground">{subText}</span>
    </span>
    {#if tail}<span class="mt-0.5 flex flex-none items-center gap-[7px]"><RowTail {id} /></span>{/if}
  </div>
{/if}
