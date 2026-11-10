<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import { editorHref, esc } from "$lib/book/html";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import { codeCls, proseOf, provisionLinkHtml } from "../helpers";

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
  <Block blockKey="exit" heading="exit"><Prose html={exit} /></Block>
{/if}
{#if consequences}
  <Block blockKey="cons" heading="consequences"><Prose html={consequences} /></Block>
{/if}
<Block blockKey="cov" heading="covers">{@html covers}</Block>
{#if paths.length}
  <Block blockKey="paths" heading="paths" count={paths.length}>
    <div class="flex flex-wrap gap-1">
      {#each paths as p}<span class={codeCls}>{p}</span>{/each}
    </div>
  </Block>
{/if}
{#if f.reason}
  <Block blockKey="why" heading="reason">
    <Callout kind="quiet" icon="quote">Because {String(f.reason)}</Callout>
  </Block>
{/if}
