<script lang="ts">
  import { book, inline, safeHref } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import { codeCls } from "../page-html";
  import { objectList, stringField } from "../page-values";

  // Research: the conclusion, and the sources it rests on.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const conclusionText = $derived(stringField(f, "conclusion"));
  const conclusion = $derived(conclusionText ? inline(book, conclusionText, r.src ?? null) : "");
  const sources = $derived(objectList(f, "sources"));

  function hostOf(url: string) {
    try {
      return new URL(url).host.replace(/^www\./, "");
    } catch {
      return "";
    }
  }
</script>

{#if conclusion}
  <PageSection blockKey="con" heading="conclusion"><RenderedProse html={conclusion} /></PageSection>
{/if}
{#if sources.length}
  <PageSection blockKey="srcs" heading="Sources" count={sources.length}>
    <ol class="m-0 list-decimal border-t border-border p-0 pl-7">
      {#each sources as x, i (String(x.key ?? i))}
        {@const readOn = stringField(x, "readOn")}
        <li class="border-b border-border py-2 text-sm marker:font-mono marker:text-xs marker:text-muted-foreground">
          {#if x.path}
            {#if x.key}<span class={codeCls}>{String(x.key)}</span>{" "}{/if}
            <span class={codeCls}>{String(x.path)}</span>
            {#if x.title} {String(x.title)}{/if}
            <span class="mt-px block text-xs text-muted-foreground">{readOn ? "read " + book.fmtDate(readOn) : ""}</span>
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
              {hostOf(String(x.url ?? ""))}{readOn ? ", read " + book.fmtDate(readOn) : ""}
            </span>
          {/if}
        </li>
      {/each}
    </ol>
  </PageSection>
{/if}
