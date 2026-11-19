<script lang="ts">
  import { book, safeHref } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import { stringField } from "./page-values";

  // The sources a research or constraint record rests on: each source and when it was read.
  type Props = { sources: RecordFields[] };
  let { sources }: Props = $props();
  const uid = $props.id();
  const captionId = $derived(uid + "-cap");

  function hostOf(url: string) {
    try {
      return new URL(url).host.replace(/^www\./, "");
    } catch {
      return "";
    }
  }
</script>

{#if sources.length}
  <PageSection blockKey="srcs" heading="Sources" count={sources.length}>
    <!-- The scroll region takes keyboard focus so a keyboard user can scroll a wide table. -->
    <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
    <div class="max-w-full overflow-x-auto" tabindex="0" role="region" aria-labelledby={captionId}>
      <table class="w-full text-sm [overflow-wrap:normal]">
        <caption class="sr-only" id={captionId}>Sources</caption>
        <thead>
          <tr>
            <th scope="col" class="bg-layer-accent h-10 px-4 text-left font-semibold">Source</th>
            <th scope="col" class="bg-layer-accent h-10 px-4 text-left font-semibold">Read on</th>
          </tr>
        </thead>
        <tbody class="[&_tr:hover_td]:bg-layer-hover [&_tr:hover_th]:bg-layer-hover">
          {#each sources as x, i (String(x.name ?? i))}
            {@const readOn = stringField(x, "readOn")}
            {@const published = stringField(x, "publishedOn")}
            {@const note = stringField(x, "note")}
            {@const href = x.path ? null : safeHref(book, x.url)}
            {@const host = href ? hostOf(String(x.url ?? "")) : ""}
            {@const title = stringField(x, "title")}
            <tr>
              <th scope="row" class="bg-layer border-t border-border-subtle px-4 py-3 text-left align-top font-normal">
                {#if x.name}<code class="bg-layer-accent px-1 font-mono text-xs">{String(x.name)}</code>{" "}{/if}
                {#if x.path}
                  {#if title}<span>{title}</span>{" "}{/if}
                  <code class="bg-layer-accent px-1 font-mono text-xs">{String(x.path)}</code>
                {:else if href}
                  <a {href} target="_blank" rel="noopener" class="inline-flex items-center gap-1 text-link hover:underline">
                    {title || host || String(x.url ?? "")}<RecordIcon name="ext" class="size-3.5" />
                  </a>
                {:else}
                  {title || String(x.url ?? "")}
                {/if}
                {#if published}
                  <span class="mt-1 block text-xs tracking-[0.32px] text-text-secondary">published {book.fmtDate(published)}</span>
                {/if}
                {#if note}
                  <span class="mt-1 block text-sm">{note}</span>
                {/if}
              </th>
              <td class="bg-layer border-t border-border-subtle px-4 py-3 align-top text-sm">{readOn ? book.fmtDate(readOn) : ""}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </PageSection>
{/if}
