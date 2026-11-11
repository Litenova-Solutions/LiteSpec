<script lang="ts">
  import { onDestroy } from "svelte";
  import { book, inline } from "$lib/frame";
  import type { Explanation } from "$lib/frame";
  import * as Popover from "$lib/ui/popover";
  import RecordIcon from "./RecordIcon.svelte";

  // One explanation popover for the whole book. Any element with data-explain,
  // including ones inside rendered prose, opens it on hover, focus or click; the
  // text always comes from LiteSpec: a glossary term or a section of the model page.
  // This component takes no props.
  let open = $state(false);
  let anchor = $state<HTMLElement | null>(null);
  let text = $state<Explanation | null>(null);
  let timer: ReturnType<typeof setTimeout> | undefined;
  onDestroy(() => clearTimeout(timer));

  function show(el: HTMLElement) {
    const e = book.explain(el.dataset.explain ?? "");
    if (!e) return;
    anchor = el;
    text = e;
    open = true;
  }

  function target(e: Event): HTMLElement | null {
    if (!(e.target instanceof Element)) return null;
    const found = e.target.closest("[data-explain]");
    return found instanceof HTMLElement ? found : null;
  }

  function over(e: MouseEvent) {
    const t = target(e);
    if (!t || t === anchor) return;
    clearTimeout(timer);
    timer = setTimeout(() => show(t), 300);
  }
  function out(e: MouseEvent) {
    const t = target(e);
    if (!t) return;
    const to = e.relatedTarget;
    if (to instanceof Element && (t.contains(to) || to.closest("[data-explain-popover]"))) return;
    clearTimeout(timer);
    open = false;
    anchor = null;
  }
  function click(e: MouseEvent) {
    const t = target(e);
    if (!t) return;
    e.stopPropagation();
    show(t);
  }
  function focusin(e: FocusEvent) {
    const t = target(e);
    if (t) show(t);
  }
</script>

<svelte:document onmouseover={over} onmouseout={out} onclickcapture={click} onfocusin={focusin} />

<Popover.Root bind:open>
  <Popover.Content customAnchor={anchor} side="bottom" align="start" class="w-80" data-explain-popover onOpenAutoFocus={(e) => e.preventDefault()}>
    {#if text}
      <p class="text-sm font-semibold">{text.title}</p>
      <p class="text-sm leading-relaxed">{@html inline(book, text.text)}</p>
      <p class="flex items-center gap-1.5 text-xs text-muted-foreground"><RecordIcon name="book" class="size-3.5" />From LiteSpec: {text.source}</p>
    {/if}
  </Popover.Content>
</Popover.Root>
