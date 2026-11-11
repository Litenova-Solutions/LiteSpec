<script lang="ts">
  import { book, editorHref, esc } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { codeCls, provisionLinkHtml } from "../page-html";
  import { proseOf } from "../page-prose";

  // A departure: its exit and consequences, what it covers, and why it holds.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const exit = $derived(proseOf(r, "exit"));
  const consequences = $derived(proseOf(r, "consequences"));
  const paths = $derived(((f.paths as string[] | undefined) ?? []).map(String));
  const covers = $derived.by(() => {
    if (Array.isArray(f.covers)) {
      const href = editorHref(book, book.man?.roots?.litespec ?? "", "docs/tools/model.md");
      return (f.covers as unknown[])
        .map((c) =>
          href
            ? `<a class="${codeCls}" href="${esc(href)}" title="Open the finding codes in LiteSpec">${esc(c)}</a>`
            : `<span class="${codeCls}">${esc(c)}</span>`,
        )
        .join(" ");
    }
    return provisionLinkHtml(f.covers);
  });
</script>

{#if exit}
  <PageSection blockKey="exit" heading="exit"><RenderedProse html={exit} /></PageSection>
{/if}
{#if consequences}
  <PageSection blockKey="cons" heading="consequences"><RenderedProse html={consequences} /></PageSection>
{/if}
<PageSection blockKey="cov" heading="covers">{@html covers}</PageSection>
{#if paths.length}
  <PageSection blockKey="paths" heading="paths" count={paths.length}>
    <div class="flex flex-wrap gap-1">
      {#each paths as p}<span class={codeCls}>{p}</span>{/each}
    </div>
  </PageSection>
{/if}
{#if f.reason}
  <PageSection blockKey="why" heading="reason">
    <Note kind="quiet" icon="quote">Because {String(f.reason)}</Note>
  </PageSection>
{/if}
