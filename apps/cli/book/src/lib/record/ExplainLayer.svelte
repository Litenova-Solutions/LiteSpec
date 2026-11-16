<script lang="ts">
  import { onDestroy } from "svelte";
  import { book, hoverState, inline } from "$lib/frame";
  import type { Hover } from "$lib/frame";
  import * as Popover from "$lib/ui/popover";

  // One explanation panel for the whole book. Any element with data-explain,
  // including ones inside rendered prose, opens it on hover, focus or click; the
  // title and body match the hover voice, and the source line names where in
  // LiteSpec the words come from. Content wrapped in Hover shows its own hover
  // tooltip instead, so this layer opens for it on click only. This component
  // takes no props.
  let open = $state(false);
  let anchor = $state<HTMLElement | null>(null);
  let text = $state<(Hover & { source: string }) | null>(null);
  let timer: ReturnType<typeof setTimeout> | undefined;
  onDestroy(() => clearTimeout(timer));

  function show(el: HTMLElement) {
    const key = el.dataset.explain ?? "";
    const e = book.explain(key);
    if (!e) return;
    const hover = hoverState(book, key);
    anchor = el;
    text = { title: hover.title, body: hover.body, source: e.source };
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
    if (t.closest("[data-hover]")) return;
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
    if (t && !t.closest("[data-hover]")) show(t);
  }
</script>

<svelte:document onmouseover={over} onmouseout={out} onclickcapture={click} onfocusin={focusin} />

<Popover.Root bind:open>
  <Popover.Content
    customAnchor={anchor}
    side="bottom"
    align="start"
    sideOffset={6}
    class="w-72 rounded-[2px] border-0 bg-tip px-3 py-2 text-tip-foreground shadow-md"
    data-explain-popover
    onOpenAutoFocus={(e) => e.preventDefault()}
  >
    {#if text}
      <p class="text-xs font-semibold tracking-[0.32px]">{text.title}</p>
      {#if text.body}<p class="mt-1 text-xs leading-4 tracking-[0.32px]">{@html inline(book, text.body)}</p>{/if}
      <p class="mt-1 text-xs leading-4 tracking-[0.32px] opacity-80">From LiteSpec: {text.source}</p>
    {/if}
  </Popover.Content>
</Popover.Root>
