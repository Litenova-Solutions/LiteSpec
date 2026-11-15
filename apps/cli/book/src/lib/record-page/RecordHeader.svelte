<script lang="ts">
  import { book, copyText, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RecordIcon, SourceLink, TypeTag } from "$lib/record";
  import { Button } from "$lib/ui/button";

  // The page header: the breadcrumb from the parent chain, the title, the type
  // tag, the identifier, the folder and the module, and the traversal row.
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

<nav class="mb-4 flex items-center gap-2 overflow-hidden text-xs whitespace-nowrap text-slate-600" aria-label="Location">
  {#each shown as crumb, i (i)}
    {#if i > 0}<span class="shrink-0 text-slate-300" aria-hidden="true">/</span>{/if}
    {#if crumb === null}
      <span class="shrink-0" aria-hidden="true">&hellip;</span><span class="sr-only">Skipped levels</span>
    {:else if crumb.id}
      <a
        href={"#" + encodeURIComponent(crumb.id)}
        data-go={crumb.id}
        title={crumb.label}
        class="max-w-48 shrink-0 truncate hover:text-blue-700"
      >
        {crumb.label}
      </a>
    {:else}
      <span class="max-w-48 shrink-0 truncate">{crumb.label}</span>
    {/if}
  {/each}
  {#if shown.length}<span class="shrink-0 text-slate-300" aria-hidden="true">/</span>{/if}
  <b class="min-w-0 truncate font-semibold text-slate-900">{rTitle}</b>
</nav>
<h1 id="record-title" tabindex="-1" class="outline-none m-0 mb-2 max-w-[72ch] text-2xl leading-[1.15] font-semibold tracking-[-0.03em]">{rTitle}</h1>
{#if r}
  <p class="mt-0 mb-2"><TypeTag type={r.type} /></p>
{:else}
  <p class="mt-0 mb-2 text-sm font-semibold text-slate-600">Record</p>
{/if}
<div class="mb-4 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-slate-600">
  <button
    type="button"
    class="cursor-pointer rounded-sm"
    aria-label={copied ? "Copy ID " + b + " (Copied)" : "Copy ID " + b}
    title="Copy ID"
    onclick={() => copy(b)}
  >
    <code class="font-mono text-xs font-medium break-all text-slate-900 hover:underline">{b}</code>
  </button>
  {#if r}
    {#if r.folder}
      <span class="text-slate-600">Folder</span>
      <button
        type="button"
        class="cursor-pointer rounded-sm"
        aria-label={"Copy folder " + r.folder}
        title="Copy folder"
        onclick={() => copy(r.folder ?? "")}
      >
        <code class="font-mono text-xs font-medium break-all text-slate-900 hover:underline">{r.folder}</code>
      </button>
    {/if}
    {#if r.module}
      <span class="text-slate-600">Module</span>
      <code class="font-mono text-xs font-medium break-all text-slate-900">{r.module}</code>
    {/if}
    {#if r.src}
      <SourceLink src={r.src} />
    {/if}
    <span class="ml-auto inline-flex items-center gap-2">
      <Button variant="outline" size="sm" class="h-7" aria-label="Copy Title" onclick={() => copy(r.title)}>
        <RecordIcon name="copy" class="size-3.5" /><span>Copy Title</span>
      </Button>
      <Button variant="outline" size="sm" class="h-7" aria-label={copied ? "Copy ID (Copied)" : "Copy ID"} onclick={() => copy(b)}>
        <RecordIcon name="copy" class="size-3.5" /><span>Copy ID</span>
      </Button>
      {#if copied}
        <span class="text-xs font-semibold text-success-foreground" role="status" aria-live="polite">Copied</span>
      {/if}
    </span>
  {/if}
</div>
{#if !r}
  <p class="mt-0 mb-2 text-xs text-slate-600">No record with this identifier is in the book.</p>
{/if}
{#if r && book.layerOf[id]}
  <div class="mb-4 flex items-center gap-2 text-xs text-slate-600">
    <Button variant="outline" size="sm" data-go={parent ?? ""} disabled={!parent} title="Up to the parent (u)">
      <RecordIcon name="up" class="size-3.5" />Up
    </Button>
    <Button
      variant="outline"
      size="icon-sm"
      aria-label="Previous sibling"
      title="Previous sibling ([)"
      data-go={sib[at - 1] ?? ""}
      disabled={at <= 0}
    >
      <RecordIcon name="back" class="size-3.5" />
    </Button>
    <span class="mx-1 min-w-0 tabular-nums">
      {at + 1} of {sib.length} {pluralName.toLowerCase()}{parent ? " in " + book.title(parent) : ""}
    </span>
    <Button
      variant="outline"
      size="icon-sm"
      aria-label="Next sibling"
      title="Next sibling (])"
      data-go={sib[at + 1] ?? ""}
      disabled={at < 0 || at >= sib.length - 1}
    >
      <RecordIcon name="chev" class="size-3.5" />
    </Button>
    <span class="flex-1"></span>
  </div>
{/if}
