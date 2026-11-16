<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { hoverRecord } from "$lib/frame/hover-text";
  import { outlineState } from "./outline-state.svelte";
  import { RecordIcon, TypeTag } from "$lib/record";
  import Hover from "$lib/record/Hover.svelte";
  import FilterMatch from "./FilterMatch.svelte";
  import RowGuides from "./RowGuides.svelte";
  import RowMarks from "./RowMarks.svelte";
  import { belowSummary, indentClass, isFindingId, navTitle } from "./outline-rows";

  // One record row in the outline tree: the tree guide lines, the kind
  // badge, the title, the row marks and, for a record with children, the
  // toggle at the right end. At one depth every badge starts at the same x;
  // leaf rows keep no chevron space. The chevron only expands or collapses;
  // the row body selects. The best search hit carries a marker while a query
  // is active. The row shows no counts; hovering or focusing it shows the
  // record's summary first. The row's accessible name holds the full title
  // with the records below it and their completion. A finding row reads its
  // message as its title with the finding badge, and choosing it opens its
  // subject record at that finding.
  type Props = {
    id: string;
    depth?: number;
    level?: number;
    kids?: number;
    open?: boolean;
    dim?: boolean;
    best?: boolean;
    guides?: boolean[];
    branch?: boolean;
  };
  let { id, depth = 1, level = 1, kids = 0, open = false, dim = false, best = false, guides = [], branch = false }: Props = $props();

  const finding = $derived(isFindingId(id));
  const type = $derived(finding ? "finding" : book.typeOf(id));
  const title = $derived(finding ? navTitle(book, id) : book.title(id));
  const selected = $derived(book.selected === id);
  const hover = $derived(hoverRecord(book, id));
  const label = $derived.by(() => {
    if (finding) return "Finding, " + title;
    return (
      book.typeName(book.typeOf(id)) + ", " + title +
      ", " + belowSummary(book, id) +
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

<Hover {hover} side="right" block>
  {#snippet children()}
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
        "group relative flex h-7 cursor-pointer items-center gap-2 pr-2 text-sm leading-[1.35] select-none",
        indentClass(depth),
        selected ? "bg-layer-selected font-semibold text-foreground" : "text-foreground hover:bg-layer-hover",
        dim && "opacity-[0.55]",
        best && !selected && "bg-hover-wash",
      )}
    >
      {#if selected}<span aria-hidden="true" class="absolute top-0 bottom-0 left-0 w-1 bg-interactive"></span>{/if}
      <RowGuides {guides} strong={branch} />
      <TypeTag type={type} />
      <span class="min-w-0 flex-1 truncate"><FilterMatch text={title} /></span>
      {#if !finding}<RowMarks {id} />{/if}
      {#if kids}
        <button type="button" tabindex={-1} aria-hidden="true" onclick={toggle} class="grid h-7 w-5 flex-none cursor-pointer place-items-center text-text-secondary">
          <RecordIcon name="chev" class={cn("size-4 motion-safe:transition-transform motion-safe:duration-150", open && "rotate-90")} />
        </button>
      {/if}
    </div>
  {/snippet}
</Hover>
