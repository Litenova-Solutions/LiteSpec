<script lang="ts">
  import { base, book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import SourceLink from "$lib/components/book/SourceLink.svelte";
  import { Button } from "$lib/components/ui/button";

  // The page header: the breadcrumb from the parent chain, the title, the type
  // line, the identifier and the qualified identifier with their copy buttons,
  // and the traversal row.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const b = $derived(base(id));
  const chain = $derived.by(() => {
    const out: string[] = [];
    let p = book.parent(id);
    while (p) {
      out.unshift(p);
      p = book.parent(p);
    }
    return out;
  });
  const layerKey = $derived(book.layerOf[id] ?? (id.indexOf("layer/") === 0 ? id.slice(6) : ""));
  const layerLabel = $derived(layerKey ? (book.layerByKey[layerKey]?.label ?? "") : "");
  const plural = $derived(book.pluralName(r.type));
  const question = $derived(book.typeQuestion(r.type));
  const sib = $derived(book.siblingsOf(id).filter((x) => book.typeOf(x) === r.type));
  const at = $derived(sib.indexOf(id));
  const parent = $derived(book.parent(id));

  let copied = $state(false);
  let timer: number | null = null;

  function done() {
    copied = true;
    if (timer) clearTimeout(timer);
    timer = window.setTimeout(() => (copied = false), 1600);
  }

  function copyText(text: string) {
    function fallback() {
      const area = document.createElement("textarea");
      area.value = text;
      area.setAttribute("readonly", "");
      area.style.position = "fixed";
      area.style.opacity = "0";
      document.body.appendChild(area);
      area.select();
      try {
        document.execCommand("copy");
      } catch {
        // The clipboard is unavailable; the note still confirms the tap.
      }
      area.remove();
      done();
    }
    if (navigator.clipboard?.writeText) {
      let p: Promise<void> | null = null;
      try {
        p = navigator.clipboard.writeText(text);
      } catch {
        p = null;
      }
      if (p?.then) p.then(done, fallback);
      else fallback();
    } else fallback();
  }
</script>

<nav class="mb-[18px] flex flex-wrap items-center gap-1.5 text-[13px] text-muted-foreground" aria-label="Location">
  {#if layerLabel && id.indexOf("layer/") !== 0}
    <button type="button" class="hover:text-foreground" data-go={"layer/" + layerKey}>{layerLabel}</button>
    <span class="text-strong">/</span>
  {/if}
  {#each chain as c}
    <button type="button" class="hover:text-foreground" data-go={c}>{book.title(c)}</button>
    <span class="text-strong">/</span>
  {/each}
  {#if layerLabel}
    <span>{plural}</span>
    <span class="text-strong">/</span>
  {/if}
  <b class="font-semibold text-foreground">{r.title}</b>
</nav>
<h1 class="m-0 mb-1.5 max-w-[72ch] text-[34px] leading-[1.15] font-semibold tracking-[-0.03em]">{r.title}</h1>
<p class="mt-0 mb-2 text-sm font-semibold text-subtle-foreground">
  <button type="button" class="cursor-help text-left" data-explain={"type:" + r.type}>
    {book.typeName(r.type)}{question ? ": " + question : ""}
  </button>
</p>
<div class="mb-[18px] flex flex-wrap items-center gap-x-3.5 gap-y-2 text-xs text-muted-foreground">
  <button
    type="button"
    class="cursor-pointer rounded-sm"
    aria-label={copied ? "Copy ID " + b + " (Copied)" : "Copy ID " + b}
    title="Copy ID"
    onclick={() => copyText(b)}
  >
    <code class="font-mono text-xs font-medium break-all text-foreground hover:underline">{b}</code>
  </button>
  {#if r.qualifiedId && r.qualifiedId !== b}
    <span class="text-muted-foreground">Qualified</span>
    <button
      type="button"
      class="cursor-pointer rounded-sm"
      aria-label={"Copy qualified ID " + r.qualifiedId}
      title="Copy qualified ID"
      onclick={() => copyText(r.qualifiedId ?? "")}
    >
      <code class="font-mono text-xs font-medium break-all text-foreground hover:underline">{r.qualifiedId}</code>
    </button>
  {/if}
  {#if r.src}
    <SourceLink src={r.src} />
  {/if}
  <span class="ml-auto inline-flex items-center gap-1.5">
    <Button variant="outline" size="sm" class="h-7" aria-label="Copy Title" onclick={() => copyText(r.title)}>
      <Icon name="copy" class="size-3.5" /><span>Copy Title</span>
    </Button>
    <Button variant="outline" size="sm" class="h-7" aria-label={copied ? "Copy ID (Copied)" : "Copy ID"} onclick={() => copyText(b)}>
      <Icon name="copy" class="size-3.5" /><span>Copy ID</span>
    </Button>
    {#if copied}
      <span class="text-xs font-semibold text-success-foreground" role="status" aria-live="polite">Copied</span>
    {/if}
  </span>
</div>
{#if book.layerOf[id]}
  <div class="mb-4 flex items-center gap-1.5 text-xs text-muted-foreground">
    <Button variant="outline" size="sm" data-go={parent ?? ""} disabled={!parent} title="Up to the parent (u)">
      <Icon name="up" class="size-3.5" />Up
    </Button>
    <Button
      variant="outline"
      size="icon-sm"
      aria-label="Previous sibling"
      title="Previous sibling ([)"
      data-go={sib[at - 1] ?? ""}
      disabled={at <= 0}
    >
      <Icon name="back" class="size-3.5" />
    </Button>
    <span class="mx-1 whitespace-nowrap">
      {at + 1} of {sib.length} {plural.toLowerCase()}{parent ? " in " + book.title(parent) : ""}
    </span>
    <Button
      variant="outline"
      size="icon-sm"
      aria-label="Next sibling"
      title="Next sibling (])"
      data-go={sib[at + 1] ?? ""}
      disabled={at < 0 || at >= sib.length - 1}
    >
      <Icon name="chev" class="size-3.5" />
    </Button>
    <span class="flex-1"></span>
  </div>
{/if}
