<script lang="ts">
  import { book, editorHref, esc } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { codeCls, provisionLinkHtml } from "../page-html";
  import { proseOf } from "../page-prose";
  import { stringField, stringList } from "../page-values";

  // A departure: its exit and consequences, what it covers, and why it holds.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const exit = $derived(proseOf(r, "exit"));
  const consequences = $derived(proseOf(r, "consequences"));
  const paths = $derived(stringList(f, "paths"));
  const reason = $derived(stringField(f, "reason"));
  const covers = $derived.by(() => {
    if (Array.isArray(f.covers)) {
      const href = editorHref(book, book.man?.roots?.litespec ?? "", "docs/tools/model.md");
      return f.covers
        .map((c) =>
          href
            ? `<a class="${codeCls}" href="${esc(href)}" title="Open the finding codes in LiteSpec">${esc(c)}</a>`
            : `<span class="${codeCls}">${esc(c)}</span>`,
        )
        .join(" ");
    }
    return provisionLinkHtml(book, f.covers);
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
      {#each paths as p (p)}<span class={codeCls}>{p}</span>{/each}
    </div>
  </PageSection>
{/if}
{#if reason}
  <PageSection blockKey="why" heading="reason">
    <Note kind="quiet" icon="quote">Because {reason}</Note>
  </PageSection>
{/if}
