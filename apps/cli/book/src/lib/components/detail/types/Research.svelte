<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { inline, safeHref } from "$lib/book/html";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import { codeCls } from "../helpers";

  // Research: the conclusion, and the sources it rests on.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const conclusion = $derived(f.conclusion ? inline(book, String(f.conclusion), r.src ?? null) : "");
  const sources = $derived(((f.sources as Fields[] | undefined) ?? []) as Fields[]);

  function hostOf(url: string) {
    try {
      return new URL(url).host.replace(/^www\./, "");
    } catch {
      return "";
    }
  }
</script>

{#if conclusion}
  <Block blockKey="con" heading="conclusion"><Prose html={conclusion} /></Block>
{/if}
{#if sources.length}
  <Block blockKey="srcs" heading="Sources" count={sources.length}>
    <ol class="m-0 list-decimal border-t border-border p-0 pl-7">
      {#each sources as x}
        <li class="border-b border-border py-2 text-sm marker:font-mono marker:text-xs marker:text-muted-foreground">
          {#if x.path}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            <span class={codeCls}>{String(x.path)}</span>
            {#if x.title} {String(x.title)}{/if}
            <span class="mt-px block text-xs text-muted-foreground">{x.readOn ? "read " + book.fmtDate(String(x.readOn)) : ""}</span>
          {:else}
            {@const href = safeHref(book, x.url)}
            {@const label = String(x.title ?? String(x.url ?? "").replace(/^https?:\/\/(www\.)?/, ""))}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            {#if href}
              <a href={href} target="_blank" rel="noopener">{label}</a>
            {:else}
              {label}
            {/if}
            <span class="mt-px block text-xs text-muted-foreground">
              {hostOf(String(x.url ?? ""))}{x.readOn ? ", read " + book.fmtDate(String(x.readOn)) : ""}
            </span>
          {/if}
        </li>
      {/each}
    </ol>
  </Block>
{/if}
