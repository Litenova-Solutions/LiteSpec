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
</script>

<li>
  <button
    type="button"
    aria-current={selected ? "page" : undefined}
    aria-label={label}
    data-rk={id}
    onclick={choose}
    class={cn(
      "relative flex h-12 w-full cursor-pointer items-start gap-2 px-2 py-1.5 text-left text-sm select-none",
      selected ? "bg-blue-50 font-medium text-slate-900" : "text-slate-900 hover:bg-slate-100",
    )}
  >
    {#if selected}<span aria-hidden="true" class="absolute top-[5px] bottom-[5px] left-0 w-0.5 rounded-full bg-blue-700"></span>{/if}
    <span class="mt-0.5 grid flex-none text-slate-600"><RecordIcon name={book.typeOf(id)} class="size-4" /></span>
    <span class="min-w-0 flex-1">
      <span class="block overflow-hidden text-ellipsis whitespace-nowrap">{book.title(id)}</span>
      <span class="block overflow-hidden text-ellipsis whitespace-nowrap text-xs font-normal text-slate-600">{suffix}</span>
    </span>
  </button>
</li>
