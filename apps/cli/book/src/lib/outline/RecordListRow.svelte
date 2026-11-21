<script lang="ts">
  import { book, cn } from "$lib/frame";
  import { outlineState } from "./outline-state.svelte";
  import { TypeTag } from "$lib/record";

  // One record row in the Security view: the kind badge, the title and the
  // finding text. The row's accessible name holds the full title and finding.
  type Props = { id: string; suffix: string };
  let { id, suffix }: Props = $props();

  const selected = $derived(book.selected === id);
  const label = $derived.by(() => {
    return (
      book.typeName(book.typeOf(id)) + ", " + book.title(id) + ", " + suffix +
      (book.hasFailingProof(id) ? ", a test fails" : "") +
      (id !== book.selected && book.related.has(id) ? ", linked" : "")
    );
  });

  function choose(): void { outlineState.choose(id); }
</script>

<li>
  <button
    type="button"
    aria-current={selected ? "page" : undefined}
    aria-label={label}
    data-rk={id}
    onclick={choose}
    class={cn(
      "relative flex h-7 w-full cursor-pointer items-center gap-2 px-3 text-left text-sm select-none",
      selected ? "bg-layer-selected font-semibold text-foreground" : "text-foreground hover:bg-layer-hover",
    )}
  >
    {#if selected}<span aria-hidden="true" class="absolute top-0 bottom-0 left-0 w-1 bg-interactive"></span>{/if}
    <TypeTag type={book.typeOf(id)} />
    <span class="min-w-0 flex-1 truncate">{book.title(id)}</span>
    <span class={cn("max-w-[45%] flex-none truncate text-xs", selected ? "text-text-secondary" : "text-text-helper")}>{suffix}</span>
  </button>
</li>
