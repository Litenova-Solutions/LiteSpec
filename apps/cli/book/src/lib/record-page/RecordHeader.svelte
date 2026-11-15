<script lang="ts">
  import { book, copyText, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, SourceLink, TypeTag } from "$lib/record";

  // The page header: the breadcrumb from the parent chain, the title, the kind
  // badge, the identifier, the source file and the traversal row.
  // Without a record it shows the missing-record header instead.
  type Props = { id: string; r: BookRecord | null };
  let { id, r }: Props = $props();
  const b = $derived(stripStep(id));
  const chain = $derived(book.breadcrumbOf(id));
  const layerKey = $derived(book.layerOf[id] ?? (id.indexOf("layer/") === 0 ? id.slice(6) : ""));
  const layerLabel = $derived(layerKey ? (book.layerByKey[layerKey]?.label ?? "") : "");
  const rTitle = $derived(r?.title ?? id);
  const pluralName = $derived(r ? book.pluralName(r.type) : "");
  const sib = $derived(r ? book.siblingsOf(id).filter((x) => book.typeOf(x) === r.type) : []);
  const at = $derived(sib.indexOf(id));
  const parent = $derived(book.parent(id));
  const upLabel = $derived(parent ? "Up to " + book.title(parent) : "Up to the parent");

  interface Crumb {
    id: string | null;
    label: string;
  }
  const links = $derived.by(() => {
    const out: Crumb[] = [];
    if (layerLabel && id.indexOf("layer/") !== 0) out.push({ id: "layer/" + layerKey, label: layerLabel });
    for (const c of chain) out.push({ id: c, label: book.title(c) });
    if (layerLabel && r) out.push({ id: null, label: pluralName });
    return out;
  });
  // A long chain keeps its ends: the first crumb, an ellipsis, the last two.
  const shown = $derived(links.length > 4 ? [...links.slice(0, 1), null, ...links.slice(-2)] : links);

  let copied = $state(false);
  let timer: number | null = null;

  function done() {
    copied = true;
    if (timer) clearTimeout(timer);
    timer = window.setTimeout(() => (copied = false), 1600);
  }

  function copy(text: string) {
    copyText(text).then((ok) => {
      if (ok) done();
    });
  }
</script>

<nav aria-label="Breadcrumbs">
  <ol class="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm">
    {#each shown as crumb, i (i)}
      <li class="flex min-w-0 items-center gap-2">
        {#if i > 0}<span class="shrink-0 text-text-secondary" aria-hidden="true">/</span>{/if}
        {#if crumb === null}
          <span class="shrink-0" aria-hidden="true">&hellip;</span><span class="sr-only">Skipped levels</span>
        {:else if crumb.id}
          <a
            href={"#" + encodeURIComponent(crumb.id)}
            data-go={crumb.id}
            class="max-w-48 shrink-0 truncate text-link hover:underline"
          >
            {crumb.label}
          </a>
        {:else}
          <span class="max-w-48 shrink-0 truncate text-text-secondary">{crumb.label}</span>
        {/if}
      </li>
    {/each}
    <li class="flex min-w-0 items-center gap-2">
      {#if shown.length}<span class="shrink-0 text-text-secondary" aria-hidden="true">/</span>{/if}
      <span aria-current="page" class="min-w-0 truncate">{rTitle}</span>
    </li>
  </ol>
</nav>
<header class="mt-5 flex items-start justify-between gap-6">
  <div class="min-w-0">
    <h1 id="record-title" tabindex="-1" class="m-0 text-[32px] leading-10 font-normal outline-none">{rTitle}</h1>
    <div class="mt-4 flex flex-wrap items-center gap-x-4 gap-y-2">
      <TypeTag type={r ? r.type : book.typeOf(id)} size="lg" />
      {#if !r}
        <span class="inline-flex h-6 items-center rounded-full bg-tag-red px-2 text-xs tracking-[0.32px] text-tag-red-foreground">Not found</span>
      {/if}
      <span class="inline-flex min-h-6 items-center gap-2 bg-layer pl-2 font-mono text-xs [overflow-wrap:anywhere]">
        {b}
        <button
          type="button"
          class="grid size-6 cursor-pointer place-items-center hover:bg-hover-wash"
          aria-label="Copy ID"
          onclick={() => copy(b)}
        >
          <RecordIcon name="copy" />
        </button>
        {#if r}
          <button
            type="button"
            class="grid size-6 cursor-pointer place-items-center hover:bg-hover-wash"
            aria-label="Copy title"
            onclick={() => copy(r.title)}
          >
            <RecordIcon name="copy" />
          </button>
        {/if}
      </span>
      {#if r?.src}
        <SourceLink src={r.src} />
      {/if}
      {#if copied}
        <span class="text-xs tracking-[0.32px] text-text-secondary" role="status" aria-live="polite">Copied</span>
      {/if}
    </div>
  </div>
  {#if r && book.layerOf[id]}
    <nav aria-label="Siblings" class="flex flex-none items-center">
      <button
        type="button"
        class="grid size-8 cursor-pointer place-items-center hover:bg-hover-wash disabled:cursor-not-allowed disabled:opacity-30"
        aria-label={upLabel}
        data-go={parent ?? ""}
        disabled={!parent}
      >
        <RecordIcon name="up" />
      </button>
      <button
        type="button"
        class="grid size-8 cursor-pointer place-items-center hover:bg-hover-wash disabled:cursor-not-allowed disabled:opacity-30"
        aria-label="Previous sibling"
        data-go={sib[at - 1] ?? ""}
        disabled={at <= 0}
      >
        <RecordIcon name="back" />
      </button>
      <span class="min-w-0 px-2 text-xs whitespace-nowrap tracking-[0.32px] text-text-secondary tabular-nums">
        {at + 1} of {sib.length} {pluralName.toLowerCase()}
      </span>
      <button
        type="button"
        class="grid size-8 cursor-pointer place-items-center hover:bg-hover-wash disabled:cursor-not-allowed disabled:opacity-30"
        aria-label="Next sibling"
        data-go={sib[at + 1] ?? ""}
        disabled={at < 0 || at >= sib.length - 1}
      >
        <RecordIcon name="chev" />
      </button>
    </nav>
  {/if}
</header>
{#if !r}
  <p class="mt-6 max-w-[44rem] text-xl leading-7 font-light">No record with this identifier is in the book.</p>
{/if}
